using System.Diagnostics;
using System.Text;
using KztekAdbPublishTool.Web.Configuration;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Services;

public sealed class AdbCommandResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;
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
public sealed class AdbService
{
    private readonly string _adbPath;

    public AdbService(IOptions<AdbSettings> options)
    {
        _adbPath = options.Value.AdbPath;
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

        return await RunAsync($"-s {serial} shell am start -n {component}", timeoutMs: 10000, ct: ct);
    }

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
