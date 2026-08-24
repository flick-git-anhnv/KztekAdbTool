using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho RebootEndpoints — kiểm tra CheckDeviceState, MapRebootResult.
/// Các method này là public static → không cần WebApplicationFactory hay AdbService thật.
/// </summary>
public sealed class RebootEndpointsTests
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // CheckDeviceState — 404 / 422 / null
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void CheckDeviceState_UnknownSerial_Returns404()
    {
        var deviceState = new DeviceState();
        var result = RebootEndpoints.CheckDeviceState(
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

        var result = RebootEndpoints.CheckDeviceState(
            "192.168.1.100:5555", deviceState, NullLogger.Instance);

        Assert.NotNull(result);
        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(422, sc.StatusCode);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MapRebootResult — happy path + error cases
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void MapRebootResult_Success_Returns200()
    {
        var adbResult = new AdbCommandResult { ExitCode = 0, StdOut = "", StdErr = "" };

        var httpResult = RebootEndpoints.MapRebootResult(
            "192.168.1.100:5555", adbResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(200, sc.StatusCode);
    }

    [Fact]
    public void MapRebootResult_AdbNotFound_Returns500()
    {
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut   = "",
            StdErr   = "Không tìm thấy adb tại: /opt/platform-tools/adb"
        };

        var httpResult = RebootEndpoints.MapRebootResult(
            "192.168.1.100:5555", adbResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(500, sc.StatusCode);
    }

    [Fact]
    public void MapRebootResult_AdbTimeout_Returns422()
    {
        var adbResult = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut   = "",
            StdErr   = "adb -s 192.168.1.100:5555 reboot timeout sau 10000ms"
        };

        var httpResult = RebootEndpoints.MapRebootResult(
            "192.168.1.100:5555", adbResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, sc.StatusCode);
    }

    [Fact]
    public void MapRebootResult_AdbError_Returns422()
    {
        var adbResult = new AdbCommandResult
        {
            ExitCode = 1,
            StdOut   = "",
            StdErr   = "error: device offline"
        };

        var httpResult = RebootEndpoints.MapRebootResult(
            "192.168.1.100:5555", adbResult, NullLogger.Instance);

        var sc = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, sc.StatusCode);
    }
}
