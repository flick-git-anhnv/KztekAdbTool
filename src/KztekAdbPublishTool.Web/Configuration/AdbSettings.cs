namespace KztekAdbPublishTool.Web.Configuration;

public sealed class AdbSettings
{
    public const string SectionName = "Adb";

    /// <summary>Đường dẫn tuyệt đối tới adb binary. Trong container Linux: /opt/platform-tools/adb</summary>
    public string AdbPath { get; set; } = "/opt/platform-tools/adb";

    /// <summary>Chu kỳ poll thiết bị (ms). Mặc định 3000ms.</summary>
    public int PollIntervalMs { get; set; } = 3000;

    /// <summary>Đường dẫn SQLite DB. Trong container Linux mount vào /app/data/</summary>
    public string DbPath { get; set; } = "/app/data/adbpublishtool.db";

    /// <summary>Thư mục lưu APK upload tạm thời. Mount vào /app/uploads/ trong container.</summary>
    public string UploadsPath { get; set; } = "/app/uploads";

    /// <summary>Giới hạn kích thước APK upload (bytes). Mặc định 500 MB.</summary>
    public long MaxUploadBytes { get; set; } = 500_000_000;
}
