namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Hằng số dùng chung cho feature API Request Log — tránh magic string phân tán.
/// </summary>
public static class ApiRequestLogConstants
{
    // ── ApiName enum-string (endpoint filter, JS handler, test) ──────────────
    public const string ApiAddDevice = "AddDevice";
    public const string ApiLaunchApp = "LaunchApp";

    // ── Result enum-string ────────────────────────────────────────────────────
    public const string ResultSuccess      = "Success";
    public const string ResultFailure      = "Failure";
    public const string ResultUnauthorized = "Unauthorized";

    // ── SignalR event name — cố định; JS đăng ký chuỗi này ──────────────────
    public const string SignalREvent = "ApiRequestLogged";

    // ── Length caps (truncate ở tầng service trước khi ghi/broadcast) ────────
    public const int MaxParametersLength   = 1024;
    public const int MaxErrorMessageLength = 500;
}
