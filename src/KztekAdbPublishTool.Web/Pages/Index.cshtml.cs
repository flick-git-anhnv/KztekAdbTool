using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Pages;

public class IndexModel : PageModel
{
    private readonly DeviceRepository _repo;
    private readonly ILogger<IndexModel> _logger;
    private readonly LaunchAppSettings _launchAppSettings;

    public List<DeviceRecord> Devices { get; private set; } = new();
    public string PackageName { get; private set; } = string.Empty;
    public string ApkPath { get; private set; } = string.Empty;
    public bool UninstallBeforeInstall { get; private set; }

    /// <summary>
    /// API key dùng cho các API được bảo vệ (AppStatus, Reboot). Embed vào Razor
    /// dưới dạng window.KZ_API_KEY — chấp nhận trong scope internal LAN (TDD Q9).
    /// </summary>
    public string PublicApiKey { get; private set; } = string.Empty;

    public IndexModel(DeviceRepository repo, ILogger<IndexModel> logger, IOptions<LaunchAppSettings> launchAppOptions)
    {
        _repo               = repo;
        _logger             = logger;
        _launchAppSettings  = launchAppOptions.Value;
    }

    public void OnGet()
    {
        Devices = _repo.GetAll();
        PackageName = _repo.GetSetting("PackageName") ?? string.Empty;
        ApkPath = _repo.GetSetting("ApkPath") ?? string.Empty;
        // EC5: fallback false nếu key chưa có / parse thất bại
        UninstallBeforeInstall = bool.TryParse(_repo.GetSetting("UninstallBeforeInstall"), out var v) && v;
        PublicApiKey = _launchAppSettings.ApiKey;
    }
}
