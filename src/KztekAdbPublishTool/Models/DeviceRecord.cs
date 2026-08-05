namespace KztekAdbPublishTool.Models;

public sealed class DeviceRecord
{
    public string Serial { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "USB";
    public string Status { get; set; } = "Offline";
    public string? InstalledVersion { get; set; }
    public string? LastInstallStatus { get; set; }
    public DateTime? LastInstallTime { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}
