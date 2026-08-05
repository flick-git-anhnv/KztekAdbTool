---
step: 2.2
title: Install workflow (parallel + queue per-device + SignalR progress)
assignee: senior-developer
status: todo
completed_at: —
deps: [1.3]
---

## Nhiệm vụ
`POST /api/install` `{ serials?, selectedOnly, packageName, apkPath }`:
- `InstallCoordinator` singleton: `ConcurrentDictionary<serial, SemaphoreSlim(1)>` chặn 2 request cùng cài 1 serial + `SemaphoreSlim(4)` global (giữ concurrency 4 như gốc).
- Với mỗi Online device: `ListThirdPartyPackagesAsync` (before) → `InstallApkAsync` → after → so sánh → `installedPackage` → `GetPackageVersionAsync` → `LaunchAppAsync` (giữ nguyên logic ignore monkey exit code, chỉ log "No activities found").
- SignalR: `InstallProgress {serial,current,total,message}`, `DeviceInstalled {serial,status,version,time}`.
- Cập nhật `DeviceState` + `DeviceRepository.UpdateInstallResult`.

## Definition of Done
- [ ] Behavior parity: đúng trước/install/sau/launch, status text `Thành công` / `Thất bại: <stderr>` / `Lỗi: <ex>`.
- [ ] 2 tab cùng cài 1 serial → tab 2 chờ tab 1.
- [ ] Không có device Online → HTTP 400 "Không có thiết bị Online phù hợp".

## Artifact
- `Endpoints/InstallEndpoints.cs`, `Services/InstallCoordinator.cs`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
