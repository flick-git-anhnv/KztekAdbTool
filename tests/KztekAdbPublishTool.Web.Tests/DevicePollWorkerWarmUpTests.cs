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

    /// <summary>
    /// Bug fix 2.3: device đầu tiên timeout (per-device timeout, ExitCode=-1) →
    /// device thứ 2 và 3 VẪN được gọi ConnectAsync — không break toàn vòng warm-up.
    /// Test path: AdbService.RunAsync trả về ExitCode=-1 (không throw OCE) cho per-device timeout.
    /// </summary>
    [Fact]
    public async Task WarmUpReconnect_FirstDeviceTimesOut_ContinuesToRemainingDevices()
    {
        // Arrange: 3 WiFi devices; device 1 simulate per-device timeout (ExitCode=-1)
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        _repo.Upsert(MakeDevice("192.168.1.11:5555"));
        _repo.Upsert(MakeDevice("192.168.1.12:5555"));

        var fake = new FakeAdbService();
        fake.TimeoutOnSerial.Add("192.168.1.10:5555"); // device 1 timeout: trả về ExitCode=-1

        var worker = CreateWorker(fake);

        // Act — ct KHÔNG bị cancel (không phải service shutdown)
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert: cả 3 device đều được gọi ConnectAsync
        // (device 1 timeout không được phép break vòng lặp)
        Assert.Equal(3, fake.ConnectCalls.Count);
        Assert.Contains("192.168.1.10:5555", fake.ConnectCalls);
        Assert.Contains("192.168.1.11:5555", fake.ConnectCalls);
        Assert.Contains("192.168.1.12:5555", fake.ConnectCalls);
    }

    /// <summary>
    /// Bug fix 2.3 — defensive path: device đầu tiên ném OperationCanceledException
    /// mà KHÔNG cancel ct gốc (giả lập OCE propagate từ internal timeout) →
    /// device thứ 2 VẪN được gọi ConnectAsync.
    /// Test path: catch(OCE) { if (!ct.IsCancellationRequested) continue; }
    /// </summary>
    [Fact]
    public async Task WarmUpReconnect_FirstDeviceOcesWithoutCtCancel_ContinuesToNextDevice()
    {
        // Arrange: 2 WiFi devices; device 1 ném OCE nhưng ct gốc KHÔNG bị cancel
        _repo.Upsert(MakeDevice("192.168.1.10:5555"));
        _repo.Upsert(MakeDevice("192.168.1.11:5555"));

        var fake = new FakeAdbService();
        fake.OceOnSerial.Add("192.168.1.10:5555"); // ném OCE, ct.IsCancellationRequested == false

        var worker = CreateWorker(fake);

        // Act — ct KHÔNG bị cancel
        await worker.WarmUpReconnectAsync(CancellationToken.None);

        // Assert: cả 2 device đều được gọi (OCE của device 1 không break vòng lặp)
        Assert.Equal(2, fake.ConnectCalls.Count);
        Assert.Contains("192.168.1.10:5555", fake.ConnectCalls);
        Assert.Contains("192.168.1.11:5555", fake.ConnectCalls);
    }

    // ── Fakes ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fake IAdbService ghi nhận ConnectAsync calls và cho phép simulate lỗi cho serial cụ thể.
    ///
    /// - FailOnSerial    : ném InvalidOperationException (lỗi thông thường)
    /// - TimeoutOnSerial : trả về AdbCommandResult ExitCode=-1 (giả lập per-device timeout
    ///                     sau khi AdbService.RunAsync đã xử lý nội bộ, theo fix 2.3)
    /// - OceOnSerial     : ném OperationCanceledException MÀ KHÔNG cancel ct gốc
    ///                     (test defensive catch path trong WarmUpReconnectAsync)
    /// </summary>
    internal sealed class FakeAdbService : IAdbService
    {
        public List<string> ConnectCalls { get; } = new();
        public HashSet<string> FailOnSerial { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Simulate per-device timeout: ConnectAsync trả về AdbCommandResult với ExitCode=-1
        /// (giống hành vi AdbService.RunAsync sau fix 2.3 — không re-throw khi chỉ internal timeout).
        /// </summary>
        public HashSet<string> TimeoutOnSerial { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Simulate OperationCanceledException propagate mà KHÔNG cancel ct gốc
        /// (test defensive catch block trong WarmUpReconnectAsync).
        /// </summary>
        public HashSet<string> OceOnSerial { get; } = new(StringComparer.Ordinal);

        public Task<AdbCommandResult> ConnectAsync(string ipPort, int timeoutMs = 10000, CancellationToken ct = default)
        {
            ConnectCalls.Add(ipPort);

            // Simulate real AdbService: re-throw nếu ct gốc (stoppingToken) bị cancel.
            ct.ThrowIfCancellationRequested();

            if (FailOnSerial.Contains(ipPort))
                return Task.FromException<AdbCommandResult>(
                    new InvalidOperationException($"Simulated connect failure for {ipPort}"));

            if (TimeoutOnSerial.Contains(ipPort))
                return Task.FromResult(new AdbCommandResult
                {
                    ExitCode = -1,
                    StdOut = string.Empty,
                    StdErr = $"adb connect {ipPort} timeout sau 5000ms",
                });

            if (OceOnSerial.Contains(ipPort))
                return Task.FromException<AdbCommandResult>(
                    new OperationCanceledException($"Simulated per-device timeout OCE for {ipPort}"));

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
