using KztekAdbPublishTool.Web.Services;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho AdbService.IsForegroundInDumpsys — public static, không cần AdbService thật.
/// Kiểm tra 5 case: Foreground (mResumedActivity), Background (running nhưng không foreground),
/// NotRunning (stdout rỗng), Fallback Android6 (mFocusedActivity), và package khác foreground.
/// </summary>
public sealed class AdbServiceAppStatusTests
{
    private const string Serial  = "192.168.1.100:5555";
    private const string Package = "com.kztek.demo";

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // IsForegroundInDumpsys — 5 cases
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void IsForegroundInDumpsys_MResumedActivity_ContainsPackage_ReturnsTrue()
    {
        // Android 8+: mResumedActivity có tên package → Foreground
        var dumpsysOut =
            "  mResumedActivity: ActivityRecord{abc123 u0 com.kztek.demo/.MainActivity t42}\n" +
            "  mFocusedStack=...\n";

        var result = AdbService.IsForegroundInDumpsys(dumpsysOut, Package);

        Assert.True(result);
    }

    [Fact]
    public void IsForegroundInDumpsys_MResumedActivity_OtherPackage_ReturnsFalse()
    {
        // mResumedActivity có nhưng là package khác → Background
        var dumpsysOut =
            "  mResumedActivity: ActivityRecord{abc123 u0 com.android.launcher/.Launcher t1}\n";

        var result = AdbService.IsForegroundInDumpsys(dumpsysOut, Package);

        Assert.False(result);
    }

    [Fact]
    public void IsForegroundInDumpsys_EmptyStdOut_ReturnsFalse()
    {
        // stdout rỗng → false (safe default)
        var result = AdbService.IsForegroundInDumpsys("", Package);

        Assert.False(result);
    }

    [Fact]
    public void IsForegroundInDumpsys_MFocusedActivity_FallbackAndroid6_ReturnsTrue()
    {
        // Android 6-7 fallback: không có mResumedActivity, có mFocusedActivity
        var dumpsysOut =
            "  mFocusedActivity: ActivityRecord{abc123 u0 com.kztek.demo/.MainActivity t42}\n";

        var result = AdbService.IsForegroundInDumpsys(dumpsysOut, Package);

        Assert.True(result);
    }

    [Fact]
    public void IsForegroundInDumpsys_NeitherResumedNorFocused_ReturnsFalse()
    {
        // Không có dòng mResumedActivity hay mFocusedActivity với package → Background
        var dumpsysOut =
            "  ACTIVITY MANAGER ACTIVITIES (dumpsys activity activities)\n" +
            "  Stack #0: type=home mode=fullscreen\n" +
            "    mTask #1234\n";

        var result = AdbService.IsForegroundInDumpsys(dumpsysOut, Package);

        Assert.False(result);
    }
}
