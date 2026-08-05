namespace KztekAdbPublishTool.Web.State;

/// <summary>
/// Global flag kiểm soát DevicePollWorker có chủ động poll không.
/// Singleton. Toggle qua /api/polling/toggle (STEP-2.5 — Junior Dev).
/// Dùng volatile để đảm bảo write từ một thread thấy ngay ở thread khác.
/// </summary>
public sealed class PollingState
{
    private volatile bool _isEnabled = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }
}
