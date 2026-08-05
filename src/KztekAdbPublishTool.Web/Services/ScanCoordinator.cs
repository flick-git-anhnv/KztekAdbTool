using System.Net.Sockets;
using KztekAdbPublishTool.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KztekAdbPublishTool.Web.Services;

public enum ScanStartResult { Started, AlreadyRunning, ValidationError }

/// <summary>
/// Quản lý network scan: parse IP range, TCP probe song song (SemaphoreSlim(24)), timeout 1200ms/IP.
/// Chỉ 1 scan chạy tại 1 thời điểm — gọi start khi đang scan → AlreadyRunning (409).
/// Cancel qua CancelScan() — dừng trong 2s.
///
/// Thread-safety của _cts:
///   - Gán _cts = localCts TRƯỚC khi release _scanGuard → không có race với scan mới
///   - Null out _cts TRƯỚC khi release _scanGuard (volatile write) → scan mới thấy null trước khi acquire guard
///   - Cancel() đọc _cts volatile → best-effort (acceptable: cancel là fire-and-forget)
/// </summary>
public sealed class ScanCoordinator
{
    private readonly IHubContext<DeviceHub> _hub;
    private readonly ILogger<ScanCoordinator> _logger;

    // Guard đảm bảo chỉ 1 scan tại 1 thời điểm (non-blocking tryacquire)
    private readonly SemaphoreSlim _scanGuard = new(1, 1);

    // volatile: viết trước khi release guard (an toàn), đọc bởi Cancel() (best-effort)
    private volatile CancellationTokenSource? _cts;

    public bool IsScanning => _scanGuard.CurrentCount == 0;

    public ScanCoordinator(IHubContext<DeviceHub> hub, ILogger<ScanCoordinator> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Bắt đầu scan. Trả về Started | AlreadyRunning | ValidationError.
    /// Khi ValidationError, error chứa thông báo lỗi.
    /// Scan chạy background; tiến độ push qua SignalR.
    /// </summary>
    public ScanStartResult TryStartScan(string rangeText, int port, out string? error)
    {
        error = null;

        if (!ScanRangeParser.TryParse(rangeText, out var ips, out error))
            return ScanStartResult.ValidationError;

        // Non-blocking tryacquire — nếu đang scan → trả về AlreadyRunning
        if (!_scanGuard.Wait(0))
            return ScanStartResult.AlreadyRunning;

        // Đã giữ _scanGuard. Tạo CTS rồi start background task.
        var localCts = new CancellationTokenSource();
        _cts = localCts; // volatile write (guard đang được giữ, không có race ghi)

        _ = Task.Run(async () =>
        {
            try
            {
                await RunScanAsync(ips!, port, localCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScanCoordinator: unexpected error");
            }
            finally
            {
                _cts = null;          // null TRƯỚC khi release guard (volatile write)
                _scanGuard.Release(); // chỉ sau đó mới cho scan mới bắt đầu
                localCts.Dispose();
            }
        });

        return ScanStartResult.Started;
    }

    /// <summary>Cancel scan đang chạy. No-op nếu không có scan nào.</summary>
    public void CancelScan() => _cts?.Cancel();

    private async Task RunScanAsync(List<string> ips, int port, CancellationToken ct)
    {
        var total = ips.Count;
        var scanned = 0;
        var found = 0;

        _logger.LogInformation("ScanCoordinator: starting scan {Total} IPs on port {Port}", total, port);

        var innerSem = new SemaphoreSlim(24, 24); // concurrency 24 kết nối song song

        var tasks = ips.Select(ip => Task.Run(async () =>
        {
            try
            {
                await innerSem.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return; // cancelled while waiting for slot
            }

            try
            {
                if (ct.IsCancellationRequested) return;

                var isOpen = await TcpProbeAsync(ip, port, timeoutMs: 1200, ct);
                var current = Interlocked.Increment(ref scanned);
                var currentFound = isOpen
                    ? Interlocked.Increment(ref found)
                    : Volatile.Read(ref found);

                if (isOpen)
                {
                    var ipPort = $"{ip}:{port}";
                    _logger.LogInformation("ScanCoordinator: found {IpPort}", ipPort);
                    // Không truyền ct — tránh mất event khi cancel được gọi ngay sau khi found
                    await _hub.Clients.All.SendAsync("ScanFound", new { ipPort });
                }

                await _hub.Clients.All.SendAsync("ScanProgress",
                    new { current, total, foundCount = currentFound });
            }
            catch (OperationCanceledException) { /* scan bị cancel */ }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScanCoordinator: probe error for {Ip}", ip);
            }
            finally
            {
                innerSem.Release();
            }
        }, ct)).ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { /* bình thường khi cancel */ }
        catch (AggregateException aex)
        {
            // Bỏ qua tất cả OperationCanceledException bên trong
            var real = aex.InnerExceptions.Where(e => e is not OperationCanceledException).ToList();
            if (real.Count > 0)
                _logger.LogError(real[0], "ScanCoordinator: error in scan tasks");
        }

        var finalFound = Volatile.Read(ref found);
        var finalScanned = Volatile.Read(ref scanned);
        _logger.LogInformation("ScanCoordinator: completed. Scanned={Scanned}, Found={Found}", finalScanned, finalFound);

        // Push ScanCompleted — không dùng ct (đã có thể bị cancel)
        await _hub.Clients.All.SendAsync("ScanCompleted", new { total, found = finalFound });
    }

    private static async Task<bool> TcpProbeAsync(string ip, int port, int timeoutMs, CancellationToken ct)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(timeoutMs);

            using var client = new TcpClient();
            await client.ConnectAsync(ip, port, linkedCts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
