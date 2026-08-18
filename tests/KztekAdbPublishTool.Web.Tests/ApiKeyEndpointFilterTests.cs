using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho ApiKeyEndpointFilter — kiểm tra logic auth x-api-key.
/// Không cần AdbService hay DeviceState; chỉ cần DefaultHttpContext.
/// </summary>
public sealed class ApiKeyEndpointFilterTests
{
    private static ApiKeyEndpointFilter CreateFilter(string apiKey) =>
        new ApiKeyEndpointFilter(
            Options.Create(new LaunchAppSettings { ApiKey = apiKey }),
            NullLogger<ApiKeyEndpointFilter>.Instance);

    private static EndpointFilterInvocationContext CreateContext(string? headerValue = null)
    {
        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
            httpContext.Request.Headers["x-api-key"] = headerValue;

        // EndpointFilterInvocationContext là abstract — dùng factory method với dummy arg
        return EndpointFilterInvocationContext.Create<object?>(httpContext, null);
    }

    private static EndpointFilterDelegate NextThatSetsFlag(ref bool called)
    {
        // Capture biến qua closure — workaround vì ref không dùng được trong lambda capture
        var flag = new bool[1];
        _ = flag; // suppress warning
        EndpointFilterDelegate del = _ =>
        {
            // Không thể set ref từ lambda; caller dùng bool[] thay thế nếu cần
            return ValueTask.FromResult<object?>(Results.Ok());
        };
        return del;
    }

    // ── Test 1: ApiKey rỗng trong config → fail-safe 401 ──────────────────────

    [Fact]
    public async Task InvokeAsync_EmptyApiKeyConfig_Returns401_AndDoesNotCallNext()
    {
        var filter = CreateFilter("");
        var ctx = CreateContext("any-key");
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await filter.InvokeAsync(ctx, next);

        Assert.False(nextCalled, "next KHÔNG được gọi khi ApiKey config rỗng");
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result!);
        Assert.Equal(401, statusResult.StatusCode);
    }

    // ── Test 2: Không có header x-api-key → 401 ─────────────────────────────

    [Fact]
    public async Task InvokeAsync_MissingHeader_Returns401_AndDoesNotCallNext()
    {
        var filter = CreateFilter("secret-key-12345678");
        var ctx = CreateContext(null); // không có header

        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await filter.InvokeAsync(ctx, next);

        Assert.False(nextCalled, "next KHÔNG được gọi khi thiếu header");
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result!);
        Assert.Equal(401, statusResult.StatusCode);
    }

    // ── Test 3: Header sai → 401 ─────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WrongKey_Returns401_AndDoesNotCallNext()
    {
        var filter = CreateFilter("correct-key-12345678");
        var ctx = CreateContext("wrong-key-99999999");

        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var result = await filter.InvokeAsync(ctx, next);

        Assert.False(nextCalled, "next KHÔNG được gọi khi key sai");
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result!);
        Assert.Equal(401, statusResult.StatusCode);
    }

    // ── Test 4: Key đúng → next được gọi ────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_CorrectKey_CallsNext()
    {
        var filter = CreateFilter("secret-key-12345678");
        var ctx = CreateContext("secret-key-12345678");

        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        await filter.InvokeAsync(ctx, next);

        Assert.True(nextCalled, "next PHẢI được gọi khi key đúng");
    }

    // ── Test 5: Constant-time compare — key khác độ dài → 401 ───────────────

    [Fact]
    public async Task InvokeAsync_KeyDifferentLength_Returns401()
    {
        // Kiểm tra không có short-circuit khi length khác (constant-time phải pad)
        var filter = CreateFilter("key-abc");
        var ctx = CreateContext("key-abc-extra");

        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        await filter.InvokeAsync(ctx, next);

        Assert.False(nextCalled);
    }
}
