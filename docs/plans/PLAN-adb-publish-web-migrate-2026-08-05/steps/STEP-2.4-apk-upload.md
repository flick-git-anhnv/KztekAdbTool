---
step: 2.4
title: APK upload + auto-detect package
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:20
deps: [1.2]
---

## Nhiệm vụ
- `POST /api/apk/upload` (multipart, field `apk`): lưu `UploadsPath/<yyyyMMddHHmmss>-<orig>.apk`, giới hạn `MaxUploadBytes` (500MB), validate `.apk`.
- Gọi `ApkManifestReader.TryGetPackageName(path)`; nếu có → `_repo.SetSetting("PackageName", pkg)` + `_repo.SetSetting("ApkPath", path)`.
- Trả `{ path, packageName }` (packageName null nếu không đọc được).

## Definition of Done
- [x] Upload 200MB thành công (Kestrel MaxRequestBodySize = 500MB đã set ở Phase 1).
- [x] File không phải .apk → 400.
- [x] APK không có manifest → `packageName: null` không lỗi.
- [x] DELETE /api/apk xóa file + clear setting.

## Đã làm
- `Endpoints/ApkEndpoints.cs`: POST /api/apk/upload (multipart "apk"), DELETE /api/apk.
- Tên file: `<yyyyMMddHHmmss>-<sanitized>.apk` (chỉ giữ ký tự alphanumeric/_/-).
- Luôn set "ApkPath" kể cả khi không đọc được package; chỉ set "PackageName" khi TryGetPackageName thành công.
- `.DisableAntiforgery()` trên MapPost để JS fetch không bị 400 CSRF.
- Gotcha: `ILogger<ApkEndpoints>` compile error vì static class → fix bằng nested non-static marker class `Log`.

## Artifact
- `Endpoints/ApkEndpoints.cs`

## Handoff Payload — bước sau đọc phần này

- Đã làm: POST /api/apk/upload trả `{path: string, packageName: string|null}`. DELETE /api/apk trả `{message, path}`.
- do_not_redo: KHÔNG gọi `.DisableAntiforgery()` thủ công nữa — đã có. KHÔNG dùng `ILogger<ApkEndpoints>` (compile error do static class) — dùng nested `private sealed class Log {}` rồi `ILogger<Log>`.
- watch_out: **Gotcha static class + ILogger<T>**: `ILogger<StaticClass>` gây CS0718 compile error. Fix: nested non-static marker class `private sealed class Log {}`. Frontend cần set `Content-Type: multipart/form-data` và field name đúng là `"apk"` (không phải "file").
- next_inputs: Frontend (STEP-2.6/2.8) POST multipart form với field `apk`. Response `{path, packageName}` → frontend hiển thị tên APK và package đã detect. InstallCoordinator đọc "ApkPath" và "PackageName" từ DB Settings khi InstallRequest không cung cấp.
