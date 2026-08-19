using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Models;
using Microsoft.AspNetCore.SignalR;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Implement <see cref="IApiRequestLogService"/> — ghi DB + broadcast SignalR.
/// Singleton: stateless, thread-safe (repository và hub context đều thread-safe).
/// </summary>
public sealed class ApiRequestLogService : IApiRequestLogService
{
    private readonly ApiRequestLogRepository _repo;
    private readonly IHubContext<DeviceHub> _hub;
    private readonly ILogger<ApiRequestLogService> _logger;

    public ApiRequestLogService(
        ApiRequestLogRepository repo,
        IHubContext<DeviceHub> hub,
        ILogger<ApiRequestLogService> logger)
    {
        _repo   = repo;
        _hub    = hub;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(ApiRequestLogEntry entry, CancellationToken ct)
    {
        // Truncate TRƯỚC khi ghi/broadcast (TDD §Service Implementation)
        entry.Parameters   = Truncate(entry.Parameters,   ApiRequestLogConstants.MaxParametersLength);
        entry.ErrorMessage = Truncate(entry.ErrorMessage, ApiRequestLogConstants.MaxErrorMessageLength);

        // 1. Persist vào SQLite — swallow exception (BR-G9)
        try
        {
            var id = await _repo.InsertAsync(entry, ct);
            if (id > 0)
                entry.Id = id;  // cập nhật Id để broadcast có Id đúng
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApiRequestLogService: DB insert failed — lỗi log sẽ không ảnh hưởng request gốc");
        }

        // 2. Broadcast SignalR — best-effort, swallow (US-005 EC2)
        try
        {
            await _hub.Clients.All.SendAsync(ApiRequestLogConstants.SignalREvent, entry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApiRequestLogService: SignalR broadcast '{Event}' failed (best-effort — bỏ qua)",
                ApiRequestLogConstants.SignalREvent);
        }
    }

    private static string? Truncate(string? s, int max)
        => (s is null || s.Length <= max) ? s : s[..max];
}
