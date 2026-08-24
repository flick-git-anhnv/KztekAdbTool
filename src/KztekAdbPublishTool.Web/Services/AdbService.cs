using System.Diagnostics;
using System.Text;
using KztekAdbPublishTool.Web.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Services;

public sealed class AdbCommandResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;
}

public enum AppState
{
    NotRunning,
    Background,
    Foreground
}

public sealed class AppStatusResult
{
    public bool Running { get; init; }
    public AppState State { get; init; }
    public AdbCommandResult PidofResult { get; init; } = default!;   // endpoint biết stdout/stderr khi map error
    public AdbCommandResult? DumpsysResult { get; init; }             // null nếu skip do NotRunning
}

public sealed class AdbDevice
{
    public string Serial { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
}

/// <summary>
/// Wraps adb binary. Trong web/container, adb không có broadcast API —
/// phát hiện thiết bị dùng polling "adb devices -l" định kỳ qua DevicePollWorker.
/// Constructor nhận IOptions&lt;AdbSettings&gt; thay vì string trực tiếp để hỗ trợ DI.
/// </summary>
public sealed class AdbService : IAdbService
{
    private readonly string _adbPath;

    public AdbService(IOptions<AdbSettings> options, IWebHostEnvironment env)
    {
        var raw = options.Value.AdbPath;
        // Nếu là đường dẫn tương đối → resolve theo ContentRootPath (đảm bảo đúng dù chạy từ thư mục nào)
        _adbPath = Path.IsPathRooted(raw)
            ? raw
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, raw));
    }

    public async Task<AdbCommandResult> RunAsync(string arguments, int timeoutMs = 30000, CancellationToken ct = default)
    {
        if (!File.Exists(_adbPath))
        {
            return new AdbCommandResult
            {
                ExitCode = -1,
                StdErr = $"Không tìm thấy adb tại: {_adbPath}"
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { /* best effort */ }

            // Nếu ct gốc (stoppingToken của caller) đã bị cancel → service đang shutdown thật.
            // Re-throw để caller phân biệt "service shutdown" vs "per-device timeout"
            // (chỉ timeoutCts nội bộ fired, không phải ct gốc).
            ct.ThrowIfCancellationRequested();

            return new AdbCommandResult
            {
                ExitCode = -1,
                StdOut = stdOut.ToString(),
                StdErr = $"adb {arguments} timeout sau {timeoutMs}ms"
            };
        }

        return new AdbCommandResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdOut.ToString(),
            StdErr = stdErr.ToString()
        };
    }

    public async Task<List<AdbDevice>> GetDevicesAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("devices -l", ct: ct);
        var devices = new List<AdbDevice>();
        if (!result.Success) return devices;

        var lines = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var serial = parts[0];
            var state = parts[1];
            var model = string.Empty;
            foreach (var p in parts.Skip(2))
            {
                if (p.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
                    model = p["model:".Length..].Replace('_', ' ');
            }

            devices.Add(new AdbDevice { Serial = serial, State = state, Model = model });
        }

        return devices;
    }

    public Task<AdbCommandResult> ConnectAsync(string ipPort, int timeoutMs = 10000, CancellationToken ct = default)
        => RunAsync($"connect {ipPort}", timeoutMs: timeoutMs, ct: ct);

    /// <summary>
    /// Cắt kết nối ADB WiFi. Gọi khi user xóa thiết bị — nếu không, thiết bị vẫn "device" ở
    /// tầng adb daemon và bị DevicePollWorker phát hiện lại như thiết bị mới trong vòng poll kế tiếp.
    /// Chỉ áp dụng cho serial dạng "ip:port" (kết nối WiFi); serial USB không hỗ trợ "adb disconnect".
    /// </summary>
    public Task<AdbCommandResult> DisconnectAsync(string serial, int timeoutMs = 10000, CancellationToken ct = default)
        => RunAsync($"disconnect {serial}", timeoutMs: timeoutMs, ct: ct);

    public Task<AdbCommandResult> InstallApkAsync(string serial, string apkPath, CancellationToken ct = default)
        => RunAsync($"-s {serial} install -r \"{apkPath}\"", timeoutMs: 180000, ct: ct);

    /// <summary>
    /// Danh sách package bên thứ 3 (không phải app hệ thống) đang cài trên thiết bị.
    /// Dùng để so sánh trước/sau khi install nhằm xác định chính xác package vừa được cài.
    /// </summary>
    public async Task<HashSet<string>> ListThirdPartyPackagesAsync(string serial, CancellationToken ct = default)
    {
        var result = await RunAsync($"-s {serial} shell pm list packages -3", timeoutMs: 15000, ct: ct);
        var packages = new HashSet<string>();
        if (!result.Success) return packages;

        foreach (var line in result.StdOut.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                packages.Add(trimmed["package:".Length..].Trim());
        }

        return packages;
    }

    /// <summary>
    /// Mở app vừa cài trên thiết bị: resolve đúng Activity launcher của package rồi "am start -n" thẳng vào đó.
    /// KHÔNG dùng "monkey -p pkg -c LAUNCHER" — exit code không nhất quán giữa các bản ROM.
    /// </summary>
    public async Task<AdbCommandResult> LaunchAppAsync(string serial, string packageName, CancellationToken ct = default)
    {
        var resolve = await RunAsync(
            $"-s {serial} shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER -a android.intent.action.MAIN {packageName}",
            timeoutMs: 10000, ct: ct);

        var component = resolve.StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .LastOrDefault(l => l.Contains('/') && l.StartsWith(packageName, StringComparison.OrdinalIgnoreCase));

        if (component == null)
        {
            return new AdbCommandResult { ExitCode = -1, StdErr = "No activities found to run" };
        }

        return await RunAsync($"-s {serial} shell am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -n {component}", timeoutMs: 10000, ct: ct);
    }

    /// <summary>
    /// Gỡ cài đặt package trên thiết bị. LUÔN được coi là graceful ở tầng caller —
    /// caller (InstallCoordinator) chỉ dùng result để chọn message SignalR + log,
    /// không abort luồng install dù uninstall thất bại. Xem TDD-adb-uninstall-before-install §D2.
    /// </summary>
    public Task<AdbCommandResult> UninstallApkAsync(string serial, string packageName, CancellationToken ct = default)
        => RunAsync($"-s {serial} uninstall {packageName}", timeoutMs: 30000, ct: ct);

    /// <summary>
    /// Kiểm tra trạng thái 1 app trên 1 thiết bị.
    /// KHÔNG throw — mọi lỗi ADB trả qua AppStatusResult.PidofResult.Success/StdErr,
    /// endpoint sẽ map thành 422/500 tương tự MapAdbResult trong LaunchAppEndpoints.
    /// </summary>
    public async Task<AppStatusResult> GetAppStatusAsync(
        string serial, string packageName, CancellationToken ct = default)
    {
        // Bước 1: pidof
        var pidof = await RunAsync($"-s {serial} shell pidof {packageName}", timeoutMs: 10000, ct: ct);

        // ADB binary missing / timeout / hard error → trả về, endpoint map thành 500/422
        if (pidof.ExitCode == -1)
            return new AppStatusResult { Running = false, State = AppState.NotRunning, PidofResult = pidof };

        // pidof exit 1 với stdout empty = process không tồn tại — coi như not running (không phải lỗi)
        bool running = pidof.Success && !string.IsNullOrWhiteSpace(pidof.StdOut);

        if (!running)
            return new AppStatusResult { Running = false, State = AppState.NotRunning, PidofResult = pidof };

        // Bước 2: dumpsys activity — chỉ chạy khi running=true
        var dumpsys = await RunAsync($"-s {serial} shell dumpsys activity activities", timeoutMs: 10000, ct: ct);

        // Nếu dumpsys hard-fail → coi như Background (đã biết chắc app đang chạy từ pidof)
        bool foreground = dumpsys.Success && IsForegroundInDumpsys(dumpsys.StdOut, packageName);

        return new AppStatusResult
        {
            Running       = true,
            State         = foreground ? AppState.Foreground : AppState.Background,
            PidofResult   = pidof,
            DumpsysResult = dumpsys
        };
    }

    // public static để unit test không cần AdbService thật
    public static bool IsForegroundInDumpsys(string dumpsysStdOut, string packageName)
    {
        if (string.IsNullOrEmpty(dumpsysStdOut) || string.IsNullOrEmpty(packageName))
            return false;

        var lines = dumpsysStdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim());

        // Primary: mResumedActivity (Android 8+)
        bool matched = lines
            .Where(l => l.StartsWith("mResumedActivity", StringComparison.Ordinal))
            .Any(l => l.Contains($" {packageName}/", StringComparison.Ordinal));

        if (matched) return true;

        // Fallback: mFocusedActivity (Android 6-7)
        return lines
            .Where(l => l.StartsWith("mFocusedActivity", StringComparison.Ordinal))
            .Any(l => l.Contains($" {packageName}/", StringComparison.Ordinal));
    }

    /// <summary>
    /// Gửi lệnh reboot thiết bị. Trả về AdbCommandResult như các method khác —
    /// endpoint tự map thành 200/422/500.
    /// </summary>
    public Task<AdbCommandResult> RebootDeviceAsync(string serial, CancellationToken ct = default)
        => RunAsync($"-s {serial} reboot", timeoutMs: 10000, ct: ct);

    /// <summary>
    /// Trả về versionName của package trên thiết bị, hoặc null nếu chưa cài / không đọc được.
    /// </summary>
    public async Task<string?> GetPackageVersionAsync(string serial, string packageName, CancellationToken ct = default)
    {
        var result = await RunAsync($"-s {serial} shell dumpsys package {packageName}", timeoutMs: 15000, ct: ct);
        if (!result.Success) return null;

        foreach (var line in result.StdOut.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("versionName=", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed["versionName=".Length..].Trim();
                // Android trả về literal "versionName=null" khi APK không khai báo android:versionName
                return string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ? null : value;
            }
        }

        return null;
    }
}
