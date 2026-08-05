using KztekAdbPublishTool.Web.Configuration;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Workers;

/// <summary>
/// Background worker poll thiết bị ADB định kỳ.
/// Phase 1 (khung): chỉ log heartbeat mỗi PollIntervalMs.
/// Phase 2 sẽ bổ sung logic: GetDevicesAsync → Upsert → RefreshVersion → push SignalR DevicesUpdated.
/// </summary>
public sealed class DevicePollWorker : BackgroundService
{
    private readonly ILogger<DevicePollWorker> _logger;
    private readonly int _pollIntervalMs;

    public DevicePollWorker(ILogger<DevicePollWorker> logger, IOptions<AdbSettings> options)
    {
        _logger = logger;
        _pollIntervalMs = options.Value.PollIntervalMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DevicePollWorker started. PollInterval={PollIntervalMs}ms", _pollIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("DevicePollWorker heartbeat");

            // Phase 2: thay khối này bằng logic poll thực tế
            // var devices = await _adbService.GetDevicesAsync(stoppingToken);
            // foreach (var d in devices) { _repo.Upsert(...); }
            // await _hub.Clients.All.SendAsync("DevicesUpdated", _repo.GetAll(), stoppingToken);

            await Task.Delay(_pollIntervalMs, stoppingToken);
        }

        _logger.LogInformation("DevicePollWorker stopped.");
    }
}
