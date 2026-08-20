---
step: 2.1
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-20 14:05
deps: [1.4]
---

# STEP 2.1 — Viết TDD: Thiết kế kỹ thuật Uninstall Before Install

## Input nhận
- PRD: `docs/prd/PRD-adb-uninstall-before-install.md`
- User story/AC: `docs/user-stories/US-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-1.4 (đọc mục "Handoff Payload" trong step file đó)
- Bối cảnh kỹ thuật đã khảo sát (không cần khảo sát lại):
  - `InstallEndpoints.cs`: `InstallRequest` có `Serials`, `SelectedOnly`, `PackageName`, `ApkPath` — cần thêm `UninstallBeforeInstall` (bool, default false)
  - `InstallCoordinator.cs`: `DoInstallAsync` — nơi chèn gọi `UninstallApkAsync` trước `ListThirdPartyPackagesAsync`/`InstallApkAsync`
  - `AdbService.cs`/`IAdbService.cs`: chưa có method uninstall — cần thêm `UninstallApkAsync(serial, packageName, ct)`
  - `Index.cshtml`: toolbar hàng 3 có `chk-auto-detect` dạng `form-check form-switch` — thêm checkbox mới cùng pattern
  - `dashboard.js`: dòng ~466-510, hai handler nút install gọi `apiPost('/api/install', { serials, selectedOnly })` — cần thêm `uninstallBeforeInstall` vào payload

## Nhiệm vụ
Viết Technical Design Document chốt các điểm thiết kế sau: (1) signature `UninstallApkAsync`, xử lý lỗi graceful khi package chưa cài, (2) vị trí checkbox trên toolbar + label text, (3) thứ tự gọi trong `DoInstallAsync` + bước SignalR progress mới "Đang gỡ cài đặt...", (4) quyết định có lưu checkbox state vào DB Settings hay không.

## Definition of Done
- [x] `docs/tech-design/TDD-adb-uninstall-before-install.md` đã tạo với các mục: API contract (`UninstallApkAsync` signature, error handling), thay đổi `InstallRequest`, thay đổi `DoInstallAsync` (pseudocode/flow), thay đổi UI (vị trí checkbox, label, id), thay đổi JS, quyết định DB Settings
- [x] Các câu hỏi mở (điểm quyết định trong PLAN-MASTER "Quyết định / Ghi chú tổng") đã được chốt hết
- [x] `docs/tech-design/TDD-adb-uninstall-before-install.docx` đã xuất thành công (PDF fail LaTeX — bình thường)
- [x] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
- Đọc PRD (`PRD-adb-uninstall-before-install.md`), US (13 scenario), RESOURCE, code hiện tại: `IAdbService.cs`, `AdbService.cs`, `InstallCoordinator.cs`, `InstallEndpoints.cs`, `DeviceEndpoints.cs`, `Index.cshtml`, `Index.cshtml.cs`, `DeviceRepository.cs`, `wwwroot/js/dashboard.js`.
- Chốt dứt điểm 3 câu hỏi mở (D1 lưu DB Settings, D2 cách phân biệt lỗi uninstall, D3 hành vi khi MDM lock) — xem mục "Quyết định quan trọng" bên dưới.
- Thiết kế đầy đủ TDD tại `docs/tech-design/TDD-adb-uninstall-before-install.md` gồm: sequence diagram mermaid, API contract (method `UninstallApkAsync`, endpoint `POST /api/settings/uninstall-before-install`, field mới `InstallRequest.UninstallBeforeInstall`, đổi signature `QueueInstalls`), pseudocode `DoInstallAsync` với bảng percent SignalR trước/sau, migration plan (không DB migration), rủi ro, task breakdown 8 sub-task (~7h), code review checklist.
- Xuất DOCX: `python3 scripts/md_to_docx_kztek.py docs/tech-design/TDD-adb-uninstall-before-install.md` → DOCX OK, PDF fail LaTeX (bình thường).
- Commit local (không push, theo tiền lệ plan này).

## Artifact
- `docs/tech-design/TDD-adb-uninstall-before-install.md` — TDD đầy đủ (11 section)
- `docs/tech-design/TDD-adb-uninstall-before-install.docx` — bản KZTEK brand, đồng bộ với `.md`

## Quyết định quan trọng
- **D1 (Q1 từ handoff — có lưu DB Settings không):** CÓ lưu. Key = `"UninstallBeforeInstall"`, value string `"true"`/`"false"`. Lưu qua `DeviceRepository.SetSetting` (pattern giống `PackageName`/`ApkPath`/`PollingEnabled`). Load ở `IndexModel.OnGet` dùng `bool.TryParse` fallback `false` (EC5). Save qua endpoint mới `POST /api/settings/uninstall-before-install` gọi ngay ở event `change` của checkbox (không đợi Install click). Nhất quán với AC5 trong PRD; không cần migration DDL (bảng `Settings` đã tồn tại).
- **D2 (Q2 từ handoff — phân biệt lỗi uninstall):** **LUÔN graceful — mọi lỗi uninstall đều log WARNING và tiếp tục install, không phân biệt case để quyết abort.** Chỉ dùng detection `AdbCommandResult.Success && StdOut.Contains("Success", OrdinalIgnoreCase)` để chọn 1 trong 2 message SignalR: "Đã gỡ cài đặt <pkg>" (real success) hoặc "Bỏ qua gỡ (chưa cài hoặc bị chặn)" (mọi trường hợp khác). Log warning kèm nguyên `StdOut`/`StdErr` để DevOps điều tra sau. Lý do: uninstall là bước tùy chọn tiền xử lý (BR3, US-003), không được là single point of failure; install `-r` sau đó sẽ tự fail với message rõ hơn nếu có vấn đề thực sự.
- **D3 (EC1 từ handoff — MDM/device admin lock):** Cùng ứng xử D2 — graceful-skip, log WARNING, tiếp tục install. Không rẽ nhánh riêng cho MDM (nếu MDM cấm uninstall thì rất có thể cũng cấm install → install fail sẽ tự báo rõ, không cần double-handle).
- **Bonus (từ Q1 PRD — vị trí checkbox):** Đặt ở **hàng 3 toolbar**, **ngay sau** `chk-auto-detect` (cùng nhóm option cấu hình luồng install), pattern `form-check form-switch`, id `chk-uninstall-before-install`, label "Gỡ cài đặt app trước khi cài".
- **Bonus (SignalR progress):** Khi flag TẮT giữ nguyên hoàn toàn (0/20/40/60/80/100 — không đổi hành vi). Khi flag BẬT chèn 2 message mới ở đầu (0% "Đang gỡ cài đặt...", 15% "Đã gỡ cài đặt" / "Bỏ qua gỡ") + shift các step cũ lên 25/40/55/70/85/100. Progress bar luôn tăng đơn điệu.
- **Bonus (interface):** KHÔNG thêm `UninstallApkAsync` vào `IAdbService` — interface hiện tại chỉ phục vụ mock cho `DevicePollWorker` (3 method: `GetDevicesAsync`, `ConnectAsync`, `GetPackageVersionAsync`). `InstallApkAsync`/`LaunchAppAsync`/`ListThirdPartyPackagesAsync` đã inject `AdbService` cụ thể — `UninstallApkAsync` giữ đúng convention.
- **Bonus (signature `QueueInstalls`):** Thêm param `bool uninstallBeforeInstall` KHÔNG có default value — bắt caller khai báo rõ để không ai vô tình bỏ qua khi thêm callsite mới.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- **Đã làm:** TDD chốt xong 3 quyết định lớn (D1 lưu DB Settings key `UninstallBeforeInstall`, D2/D3 uninstall LUÔN graceful) + toàn bộ contract kỹ thuật (method signature, endpoint mới, field mới, pseudocode `DoInstallAsync` với bảng percent, thay đổi UI/JS). Xuất DOCX brand KZTEK. Commit local đã tạo, chưa push.
- **do_not_redo:** KHÔNG viết lại TDD; KHÔNG hỏi lại 3 câu hỏi mở đã chốt (lưu DB Settings key `UninstallBeforeInstall`, luôn graceful mọi lỗi uninstall, MDM lock cũng graceful); KHÔNG thêm `UninstallApkAsync` vào interface `IAdbService`; KHÔNG dùng cấu trúc percent progress khác bảng đã chốt trong TDD.
- **watch_out:**
  - **Percent SignalR:** khi flag TẮT phải giữ NGUYÊN 100% giá trị cũ (0/20/40/60/80/100) — bất kỳ đổi số nào cũng vi phạm BR1 (backward-compat). Chỉ khi flag BẬT mới dùng bảng shift 0/15/25/40/55/70/85/100.
  - **Detection classify uninstall:** dùng `result.Success && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase)` — vì có ROM Android trả exit code 0 kể cả khi stdout là "Failure [DELETE_FAILED_INTERNAL_ERROR]". CHỈ kiểm exit code sẽ sai — phải parse stdout.
  - **`QueueInstalls` signature đổi:** không có default cho `uninstallBeforeInstall` — tất cả callsite phải khai báo rõ. Hiện chỉ có 1 callsite trong `InstallEndpoints.cs` — grep để chắc chắn không sót.
  - **`IndexModel.OnGet` EC5 fallback:** dùng `bool.TryParse(...) && v` — không throw khi key chưa tồn tại.
  - **JS `change` handler:** đặt gần txt-package blur handler (~dòng 452 `dashboard.js`), pattern giống nhau.
  - **Endpoint mới thêm vào `DeviceEndpoints.cs`** (không phải `InstallEndpoints.cs`) — nhất quán với `POST /api/settings/package` đã có.
  - **KHÔNG dùng `-k` flag** khi chạy `adb uninstall` — mục tiêu clean install, không giữ data cũ.
  - **Package name không cần escape** dấu ngoặc kép (khác `apkPath`) — chỉ chứa `[a-zA-Z0-9._]`.
  - **Test yêu cầu 2 case classify:** real success (stdout "Success") + any fail (stdout "Failure [...]") — cả 2 cùng return `Success = true` từ mock nhưng phân biệt qua stdout.
- **next_inputs:**
  - **TDD chính:** `docs/tech-design/TDD-adb-uninstall-before-install.md` — code theo đúng pseudocode + contract, không tự chế thêm.
  - **File code cần sửa (Senior Dev @ Bước 3.1):**
    - `src/KztekAdbPublishTool.Web/Services/AdbService.cs` — thêm `UninstallApkAsync` (không đụng `IAdbService.cs`)
    - `src/KztekAdbPublishTool.Web/Services/InstallCoordinator.cs` — thêm param `uninstallBeforeInstall` vào 3 method (`QueueInstalls`/`InstallOneAsync`/`DoInstallAsync`); chèn block uninstall + shift percent theo bảng TDD
    - `src/KztekAdbPublishTool.Web/Endpoints/InstallEndpoints.cs` — thêm field `UninstallBeforeInstall` vào `InstallRequest`; truyền `req.UninstallBeforeInstall` vào `QueueInstalls`
    - `src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs` — thêm endpoint `POST /api/settings/uninstall-before-install` + DTO `UninstallBeforeInstallSettingRequest`
    - `src/KztekAdbPublishTool.Web/Pages/Index.cshtml.cs` — thêm property `UninstallBeforeInstall`, load trong `OnGet`
    - `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` — thêm div checkbox hàng 3 sau `chk-auto-detect` (mã HTML đã có sẵn trong TDD §Thay đổi UI)
    - `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` — 3 chỗ: (JS-1) change handler lưu setting, (JS-2 + JS-3) truyền flag trong 2 install button handler
  - **Test cần viết:** unit test `AdbService.UninstallApkAsync` (mock `RunAsync`); 2 branch classify trong `DoInstallAsync` (real success / any fail — cùng exit 0, khác stdout); integration test endpoint `/api/settings/uninstall-before-install`.
  - **CODE-GRAPH:** cập nhật entry `InstallCoordinator` (signature đổi), `AdbService` (thêm method), `DeviceEndpoints` (thêm endpoint) — theo §17.2. Confidence label CONFIRMED sau khi đọc trực tiếp code đã sửa.

## Commit
- Hash: [sẽ điền sau khi commit]
- Đã push: không (theo tiền lệ)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
