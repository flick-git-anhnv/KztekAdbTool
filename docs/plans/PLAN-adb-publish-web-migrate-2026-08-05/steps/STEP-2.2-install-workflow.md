---
step: 2.2
title: Install workflow (parallel + queue per-device + SignalR progress)
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:20
deps: [1.3]
---

## Nhiệm vụ
`POST /api/install` `{ serials?, selectedOnly, packageName, apkPath }`:
- `InstallCoordinator` singleton: `ConcurrentDictionary<serial, SemaphoreSlim(1)>` chặn 2 request cùng cài 1 serial + `SemaphoreSlim(4)` global (giữ concurrency 4 như gốc).
- Với mỗi Online device: `ListThirdPartyPackagesAsync` (before) → `InstallApkAsync` → after → so sánh → `installedPackage` → `GetPackageVersionAsync` → `LaunchAppAsync` (giữ nguyên logic ignore monkey exit code, chỉ log "No activities found").
- SignalR: `InstallProgress {serial,current,total,message}`, `DeviceInstalled {serial,status,version,time}`.
- Cập nhật `DeviceState` + `DeviceRepository.UpdateInstallResult`.

## Definition of Done
- [x] Behavior parity: đúng trước/install/sau/launch, status text `Thành công` / `Thất bại: <stderr>` / `Lỗi: <ex>`.
- [x] 2 tab cùng cài 1 serial → tab 2 chờ tab 1 (per-device SemaphoreSlim(1)).
- [x] Không có device Online → HTTP 400 "Không có thiết bị Online phù hợp".

## Đã làm
- `Services/InstallCoordinator.cs`: QueueInstalls (fire-and-forget), InstallOneAsync (per-device sem → global sem → DoInstallAsync), DoInstallAsync (5 bước + timeout 10 phút + FinishWithStatus).
- `Endpoints/InstallEndpoints.cs`: POST /api/install → validate → filter Online → 202 Accepted. InstallRequest POCO (Serials, SelectedOnly, PackageName, ApkPath).
- SignalR method names: "InstallProgress" `{serial, current, total, message}`, "DeviceInstalled" `{serial, status, version, time}`.
- PackageName/ApkPath fallback: ưu tiên request body → fallback DB Settings ("PackageName", "ApkPath").

## Artifact
- `Endpoints/InstallEndpoints.cs`
- `Services/InstallCoordinator.cs`

## Handoff Payload — bước sau đọc phần này

- Đã làm: POST /api/install trả 202 ngay, InstallCoordinator fire-and-forget từng serial.
- do_not_redo: KHÔNG tạo thêm SemaphoreSlim hay ConcurrentDictionary trong endpoint handler — đã có đủ trong InstallCoordinator singleton.
- watch_out: SignalR push dùng `CancellationToken.None` (không dùng timeout CTS) — tránh mất event khi install timeout. "LaunchApp failed" chỉ log warning, KHÔNG fail install — behavior parity với WinForms gốc.
- next_inputs: Frontend (STEP-2.6/2.8) cần lắng nghe SignalR events "InstallProgress" và "DeviceInstalled" với đúng payload shape: `{serial, current, total, message}` và `{serial, status, version, time}`. POST /api/install body: `{serials: string[]|null, selectedOnly: bool, packageName?: string, apkPath?: string}`.
