namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Abstraction trên ADB binary — cho phép unit test DevicePollWorker (và các service khác)
/// mà không cần ADB daemon thật.
///
/// Chỉ bao gồm các method được dùng bởi DevicePollWorker; các method khác
/// (InstallApkAsync, LaunchAppAsync, ...) vẫn truy cập qua AdbService cụ thể.
/// </summary>
public interface IAdbService
{
    Task<List<AdbDevice>> GetDevicesAsync(CancellationToken ct = default);

    Task<AdbCommandResult> ConnectAsync(string ipPort, int timeoutMs = 10000, CancellationToken ct = default);

    Task<string?> GetPackageVersionAsync(string serial, string packageName, CancellationToken ct = default);
}
