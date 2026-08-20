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
    /// <param name="uninstallBeforeInstall">Khi true → chạy adb uninstall trước adb install. KHÔNG có default — caller phải khai báo rõ.</param>
    public int QueueInstalls(IEnumerable<string> serials, string packageName, string apkPath, bool uninstallBeforeInstall)
    {
        var count = 0;
        foreach (var serial in serials)
        {
            // Capture loop variable
            var s = serial;
            _ = Task.Run(() => InstallOneAsync(s, packageName, apkPath, uninstallBeforeInstall));
            count++;
        }
        return count;
    }

    private async Task InstallOneAsync(string serial, string packageName, string apkPath, bool uninstallBeforeInstall)
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
                await DoInstallAsync(serial, packageName, apkPath, uninstallBeforeInstall);
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

    private async Task DoInstallAsync(string serial, string packageName, string apkPath, bool uninstallBeforeInstall)
    {
        // Timeout tổng 10 phút — ADB command đã có timeout riêng, đây là safety net
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = timeoutCts.Token;

        try
        {
            // ── BƯỚC MỚI: Uninstall (chỉ khi flag BẬT + có packageName) ─────────────
            if (uninstallBeforeInstall && !string.IsNullOrWhiteSpace(packageName))
            {
                await _hub.Clients.All.SendAsync("InstallProgress", serial, 0,
                    $"Đang gỡ cài đặt {packageName}...");

                var uninstallResult = await _adb.UninstallApkAsync(serial, packageName, ct);
                var isRealSuccess = uninstallResult.Success
                    && uninstallResult.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

                if (isRealSuccess)
                {
                    _logger.LogInformation("Uninstall {Package} trên {Serial} thành công", packageName, serial);
                    await _hub.Clients.All.SendAsync("InstallProgress", serial, 15,
                        $"Đã gỡ cài đặt {packageName}");
                }
                else
                {
                    // D2 + D3: LUÔN graceful. Log warning kèm StdOut/StdErr; KHÔNG abort.
                    _logger.LogWarning(
                        "Uninstall {Package} trên {Serial} không thành công (vẫn tiếp tục install). StdOut={StdOut} StdErr={StdErr}",
                        packageName, serial, uninstallResult.StdOut.Trim(), uninstallResult.StdErr.Trim());
                    await _hub.Clients.All.SendAsync("InstallProgress", serial, 15,
                        "Bỏ qua gỡ (chưa cài hoặc bị chặn)");
                }
            }

            // ── LUỒNG CŨ giữ nguyên logic, chỉ percent shift khi flag BẬT ───────────
            int pList    = uninstallBeforeInstall ? 25 : 0;
            int pInstall = uninstallBeforeInstall ? 40 : 20;
            int pVerify  = uninstallBeforeInstall ? 55 : 40;
            int pVersion = uninstallBeforeInstall ? 70 : 60;
            int pLaunch  = uninstallBeforeInstall ? 85 : 80;

            // FIX-3.1b: gửi primitive args thay vì anonymous object để khớp JS:
            //   InstallProgress(string serial, int percent, string msg)  — percent 0-100
            await _hub.Clients.All.SendAsync("InstallProgress", serial, pList, "Đang lấy danh sách package...");
            var before = await _adb.ListThirdPartyPackagesAsync(serial, ct);

            await _hub.Clients.All.SendAsync("InstallProgress", serial, pInstall, "Đang cài APK...");
            var installResult = await _adb.InstallApkAsync(serial, apkPath, ct);

            if (!installResult.Success)
            {
                var failMsg = $"Thất bại: {installResult.StdErr.Trim()}";
                await FinishWithStatus(serial, failMsg, null);
                return;
            }

            await _hub.Clients.All.SendAsync("InstallProgress", serial, pVerify, "Đang kiểm tra kết quả...");
            var after = await _adb.ListThirdPartyPackagesAsync(serial, ct);

            // Tìm package vừa được cài (so sánh before/after)
            var installedPkg = after.Except(before).FirstOrDefault() ?? packageName;

            await _hub.Clients.All.SendAsync("InstallProgress", serial, pVersion, "Đang lấy version...");
            var version = await _adb.GetPackageVersionAsync(serial, installedPkg, ct);

            await _hub.Clients.All.SendAsync("InstallProgress", serial, pLaunch, "Đang mở ứng dụng...");
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

        // FIX-3.1b: percent=100 khi hoàn thành; DeviceInstalled gửi bool success (không phải string status)
        //   khớp JS: DeviceInstalled(string serial, bool success, string version)
        await _hub.Clients.All.SendAsync("InstallProgress", serial, 100, status);
        var success = status == "Thành công";
        await _hub.Clients.All.SendAsync("DeviceInstalled", serial, success, version);

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

}
