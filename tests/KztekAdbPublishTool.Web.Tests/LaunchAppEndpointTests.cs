using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho LaunchAppEndpoints — kiểm tra logic ValidateInput, CheckDeviceState, MapAdbResult.
/// Các method này là public static → không cần WebApplicationFactory, không cần AdbService thật.
/// </summary>
public sealed class LaunchAppEndpointTests
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ValidateInput — 400 cases (EC2, EC6, BR1)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void ValidateInput_SerialEmpty_ReturnsSerialRequired()
    {
        // Handler trim trước khi gọi ValidateInput, nên "" là đầu vào sau Trim()
        var error = LaunchAppEndpoints.ValidateInput("", "com.kztek.abc");

        Assert.Equal("serial is required", error);
    }

    [Fact]
    public void ValidateInput_AppEmpty_ReturnsAppRequired()
    {
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "");

        Assert.Equal("app is required", error);
    }

    [Fact]
    public void ValidateInput_PackageNoDot_ReturnsInvalidPackageName()
    {
        // "myapp" không có '.' → không hợp lệ theo BR1
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "myapp");

        Assert.Equal("invalid package name", error);
    }

    [Fact]
    public void ValidateInput_PackageWithWhitespace_ReturnsInvalidPackageName()
    {
        // "com.kztek abc" có whitespace → không hợp lệ
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "com.kztek abc");

        Assert.Equal("invalid package name", error);
    }

    [Fact]
    public void ValidateInput_PackageStartsWithDigit_ReturnsInvalidPackageName()
    {
        // "1com.kztek.abc" — segment đầu bắt đầu bằng số → không hợp lệ
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "1com.kztek.abc");

        Assert.Equal("invalid package name", error);
    }

    [Fact]
    public void ValidateInput_PackageEmptySegment_ReturnsInvalidPackageName()
    {
        // "com..kztek" — segment rỗng giữa hai dấu chấm → không hợp lệ
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "com..kztek");

        Assert.Equal("invalid package name", error);
    }

    [Fact]
    public void ValidateInput_ValidPackageTwoSegments_ReturnsNull()
    {
        // "com.example" — 2 segment, hợp lệ
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "com.example");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateInput_ValidPackageThreeSegments_ReturnsNull()
    {
        // "com.kztek.abc" — 3 segment, hợp lệ (happy path)
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "com.kztek.abc");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateInput_ValidPackageWithUnderscore_ReturnsNull()
    {
        // "com.kztek_corp.my_app" — underscore được phép
        var error = LaunchAppEndpoints.ValidateInput("192.168.1.100:5555", "com.kztek_corp.my_app");

        Assert.Null(error);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // CheckDeviceState — 404 và 422 DeviceOffline
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void CheckDeviceState_UnknownSerial_Returns404()
    {
        // SC-03: serial không tồn tại trong DeviceState
        var deviceState = new DeviceState(); // empty
        var result = LaunchAppEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.NotNull(result);
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(404, statusResult.StatusCode);
    }

    [Fact]
    public void CheckDeviceState_OfflineDevice_Returns422()
    {
        // SC-05: serial đã biết nhưng Status != "Online"
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord
        {
            Serial = "192.168.1.100:5555",
            Status = "Offline"
        });

        var result = LaunchAppEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.NotNull(result);
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(422, statusResult.StatusCode);
    }

    [Fact]
    public void CheckDeviceState_OnlineDevice_ReturnsNull()
    {
        // Device Online → null = handler tiếp tục gọi AdbService
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord
        {
            Serial = "192.168.1.100:5555",
            Status = "Online"
        });

        var result = LaunchAppEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.Null(result);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MapAdbResult — 200 / 422 / 500 (SC-01, SC-06, EC1, EC4)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void MapAdbResult_ExitCode0_Returns200()
    {
        // SC-01: Launch thành công (ExitCode = 0)
        var adbResult = new AdbCommandResult
        {
            ExitCode = 0,
            StdOut = "Starting: Intent { cmp=com.kztek.abc/.MainActivity }",
            StdErr = ""
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(200, statusResult.StatusCode);
    }

    [Fact]
    public void MapAdbResult_AppNotInstalled_Returns422()
    {
        // SC-06: "No activities found to run" từ AdbService.LaunchAppAsync
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut = "",
            StdErr = "No activities found to run"
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, statusResult.StatusCode);
    }

    [Fact]
    public void MapAdbResult_AdbTimeout_Returns422()
    {
        // EC1: "timeout sau" từ AdbService.RunAsync khi OperationCanceledException
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut = "",
            StdErr = "adb -s 192.168.1.100:5555 shell am start timeout sau 10000ms"
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, statusResult.StatusCode);
    }

    [Fact]
    public void MapAdbResult_AdbNotFound_Returns500()
    {
        // EC4: "Không tìm thấy adb tại:" từ AdbService.RunAsync khi !File.Exists(adbPath)
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut = "",
            StdErr = "Không tìm thấy adb tại: /opt/platform-tools/adb"
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public void MapAdbResult_OtherAdbError_Returns422AdbError()
    {
        // Lỗi ADB không rơi vào 3 case trên → AdbError 422
        var adbResult = new AdbCommandResult
        {
            ExitCode = 1,
            StdOut = "",
            StdErr = "error: device offline"
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, statusResult.StatusCode);
    }

    [Fact]
    public void MapAdbResult_EmptyStdErr_Returns422AdbErrorWithDefaultMessage()
    {
        // StdErr rỗng → message fallback "ADB command failed"
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut = "",
            StdErr = ""
        };

        var httpResult = LaunchAppEndpoints.MapAdbResult(
            "192.168.1.100:5555", "com.kztek.abc", adbResult, NullLogger.Instance);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, statusResult.StatusCode);
    }
}
