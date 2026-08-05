---
step: 3.2
title: Code Migrator review (Opus)
assignee: code-migrator
status: done
completed_at: 2026-08-05 17:05
deps: [3.1]
---

## Nhiệm vụ
Review `src/KztekAdbPublishTool.Web/`:
1. **Correctness:** logic Install (before/install/after/launch), Filter, Select-All, Toggle timer còn đúng semantics gốc?
2. **Behavior parity:** đối chiếu ADR-001 §4.3–§4.4 — mỗi event có handler tương đương? Message tiếng Việt giữ nguyên?
3. **Security:** APK upload validate extension + không path traversal; adb command không nhận input chưa validate (serial regex); SignalR không leak nhạy cảm.
4. **Style:** DI đúng scope, async không `.Result`/`.Wait()`, không nuốt exception.
5. Rà csproj + using → KHÔNG còn `System.Windows.Forms`, `KztekComponent`, `Guna.UI2`.

## Definition of Done
- [x] Liệt kê phát hiện + severity (Critical/Major/Minor).
- [x] Critical/Major fix trước khi sang QA.

## Artifact
- Commit 833972f trên nhánh docker-deploy (3 file fix + 1 file xóa).

## Đã làm

**Verdict tổng thể: PASS (sau fix)** — 3 vấn đề Critical đã tự sửa; không còn Major/Blocker chặn QA.

### Vấn đề tìm thấy

| # | Severity | File | Vấn đề | Trạng thái |
|---|---|---|---|---|
| C1 | **Critical** | `wwwroot/js/dashboard.js:377` | `fd.append('file', file)` — backend `ApkEndpoints.cs:42` đọc `form.Files["apk"]` → upload APK **luôn 400 "Không tìm thấy trường 'apk'"**. Break behavior parity nghiêm trọng (không cài được app nào). | ✅ Đã fix: đổi `'file'` → `'apk'` |
| C2 | **Critical** | `wwwroot/js/scan-modal.js:176` | Payload `{ipRange: range, port}` — DTO backend là `ScanStartRequest.RangeText` → JSON deserializer trả `RangeText=null` → `ScanRangeParser.TryParse` fail → **scan mạng luôn báo lỗi validation dải IP**. | ✅ Đã fix: đổi key `ipRange` → `rangeText` |
| C3 | **Minor→Critical (dọn dẹp)** | `State/PollingState.cs` | Dead code (STEP-3.1 đã bỏ DI registration nhưng file vẫn tồn tại). | ✅ Đã xóa file + cập nhật comment Program.cs |
| — | Minor (không fix — ghi nhận) | `scan-modal.js:110` `addFoundRow` | `ipPort` nhét thẳng vào innerHTML không escape. Nguồn là backend TCP probe (dạng `x.x.x.x:port`) → risk thấp, single-tenant local. Đề xuất escape ở refactor sau. | Ghi nhận, không block |
| — | Minor (không fix — ghi nhận) | `AdbService.RunAsync` | `ipPort`, `serial`, `packageName` interpolate thẳng vào Arguments string. Không phải shell injection (UseShellExecute=false), nhưng có nguy cơ argument-injection nếu input chứa space + flag. Trong bối cảnh single-tenant local + serial từ `adb devices` output (trusted), risk thấp. | Ghi nhận |
| — | Minor (không fix) | `wwwroot/js/dashboard.js` `conn.on('Log')` | Backend chưa gửi event `Log` — handler tồn tại nhưng no-op. Không lỗi, chỉ dư. | Ghi nhận |
| — | Info | Kiến trúc | SignalR primitive args đã fix đúng ở 3.1. Tất cả SQL parameterized ✅. Razor auto-escape ✅. JS `esc()` helper dùng đúng ở dashboard.js ✅. APK upload: extension check + size check + sanitize filename ✅. csproj sạch, không còn WinForms/Guna/KztekComponent ✅. | OK |

### Đánh giá theo tiêu chí

- **Correctness (logic):** OK. Install workflow (before/install/after/launch) đúng semantics WinForms; Filter/Select-All hoạt động visible-rows-only đúng bản gốc; DevicePollWorker race Task.Delay ∥ trigger channel clean; ScanCoordinator single-scan guard đúng.
- **Behavior parity:** Sau fix C1+C2 mới thực sự parity (trước fix, 2 luồng chính vỡ). Tiếng Việt UI giữ nguyên chuẩn.
- **Security:** Không có SQL injection, không XSS ở phần Razor render; APK upload đủ 3 lớp (ext + size + sanitized filename). Còn 2 điểm minor arg-injection/HTML-escape ghi nhận không block.
- **Style:** DI Singleton đúng cho stateless service; async không `.Result`/`.Wait()`; try/catch có log, không nuốt exception (có LogError/LogWarning); volatile dùng đúng cho `_cts` và `_pollingEnabled`.
- **Dependency:** csproj chỉ còn `Microsoft.Data.Sqlite 8.0.8` — không còn dấu vết WinForms/KztekComponent/Guna.

### Kết quả
- `dotnet build -c Release`: 0 lỗi, 0 warning.
- Commit: **833972f** trên branch docker-deploy.

## Handoff Payload
- Đã làm: Review xong toàn bộ Web project. Fix 3 Critical (2 contract mismatch JS↔C#, xóa dead code). Build sạch. Còn 2 minor security (arg-injection, HTML-escape ipPort) ghi nhận không block QA.
- do_not_redo: KHÔNG review lại C1/C2/C3 — đã fix và build sạch; KHÔNG chạy lại contract check 8 endpoint (đã cover trong integration + review).
- watch_out: **Bài học lớn cho QA (3.3):** STEP-3.1 test curl với payload khớp backend DTO nhưng KHÔNG test payload thực tế JS gửi → miss 2 field-name mismatch (`file`↔`apk`, `ipRange`↔`rangeText`). QA smoke test PHẢI đi qua UI browser thật (không chỉ curl) để bắt loại bug này. Luồng 7 (upload APK) và Luồng 6 (scan modal) là ưu tiên cao.
- next_inputs: Commit 833972f. 3 file thay đổi: `dashboard.js`, `scan-modal.js`, `Program.cs`; 1 file xóa: `State/PollingState.cs`. QA có thể chạy `dotnet run --project src/KztekAdbPublishTool.Web`, mở `http://localhost:5xxx`, verify 12 luồng — trọng tâm luồng 6 (quét dải mạng) và luồng 7 (upload APK) vốn trước fix là broken.
