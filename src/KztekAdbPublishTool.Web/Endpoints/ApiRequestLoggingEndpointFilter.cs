using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// IEndpointFilter ghi lịch sử mỗi request vào AddDevice / LaunchApp API.
/// PHẢI đăng ký OUTER (trước ApiKeyEndpointFilter) để bắt được cả response 401.
/// Pattern: capture request context → await next() → capture result → LogAsync → return result gốc.
/// Response của caller KHÔNG thay đổi (BR-G3).
/// </summary>
public sealed class ApiRequestLoggingEndpointFilter : IEndpointFilter
{
    private readonly IApiRequestLogService _logService;
    private readonly ILogger<ApiRequestLoggingEndpointFilter> _logger;

    public ApiRequestLoggingEndpointFilter(
        IApiRequestLogService logService,
        ILogger<ApiRequestLoggingEndpointFilter> logger)
    {
        _logService = logService;
        _logger     = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http      = context.HttpContext;
        var sw        = System.Diagnostics.Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        var path    = http.Request.Path.Value ?? string.Empty;
        var apiName = ResolveApiName(path);

        // Đọc body trước khi vào chain (EnableBuffering + rewind) — bắt được Parameters kể cả khi 401
        string? parametersJson = await TryReadBodyJsonAsync(http, http.RequestAborted);

        var callerIp = ResolveCallerIp(http);

        // Khởi tạo với giá trị mặc định an toàn — sẽ bị ghi đè trong try hoặc catch
        object? result   = null;
        int statusCode   = 200;
        string outcome   = ApiRequestLogConstants.ResultFailure;
        string? errorMsg = null;

        try
        {
            result     = await next(context);
            statusCode = ExtractStatusCode(result, http);
            outcome    = statusCode switch
            {
                401                  => ApiRequestLogConstants.ResultUnauthorized,
                >= 200 and < 300     => ApiRequestLogConstants.ResultSuccess,
                _                    => ApiRequestLogConstants.ResultFailure
            };
            // ErrorMessage: best-effort null trong happy path (không moi response body)
        }
        catch (Exception ex)
        {
            // Handler/chain throw unhandled → 500; log entry vẫn ghi ở finally
            statusCode = 500;
            outcome    = ApiRequestLogConstants.ResultFailure;
            errorMsg   = Truncate(ex.Message, ApiRequestLogConstants.MaxErrorMessageLength);
            result     = null;
            _logger.LogError(ex, "Unhandled exception trong logged endpoint: {Path}", path);
            throw;   // pipeline 500-handler vẫn hoạt động; finally bên dưới sẽ chạy trước
        }
        finally
        {
            sw.Stop();
            var entry = new ApiRequestLogEntry
            {
                Timestamp      = startedAt,
                ApiName        = apiName,
                HttpMethod     = http.Request.Method,
                Path           = path,
                Parameters     = parametersJson,
                Result         = outcome,
                HttpStatusCode = statusCode,
                ErrorMessage   = errorMsg,
                CallerIp       = callerIp,
                DurationMs     = (int)sw.ElapsedMilliseconds
            };
            // Await (không fire-and-forget) — service swallow exception nên an toàn (TDD Q-05)
            await _logService.LogAsync(entry, http.RequestAborted);
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ResolveApiName(string path)
    {
        if (path.Equals("/api/devices/connect-by-ip", StringComparison.OrdinalIgnoreCase))
            return ApiRequestLogConstants.ApiAddDevice;
        if (path.Equals("/api/launch-app", StringComparison.OrdinalIgnoreCase))
            return ApiRequestLogConstants.ApiLaunchApp;
        return "Unknown";   // guard — filter chỉ gắn vào 2 route trên
    }

    private static string ResolveCallerIp(HttpContext http)
    {
        // XFF first-hop (defensive) → RemoteIpAddress → "unknown"
        var xff = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(first)) return first;
        }
        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async Task<string?> TryReadBodyJsonAsync(HttpContext http, CancellationToken ct)
    {
        try
        {
            http.Request.EnableBuffering();
            using var reader = new StreamReader(
                http.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            var body = await reader.ReadToEndAsync(ct);
            http.Request.Body.Position = 0;   // rewind để handler đọc lại được

            if (string.IsNullOrWhiteSpace(body)) return null;

            // Validate JSON hợp lệ — nếu không parse được, ghi "invalid body" thay vì rác
            try
            {
                _ = System.Text.Json.JsonDocument.Parse(body);
                return body;
            }
            catch
            {
                return "invalid body";
            }
        }
        catch
        {
            return null;
        }
    }

    private static int ExtractStatusCode(object? result, HttpContext http)
    {
        // Results.Ok/BadRequest/NotFound/... đều implement IStatusCodeHttpResult
        if (result is Microsoft.AspNetCore.Http.IStatusCodeHttpResult sc && sc.StatusCode.HasValue)
            return sc.StatusCode.Value;
        // Fallback: đọc từ http.Response nếu handler ghi trực tiếp
        return http.Response.StatusCode == 0 ? 200 : http.Response.StatusCode;
    }

    private static string? Truncate(string? s, int max)
        => (s is null || s.Length <= max) ? s : s[..max];
}
