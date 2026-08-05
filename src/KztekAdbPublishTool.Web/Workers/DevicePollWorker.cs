using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Workers;

/// <summary>
/// Background worker poll ADB mỗi PollIntervalMs (mặc định 3s).
///
/// Logic mỗi vòng:
///   1. Kiểm tra PollControlService.PollingEnabled — nếu false → bỏ qua vòng này.
///   2. AdbService.GetDevicesAsync() → danh sách serial đang kết nối.
///   3. Upsert tất cả device live vào DeviceState + SQLite.
///   4. Device trong DeviceState mà không còn live → đánh dấu Offline.
///   5. Với mỗi Online device + có PackageName → GetPackageVersionAsync → cập nhật InstalledVersion.
///   6. Push "DevicesUpdated" qua SignalR tới tất cả client.
///   7. Log khi phát hiện device mới.
///
/// Trigger thủ công: POST /api/devices/poll → PollControlService.TriggerAsync() →
///   worker race giữa Task.Delay và WaitTriggerAsync → poll ngay lập tức mà không đợi hết interval.
///
/// Thread-safety: DeviceState.AddOrUpdate luôn thay thế object, không mutate in-place.
/// </summary>
public sealed class DevicePollWorker : BackgroundService
{
    private readonly ILogger<DevicePollWorker> _logger;
    private readonly AdbService _adb;
    private readonly DeviceRepository _repo;
    private readonly DeviceState _deviceState;
    private readonly PollControlService _pollControl;
    private readonly IHubContext<DeviceHub> _hub;
    private readonly int _pollIntervalMs;

    public DevicePollWorker(
        ILogger<DevicePollWorker> logger,
        AdbService adb,
        DeviceRepository repo,
        DeviceState deviceState,
        PollControlService pollControl,
        IHubContext<DeviceHub> hub,
        IOptions<AdbSettings> options)
    {
        _logger = logger;
        _adb = adb;
        _repo = repo;
        _deviceState = deviceState;
        _pollControl = pollControl;
        _hub = hub;
        _pollIntervalMs = options.Value.PollIntervalMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DevicePollWorker started. PollInterval={Ms}ms", _pollIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_pollControl.PollingEnabled)
                await PollAsync(stoppingToken);

            // Chờ interval HOẶC trigger thủ công — whichever comes first
            await WaitIntervalOrTriggerAsync(stoppingToken);
        }

        _logger.LogInformation("DevicePollWorker stopped.");
    }

    /// <summary>
    /// Race giữa Task.Delay(pollIntervalMs) và PollControlService.WaitTriggerAsync.
    /// Trigger thủ công (POST /api/devices/poll) thắng → poll ngay, không đợi hết interval.
    /// </summary>
    private async Task WaitIntervalOrTriggerAsync(CancellationToken stoppingToken)
    {
        using var intervalCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        intervalCts.CancelAfter(_pollIntervalMs);

        try
        {
            // WaitTriggerAsync blocking — cancel sau pollIntervalMs hoặc khi app shutdown
            await _pollControl.WaitTriggerAsync(intervalCts.Token);
            // Thoát sớm vì có trigger thủ công → poll ngay (tiếp vòng lặp)
        }
        catch (OperationCanceledException)
        {
            // OperationCanceledException có 2 nguyên nhân:
            //   (a) intervalCts timeout (pollIntervalMs trôi qua) → bình thường, tiếp tục poll
            //   (b) stoppingToken được cancel (app shutdown) → vòng lặp ngoài sẽ break
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            var liveDevices = await _adb.GetDevicesAsync(ct);
            var liveSerials = liveDevices
                .Select(d => d.Serial)
                .ToHashSet(StringComparer.Ordinal);

            var packageName = _repo.GetSetting("PackageName");
            var now = DateTime.UtcNow;

            // ── 1. Upsert tất cả device live vào state + DB ──────────────────────
            foreach (var adb in liveDevices)
            {
                _deviceState.TryGet(adb.Serial, out var prev);
                var isNew = prev == null;

                // Giữ model nếu adb trả về rỗng (một số ROM không báo model qua "adb devices -l")
                var model = string.IsNullOrEmpty(adb.Model)
                    ? (prev?.Model ?? string.Empty)
                    : adb.Model;

                var record = new DeviceRecord
                {
                    Serial = adb.Serial,
                    Model = model,
                    ConnectionType = "WiFi",
                    Status = adb.State == "device" ? "Online" : "Offline",
                    InstalledVersion = prev?.InstalledVersion,
                    LastInstallStatus = prev?.LastInstallStatus,
                    LastInstallTime = prev?.LastInstallTime,
                    FirstSeen = prev?.FirstSeen ?? now,
                    LastSeen = now,
                };

                _deviceState.AddOrUpdate(record);
                _repo.Upsert(record);

                if (isNew)
                    _logger.LogInformation(
                        "DevicePollWorker: new device {Serial} ({Model})", adb.Serial, model);
            }

            // ── 2. Đánh dấu Offline device đã biến mất ───────────────────────────
            foreach (var serial in _deviceState.GetSerials())
            {
                if (liveSerials.Contains(serial)) continue;
                if (!_deviceState.TryGet(serial, out var existing) || existing == null) continue;
                if (existing.Status == "Offline") continue; // đã offline rồi — bỏ qua

                var offlineRecord = new DeviceRecord
                {
                    Serial = existing.Serial,
                    Model = existing.Model,
                    ConnectionType = existing.ConnectionType,
                    Status = "Offline",
                    InstalledVersion = existing.InstalledVersion,
                    LastInstallStatus = existing.LastInstallStatus,
                    LastInstallTime = existing.LastInstallTime,
                    FirstSeen = existing.FirstSeen,
                    LastSeen = now,
                };

                _deviceState.AddOrUpdate(offlineRecord);
                _repo.Upsert(offlineRecord);
            }

            // ── 3. Refresh InstalledVersion cho device Online (nếu có PackageName) ─
            if (!string.IsNullOrEmpty(packageName))
            {
                var onlineSerials = _deviceState.GetAll()
                    .Where(d => d.Status == "Online")
                    .Select(d => d.Serial)
                    .ToList();

                foreach (var serial in onlineSerials)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var version = await _adb.GetPackageVersionAsync(serial, packageName, ct);

                        // Chỉ cập nhật khi version thay đổi (tránh ghi DB thừa)
                        if (_deviceState.TryGet(serial, out var current) && current != null
                            && current.InstalledVersion != version)
                        {
                            var updated = new DeviceRecord
                            {
                                Serial = current.Serial,
                                Model = current.Model,
                                ConnectionType = current.ConnectionType,
                                Status = current.Status,
                                InstalledVersion = version,
                                LastInstallStatus = current.LastInstallStatus,
                                LastInstallTime = current.LastInstallTime,
                                FirstSeen = current.FirstSeen,
                                LastSeen = current.LastSeen,
                            };
                            _deviceState.AddOrUpdate(updated);
                            _repo.Upsert(updated);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "DevicePollWorker: GetPackageVersion failed for {Serial}", serial);
                    }
                }
            }

            // ── 4. Push DevicesUpdated tới toàn bộ SignalR client ────────────────
            var snapshot = _deviceState.GetAll();
            await _hub.Clients.All.SendAsync("DevicesUpdated", snapshot, ct);
        }
        catch (OperationCanceledException)
        {
            // Shutdown bình thường — không log error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DevicePollWorker: unhandled exception in poll loop");
        }
    }
}
