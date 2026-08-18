namespace KztekAdbPublishTool.Web.Configuration;

/// <summary>
/// Cấu hình cho endpoint POST /api/launch-app.
/// Giá trị ApiKey PHẢI được đặt qua env var LaunchApp__ApiKey khi deploy.
/// Mặc định rỗng trong appsettings.json để tránh commit key vào git.
/// Rỗng = endpoint từ chối 401 mọi request (fail-safe).
/// </summary>
public sealed class LaunchAppSettings
{
    public const string SectionName = "LaunchApp";

    /// <summary>
    /// API key tĩnh bảo vệ endpoint launch-app.
    /// Rỗng = fail-safe, từ chối 401 mọi request (không bypass).
    /// Override bằng env var: LaunchApp__ApiKey=...
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
