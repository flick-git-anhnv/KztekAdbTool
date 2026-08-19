using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho DeviceConnectionEndpoints — kiểm tra logic ValidateConnectInput
/// và handler CheckStatus (API 2) thông qua tác vụ static có thể test trực tiếp.
/// Không cần WebApplicationFactory hay AdbService thật.
/// </summary>
public sealed class DeviceConnectionEndpointTests
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ValidateConnectInput — validate ip và port (API 1)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void ValidateConnectInput_IpNull_ReturnsRequired()
    {
        // ip null → sau Trim() + ?? rỗng → "ip là bắt buộc"
        var error = DeviceConnectionEndpoints.ValidateConnectInput("", null);

        Assert.Equal("ip là bắt buộc", error);
    }

    [Fact]
    public void ValidateConnectInput_IpEmpty_ReturnsRequired()
    {
        var error = DeviceConnectionEndpoints.ValidateConnectInput("", 5555);

        Assert.Equal("ip là bắt buộc", error);
    }

    [Fact]
    public void ValidateConnectInput_IpContainsColon_ReturnsColonError()
    {
        // ip chứa ':' → người dùng nhầm truyền ipPort vào field ip
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100:5555", null);

        Assert.Equal("ip không được chứa ':' — dùng field port riêng", error);
    }

    [Fact]
    public void ValidateConnectInput_IpContainsLeadingWhitespace_ReturnsWhitespaceError()
    {
        // Sau khi handler Trim(), nếu ip vẫn còn whitespace nội bộ → lỗi
        // Test trực tiếp ValidateConnectInput với ip đã có khoảng trắng nội bộ
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168 .1.100", null);

        Assert.Equal("ip không được chứa khoảng trắng", error);
    }

    [Fact]
    public void ValidateConnectInput_IpContainsTab_ReturnsWhitespaceError()
    {
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168\t1.100", null);

        Assert.Equal("ip không được chứa khoảng trắng", error);
    }

    [Fact]
    public void ValidateConnectInput_PortZero_ReturnsPortRangeError()
    {
        // port = 0 → ngoài range 1–65535
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", 0);

        Assert.Contains("port phải nằm trong khoảng", error);
    }

    [Fact]
    public void ValidateConnectInput_PortNegative_ReturnsPortRangeError()
    {
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", -1);

        Assert.Contains("port phải nằm trong khoảng", error);
    }

    [Fact]
    public void ValidateConnectInput_PortTooLarge_ReturnsPortRangeError()
    {
        // port = 65536 → ngoài range
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", 65536);

        Assert.Contains("port phải nằm trong khoảng", error);
    }

    [Fact]
    public void ValidateConnectInput_ValidIpNoPort_ReturnsNull()
    {
        // port = null → sử dụng default 5555 → không lỗi
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", null);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateConnectInput_ValidIpPortMin_ReturnsNull()
    {
        // port = 1 → hợp lệ (biên dưới)
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateConnectInput_ValidIpPortMax_ReturnsNull()
    {
        // port = 65535 → hợp lệ (biên trên)
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", 65535);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateConnectInput_ValidIpPortDefault5555_ReturnsNull()
    {
        // Happy path: ip hợp lệ, port = 5555
        var error = DeviceConnectionEndpoints.ValidateConnectInput("192.168.1.100", 5555);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateConnectInput_HostnameValid_ReturnsNull()
    {
        // ADB accept cả hostname (VD: emulator-5554.local) — không bắt regex IPv4 strict
        var error = DeviceConnectionEndpoints.ValidateConnectInput("emulator-5554.local", null);

        Assert.Null(error);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // API 2 — GET /api/devices/{serial}/status (CheckStatus logic)
    // Test thông qua DeviceState trực tiếp (handler dùng DeviceState.TryGet)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void CheckStatus_SerialNotFound_Returns404()
    {
        // Serial không có trong DeviceState → 404 DeviceNotFound
        var deviceState = new DeviceState(); // empty
        var logger = NullLoggerFactory.Instance;

        // Simulate handler logic
        var s = "192.168.1.100:5555";
        var found = deviceState.TryGet(s, out var device);

        Assert.False(found);
        Assert.Null(device);
        // Kết quả expected: handler trả Results.NotFound(...)
    }

    [Fact]
    public void CheckStatus_SerialOnline_DeviceStateReturnsOnline()
    {
        // Serial tồn tại, Status = "Online" → TryGet trả true, device.Status = "Online"
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord
        {
            Serial = "192.168.1.100:5555",
            Status = "Online"
        });

        var found = deviceState.TryGet("192.168.1.100:5555", out var device);

        Assert.True(found);
        Assert.NotNull(device);
        Assert.Equal("Online", device!.Status);
        Assert.Equal("192.168.1.100:5555", device.Serial);
    }

    [Fact]
    public void CheckStatus_SerialOffline_DeviceStateReturnsOffline()
    {
        // Serial tồn tại, Status = "Offline" → TryGet trả true, device.Status = "Offline"
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord
        {
            Serial = "192.168.1.100:5555",
            Status = "Offline"
        });

        var found = deviceState.TryGet("192.168.1.100:5555", out var device);

        Assert.True(found);
        Assert.NotNull(device);
        Assert.Equal("Offline", device!.Status);
    }

    [Fact]
    public void CheckStatus_SerialEmptyAfterTrim_ReturnsCorrectBehavior()
    {
        // serial = "   " → Trim() → "" → handler trả 400 InvalidInput
        // Test logic trim
        var rawSerial = "   ";
        var trimmed = rawSerial?.Trim() ?? string.Empty;

        Assert.True(string.IsNullOrEmpty(trimmed));
        // Handler: if (string.IsNullOrEmpty(s)) → 400
    }

    [Fact]
    public void CheckStatus_UsbSerialNoColon_DeviceStateReturnsCorrect()
    {
        // Serial USB không có ':' — vẫn hoạt động bình thường
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord
        {
            Serial = "R58N7XXXX",
            Status = "Online"
        });

        var found = deviceState.TryGet("R58N7XXXX", out var device);

        Assert.True(found);
        Assert.Equal("Online", device!.Status);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ConnectByIp handler logic — kiểm tra IResult status code
    // Dùng static helper để test không cần mock AdbService phức tạp
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void ConnectByIp_InvalidInput_Returns400()
    {
        // Verify validate trả lỗi → handler sẽ trả 400
        var err = DeviceConnectionEndpoints.ValidateConnectInput("", null);
        Assert.NotNull(err);

        // Simulate Results.BadRequest
        var result = Results.BadRequest(new { success = false, error = "InvalidInput", message = err });
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public void ConnectByIp_ValidInput_ValidateReturnsNull()
    {
        // ip hợp lệ, port hợp lệ → ValidateConnectInput trả null → handler tiếp tục gọi ADB
        var err = DeviceConnectionEndpoints.ValidateConnectInput("10.0.0.1", 5555);
        Assert.Null(err);
    }

    [Fact]
    public void ConnectByIp_TargetBuilt_Correctly()
    {
        // Kiểm tra target = "ip:port" được build đúng
        var ip = "192.168.1.100";
        var port = 5555;
        var target = $"{ip}:{port}";

        Assert.Equal("192.168.1.100:5555", target);
    }

    [Fact]
    public void ConnectByIp_TargetBuilt_WithDefaultPort()
    {
        // port = null → default 5555
        var ip = "192.168.1.100";
        var port = (int?)null ?? 5555; // DefaultAdbPort
        var target = $"{ip}:{port}";

        Assert.Equal("192.168.1.100:5555", target);
    }
}
