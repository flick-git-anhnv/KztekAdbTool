using System.Text.RegularExpressions;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// POST /api/launch-app — mở app Android trên thiết bị qua ADB.
/// Yêu cầu header x-api-key khớp với LaunchApp:ApiKey (qua ApiKeyEndpointFilter).
/// </summary>
public static class LaunchAppEndpoints
{
    // Regex chuẩn Android package name (BR1, Q4-TDD):
    // - Mỗi segment bắt đầu bằng chữ cái, chỉ chứa chữ/số/underscore
    // - Phải có ≥ 2 segment ngăn cách bởi '.'
    private static readonly Regex PackageNameRegex = new(
        @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$",
        RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapLaunchAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/launch-app", async (
            LaunchAppRequest req,
            DeviceState deviceState,
            AdbService adbService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext) =>
        {
            // ILoggerFactory vì LaunchAppEndpoints là static class — không dùng được làm type arg
            ILogger logger = loggerFactory.CreateLogger(nameof(LaunchAppEndpoints));

            // Trim input (EC2, EC6)
            var serial = req.Serial?.Trim() ?? string.Empty;
            var packageName = req.App?.Trim() ?? string.Empty;

            // Bước 1: Validate input → 400
            var validationError = ValidateInput(serial, packageName);
            if (validationError is not null)
            {
                logger.LogInformation("Invalid input — reason={Reason}", validationError);
                return Results.BadRequest(new
                {
                    success = false,
                    error = "InvalidInput",
                    message = validationError
                });
            }

            // Bước 2: Kiểm tra DeviceState → 404 hoặc 422 DeviceOffline
            var deviceStateResult = CheckDeviceState(serial, deviceState, logger);
            if (deviceStateResult is not null)
                return deviceStateResult;

            // Bước 3: Gọi AdbService (device đã xác nhận Online)
            var ct = httpContext.RequestAborted;
            var result = await adbService.LaunchAppAsync(serial, packageName, ct);

            return MapAdbResult(serial, packageName, result, logger);
        })
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        return app;
    }

    /// <summary>
    /// Validate serial và packageName đã được Trim() trước khi gọi.
    /// Trả về null nếu hợp lệ, trả về message lỗi nếu không hợp lệ.
    /// </summary>
    public static string? ValidateInput(string serial, string app)
    {
        if (string.IsNullOrEmpty(serial))
            return "serial is required";

        if (string.IsNullOrEmpty(app))
            return "app is required";

        if (!PackageNameRegex.IsMatch(app))
            return "invalid package name";

        return null;
    }

    /// <summary>
    /// Kiểm tra DeviceState cho serial đã cho.
    /// Trả về null nếu device Online (handler tiếp tục gọi ADB).
    /// Trả về IResult 404 hoặc 422 nếu device không tồn tại hoặc offline.
    /// </summary>
    public static IResult? CheckDeviceState(string serial, DeviceState deviceState, ILogger logger)
    {
        if (!deviceState.TryGet(serial, out var device) || device is null)
        {
            // SC-03: serial chưa bao giờ được thấy hoặc đã bị Remove
            logger.LogInformation("Device not found — serial={Serial}", serial);
            return Results.NotFound(new
            {
                success = false,
                error = "DeviceNotFound",
                message = $"Device '{serial}' not found."
            });
        }

        if (!string.Equals(device.Status, "Online", StringComparison.Ordinal))
        {
            // SC-05: serial đã biết nhưng không phải Online
            logger.LogInformation(
                "Device offline — serial={Serial}, status={Status}",
                serial, device.Status);
            return Results.UnprocessableEntity(new
            {
                success = false,
                error = "DeviceOffline",
                message = $"Device '{serial}' is offline."
            });
        }

        return null; // Device Online → tiếp tục gọi ADB
    }

    /// <summary>
    /// Map AdbCommandResult → IResult theo bảng status code TDD.
    /// Tách thành method riêng để unit test không cần AdbService thật.
    /// </summary>
    public static IResult MapAdbResult(string serial, string app, AdbCommandResult result, ILogger logger)
    {
        // SC-01, EC7: ExitCode = 0 → thành công (kể cả bring-to-foreground)
        if (result.Success)
        {
            logger.LogInformation(
                "Launch OK — serial={Serial}, app={App}, exitCode={ExitCode}",
                serial, app, result.ExitCode);
            return Results.Ok(new
            {
                success = true,
                message = "App launched successfully.",
                serial,
                app,
                exitCode = result.ExitCode,
                stdOut = result.StdOut,
                stdErr = result.StdErr
            });
        }

        // EC4: ADB binary không tồn tại trên server (lỗi hạ tầng) → 500
        // Text khớp với AdbService.RunAsync khi !File.Exists(_adbPath)
        if (result.StdErr.StartsWith("Không tìm thấy adb tại:", StringComparison.Ordinal))
        {
            logger.LogError("ADB binary missing — check AdbSettings.AdbPath. StdErr={StdErr}", result.StdErr);
            return Results.Json(
                new { success = false, error = "AdbNotFound", message = "adb binary not found on server." },
                statusCode: 500);
        }

        // SC-06: Package chưa được cài (resolve-activity không tìm ra component) → 422
        // Text khớp với AdbService.LaunchAppAsync khi component == null
        if (result.StdErr.Contains("No activities found to run", StringComparison.Ordinal))
        {
            logger.LogInformation("App not installed — serial={Serial}, app={App}", serial, app);
            return Results.UnprocessableEntity(new
            {
                success = false,
                error = "AppNotInstalled",
                message = "No activities found to run",
                exitCode = result.ExitCode,
                stdOut = result.StdOut,
                stdErr = result.StdErr
            });
        }

        // EC1: ADB timeout (device rớt mạng giữa chừng) → 422
        // Text khớp với AdbService.RunAsync khi OperationCanceledException
        if (result.StdErr.Contains("timeout sau", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "ADB timeout — serial={Serial}, app={App}, stdErr={StdErr}",
                serial, app, result.StdErr);
            return Results.UnprocessableEntity(new
            {
                success = false,
                error = "AdbTimeout",
                message = result.StdErr.Trim(),
                exitCode = result.ExitCode,
                stdOut = result.StdOut,
                stdErr = result.StdErr
            });
        }

        // Lỗi ADB khác → 422 AdbError
        logger.LogWarning(
            "ADB launch failed — serial={Serial}, app={App}, exitCode={ExitCode}, stdErr={StdErr}",
            serial, app, result.ExitCode, result.StdErr);
        return Results.UnprocessableEntity(new
        {
            success = false,
            error = "AdbError",
            message = string.IsNullOrWhiteSpace(result.StdErr) ? "ADB command failed" : result.StdErr.Trim(),
            exitCode = result.ExitCode,
            stdOut = result.StdOut,
            stdErr = result.StdErr
        });
    }
}

/// <summary>Request body cho POST /api/launch-app.</summary>
public sealed class LaunchAppRequest
{
    /// <summary>Serial ADB của thiết bị đích (VD: "192.168.1.100:5555" hoặc "R58N7XXXX").</summary>
    public string? Serial { get; set; }

    /// <summary>Package name Android hợp lệ (VD: "com.kztek.abc").</summary>
    public string? App { get; set; }
}
