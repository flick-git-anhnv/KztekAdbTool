using System.Collections.Concurrent;
using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.SignalR;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Quản lý install workflow song song với ràng buộc:
///   - Per-device: chỉ 1 install tại 1 thời điểm cho mỗi serial (SemaphoreSlim(1))
///     → Tab 2 cài cùng serial với Tab 1 phải chờ Tab 1 xong.
///   - Global: tối đa 4 install đồng thời trên toàn hệ thống (SemaphoreSlim(4))
///     → Giữ nguyên hành vi WinForms gốc.
/// Tất cả install là fire-and-forget; tiến độ push qua SignalR InstallProgress / DeviceInstalled.
/// </summary>
public sealed class InstallCoordinator
{
    private readonly AdbService _adb;
    private readonly DeviceRepository _repo;
    private readonly DeviceState _deviceState;
    private readonly IHubContext<DeviceHub> _hub;
    private readonly ILogger<InstallCoordinator> _logger;

    // Per-device semaphore: chặn cài đồng thời trên cùng 1 serial
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _perDevice =
        new(StringComparer.Ordinal);

    // Global semaphore: giới hạn tổng concurrency = 4 (behavior parity với WinForms)
    private readonly SemaphoreSlim _global = new(4, 4);

    public InstallCoordinator(
        AdbService adb,
        DeviceRepository repo,
        DeviceState deviceState,
        IHubContext<DeviceHub> hub,
        ILogger<InstallCoordinator> logger)
    {
        _adb = adb;
        _repo = repo;
        _deviceState = deviceState;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Fire-and-forget: queue install cho từng serial. Trả về số serial được queue.
    /// Caller không cần await — tiến độ sẽ được push qua SignalR.
    /// </summary>
    public int QueueInstalls(IEnumerable<string> serials, string packageName, string apkPath)
    {
        var count = 0;
        foreach (var serial in serials)
        {
            // Capture loop variable
            var s = serial;
            _ = Task.Run(() => InstallOneAsync(s, packageName, apkPath));
            count++;
        }
        return count;
    }

    private async Task InstallOneAsync(string serial, string packageName, string apkPath)
    {
        // Bước 1: lấy (hoặc tạo) per-device semaphore → đảm bảo chỉ 1 install/device
        var deviceSem = _perDevice.GetOrAdd(serial, _ => new SemaphoreSlim(1, 1));

        await deviceSem.WaitAsync(); // chờ install trước (nếu có) trên device này hoàn thành
        try
        {
            // Bước 2: lấy global slot (tối đa 4 đồng thời)
            await _global.WaitAsync();
            try
            {
                await DoInstallAsync(serial, packageName, apkPath);
            }
            finally
            {
                _global.Release();
            }
        }
        finally
        {
            deviceSem.Release();
        }
    }

    private async Task DoInstallAsync(string serial, string packageName, string apkPath)
    {
        // Timeout tổng 10 phút — ADB command đã có timeout riêng, đây là safety net
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = timeoutCts.Token;

        try
        {
            await Push("InstallProgress", new { serial, current = 0, total = 5, message = "Đang lấy danh sách package..." });
            var before = await _adb.ListThirdPartyPackagesAsync(serial, ct);

            await Push("InstallProgress", new { serial, current = 1, total = 5, message = "Đang cài APK..." });
            var installResult = await _adb.InstallApkAsync(serial, apkPath, ct);

            if (!installResult.Success)
            {
                var failMsg = $"Thất bại: {installResult.StdErr.Trim()}";
                await FinishWithStatus(serial, failMsg, null);
                return;
            }

            await Push("InstallProgress", new { serial, current = 2, total = 5, message = "Đang kiểm tra kết quả..." });
            var after = await _adb.ListThirdPartyPackagesAsync(serial, ct);

            // Tìm package vừa được cài (so sánh before/after)
            var installedPkg = after.Except(before).FirstOrDefault() ?? packageName;

            await Push("InstallProgress", new { serial, current = 3, total = 5, message = "Đang lấy version..." });
            var version = await _adb.GetPackageVersionAsync(serial, installedPkg, ct);

            await Push("InstallProgress", new { serial, current = 4, total = 5, message = "Đang mở ứng dụng..." });
            var launchResult = await _adb.LaunchAppAsync(serial, installedPkg, ct);
            if (!launchResult.Success)
            {
                // "No activities found" — chỉ log, không coi là lỗi install (giữ hành vi gốc)
                _logger.LogWarning("LaunchApp {Serial}: {Msg}", serial, launchResult.StdErr.Trim());
            }

            await FinishWithStatus(serial, "Thành công", version);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Install timeout for {Serial}", serial);
            await FinishWithStatus(serial, "Lỗi: Timeout", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install failed for {Serial}", serial);
            await FinishWithStatus(serial, $"Lỗi: {ex.Message}", null);
        }
    }

    private async Task FinishWithStatus(string serial, string status, string? version)
    {
        var now = DateTime.UtcNow;

        await Push("InstallProgress", new { serial, current = 5, total = 5, message = status });
        await Push("DeviceInstalled", new { serial, status, version, time = now });

        // Cập nhật DeviceState + DB
        _repo.UpdateInstallResult(serial, status, now, version);

        if (_deviceState.TryGet(serial, out var existing) && existing != null)
        {
            var updated = new DeviceRecord
            {
                Serial = existing.Serial,
                Model = existing.Model,
                ConnectionType = existing.ConnectionType,
                Status = existing.Status,
                InstalledVersion = version ?? existing.InstalledVersion,
                LastInstallStatus = status,
                LastInstallTime = now,
                FirstSeen = existing.FirstSeen,
                LastSeen = existing.LastSeen,
            };
            _deviceState.AddOrUpdate(updated);
        }
    }

    // Dùng CancellationToken.None cho SignalR push để tránh bị cancel khi timeout CTS đã kích hoạt
    private Task Push(string method, object data) =>
        _hub.Clients.All.SendAsync(method, data);
}
