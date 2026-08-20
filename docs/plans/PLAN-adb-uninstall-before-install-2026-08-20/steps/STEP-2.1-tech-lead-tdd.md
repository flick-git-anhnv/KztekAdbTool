---
step: 2.1
plan: ../PLAN-MASTER.md
agent: tech-lead
status: todo
completed_at:
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
- [ ] `docs/tech-design/TDD-adb-uninstall-before-install.md` đã tạo với các mục: API contract (`UninstallApkAsync` signature, error handling), thay đổi `InstallRequest`, thay đổi `DoInstallAsync` (pseudocode/flow), thay đổi UI (vị trí checkbox, label, id), thay đổi JS, quyết định DB Settings
- [ ] Các câu hỏi mở (điểm quyết định trong PLAN-MASTER "Quyết định / Ghi chú tổng") đã được chốt hết
- [ ] `docs/tech-design/TDD-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
