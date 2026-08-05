---
step: 2.5
title: Endpoints device management + settings
assignee: junior-developer
status: todo
completed_at: —
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

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
