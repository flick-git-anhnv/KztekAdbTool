using System.Security.Cryptography;
using System.Text;
using KztekAdbPublishTool.Web.Configuration;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// IEndpointFilter kiểm tra header x-api-key cho endpoint POST /api/launch-app.
/// Gắn qua .AddEndpointFilter&lt;ApiKeyEndpointFilter&gt;() — KHÔNG áp toàn cục.
/// </summary>
public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private const string HeaderName = "x-api-key";
    private readonly LaunchAppSettings _settings;
    private readonly ILogger<ApiKeyEndpointFilter> _logger;

    public ApiKeyEndpointFilter(
        IOptions<LaunchAppSettings> options,
        ILogger<ApiKeyEndpointFilter> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var expected = _settings.ApiKey;

        // Fail-safe: ApiKey chưa cấu hình → từ chối tất cả request
        if (string.IsNullOrEmpty(expected))
        {
            _logger.LogWarning("LaunchApp:ApiKey chưa cấu hình — tất cả request bị 401.");
            return Results.Json(
                new { success = false, error = "Unauthorized", message = "Invalid or missing API key." },
                statusCode: 401);
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        // Constant-time comparison để chống timing attack
        if (!FixedTimeEqualsUtf8(provided, expected))
        {
            _logger.LogWarning(
                "Unauthorized launch-app request — path={Path}, ip={RemoteIP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress);
            return Results.Json(
                new { success = false, error = "Unauthorized", message = "Invalid or missing API key." },
                statusCode: 401);
        }

        return await next(context);
    }

    /// <summary>
    /// So sánh hai chuỗi constant-time (chống timing attack).
    /// Dùng CryptographicOperations.FixedTimeEquals trên bytes UTF-8.
    /// </summary>
    private static bool FixedTimeEqualsUtf8(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
