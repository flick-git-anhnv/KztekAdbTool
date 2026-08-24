using System.Text.RegularExpressions;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

public static class AppStatusEndpoints
{
    // Regex identical với LaunchAppEndpoints.PackageNameRegex — DRY vi phạm nhẹ vì
    // 2 file endpoint không có common base; giữ tự chứa để dễ maintain.
    private static readonly Regex PackageNameRegex = new(
        @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$",
        RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapAppStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/devices/{serial}/app-status", async (
            string serial,
            string? package,               // query string ?package=
            DeviceState deviceState,
            AdbService adbService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(AppStatusEndpoints));

            var s   = serial?.Trim() ?? string.Empty;
            var pkg = package?.Trim() ?? string.Empty;

            // Validate
            var err = ValidateInput(s, pkg);
            if (err is not null)
            {
                logger.LogInformation("app-status invalid input — reason={Reason}", err);
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = err });
            }

            // DeviceState check — reuse pattern LaunchAppEndpoints.CheckDeviceState
            var dsResult = CheckDeviceState(s, deviceState, logger);
            if (dsResult is not null) return dsResult;

            // Gọi ADB
            var ct     = httpContext.RequestAborted;
            var result = await adbService.GetAppStatusAsync(s, pkg, ct);

            return MapAppStatusResult(s, pkg, result, logger);
        })
        .AddEndpointFilter<ApiRequestLoggingEndpointFilter>()
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        return app;
    }

    public static string? ValidateInput(string serial, string package)
    {
        if (string.IsNullOrEmpty(serial))   return "serial is required";
        if (string.IsNullOrEmpty(package))  return "package is required";
        if (!PackageNameRegex.IsMatch(package)) return "invalid package name";
        return null;
    }

    // Copy nội dung từ LaunchAppEndpoints.CheckDeviceState — giữ tự chứa để test độc lập.
    // Alternative: refactor thành DeviceStateHelper — hoãn tới refactor riêng, không blocker.
    public static IResult? CheckDeviceState(string serial, DeviceState deviceState, ILogger logger)
    {
        if (!deviceState.TryGet(serial, out var device) || device is null)
        {
            logger.LogInformation("app-status device not found — serial={Serial}", serial);
            return Results.NotFound(new
            {
                success = false, error = "DeviceNotFound",
                message = $"Device '{serial}' not found."
            });
        }
        if (!string.Equals(device.Status, "Online", StringComparison.Ordinal))
        {
            logger.LogInformation("app-status device offline — serial={Serial}, status={Status}",
                serial, device.Status);
            return Results.UnprocessableEntity(new
            {
                success = false, error = "DeviceOffline",
                message = $"Device '{serial}' is offline."
            });
        }
        return null;
    }

    public static IResult MapAppStatusResult(string serial, string package, AppStatusResult result, ILogger logger)
    {
        var pidof = result.PidofResult;

        // AdbNotFound → 500
        if (pidof.StdErr.StartsWith("Không tìm thấy adb tại:", StringComparison.Ordinal))
        {
            logger.LogError("ADB binary missing — {StdErr}", pidof.StdErr);
            return Results.Json(
                new { success = false, error = "AdbNotFound", message = "adb binary not found on server." },
                statusCode: 500);
        }

        // Timeout ở pidof → 422
        if (pidof.StdErr.Contains("timeout sau", StringComparison.Ordinal))
        {
            logger.LogWarning("app-status pidof timeout — serial={Serial}, pkg={Pkg}", serial, package);
            return Results.UnprocessableEntity(new
            {
                success = false, error = "AdbTimeout", message = pidof.StdErr.Trim(),
                exitCode = pidof.ExitCode, stdOut = pidof.StdOut, stdErr = pidof.StdErr
            });
        }

        // pidof exit 1 = process không tồn tại (KHÔNG phải lỗi) → NotRunning là hợp lệ.
        // Chỉ khi ExitCode < 0 (hard error khác) mới coi là AdbError.
        if (pidof.ExitCode < 0 && !result.Running)
        {
            logger.LogWarning("app-status ADB error — serial={Serial}, pkg={Pkg}, stdErr={StdErr}",
                serial, package, pidof.StdErr);
            return Results.UnprocessableEntity(new
            {
                success = false, error = "AdbError",
                message = string.IsNullOrWhiteSpace(pidof.StdErr) ? "ADB command failed" : pidof.StdErr.Trim(),
                exitCode = pidof.ExitCode, stdOut = pidof.StdOut, stdErr = pidof.StdErr
            });
        }

        // Success path
        logger.LogInformation("app-status OK — serial={Serial}, pkg={Pkg}, state={State}",
            serial, package, result.State);
        return Results.Ok(new
        {
            success = true,
            serial,
            package,
            running = result.Running,
            state   = result.State.ToString()   // "Foreground" | "Background" | "NotRunning"
        });
    }
}
