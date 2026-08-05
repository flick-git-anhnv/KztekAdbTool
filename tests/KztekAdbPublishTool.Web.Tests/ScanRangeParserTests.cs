using KztekAdbPublishTool.Web.Services;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho ScanRangeParser — pure static logic, không có side-effect/dependency.
/// </summary>
public sealed class ScanRangeParserTests
{
    // ── Dash range (octet cuối) ───────────────────────────────────────────────

    [Fact]
    public void TryParse_DashRangeOctet_Returns254Ips()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.1-254", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Null(error);
        Assert.Equal(254, ips!.Count);
        Assert.Equal("192.168.1.1", ips[0]);
        Assert.Equal("192.168.1.254", ips[^1]);
    }

    [Fact]
    public void TryParse_DashRangeOctet_StartsFromZero()
    {
        var ok = ScanRangeParser.TryParse("10.0.0.0-255", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Equal(256, ips!.Count);
        Assert.Equal("10.0.0.0", ips[0]);
        Assert.Equal("10.0.0.255", ips[^1]);
    }

    // ── Dash range (full IP) ──────────────────────────────────────────────────

    [Fact]
    public void TryParse_DashRangeFullIp_SameSubnet_Returns254Ips()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.1-192.168.1.254", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Null(error);
        Assert.Equal(254, ips!.Count);
    }

    [Fact]
    public void TryParse_DashRangeFullIp_CrossSubnet_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.1-192.168.2.254", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
        Assert.Contains("24", error); // thông báo về /24
    }

    // ── CIDR /24 ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_CidrSlash24_Returns256Ips()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.0/24", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Null(error);
        Assert.Equal(256, ips!.Count);
        Assert.Equal("192.168.1.0", ips[0]);
        Assert.Equal("192.168.1.255", ips[^1]);
    }

    [Fact]
    public void TryParse_CidrNonSlash24_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("192.168.0.0/23", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
        Assert.Contains("24", error);
    }

    // ── Single IP ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_SingleIp_Returns1Ip()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.100", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Null(error);
        Assert.Single(ips!);
        Assert.Equal("192.168.1.100", ips![0]);
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public void TryParse_EmptyString_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_WhitespaceOnly_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("   ", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_InvalidIp_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("not-an-ip", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_RangeEndLessThanStart_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.100-50", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_InvalidCidrFormat_ReturnsError()
    {
        var ok = ScanRangeParser.TryParse("192.168.1.0/abc", out var ips, out var error);

        Assert.False(ok);
        Assert.Null(ips);
        Assert.NotNull(error);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_SingleOctetRange_ReturnsCorrectIps()
    {
        // "192.168.1.5-5" → 1 IP
        var ok = ScanRangeParser.TryParse("192.168.1.5-5", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Single(ips!);
        Assert.Equal("192.168.1.5", ips![0]);
    }

    [Fact]
    public void TryParse_WithLeadingTrailingSpaces_ParsesCorrectly()
    {
        var ok = ScanRangeParser.TryParse("  192.168.1.1-10  ", out var ips, out var error);

        Assert.True(ok, $"Expected success but got error: {error}");
        Assert.Equal(10, ips!.Count);
    }
}
