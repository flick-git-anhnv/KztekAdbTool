using System.Threading.Channels;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Điều khiển vòng lặp poll: bật/tắt + trigger poll thủ công.
/// DevicePollWorker inject service này để nhận tín hiệu.
/// Bounded capacity=1: nhiều trigger liên tiếp chỉ giữ 1 tín hiệu — tránh ngập queue.
/// </summary>
public sealed class PollControlService
{
    private readonly Channel<bool> _triggerChannel;
    private volatile bool _pollingEnabled = true;

    public PollControlService()
    {
        _triggerChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <summary>Trạng thái hiện tại của vòng lặp poll.</summary>
    public bool PollingEnabled => _pollingEnabled;

    /// <summary>Bật hoặc tắt vòng lặp poll.</summary>
    public void SetPollingEnabled(bool enabled) => _pollingEnabled = enabled;

    /// <summary>Gửi tín hiệu yêu cầu poll ngay lập tức (dùng từ endpoint hoặc bất kỳ service nào).</summary>
    public ValueTask TriggerAsync(CancellationToken ct = default)
        => _triggerChannel.Writer.WriteAsync(true, ct);

    /// <summary>
    /// DevicePollWorker gọi để nhận tín hiệu trigger (blocking nếu không có tín hiệu).
    /// Sử dụng trong vòng lặp song song với Task.Delay để poll nhanh khi có trigger.
    /// </summary>
    public ValueTask<bool> WaitTriggerAsync(CancellationToken ct = default)
        => _triggerChannel.Reader.ReadAsync(ct);

    /// <summary>Thử đọc tín hiệu không blocking — dùng để drain trigger trước khi bắt đầu poll.</summary>
    public bool TryConsumeTrigger() => _triggerChannel.Reader.TryRead(out _);
}
