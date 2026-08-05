using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Endpoints;
using KztekAdbPublishTool.Web.Hubs;
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.Workers;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<AdbSettings>(
    builder.Configuration.GetSection(AdbSettings.SectionName));

// ── Kestrel: giới hạn upload 500 MB ─────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000;
});

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AdbService>();
builder.Services.AddSingleton<DeviceRepository>();
builder.Services.AddSingleton<ApkManifestReader>();
builder.Services.AddSingleton<PollControlService>(); // [STEP-2.5] điều khiển poll + trigger thủ công

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

// ── Pages ─────────────────────────────────────────────────────────────────────
app.MapRazorPages();

app.Run();
