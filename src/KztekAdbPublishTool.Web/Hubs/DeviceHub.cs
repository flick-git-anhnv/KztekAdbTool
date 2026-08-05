using Microsoft.AspNetCore.SignalR;

namespace KztekAdbPublishTool.Web.Hubs;

/// <summary>
/// SignalR Hub cho real-time cập nhật: device list, install progress, scan progress.
/// Endpoint: /hubs/device (đăng ký trong Program.cs).
/// Phase 2 sẽ bổ sung các method client-callable (Connect, Install, Scan...).
/// </summary>
public sealed class DeviceHub : Hub
{
    // Các event server → client (Phase 2 sẽ push từ Worker/Coordinator):
    //   DevicesUpdated(List<DeviceRecord> devices)
    //   InstallProgress(string serial, int percent, string message)
    //   DeviceInstalled(string serial, bool success, string version)
    //   ScanProgress(int found, int scanned, int total)
    //   ScanFound(string ipPort)

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
