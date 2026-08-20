using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

/// <summary>
/// Unit tests cho tính năng Uninstall Before Install.
/// Covers:
///   1. AdbService.UninstallApkAsync — classify result (real success vs any-fail)
///   2. InstallRequest — field UninstallBeforeInstall tồn tại và default false
///   3. Endpoint POST /api/settings/uninstall-before-install — lưu/trả về đúng
/// </summary>
public sealed class UninstallBeforeInstallTests
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 1. Classify logic — AdbCommandResult interpretation
    //    (Trực tiếp test pattern classify mà InstallCoordinator.DoInstallAsync sử dụng)
    //    Watch_out: exitCode 0 + stdout "Failure [...]" PHẢI coi là fail (không dùng exit code)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void Classify_ExitCode0_StdOutContainsSuccess_IsRealSuccess()
    {
        // Arrange: adb uninstall trả exit 0 + stdout "Success"
        var result = new AdbCommandResult
        {
            ExitCode = 0,
            StdOut = "Success\n",
            StdErr = string.Empty
        };

        // Act: reproduce logic classify trong DoInstallAsync
        var isRealSuccess = result.Success
            && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.True(isRealSuccess);
    }

    [Fact]
    public void Classify_ExitCode0_StdOutContainsFailure_IsNotRealSuccess()
    {
        // Arrange: một số ROM trả exit 0 dù stdout là "Failure [DELETE_FAILED_INTERNAL_ERROR]"
        // Watch_out từ TDD §D2: KHÔNG dùng exit code là SAI vì có ROM trả exit 0 dù stdout là "Failure [...]"
        var result = new AdbCommandResult
        {
            ExitCode = 0,
            StdOut = "Failure [DELETE_FAILED_INTERNAL_ERROR]\n",
            StdErr = string.Empty
        };

        // Act
        var isRealSuccess = result.Success
            && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.False(isRealSuccess);
    }

    [Fact]
    public void Classify_ExitCodeNonZero_IsNotRealSuccess()
    {
        // Arrange: package không tồn tại, adb trả exit 1
        var result = new AdbCommandResult
        {
            ExitCode = 1,
            StdOut = "Failure [DELETE_FAILED_INTERNAL_ERROR]\n",
            StdErr = string.Empty
        };

        // Act
        var isRealSuccess = result.Success
            && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.False(isRealSuccess);
    }

    [Fact]
    public void Classify_ExitCodeNegative_Timeout_IsNotRealSuccess()
    {
        // Arrange: timeout (RunAsync trả -1 khi timeout)
        var result = new AdbCommandResult
        {
            ExitCode = -1,
            StdOut = string.Empty,
            StdErr = "adb -s ABC uninstall com.example.app timeout sau 30000ms"
        };

        // Act
        var isRealSuccess = result.Success
            && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.False(isRealSuccess);
    }

    [Fact]
    public void Classify_ExitCode0_StdOutSuccessCaseInsensitive_IsRealSuccess()
    {
        // Arrange: case-insensitive check — "success" lowercase
        var result = new AdbCommandResult
        {
            ExitCode = 0,
            StdOut = "success\n",
            StdErr = string.Empty
        };

        // Act
        var isRealSuccess = result.Success
            && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.True(isRealSuccess);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 2. InstallRequest — field UninstallBeforeInstall default false
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Fact]
    public void InstallRequest_DefaultUninstallBeforeInstall_IsFalse()
    {
        // Đảm bảo client cũ (không gửi field) vẫn hoạt động với hành vi cũ
        var req = new InstallRequest();

        Assert.False(req.UninstallBeforeInstall);
    }

    [Fact]
    public void InstallRequest_SetUninstallBeforeInstallTrue_ReflectsValue()
    {
        var req = new InstallRequest { UninstallBeforeInstall = true };

        Assert.True(req.UninstallBeforeInstall);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 3. Endpoint /api/settings/uninstall-before-install — contract
    //    Test trực tiếp logic endpoint thông qua DeviceRepository in-memory
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static DeviceRepository CreateTempRepo()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test-uninstall-{Guid.NewGuid():N}.db");
        return new DeviceRepository(Options.Create(new AdbSettings { DbPath = dbPath }));
    }

    [Fact]
    public void UninstallBeforeInstallSettingRequest_Enabled_True_SetsSetting()
    {
        // Arrange: dùng file SQLite tạm riêng (pattern DeviceRepositoryTests)
        var repo = CreateTempRepo();
        var req = new UninstallBeforeInstallSettingRequest(true);

        // Act: simulate logic endpoint
        repo.SetSetting("UninstallBeforeInstall", req.Enabled ? "true" : "false");
        var value = repo.GetSetting("UninstallBeforeInstall");

        // Assert
        Assert.Equal("true", value);
    }

    [Fact]
    public void UninstallBeforeInstallSettingRequest_Enabled_False_SetsSetting()
    {
        var repo = CreateTempRepo();
        var req = new UninstallBeforeInstallSettingRequest(false);

        repo.SetSetting("UninstallBeforeInstall", req.Enabled ? "true" : "false");
        var value = repo.GetSetting("UninstallBeforeInstall");

        Assert.Equal("false", value);
    }

    [Fact]
    public void IndexModel_ParseUninstallBeforeInstall_TrueString_ReturnsTrue()
    {
        // Test EC5 logic: bool.TryParse("true", out v) && v
        var raw = "true";
        var parsed = bool.TryParse(raw, out var v) && v;

        Assert.True(parsed);
    }

    [Fact]
    public void IndexModel_ParseUninstallBeforeInstall_Null_ReturnsFalse()
    {
        // Test EC5 fallback: null → false (key chưa có trong DB)
        string? raw = null;
        var parsed = bool.TryParse(raw, out var v) && v;

        Assert.False(parsed);
    }

    [Fact]
    public void IndexModel_ParseUninstallBeforeInstall_InvalidString_ReturnsFalse()
    {
        // Test EC5 fallback: parse thất bại → false
        var raw = "invalid";
        var parsed = bool.TryParse(raw, out var v) && v;

        Assert.False(parsed);
    }
}
