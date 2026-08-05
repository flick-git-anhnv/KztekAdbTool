---
step: 2.1
title: DevicePollWorker logic đầy đủ + push SignalR
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:20
deps: [1.3]
---

## Nhiệm vụ
Chuyển logic `MainForm.PollDevicesAsync` sang `DevicePollWorker.ExecuteAsync`:
- Lặp mỗi `PollIntervalMs` (default 3000), kiểm `PollingState.IsEnabled` mỗi vòng.
- `AdbService.GetDevicesAsync` → so sánh `DeviceState.Devices` (singleton `ConcurrentDictionary`) → Upsert DB → set Online/Offline theo `liveSerials`.
- Với Online + có `PackageName` (đọc DB Settings): `GetPackageVersionAsync` → update `InstalledVersion`.
- Cuối vòng: `hub.Clients.All.SendAsync("DevicesUpdated", devices)`; log SignalR event `Log` khi phát hiện device mới.

## Definition of Done
- [x] Behavior parity với gốc: Offline khi device biến mất, phát hiện device mới có log.
- [x] Toggle qua `PollControlService.PollingEnabled` bật/tắt worker (không kill, chỉ skip).
- [x] Tích hợp trigger thủ công qua `PollControlService.WaitTriggerAsync` (race với Task.Delay).

## Đã làm
- Tạo `State/DeviceState.cs`: ConcurrentDictionary<serial, DeviceRecord>, GetAll/GetSerials/AddOrUpdate/TryGet. AddOrUpdate luôn thay object (không mutate in-place) để thread-safe.
- Tạo `State/PollingState.cs`: volatile bool IsEnabled — giữ cho DI nhưng worker thực tế dùng `PollControlService.PollingEnabled` (xem watch_out).
- Viết lại `Workers/DevicePollWorker.cs`: inject PollControlService (thay PollingState), PollAsync 4 bước (Upsert live → mark Offline → RefreshVersion → push SignalR). WaitIntervalOrTriggerAsync racing Task.Delay vs WaitTriggerAsync.
- Đăng ký DI trong `Program.cs`: DeviceState, PollingState, InstallCoordinator, ScanCoordinator (singleton).

## Artifact
- `State/DeviceState.cs`
- `State/PollingState.cs`
- `Workers/DevicePollWorker.cs` (rewrite)
- `Program.cs` (thêm usings + service registrations)

## Handoff Payload — bước sau đọc phần này

- Đã làm: DevicePollWorker đầy đủ, tích hợp PollControlService. DeviceState singleton là nguồn truth in-memory.
- do_not_redo: KHÔNG đăng ký PollingState làm toggle mechanism — Junior STEP-2.5 đã dùng PollControlService.SetPollingEnabled(); DevicePollWorker check PollControlService.PollingEnabled (không phải PollingState.IsEnabled). PollingState vẫn còn trong DI nhưng không được dùng bởi worker.
- watch_out: **Split-brain gotcha** — plan ban đầu dùng `PollingState` để toggle nhưng Junior STEP-2.5 đã implement `PollControlService.SetPollingEnabled()` (song song). Worker đã được cập nhật để dùng `PollControlService` → đúng. Phase 3 integration: /api/polling/toggle gọi PollControlService, KHÔNG gọi PollingState — consistent.
- next_inputs: `DeviceState` (singleton) và `PollControlService` (singleton) cần được inject vào bất kỳ service nào cần đọc danh sách device thực-time. `DeviceState.GetAll()` trả IReadOnlyList<DeviceRecord> thread-safe.
