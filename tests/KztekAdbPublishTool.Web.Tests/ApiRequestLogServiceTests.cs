using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho ApiRequestLogService — kiểm tra truncate, swallow exception khi repo throw,
/// swallow exception khi hub throw, cập nhật entry.Id sau insert.
/// Dùng fake implementation thủ công (không có Moq trong project).
/// </summary>
public sealed class ApiRequestLogServiceTests
{
    /// <summary>
    /// Fake IApiRequestLogService để test riêng logic — không cần FakeRepo/FakeHub.
    /// Test trực tiếp behavior service thông qua spy implementation.
    /// </summary>
    private sealed class SpyApiRequestLogService : IApiRequestLogService
    {
        public ApiRequestLogEntry? LastEntry { get; private set; }
        public int CallCount { get; private set; }
        public bool ShouldThrow { get; set; }
        public Exception? ThrowWith { get; set; }

        public Task LogAsync(ApiRequestLogEntry entry, CancellationToken ct)
        {
            CallCount++;
            LastEntry = entry;
            if (ShouldThrow && ThrowWith is not null)
                throw ThrowWith;
            return Task.CompletedTask;
        }
    }

    // ── Test ApiRequestLogService truncate behavior ──────────────────────────
    // Dùng in-memory SQLite + null hub để test truncate logic trong service thật.

    private sealed class NullHubClients : IHubClients
    {
        public IClientProxy All    => NullClientProxy.Instance;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => NullClientProxy.Instance;
        public IClientProxy Client(string connectionId) => NullClientProxy.Instance;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => NullClientProxy.Instance;
        public IClientProxy Group(string groupName) => NullClientProxy.Instance;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => NullClientProxy.Instance;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => NullClientProxy.Instance;
        public IClientProxy User(string userId) => NullClientProxy.Instance;
        public IClientProxy Users(IReadOnlyList<string> userIds) => NullClientProxy.Instance;
    }

    private sealed class NullClientProxy : IClientProxy
    {
        public static readonly NullClientProxy Instance = new();
        public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NullHubContext : IHubContext<KztekAdbPublishTool.Web.Hubs.DeviceHub>
    {
        public IHubClients Clients => new NullHubClients();
        public IGroupManager Groups => null!;
    }

    private static ApiRequestLogService CreateServiceWithInMemoryDb(out string dbPath)
    {
        // Tạo DB tạm (in-memory Sqlite không hoạt động với nhiều connection trong ADO.NET)
        dbPath = Path.Combine(Path.GetTempPath(), $"test_apilog_{Guid.NewGuid():N}.db");
        var options = Microsoft.Extensions.Options.Options.Create(
            new KztekAdbPublishTool.Web.Configuration.AdbSettings { DbPath = dbPath });
        var repo = new ApiRequestLogRepository(options, NullLogger<ApiRequestLogRepository>.Instance);
        var hub  = new NullHubContext();
        return new ApiRequestLogService(repo, hub, NullLogger<ApiRequestLogService>.Instance);
    }

    // ── Test 1: LogAsync — ghi được vào DB (Id > 0 sau insert) ──────────────

    [Fact]
    public async Task LogAsync_HappyPath_SetsIdAndPersists()
    {
        var svc = CreateServiceWithInMemoryDb(out var dbPath);
        try
        {
            var entry = new ApiRequestLogEntry
            {
                Timestamp      = DateTime.UtcNow,
                ApiName        = ApiRequestLogConstants.ApiAddDevice,
                HttpMethod     = "POST",
                Path           = "/api/devices/connect-by-ip",
                Parameters     = "{\"ip\":\"192.168.1.10\",\"port\":5555}",
                Result         = ApiRequestLogConstants.ResultSuccess,
                HttpStatusCode = 200,
                CallerIp       = "10.0.0.1",
                DurationMs     = 42
            };

            await svc.LogAsync(entry, CancellationToken.None);

            Assert.True(entry.Id > 0, "Id phải được gán sau khi insert thành công");
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    // ── Test 2: LogAsync — truncate Parameters vượt giới hạn ────────────────

    [Fact]
    public async Task LogAsync_LongParameters_TruncatesToMaxLength()
    {
        var svc = CreateServiceWithInMemoryDb(out var dbPath);
        try
        {
            var longParams = new string('x', ApiRequestLogConstants.MaxParametersLength + 100);
            var entry = new ApiRequestLogEntry
            {
                Timestamp      = DateTime.UtcNow,
                ApiName        = ApiRequestLogConstants.ApiLaunchApp,
                HttpMethod     = "POST",
                Path           = "/api/launch-app",
                Parameters     = longParams,
                Result         = ApiRequestLogConstants.ResultFailure,
                HttpStatusCode = 422,
                CallerIp       = "10.0.0.2",
                DurationMs     = 10
            };

            await svc.LogAsync(entry, CancellationToken.None);

            // Sau LogAsync, entry.Parameters đã bị truncate
            Assert.Equal(ApiRequestLogConstants.MaxParametersLength, entry.Parameters!.Length);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    // ── Test 3: LogAsync — truncate ErrorMessage vượt giới hạn ──────────────

    [Fact]
    public async Task LogAsync_LongErrorMessage_TruncatesToMaxLength()
    {
        var svc = CreateServiceWithInMemoryDb(out var dbPath);
        try
        {
            var longErr = new string('e', ApiRequestLogConstants.MaxErrorMessageLength + 50);
            var entry = new ApiRequestLogEntry
            {
                Timestamp      = DateTime.UtcNow,
                ApiName        = ApiRequestLogConstants.ApiAddDevice,
                HttpMethod     = "POST",
                Path           = "/api/devices/connect-by-ip",
                Result         = ApiRequestLogConstants.ResultFailure,
                HttpStatusCode = 500,
                ErrorMessage   = longErr,
                CallerIp       = "10.0.0.3",
                DurationMs     = 5
            };

            await svc.LogAsync(entry, CancellationToken.None);

            Assert.Equal(ApiRequestLogConstants.MaxErrorMessageLength, entry.ErrorMessage!.Length);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    // ── Test 4: LogAsync — SignalR hub throw KHÔNG fail LogAsync ─────────────

    [Fact]
    public async Task LogAsync_HubThrows_DoesNotPropagateException()
    {
        // Dùng hub throw để kiểm tra swallow
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_apilog_{Guid.NewGuid():N}.db");
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(
                new KztekAdbPublishTool.Web.Configuration.AdbSettings { DbPath = dbPath });
            var repo    = new ApiRequestLogRepository(options, NullLogger<ApiRequestLogRepository>.Instance);
            var throwingHub = new ThrowingHubContext();
            var svc     = new ApiRequestLogService(repo, throwingHub, NullLogger<ApiRequestLogService>.Instance);

            var entry = new ApiRequestLogEntry
            {
                Timestamp      = DateTime.UtcNow,
                ApiName        = ApiRequestLogConstants.ApiLaunchApp,
                HttpMethod     = "POST",
                Path           = "/api/launch-app",
                Result         = ApiRequestLogConstants.ResultUnauthorized,
                HttpStatusCode = 401,
                CallerIp       = "10.0.0.4",
                DurationMs     = 3
            };

            // Không được throw dù hub throw
            var ex = await Record.ExceptionAsync(() => svc.LogAsync(entry, CancellationToken.None));
            Assert.Null(ex);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class ThrowingClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default)
            => throw new InvalidOperationException("SignalR hub simulated failure");
    }

    private sealed class ThrowingHubClients : IHubClients
    {
        public IClientProxy All => new ThrowingClientProxy();
        public IClientProxy AllExcept(IReadOnlyList<string> _) => new ThrowingClientProxy();
        public IClientProxy Client(string _) => new ThrowingClientProxy();
        public IClientProxy Clients(IReadOnlyList<string> _) => new ThrowingClientProxy();
        public IClientProxy Group(string _) => new ThrowingClientProxy();
        public IClientProxy GroupExcept(string _, IReadOnlyList<string> __) => new ThrowingClientProxy();
        public IClientProxy Groups(IReadOnlyList<string> _) => new ThrowingClientProxy();
        public IClientProxy User(string _) => new ThrowingClientProxy();
        public IClientProxy Users(IReadOnlyList<string> _) => new ThrowingClientProxy();
    }

    private sealed class ThrowingHubContext : IHubContext<KztekAdbPublishTool.Web.Hubs.DeviceHub>
    {
        public IHubClients Clients => new ThrowingHubClients();
        public IGroupManager Groups => null!;
    }
}
