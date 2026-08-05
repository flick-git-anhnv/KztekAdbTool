using KztekAdbPublishTool.Web.Services;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// POST /api/scan/start  — bắt đầu scan dải IP (409 nếu đang scan)
/// POST /api/scan/cancel — hủy scan đang chạy
/// Tiến độ được push qua SignalR: ScanProgress, ScanFound, ScanCompleted.
/// </summary>
public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scan/start", (
            ScanStartRequest req,
            ScanCoordinator coordinator) =>
        {
            if (req.Port is < 1 or > 65535)
                return Results.BadRequest(new { error = "Port không hợp lệ. Phải từ 1 đến 65535." });

            var startResult = coordinator.TryStartScan(
                req.RangeText ?? string.Empty,
                req.Port,
                out var validationError);

            return startResult switch
            {
                ScanStartResult.Started =>
                    Results.Accepted("/api/scan/start", new
                    {
                        message = "Bắt đầu quét mạng...",
                        range = req.RangeText,
                        port = req.Port
                    }),

                ScanStartResult.AlreadyRunning =>
                    Results.Conflict(new { error = "Đang quét mạng. Vui lòng chờ hoặc hủy trước khi bắt đầu scan mới." }),

                _ => // ValidationError
                    Results.BadRequest(new { error = validationError }),
            };
        });

        app.MapPost("/api/scan/cancel", (ScanCoordinator coordinator) =>
        {
            coordinator.CancelScan();
            return Results.Ok(new { message = "Đã gửi tín hiệu hủy quét." });
        });

        return app;
    }
}

/// <summary>Request body cho POST /api/scan/start.</summary>
public sealed class ScanStartRequest
{
    /// <summary>
    /// Dải IP cần quét. Các định dạng hỗ trợ:
    ///   "192.168.1.1-254"           → octet range
    ///   "192.168.1.1-192.168.1.254" → full IP range (cùng /24)
    ///   "192.168.1.0/24"            → CIDR /24
    ///   "192.168.1.100"             → single IP
    /// </summary>
    public string? RangeText { get; set; }

    /// <summary>Port TCP cần kiểm tra (thường là 5555 cho ADB WiFi). Phải 1-65535.</summary>
    public int Port { get; set; } = 5555;
}
