---
step: 1.1
plan: ../PLAN-MASTER.md
agent: Product Manager
status: todo
completed_at: ~
deps: []
---

# STEP 1.1 — Viết PRD phạm vi hẹp: 2 API + 2 nút bấm UI mới

## Input nhận
Từ PLAN-MASTER.md — mô tả 2 tính năng cần bổ sung vào `KztekAdbPublishTool.Web`:
1. **API Kiểm tra trạng thái app** (`GET /api/devices/{serial}/app-status?package={packageName}`): kiểm tra app có đang chạy, và đang Foreground/Background/NotRunning.
2. **API Reboot thiết bị** (`POST /api/devices/{serial}/reboot`): ra lệnh khởi động lại thiết bị qua ADB.
3. **UI**: 2 nút bấm mới trong action panel trên dashboard (`Pages/Index.cshtml` + `wwwroot/js/dashboard.js`).

Tham khảo PRD mẫu: `docs/prd/PRD-adb-add-device-api.md` để giữ format nhất quán.

## Nhiệm vụ
Viết PRD (`docs/prd/PRD-adb-app-status-reboot-api.md`) phạm vi hẹp: mục tiêu, đối tượng dùng (client HTTP nội bộ + operator thao tác qua dashboard), Acceptance Criteria tổng quan cho cả 2 API + 2 nút UI, Non-goals (không thay đổi auth mechanism, không quản lý app lifecycle phức tạp, không có notification khi reboot xong), Metric đo lường. Xuất DOCX sau khi viết.

## Definition of Done
- [ ] `docs/prd/PRD-adb-app-status-reboot-api.md` có đủ mục: Goals, AC tổng quan (2 API + 2 nút UI), Non-goals, Metric đo lường
- [ ] Xuất DOCX bằng `python3 scripts/md_to_docx_kztek.py docs/prd/PRD-adb-app-status-reboot-api.md` (PDF tùy điều kiện môi trường)
- [ ] Commit + push lên `origin/docker-deploy`
- [ ] Cập nhật STEP file này (Đã làm + Artifact + Handoff Payload + status: done + completed_at)
- [ ] Cập nhật đúng 1 dòng Bước 1.1 trong PLAN-MASTER.md → ✅

## Đã làm
(để trống — điền sau khi bước hoàn thành)

## Artifact
(để trống — điền sau khi bước hoàn thành)

## Quyết định quan trọng
(để trống — điền sau khi bước hoàn thành)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
