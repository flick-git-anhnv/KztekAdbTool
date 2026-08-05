---
step: 2.5
title: Endpoints device management + settings
assignee: junior-developer
status: done
completed_at: 2026-08-05 16:05
deps: [1.2]
---

## Nhiệm vụ
`Endpoints/DeviceEndpoints.cs` — minimal API:
- `GET /api/devices` — snapshot `DeviceState.Devices` (fallback khi SignalR mất).
- `POST /api/devices/connect` `{ipPort}` — tự append `:5555` nếu thiếu → `AdbService.ConnectAsync` → trigger poll.
- `POST /api/devices/connect-batch` `{ipPorts[]}`.
- `POST /api/devices/remove` `{serials[]}`.
- `POST /api/devices/poll` — trigger poll thủ công.
- `POST /api/settings/package` `{packageName}`.
- `POST /api/polling/toggle` `{enabled}`.
- `GET /health` — trả `{ ok: true, adbVersion }` (chạy `adb version`).

## Definition of Done
- [ ] Payload chuẩn `{ ok, data?, error? }`; validate rỗng → 400 với message tiếng Việt gốc.
- [ ] `/health` trả 200 khi adb chạy được.

## Artifact
- `Endpoints/DeviceEndpoints.cs`, `Endpoints/HealthEndpoints.cs`

## Đã làm
- Tạo `Services/PollControlService.cs`: singleton Channel<bool> bounded capacity=1, expose TriggerAsync() + SetPollingEnabled() + TryConsumeTrigger() để DevicePollWorker inject.
- Tạo `Endpoints/DeviceEndpoints.cs`: 7 endpoints minimal API (GET /api/devices, POST connect/connect-batch/remove/poll, POST /api/settings/package, POST /api/polling/toggle). Tất cả response `{ ok, data?, error? }`, validate rỗng → 400 + message tiếng Việt.
- Tạo `Endpoints/HealthEndpoints.cs`: GET /health → { ok, adbVersion } chạy `adb version` timeout 5s.
- Sửa `Program.cs`: thêm `using KztekAdbPublishTool.Web.Endpoints`, đăng ký `PollControlService`, gọi `MapDeviceEndpoints()` + `MapHealthEndpoints()`.
- Build: 0 Warning, 0 Error. Commit: 85ad97e.

## Handoff Payload
- do_not_redo: Không đăng ký lại PollControlService trong Program.cs — đã có dòng AddSingleton<PollControlService>(). Không tạo lại ConnectRequest/ConnectBatchRequest/RemoveRequest/PackageSettingRequest/PollingToggleRequest — đã khai báo cuối DeviceEndpoints.cs.
- watch_out: DevicePollWorker hiện CHƯA inject PollControlService — senior dev cần tự inject để dùng TriggerAsync()/PollingEnabled khi viết logic poll thực tế (Phase 2 comment đã có trong worker). PollControlService.TriggerAsync() dùng ValueTask — nếu gọi từ sync context cần `.AsTask()`.
- next_inputs: `Services/PollControlService.cs` (interface TriggerAsync/SetPollingEnabled/TryConsumeTrigger) cho DevicePollWorker đọc để tích hợp. `DeviceRepository.GetAll()` trả `List<DeviceRecord>` — dùng cho GET /api/devices.
