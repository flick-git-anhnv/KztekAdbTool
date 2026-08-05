---
step: 2.1
title: DevicePollWorker logic đầy đủ + push SignalR
assignee: senior-developer
status: todo
completed_at: —
deps: [1.3]
---

## Nhiệm vụ
Chuyển logic `MainForm.PollDevicesAsync` sang `DevicePollWorker.ExecuteAsync`:
- Lặp mỗi `PollIntervalMs` (default 3000), kiểm `PollingState.IsEnabled` mỗi vòng.
- `AdbService.GetDevicesAsync` → so sánh `DeviceState.Devices` (singleton `ConcurrentDictionary`) → Upsert DB → set Online/Offline theo `liveSerials`.
- Với Online + có `PackageName` (đọc DB Settings): `GetPackageVersionAsync` → update `InstalledVersion`.
- Cuối vòng: `hub.Clients.All.SendAsync("DevicesUpdated", devices)`; log SignalR event `Log` khi phát hiện device mới.

## Definition of Done
- [ ] Behavior parity với gốc: Offline khi device biến mất, phát hiện device mới có log.
- [ ] Toggle qua `PollingState.IsEnabled` bật/tắt worker (không kill, chỉ skip).
- [ ] Test 1 emulator + 1 device WiFi → grid update trong 3s.

## Artifact
- `Workers/DevicePollWorker.cs`, `State/DeviceState.cs`, `State/PollingState.cs`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
