using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;
using KztekAdbPublishTool.Web.Workers;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<AdbSettings>(
    builder.Configuration.GetSection(AdbSettings.SectionName));
builder.Services.Configure<LaunchAppSettings>(
    builder.Configuration.GetSection(LaunchAppSettings.SectionName));

// ── Kestrel: giới hạn upload 500 MB ─────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000;
});

// ── Form options: nâng giới hạn multipart body lên 500 MB (mặc định 128 MB) ──
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500_000_000;
});

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AdbService>();
builder.Services.AddSingleton<DeviceRepository>();
builder.Services.AddSingleton<ApkManifestReader>();
builder.Services.AddSingleton<PollControlService>(); // [STEP-2.5] điều khiển poll + trigger thủ công

// ── Phase 2 Backend — State + Coordinators (STEP-2.1–2.4) ────────────────
builder.Services.AddSingleton<DeviceState>();
// FIX-3.2 (Code Migrator): PollControlService đảm nhiệm điều khiển poll; PollingState dead code đã xóa hoàn toàn.
builder.Services.AddSingleton<InstallCoordinator>();
builder.Services.AddSingleton<ScanCoordinator>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Background Workers ────────────────────────────────────────────────────────
builder.Services.AddHostedService<DevicePollWorker>();

// ── Razor Pages ───────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

// ── Hubs ──────────────────────────────────────────────────────────────────────
app.MapHub<DeviceHub>("/hubs/device");

// ── Endpoints [STEP-2.5] ──────────────────────────────────────────────────────
app.MapDeviceEndpoints();
app.MapHealthEndpoints();

// ── Endpoints [STEP-2.1–2.4] ─────────────────────────────────────────────────
app.MapInstallEndpoints();
app.MapScanEndpoints();
app.MapApkEndpoints();

// ── Endpoints [STEP-3.1 adb-launch-app-api] ──────────────────────────────────
app.MapLaunchAppEndpoints();

// ── Endpoints [adb-add-device-api] ────────────────────────────────────────────
app.MapDeviceConnectionEndpoints();

// ── Pages ─────────────────────────────────────────────────────────────────────
app.MapRazorPages();

app.Run();
