using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// Public API — kết nối thiết bị theo IP và kiểm tra trạng thái kết nối ADB.
/// Bảo vệ bằng header x-api-key qua ApiKeyEndpointFilter (tái dùng cấu hình LaunchApp:ApiKey).
/// </summary>
public static class DeviceConnectionEndpoints
{
    private const int DefaultAdbPort = 5555;
    private const int MinPort = 1;
    private const int MaxPort = 65535;

    public static IEndpointRouteBuilder MapDeviceConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        // ── POST /api/devices/connect-by-ip ────────────────────────────────
        app.MapPost("/api/devices/connect-by-ip", async (
            ConnectByIpRequest req,
            AdbService adb,
            PollControlService poll,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeviceConnectionEndpoints));

            // Bước 1: Validate
            var ip = req.Ip?.Trim() ?? string.Empty;
            var port = req.Port ?? DefaultAdbPort;
            var err = ValidateConnectInput(ip, req.Port);
            if (err is not null)
            {
                logger.LogInformation("connect-by-ip invalid input: {Reason}", err);
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = err });
            }

            // Bước 2: Gọi ADB
            var target = $"{ip}:{port}";
            var result = await adb.ConnectAsync(target, ct: ct);

            // Bước 3: Thành công → trigger poll để DeviceState update sớm
            if (result.Success)
            {
                await poll.TriggerAsync(ct);
                logger.LogInformation("connect-by-ip OK: {Target}, stdOut={StdOut}", target, result.StdOut.Trim());
                return Results.Ok(new
                {
                    success = true,
                    message = "Device connected.",
                    serial = target,
                    exitCode = result.ExitCode,
                    stdOut = result.StdOut
                });
            }

            // Bước 4: ADB binary missing → 500
            if (result.StdErr.StartsWith("Không tìm thấy adb tại:", StringComparison.Ordinal))
            {
                logger.LogError("ADB binary missing — {StdErr}", result.StdErr);
                return Results.Json(
                    new { success = false, error = "AdbNotFound", message = "adb binary not found on server." },
                    statusCode: 500);
            }

            // Bước 5: ADB connect fail → 422
            logger.LogWarning("connect-by-ip failed: {Target}, exitCode={ExitCode}, stdErr={StdErr}",
                target, result.ExitCode, result.StdErr.Trim());
            return Results.UnprocessableEntity(new
            {
                success = false,
                error = "AdbConnectFailed",
                message = string.IsNullOrWhiteSpace(result.StdErr) ? "ADB connect failed" : result.StdErr.Trim(),
                exitCode = result.ExitCode,
                stdOut = result.StdOut,
                stdErr = result.StdErr
            });
        })
        .AddEndpointFilter<ApiRequestLoggingEndpointFilter>()   // OUTER — bắt được cả 401; đăng ký TRƯỚC ApiKeyEndpointFilter
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        // ── GET /api/devices/{serial}/status ───────────────────────────────
        app.MapGet("/api/devices/{serial}/status", (
            string serial,
            DeviceState deviceState,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeviceConnectionEndpoints));

            var s = serial?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s))
            {
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = "serial không được rỗng" });
            }

            if (!deviceState.TryGet(s, out var device) || device is null)
            {
                logger.LogInformation("status: device not found — serial={Serial}", s);
                return Results.NotFound(new
                {
                    success = false,
                    error = "DeviceNotFound",
                    message = $"Device '{s}' not found."
                });
            }

            return Results.Ok(new
            {
                success = true,
                serial = device.Serial,
                status = device.Status // "Online" hoặc "Offline"
            });
        })
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        return app;
    }

    /// <summary>
    /// Validate input cho connect-by-ip.
    /// Trả về null nếu hợp lệ, message lỗi nếu không hợp lệ.
    /// public static để unit test truy cập trực tiếp (giống pattern LaunchAppEndpoints.ValidateInput).
    /// </summary>
    public static string? ValidateConnectInput(string ip, int? port)
    {
        if (string.IsNullOrEmpty(ip))
            return "ip là bắt buộc";
        if (ip.Contains(':', StringComparison.Ordinal))
            return "ip không được chứa ':' — dùng field port riêng";
        if (ip.Any(char.IsWhiteSpace))
            return "ip không được chứa khoảng trắng";
        if (port.HasValue && (port.Value < MinPort || port.Value > MaxPort))
            return $"port phải nằm trong khoảng {MinPort}–{MaxPort}";
        return null;
    }
}

/// <summary>Request body cho POST /api/devices/connect-by-ip.</summary>
public sealed class ConnectByIpRequest
{
    /// <summary>Địa chỉ IP hoặc hostname của thiết bị ADB WiFi (VD: "192.168.1.100"). Bắt buộc.</summary>
    public string? Ip { get; set; }

    /// <summary>Cổng ADB (mặc định 5555 nếu không truyền). Phải trong khoảng 1–65535.</summary>
    public int? Port { get; set; }
}
