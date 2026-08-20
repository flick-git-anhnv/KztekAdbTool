using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// POST /api/install — fire-and-forget install trên các device được chỉ định.
/// Tiến độ được push qua SignalR (InstallProgress, DeviceInstalled).
/// </summary>
public static class InstallEndpoints
{
    public static IEndpointRouteBuilder MapInstallEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/install", (
            InstallRequest req,
            DeviceState deviceState,
            DeviceRepository repo,
            InstallCoordinator coordinator,
            ILogger<InstallCoordinator> logger) =>
        {
            // Xác định danh sách serial mục tiêu
            IEnumerable<string> targetSerials;

            if (req.SelectedOnly && req.Serials is { Count: > 0 })
            {
                targetSerials = req.Serials;
            }
            else
            {
                // Không chỉ định → cài tất cả device Online
                targetSerials = deviceState.GetAll()
                    .Where(d => d.Status == "Online")
                    .Select(d => d.Serial);
            }

            // Chỉ lấy device thực sự Online
            var onlineMap = deviceState.GetAll()
                .Where(d => d.Status == "Online")
                .ToDictionary(d => d.Serial, StringComparer.Ordinal);

            var validSerials = targetSerials
                .Where(s => onlineMap.ContainsKey(s))
                .ToList();

            if (validSerials.Count == 0)
                return Results.BadRequest(new { error = "Không có thiết bị Online phù hợp." });

            // Lấy packageName và apkPath — ưu tiên từ request, fallback sang DB Settings
            var packageName = !string.IsNullOrWhiteSpace(req.PackageName)
                ? req.PackageName
                : repo.GetSetting("PackageName") ?? string.Empty;

            var apkPath = !string.IsNullOrWhiteSpace(req.ApkPath)
                ? req.ApkPath
                : repo.GetSetting("ApkPath") ?? string.Empty;

            if (string.IsNullOrEmpty(apkPath) || !File.Exists(apkPath))
                return Results.BadRequest(new { error = "File APK không tồn tại. Vui lòng upload APK trước." });

            var queued = coordinator.QueueInstalls(validSerials, packageName, apkPath, req.UninstallBeforeInstall);
            logger.LogInformation("Install queued for {Count} device(s): {Serials}",
                queued, string.Join(", ", validSerials));

            return Results.Accepted("/api/install", new
            {
                queued,
                serials = validSerials,
                packageName,
                apkPath,
                uninstallBeforeInstall = req.UninstallBeforeInstall
            });
        });

        return app;
    }
}

/// <summary>Request body cho POST /api/install.</summary>
public sealed class InstallRequest
{
    /// <summary>Danh sách serial cần cài. Null hoặc empty → cài tất cả Online.</summary>
    public List<string>? Serials { get; set; }

    /// <summary>true = chỉ cài serials trong Serials; false = cài tất cả Online.</summary>
    public bool SelectedOnly { get; set; }

    /// <summary>Package name override. Nếu null → lấy từ DB Settings.</summary>
    public string? PackageName { get; set; }

    /// <summary>Đường dẫn APK override. Nếu null → lấy từ DB Settings.</summary>
    public string? ApkPath { get; set; }

    /// <summary>Khi true → chạy adb uninstall trước adb install. Mặc định false (giữ hành vi cũ).</summary>
    public bool UninstallBeforeInstall { get; set; }
}
