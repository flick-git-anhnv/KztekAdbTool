using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KztekAdbPublishTool.Web.Pages;

public class IndexModel : PageModel
{
    private readonly DeviceRepository _repo;
    private readonly ILogger<IndexModel> _logger;

    public List<DeviceRecord> Devices { get; private set; } = new();
    public string PackageName { get; private set; } = string.Empty;
    public string ApkPath { get; private set; } = string.Empty;

    public IndexModel(DeviceRepository repo, ILogger<IndexModel> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public void OnGet()
    {
        Devices = _repo.GetAll();
        PackageName = _repo.GetSetting("PackageName") ?? string.Empty;
        ApkPath = _repo.GetSetting("ApkPath") ?? string.Empty;
    }
}
