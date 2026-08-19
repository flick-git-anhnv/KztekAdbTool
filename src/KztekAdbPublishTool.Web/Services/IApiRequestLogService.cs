using KztekAdbPublishTool.Web.Models;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Ghi lịch sử request cho 2 API có ApiKey protection: AddDevice + LaunchApp.
/// Async fire-and-await (caller await trước khi trả response) — SQLite insert &lt; 10ms.
/// Failure isolation: mọi exception PHẢI được swallow + log, KHÔNG throw ra caller (BR-G9).
/// </summary>
public interface IApiRequestLogService
{
    /// <summary>
    /// Ghi 1 bản ghi log vào SQLite + broadcast SignalR event "ApiRequestLogged" tới mọi client.
    /// Không throw — mọi lỗi được log nội bộ.
    /// </summary>
    /// <param name="entry">Bản ghi đã được filter chuẩn bị đầy đủ (trước truncate).</param>
    /// <param name="ct">CancellationToken của HttpContext.RequestAborted — best-effort.</param>
    Task LogAsync(ApiRequestLogEntry entry, CancellationToken ct);
}
