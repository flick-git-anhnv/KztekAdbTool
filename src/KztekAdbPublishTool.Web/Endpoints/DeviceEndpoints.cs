using KztekAdbPublishTool.Web.Services;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// Minimal API — quản lý thiết bị ADB.
/// Tất cả response theo chuẩn { ok, data?, error? }.
/// </summary>
public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        // ── GET /api/devices ─────────────────────────────────────────────────────
        // Fallback khi SignalR mất: trả snapshot danh sách thiết bị từ DB.
        app.MapGet("/api/devices", (DeviceRepository repo) =>
        {
            var devices = repo.GetAll();
            return Results.Ok(new { ok = true, data = devices });
        });

        // ── POST /api/devices/connect ────────────────────────────────────────────
        app.MapPost("/api/devices/connect", async (
            ConnectRequest req,
            AdbService adb,
            PollControlService poll,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.IpPort))
                return Results.BadRequest(new { ok = false, error = "ipPort không được để trống" });

            var target = EnsurePort(req.IpPort.Trim());
            var result = await adb.ConnectAsync(target, ct: ct);

            // Kích hoạt poll ngay để DB / SignalR phản ánh kết quả sớm
            await poll.TriggerAsync(ct);

            return result.Success
                ? Results.Ok(new { ok = true, data = result.StdOut.Trim() })
                : Results.Ok(new { ok = false, error = result.StdErr.Trim() });
        });

        // ── POST /api/devices/connect-batch ──────────────────────────────────────
        app.MapPost("/api/devices/connect-batch", async (
            ConnectBatchRequest req,
            AdbService adb,
            PollControlService poll,
            CancellationToken ct) =>
        {
            if (req.IpPorts == null || req.IpPorts.Length == 0)
                return Results.BadRequest(new { ok = false, error = "ipPorts không được để trống" });

            var results = new List<object>();
            foreach (var ip in req.IpPorts)
            {
                if (string.IsNullOrWhiteSpace(ip)) continue;
                var target = EnsurePort(ip.Trim());
                var r = await adb.ConnectAsync(target, ct: ct);
                results.Add(new
                {
                    ipPort  = target,
                    ok      = r.Success,
                    output  = r.StdOut.Trim(),
                    error   = r.StdErr.Trim()
                });
            }

            await poll.TriggerAsync(ct);
            return Results.Ok(new { ok = true, data = results });
        });

        // ── POST /api/devices/remove ─────────────────────────────────────────────
        app.MapPost("/api/devices/remove", (RemoveRequest req, DeviceRepository repo) =>
        {
            if (req.Serials == null || req.Serials.Length == 0)
                return Results.BadRequest(new { ok = false, error = "serials không được để trống" });

            foreach (var serial in req.Serials)
                if (!string.IsNullOrWhiteSpace(serial))
                    repo.Remove(serial.Trim());

            return Results.Ok(new { ok = true });
        });

        // ── POST /api/devices/poll ───────────────────────────────────────────────
        // Trigger poll thủ công — hữu ích khi muốn refresh ngay mà không đợi interval.
        app.MapPost("/api/devices/poll", async (PollControlService poll, CancellationToken ct) =>
        {
            await poll.TriggerAsync(ct);
            return Results.Ok(new { ok = true, message = "Đã kích hoạt poll thủ công" });
        });

        // ── POST /api/settings/package ───────────────────────────────────────────
        app.MapPost("/api/settings/package", (PackageSettingRequest req, DeviceRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.PackageName))
                return Results.BadRequest(new { ok = false, error = "packageName không được để trống" });

            repo.SetSetting("PackageName", req.PackageName.Trim());
            return Results.Ok(new { ok = true });
        });

        // ── POST /api/polling/toggle ─────────────────────────────────────────────
        app.MapPost("/api/polling/toggle", (
            PollingToggleRequest req,
            PollControlService poll,
            DeviceRepository repo) =>
        {
            poll.SetPollingEnabled(req.Enabled);
            // Persist trạng thái để khởi động lại vẫn giữ setting
            repo.SetSetting("PollingEnabled", req.Enabled ? "true" : "false");
            return Results.Ok(new { ok = true, enabled = req.Enabled });
        });

        return app;
    }

    // ── Helper ───────────────────────────────────────────────────────────────────
    private static string EnsurePort(string ipPort)
        => ipPort.Contains(':') ? ipPort : $"{ipPort}:5555";
}

// ── Request DTOs ──────────────────────────────────────────────────────────────
public sealed record ConnectRequest(string IpPort);
public sealed record ConnectBatchRequest(string[] IpPorts);
public sealed record RemoveRequest(string[] Serials);
public sealed record PackageSettingRequest(string PackageName);
public sealed record PollingToggleRequest(bool Enabled);
