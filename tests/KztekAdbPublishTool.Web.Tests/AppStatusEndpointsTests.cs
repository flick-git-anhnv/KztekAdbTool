using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho AppStatusEndpoints — kiểm tra ValidateInput, CheckDeviceState, MapAppStatusResult.
/// Các method này là public static → không cần WebApplicationFactory hay AdbService thật.
/// </summary>
public sealed class AppStatusEndpointsTests
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ValidateInput — 400 cases
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void ValidateInput_PackageEmpty_ReturnsPackageRequired()
    {
        var error = AppStatusEndpoints.ValidateInput("192.168.1.100:5555", "");

        Assert.Equal("package is required", error);
    }

    [Fact]
    public void ValidateInput_PackageInvalidFormat_ReturnsInvalidPackageName()
    {
        // "myapp" không có '.' → không hợp lệ
        var error = AppStatusEndpoints.ValidateInput("192.168.1.100:5555", "myapp");

        Assert.Equal("invalid package name", error);
    }

    [Fact]
    public void ValidateInput_ValidPackage_ReturnsNull()
    {
        var error = AppStatusEndpoints.ValidateInput("192.168.1.100:5555", "com.kztek.demo");

        Assert.Null(error);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // CheckDeviceState — 404 / 422 / null
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void CheckDeviceState_UnknownSerial_Returns404()
    {
        var deviceState = new DeviceState();
        var result = AppStatusEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.NotNull(result);
        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(404, sc.StatusCode);
    }

    [Fact]
    public void CheckDeviceState_OfflineDevice_Returns422()
    {
        var deviceState = new DeviceState();
        deviceState.AddOrUpdate(new DeviceRecord { Serial = "192.168.1.100:5555", Status = "Offline" });

        var result = AppStatusEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.NotNull(result);
        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(422, sc.StatusCode);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MapAppStatusResult — happy path (3 states) + error cases
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void MapAppStatusResult_Foreground_Returns200()
    {
        var appResult = new AppStatusResult
        {
            Running     = true,
            State       = AppState.Foreground,
            PidofResult = new AdbCommandResult { ExitCode = 0, StdOut = "12345\n", StdErr = "" }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(200, sc.StatusCode);
    }

    [Fact]
    public void MapAppStatusResult_Background_Returns200()
    {
        var appResult = new AppStatusResult
        {
            Running     = true,
            State       = AppState.Background,
            PidofResult = new AdbCommandResult { ExitCode = 0, StdOut = "12345\n", StdErr = "" }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(200, sc.StatusCode);
    }

    [Fact]
    public void MapAppStatusResult_NotRunning_Returns200()
    {
        // running=false vẫn trả 200 (theo TDD §9 Error Matrix)
        var appResult = new AppStatusResult
        {
            Running     = false,
            State       = AppState.NotRunning,
            PidofResult = new AdbCommandResult { ExitCode = 1, StdOut = "", StdErr = "" }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(200, sc.StatusCode);
    }

    [Fact]
    public void MapAppStatusResult_AdbNotFound_Returns500()
    {
        var appResult = new AppStatusResult
        {
            Running     = false,
            State       = AppState.NotRunning,
            PidofResult = new AdbCommandResult
            {
                ExitCode = -1,
                StdOut   = "",
                StdErr   = "Không tìm thấy adb tại: /opt/platform-tools/adb"
            }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(500, sc.StatusCode);
    }

    [Fact]
    public void MapAppStatusResult_AdbTimeout_Returns422()
    {
        var appResult = new AppStatusResult
        {
            Running     = false,
            State       = AppState.NotRunning,
            PidofResult = new AdbCommandResult
            {
                ExitCode = -1,
                StdOut   = "",
                StdErr   = "adb -s 192.168.1.100:5555 shell pidof com.kztek.demo timeout sau 10000ms"
            }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, sc.StatusCode);
    }

    [Fact]
    public void MapAppStatusResult_AdbError_Returns422()
    {
        var appResult = new AppStatusResult
        {
            Running     = false,
            State       = AppState.NotRunning,
            PidofResult = new AdbCommandResult
            {
                ExitCode = -2,
                StdOut   = "",
                StdErr   = "error: device offline"
            }
        };

        var httpResult = AppStatusEndpoints.MapAppStatusResult(
            "192.168.1.100:5555", "com.kztek.demo", appResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, sc.StatusCode);
    }
}
