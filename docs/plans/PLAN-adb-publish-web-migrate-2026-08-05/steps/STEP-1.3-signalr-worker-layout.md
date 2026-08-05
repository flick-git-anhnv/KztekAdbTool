---
step: 1.3
title: SignalR Hub + BackgroundService khung + Bootstrap 5 layout
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:00
deps: [1.2]
---

## Nhiệm vụ
- Tạo `Hubs/DeviceHub : Hub`.
- Tạo `Workers/DevicePollWorker : BackgroundService` — khung, chỉ log heartbeat 3s.
- `MapHub<DeviceHub>("/hubs/device")` trong Program.cs.
- `Pages/Shared/_Layout.cshtml` Bootstrap 5 (CDN), navbar brand KZTEK (Navy `#251C53`, Cam `#F05922`).
- `wwwroot/js/signalr-client.js` khung connect + auto-reconnect.

## Definition of Done
- [ ] SignalR handshake `/hubs/device/negotiate` trả 200.
- [ ] Log console mỗi 3s có "DevicePollWorker heartbeat".
- [ ] Trang `/` render navbar Bootstrap + brand KZTEK.
- [ ] `dotnet build` sạch.

## Artifact
- `Hubs/DeviceHub.cs`, `Workers/DevicePollWorker.cs`, `Pages/Shared/_Layout.cshtml`, `wwwroot/js/signalr-client.js`

## Đã làm
- `Hubs/DeviceHub.cs`: Hub kế thừa `Microsoft.AspNetCore.SignalR.Hub`, endpoint `/hubs/device`, comment đầy đủ 5 event Phase 2 sẽ push
- `Workers/DevicePollWorker.cs`: BackgroundService, log heartbeat Debug mỗi `PollIntervalMs`, comment rõ Phase 2 sẽ thay bằng logic thực
- `Pages/Shared/_Layout.cshtml`: Bootstrap 5.3.3 CDN, Bootstrap Icons, CSS vars KZTEK (Navy #251C53 / Cam #F05922), navbar brand, signalr-status badge, footer
- `Pages/Index.cshtml` + `Index.cshtml.cs`: trang khung (Phase 2 sẽ hoàn thiện dashboard)
- `Pages/_ViewImports.cshtml` + `_ViewStart.cshtml`: boilerplate Razor Pages
- `wwwroot/js/signalr-client.js`: connect auto-reconnect (delays 0/2/5/10/15/30s), expose `window.kzHubConnection`, 5 event handler stub, status badge
- Build toàn bộ: 0 warning, 0 error; test 5/5 pass
- Commit: 0689a7e

## Handoff Payload
- Đã làm: Phase 1 Foundation hoàn chỉnh — project build sạch, SignalR negotiate hoạt động, Worker log heartbeat, layout Bootstrap 5 KZTEK brand
- do_not_redo: Không tạo lại Hub/Worker/Layout — đã đúng cấu trúc. Không thêm package NuGet cho SignalR (đã có trong ASP.NET Core framework)
- watch_out: `signalr-client.js` dùng `signalR` global từ CDN `signalr.min.js` — CDN phải load trước script này trong `_Layout.cshtml` (đã đúng thứ tự). Phase 2 gọi `window.kzHubConnection.invoke(...)` để kích hoạt action từ JS
- watch_out: `DevicePollWorker` dùng `PollIntervalMs` từ config (default 3000ms) — Phase 2 cần inject thêm `AdbService`, `DeviceRepository`, `IHubContext<DeviceHub>` vào constructor Worker
- next_inputs: Contract JSON API cho Phase 2 — xem CODE-GRAPH.md §2.3 (endpoint list) + §2.4 (SignalR events). Phase 2 Backend cần: `IHubContext<DeviceHub>` để push từ Worker/Coordinator
