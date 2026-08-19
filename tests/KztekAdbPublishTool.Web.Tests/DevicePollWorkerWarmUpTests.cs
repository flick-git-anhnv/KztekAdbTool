using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using KztekAdbPublishTool.Web.Workers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho DevicePollWorker.WarmUpReconnectAsync —
/// xác nhận logic warm-up sau restart: gọi ConnectAsync đúng device, bỏ USB, tiếp tục khi fail.
/// Dùng SQLite temp file cho DeviceRepository (không mock) + FakeAdbService cho IAdbService.
/// </summary>
public sealed class DevicePollWorkerWarmUpTests : IDisposable
{
    // ── Helpers ─────────────────────────────────────────────────────────────────

    private readonly string _dbPath;
    private readonly DeviceRepository _repo;

    public DevicePollWorkerWarmUpTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-warmup-{Guid.NewGuid():N}.db");
        var opts = Options.Create(new AdbSettings { DbPath = _dbPath });
        _repo = new DeviceRepository(opts);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private static DeviceRecord MakeDevice(string serial) => new()
    {
        Serial = serial,
        Model = "TestModel",
        ConnectionType = serial.Contains(':') ? "WiFi" : "USB",
        Status = "Online",
        FirstSeen = DateTime.UtcNow,
        LastSeen = DateTime.UtcNow,
    };

    private DevicePollWorker CreateWorker(FakeAdbService fake) =>
        new(
            NullLogger<DevicePollWorker>.Instance,
            fake,
            _repo,
            new DeviceState(),
            new PollControlService(),
            new FakeHubContext(),
            Options.Create(new AdbSettings { PollIntervalMs = 3000, DbPath = _dbPath }));

    // ── Test cases ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task WarmUpReconnect_SingleWifiDevice_CallsConnectOnce()
    {
        // Arrange
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        // Act
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert
        Assert.Single(fake.ConnectCalls);
        Assert.Equal("192.168.1.10:5555", fake.ConnectCalls[0]);
    }

    [Fact]
    public async Task WarmUpReconnect_MultipleWifiDevices_CallsConnectForEach()
    {
        // Arrange
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        _repo.Upsert(MakeDevice("192.168.1.11:5555"));
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        // Act
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert — ConnectAsync được gọi đúng 2 lần
        Assert.Equal(2, fake.ConnectCalls.Count);
        Assert.Contains("192.168.1.10:5555", fake.ConnectCalls);
        Assert.Contains("192.168.1.11:5555", fake.ConnectCalls);
    }

    [Fact]
    public async Task WarmUpReconnect_UsbDeviceOnly_ConnectNotCalled()
    {
        // Arrange: serial USB không chứa ':'
        _repo.Upsert(MakeDevice("HT7A21234ABC"));
        _repo.Upsert(MakeDevice("emulator-5554"));
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        // Act
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert: USB devices bị loại trừ — không gọi ConnectAsync
        Assert.Empty(fake.ConnectCalls);
    }

    [Fact]
    public async Task WarmUpReconnect_MixedDevices_OnlyWifiConnected()
    {
        // Arrange
        _repo.Upsert(MakeDevice("192.168.1.10:5555")); // WiFi
        _repo.Upsert(MakeDevice("HT7A21234ABC"));       // USB
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        // Act
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert
        Assert.Single(fake.ConnectCalls);
        Assert.Equal("192.168.1.10:5555", fake.ConnectCalls[0]);
    }

    [Fact]
    public async Task WarmUpReconnect_OneDeviceConnectFails_ContinuesToNextDevice_DoesNotThrow()
    {
        // Arrange: 2 WiFi devices; device đầu tiên connect fail (exception)
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        _repo.Upsert(MakeDevice("192.168.1.11:5555"));

        var fake = new FakeAdbService();
        fake.FailOnSerial.Add("192.168.1.10:5555");

        var worker = CreateWorker(fake);

        // Act — KHÔNG được throw, phải tiếp tục device thứ 2
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert: cả 2 device đều được thử (dù device 1 fail)
        Assert.Equal(2, fake.ConnectCalls.Count);
        Assert.Contains("192.168.1.10:5555", fake.ConnectCalls);
        Assert.Contains("192.168.1.11:5555", fake.ConnectCalls);
    }

    [Fact]
    public async Task WarmUpReconnect_EmptyDatabase_ConnectNotCalled()
    {
        // Arrange: không có device nào trong DB
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        // Act
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert
        Assert.Empty(fake.ConnectCalls);
    }

    [Fact]
    public async Task WarmUpReconnect_CancelledBeforeStart_ConnectNotCalled()
    {
        // Arrange
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        var fake = new FakeAdbService();
        var worker = CreateWorker(fake);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // already cancelled

        // Act — không throw, không gọi ConnectAsync
        await worker.WarmUpReconnectAsync(cts.Token);

        // Assert: cancelled before loop body → không gọi
        Assert.Empty(fake.ConnectCalls);
    }

    // ── Fakes ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fake IAdbService ghi nhận ConnectAsync calls và cho phép simulate lỗi cho serial cụ thể.
    /// </summary>
    internal sealed class FakeAdbService : IAdbService
    {
        public List<string> ConnectCalls { get; } = new();
        public HashSet<string> FailOnSerial { get; } = new(StringComparer.Ordinal);

        public Task<AdbCommandResult> ConnectAsync(string ipPort, int timeoutMs = 10000, CancellationToken ct = default)
        {
            ConnectCalls.Add(ipPort);

            if (FailOnSerial.Contains(ipPort))
                return Task.FromException<AdbCommandResult>(
                    new InvalidOperationException($"Simulated connect failure for {ipPort}"));

            return Task.FromResult(new AdbCommandResult
            {
                ExitCode = 0,
                StdOut = $"connected to {ipPort}",
            });
        }

        public Task<List<AdbDevice>> GetDevicesAsync(CancellationToken ct = default)
            => Task.FromResult(new List<AdbDevice>());

        public Task<string?> GetPackageVersionAsync(string serial, string packageName, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Fake IHubContext — WarmUpReconnectAsync không dùng hub nên mọi thứ là null.
    /// </summary>
    private sealed class FakeHubContext : IHubContext<DeviceHub>
    {
        // WarmUpReconnectAsync không gọi hub — safe to return null
        public IHubClients Clients => null!;
        public IGroupManager Groups => null!;
    }
}
