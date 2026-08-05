---
step: 2.4
title: APK upload + auto-detect package
assignee: senior-developer
status: todo
completed_at: —
deps: [1.2]
---

## Nhiệm vụ
- `POST /api/apk/upload` (multipart, field `apk`): lưu `UploadsPath/<yyyyMMddHHmmss>-<orig>.apk`, giới hạn `MaxUploadBytes` (500MB), validate `.apk`.
- Gọi `ApkManifestReader.TryGetPackageName(path)`; nếu có → `_repo.SetSetting("PackageName", pkg)` + `_repo.SetSetting("ApkPath", path)`.
- Trả `{ path, packageName }` (packageName null nếu không đọc được).

## Definition of Done
- [ ] Upload 200MB thành công.
- [ ] File không phải .apk → 400.
- [ ] APK không có manifest → `packageName: null` không lỗi.

## Artifact
- `Endpoints/ApkEndpoints.cs`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
