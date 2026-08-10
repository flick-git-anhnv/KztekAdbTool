using System.Collections.Concurrent;
using KztekAdbPublishTool.Web.Models;

namespace KztekAdbPublishTool.Web.State;

/// <summary>
/// In-memory snapshot của danh sách thiết bị. Singleton, thread-safe via ConcurrentDictionary.
/// Đây là nguồn dữ liệu chính cho UI; DeviceRepository là backup bền vững (SQLite).
/// Thread-safety note: AddOrUpdate luôn thay thế toàn bộ object thay vì mutate in-place.
/// </summary>
public sealed class DeviceState
{
    private readonly ConcurrentDictionary<string, DeviceRecord> _map =
        new(StringComparer.Ordinal);

    /// <summary>Snapshot toàn bộ device, sắp xếp theo Serial.</summary>
    public IReadOnlyList<DeviceRecord> GetAll() =>
        _map.Values.OrderBy(d => d.Serial).ToList();

    /// <summary>Snapshot danh sách serial đang được track.</summary>
    public IReadOnlyList<string> GetSerials() =>
        _map.Keys.ToList(); // ToList() để tránh race khi caller iterate

    public DeviceRecord AddOrUpdate(DeviceRecord device)
    {
        _map[device.Serial] = device;
        return device;
    }

    public bool TryGet(string serial, out DeviceRecord? device) =>
        _map.TryGetValue(serial, out device);

    /// <summary>
    /// Xóa thiết bị khỏi in-memory snapshot.
    /// Gọi song song với DeviceRepository.Remove() để đảm bảo 2 nguồn state đồng bộ.
    /// Nếu thiết bị vẫn live (adb daemon vẫn thấy) → poll kế tiếp sẽ tự phát hiện lại
    /// (hành vi đúng theo thiết kế, parity với WinForms MainForm.cs:572-577).
    /// </summary>
    public bool Remove(string serial) => _map.TryRemove(serial, out _);
}
