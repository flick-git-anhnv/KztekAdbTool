using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

public static class RebootEndpoints
{
    public static IEndpointRouteBuilder MapRebootEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/devices/{serial}/reboot", async (
            string serial,
            DeviceState deviceState,
            AdbService adbService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(RebootEndpoints));

            var s = serial?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s))
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = "serial is required" });

            // DeviceState check — reuse pattern
            var dsResult = CheckDeviceState(s, deviceState, logger);
            if (dsResult is not null) return dsResult;

            // Gọi ADB
            var ct     = httpContext.RequestAborted;
            var result = await adbService.RebootDeviceAsync(s, ct);

            return MapRebootResult(s, result, logger);
        })
        .AddEndpointFilter<ApiRequestLoggingEndpointFilter>()
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        return app;
    }

    public static IResult? CheckDeviceState(string serial, DeviceState deviceState, ILogger logger)
    {
        // Nội dung giống AppStatusEndpoints.CheckDeviceState — copy paste giữ tự chứa
        if (!deviceState.TryGet(serial, out var device) || device is null)
        {
            logger.LogInformation("reboot device not found — serial={Serial}", serial);
            return Results.NotFound(new
            {
                success = false, error = "DeviceNotFound",
                message = $"Device '{serial}' not found."
            });
        }
        if (!string.Equals(device.Status, "Online", StringComparison.Ordinal))
        {
            logger.LogInformation("reboot device offline — serial={Serial}, status={Status}",
                serial, device.Status);
            return Results.UnprocessableEntity(new
            {
                success = false, error = "DeviceOffline",
                message = $"Device '{serial}' is offline."
            });
        }
        return null;
    }

    public static IResult MapRebootResult(string serial, AdbCommandResult result, ILogger logger)
    {
        if (result.Success)
        {
            logger.LogInformation("reboot initiated — serial={Serial}", serial);
            return Results.Ok(new
            {
                success = true,
                serial,
                status  = "RebootInitiated",
                message = "Reboot command sent. Device will go offline shortly."
            });
        }

        if (result.StdErr.StartsWith("Không tìm thấy adb tại:", StringComparison.Ordinal))
        {
            logger.LogError("ADB binary missing — {StdErr}", result.StdErr);
            return Results.Json(
                new { success = false, error = "AdbNotFound", message = "adb binary not found on server." },
                statusCode: 500);
        }

        if (result.StdErr.Contains("timeout sau", StringComparison.Ordinal))
        {
            logger.LogWarning("reboot timeout — serial={Serial}, stdErr={StdErr}", serial, result.StdErr);
            return Results.UnprocessableEntity(new
            {
                success = false, error = "AdbTimeout", message = result.StdErr.Trim(),
                exitCode = result.ExitCode, stdOut = result.StdOut, stdErr = result.StdErr
            });
        }

        logger.LogWarning("reboot failed — serial={Serial}, exitCode={ExitCode}, stdErr={StdErr}",
            serial, result.ExitCode, result.StdErr);
        return Results.UnprocessableEntity(new
        {
            success = false, error = "AdbError",
            message = string.IsNullOrWhiteSpace(result.StdErr) ? "ADB command failed" : result.StdErr.Trim(),
            exitCode = result.ExitCode, stdOut = result.StdOut, stdErr = result.StdErr
        });
    }
}
