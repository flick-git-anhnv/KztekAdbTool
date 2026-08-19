using System.Text;
using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho ApiRequestLoggingEndpointFilter.
/// Kiểm tra: happy path (2xx), 401, exception → 500, body buffering, CallerIp từ XFF.
/// Dùng SpyLogService để capture entry thay vì mocking framework.
/// </summary>
public sealed class ApiRequestLoggingEndpointFilterTests
{
    // ── Spy ───────────────────────────────────────────────────────────────────

    private sealed class SpyLogService : IApiRequestLogService
    {
        public ApiRequestLogEntry? CapturedEntry { get; private set; }
        public int CallCount { get; private set; }

        public Task LogAsync(ApiRequestLogEntry entry, CancellationToken ct)
        {
            CallCount++;
            CapturedEntry = entry;
            return Task.CompletedTask;
        }
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    private static ApiRequestLoggingEndpointFilter CreateFilter(out SpyLogService spy)
    {
        spy = new SpyLogService();
        return new ApiRequestLoggingEndpointFilter(spy, NullLogger<ApiRequestLoggingEndpointFilter>.Instance);
    }

    private static EndpointFilterInvocationContext CreateContext(
        string path = "/api/devices/connect-by-ip",
        string? bodyJson = null,
        string? xForwardedFor = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path   = new PathString(path);

        if (bodyJson is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(bodyJson);
            httpContext.Request.Body        = new MemoryStream(bytes);
            httpContext.Request.ContentType = "application/json";
        }
        else
        {
            httpContext.Request.Body = new MemoryStream();
        }

        if (xForwardedFor is not null)
            httpContext.Request.Headers["X-Forwarded-For"] = xForwardedFor;

        return EndpointFilterInvocationContext.Create<object?>(httpContext, null);
    }

    private static EndpointFilterDelegate NextReturning(IResult result)
        => _ => ValueTask.FromResult<object?>(result);

    // ── Test 1: 200 OK → outcome = Success ───────────────────────────────────

    [Fact]
    public async Task InvokeAsync_Handler200_LogsSuccess()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/devices/connect-by-ip", "{\"ip\":\"192.168.1.10\",\"port\":5555}");

        await filter.InvokeAsync(ctx, NextReturning(Results.Ok(new { success = true })));

        Assert.Equal(1, spy.CallCount);
        Assert.NotNull(spy.CapturedEntry);
        Assert.Equal(ApiRequestLogConstants.ResultSuccess, spy.CapturedEntry!.Result);
        Assert.Equal(200, spy.CapturedEntry.HttpStatusCode);
        Assert.Equal(ApiRequestLogConstants.ApiAddDevice, spy.CapturedEntry.ApiName);
    }

    // ── Test 2: 401 → outcome = Unauthorized ─────────────────────────────────

    [Fact]
    public async Task InvokeAsync_Handler401_LogsUnauthorized()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/launch-app", "{\"serial\":\"ABC\",\"app\":\"com.test\"}");

        await filter.InvokeAsync(ctx, NextReturning(Results.Json(new { }, statusCode: 401)));

        Assert.Equal(ApiRequestLogConstants.ResultUnauthorized, spy.CapturedEntry!.Result);
        Assert.Equal(401, spy.CapturedEntry.HttpStatusCode);
        Assert.Equal(ApiRequestLogConstants.ApiLaunchApp, spy.CapturedEntry.ApiName);
    }

    // ── Test 3: 422 → outcome = Failure ──────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_Handler422_LogsFailure()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/devices/connect-by-ip");

        await filter.InvokeAsync(ctx, NextReturning(Results.UnprocessableEntity(new { error = "AdbConnectFailed" })));

        Assert.Equal(ApiRequestLogConstants.ResultFailure, spy.CapturedEntry!.Result);
        Assert.Equal(422, spy.CapturedEntry.HttpStatusCode);
    }

    // ── Test 4: Body buffering — handler vẫn đọc được body sau filter ────────

    [Fact]
    public async Task InvokeAsync_WithBody_HandlerCanStillReadBody()
    {
        var filter    = CreateFilter(out _);
        var bodyJson  = "{\"ip\":\"10.0.0.5\",\"port\":5555}";
        var ctx       = CreateContext("/api/devices/connect-by-ip", bodyJson);
        var http      = ctx.HttpContext;

        string? handlerReadBody = null;
        EndpointFilterDelegate next = async c =>
        {
            // Handler đọc body — phải thành công sau khi filter rewind Body.Position = 0
            using var reader = new StreamReader(c.HttpContext.Request.Body, leaveOpen: true);
            handlerReadBody  = await reader.ReadToEndAsync();
            return Results.Ok();
        };

        await filter.InvokeAsync(ctx, next);

        Assert.Equal(bodyJson, handlerReadBody);
    }

    // ── Test 5: LogAsync luôn được gọi dù handler throw ─────────────────────

    [Fact]
    public async Task InvokeAsync_HandlerThrows_LogStillCalled()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/devices/connect-by-ip");

        EndpointFilterDelegate throwingNext = _ => throw new InvalidOperationException("handler crashed");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeAsync(ctx, throwingNext).AsTask());

        // LogAsync PHẢI được gọi dù handler crash
        Assert.Equal(1, spy.CallCount);
        Assert.Equal(ApiRequestLogConstants.ResultFailure, spy.CapturedEntry!.Result);
        Assert.Equal(500, spy.CapturedEntry.HttpStatusCode);
    }

    // ── Test 6: XFF header → dùng XFF làm CallerIp ───────────────────────────

    [Fact]
    public async Task InvokeAsync_XffHeader_UsesXffAsCallerIp()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/devices/connect-by-ip", xForwardedFor: "203.0.113.5, 10.0.0.1");

        await filter.InvokeAsync(ctx, NextReturning(Results.Ok()));

        Assert.Equal("203.0.113.5", spy.CapturedEntry!.CallerIp);
    }

    // ── Test 7: DurationMs ≥ 0 ───────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_DurationMs_IsNonNegative()
    {
        var filter = CreateFilter(out var spy);
        var ctx    = CreateContext("/api/launch-app");

        await filter.InvokeAsync(ctx, NextReturning(Results.Ok()));

        Assert.True(spy.CapturedEntry!.DurationMs >= 0);
    }
}
