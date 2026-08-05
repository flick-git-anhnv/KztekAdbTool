using KztekAdbPublishTool.Web.Configuration;
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

// ── Pages ─────────────────────────────────────────────────────────────────────
app.MapRazorPages();

app.Run();
