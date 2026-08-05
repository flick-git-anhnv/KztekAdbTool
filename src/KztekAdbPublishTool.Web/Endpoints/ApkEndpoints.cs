using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Services;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// POST /api/apk/upload — upload APK (multipart field "apk"), auto-detect package name
/// DELETE /api/apk      — xóa APK đang được cấu hình
/// </summary>
public static class ApkEndpoints
{
    // Static class không thể làm type argument → dùng nested non-static marker class cho ILogger category
    private sealed class Log { }

    public static IEndpointRouteBuilder MapApkEndpoints(this IEndpointRouteBuilder app)
    {
        // Tắt antiforgery cho endpoint này (multipart form upload từ JS fetch)
        app.MapPost("/api/apk/upload", async (
            HttpRequest request,
            ApkManifestReader manifestReader,
            DeviceRepository repo,
            IOptions<AdbSettings> settings,
            ILogger<Log> logger) =>
        {
            var opts = settings.Value;

            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Yêu cầu phải là multipart/form-data." });

            IFormCollection form;
            try
            {
                form = await request.ReadFormAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ReadFormAsync failed");
                return Results.BadRequest(new { error = $"Không đọc được form: {ex.Message}" });
            }

            var file = form.Files["apk"];
            if (file == null)
                return Results.BadRequest(new { error = "Không tìm thấy trường 'apk' trong form." });

            if (!file.FileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Chỉ chấp nhận file .apk." });

            if (file.Length == 0)
                return Results.BadRequest(new { error = "File APK không được rỗng." });

            if (file.Length > opts.MaxUploadBytes)
                return Results.BadRequest(new
                {
                    error = $"File vượt giới hạn {opts.MaxUploadBytes / 1_000_000} MB."
                });

            // Đảm bảo thư mục upload tồn tại
            Directory.CreateDirectory(opts.UploadsPath);

            // Tên file an toàn: <timestamp>-<sanitized-original>.apk
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.Concat(baseName
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                .TrimStart('.');
            if (string.IsNullOrEmpty(safeName)) safeName = "apk";

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var destFileName = $"{timestamp}-{safeName}.apk";
            var destPath = Path.Combine(opts.UploadsPath, destFileName);

            await using (var dest = File.Create(destPath))
            {
                await file.CopyToAsync(dest);
            }

            logger.LogInformation("APK uploaded: {Path} ({Bytes} bytes)", destPath, file.Length);

            // Auto-detect package name từ AndroidManifest.xml bên trong APK
            var packageName = manifestReader.TryGetPackageName(destPath);

            // Luôn lưu đường dẫn APK; chỉ lưu packageName nếu đọc được
            repo.SetSetting("ApkPath", destPath);
            if (packageName != null)
            {
                repo.SetSetting("PackageName", packageName);
                logger.LogInformation("Auto-detected package: {Package}", packageName);
            }
            else
            {
                logger.LogWarning("Package name could not be read from APK: {Path}", destPath);
            }

            return Results.Ok(new { path = destPath, packageName });
        }).DisableAntiforgery();

        app.MapDelete("/api/apk", (
            DeviceRepository repo,
            ILogger<Log> logger) =>
        {
            var path = repo.GetSetting("ApkPath");
            if (string.IsNullOrEmpty(path))
                return Results.Ok(new { message = "Không có APK nào được cấu hình." });

            if (File.Exists(path))
            {
                File.Delete(path);
                logger.LogInformation("Deleted APK: {Path}", path);
            }

            repo.SetSetting("ApkPath", string.Empty);
            return Results.Ok(new { message = "Đã xóa APK.", path });
        });

        return app;
    }
}
