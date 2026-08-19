namespace KztekAdbPublishTool.Web.Models;

/// <summary>
/// Bản ghi log 1 API request — cả DB row + SignalR payload (mirror pattern DeviceRecord).
/// Property đặt PascalCase; SignalR serializer tự chuyển camelCase khi lên JS (System.Text.Json default).
/// </summary>
public sealed class ApiRequestLogEntry
{
    /// <summary>Id tự tăng SQLite. Bằng 0 khi chưa insert; DB tự gán sau INSERT.</summary>
    public long Id { get; set; }

    /// <summary>Thời điểm bắt đầu request — UTC.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>"AddDevice" | "LaunchApp" — enum-string cho dễ query.</summary>
    public string ApiName { get; set; } = string.Empty;

    /// <summary>"POST" cho cả 2 API hiện tại — để mở rộng sau.</summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>Route path, VD: "/api/devices/connect-by-ip".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Body request dưới dạng JSON string (max 1024 ký tự). Null nếu không đọc được.</summary>
    public string? Parameters { get; set; }

    /// <summary>"Success" | "Failure" | "Unauthorized".</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>HTTP status code trả về: 200 / 400 / 401 / 422 / 500.</summary>
    public int HttpStatusCode { get; set; }

    /// <summary>Mô tả lỗi ngắn gọn (max 500 ký tự). Null nếu Success.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>IP caller — XFF first-hop → RemoteIpAddress → "unknown".</summary>
    public string CallerIp { get; set; } = "unknown";

    /// <summary>Thời gian xử lý end-to-end (ms, ≥ 0).</summary>
    public int DurationMs { get; set; }
}
