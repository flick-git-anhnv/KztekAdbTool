using KztekAdbPublishTool.Web.Services;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// Minimal API — health check.
/// GET /health trả { ok, adbVersion } khi adb binary chạy được.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (AdbService adb, CancellationToken ct) =>
        {
            var result = await adb.RunAsync("version", timeoutMs: 5000, ct: ct);

            if (!result.Success)
                return Results.Ok(new { ok = false, error = result.StdErr.Trim() });

            // Trích dòng "Android Debug Bridge version X.Y.Z"
            var versionLine = result.StdOut
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.StartsWith("Android Debug Bridge", StringComparison.OrdinalIgnoreCase))
                ?? result.StdOut.Trim();

            return Results.Ok(new { ok = true, adbVersion = versionLine });
        });

        return app;
    }
}
