---
step: 1.1
plan: ../PLAN-MASTER.md
agent: Product Manager
status: done
completed_at: 2026-08-19 10:09
deps: []
---

# STEP 1.1 — Viết PRD phạm vi hẹp: 2 API mới

## Input nhận
Từ PLAN-MASTER.md — mô tả 2 API cần bổ sung vào server `KztekAdbPublishTool.Web`:
1. **API "Kết nối thiết bị theo IP"** (`POST /api/devices/connect-by-ip`): nhận `ip` (bắt buộc) + `port` (tùy chọn, mặc định `5555`), gọi `AdbService.ConnectAsync` — vừa kết nối vừa đưa thiết bị vào danh sách theo dõi. Public API, auth `x-api-key`.
2. **API "Kiểm tra trạng thái kết nối ADB"** (`GET /api/devices/{serial}/status`): nhận `serial`, trả về `Online`/`Offline`/`NotFound` dựa trên `DeviceState` (không gọi ADB trực tiếp).

Tham khảo plan mẫu đã hoàn thành: `docs/plans/PLAN-adb-launch-app-api-2026-08-18/steps/STEP-1.1-product-manager-prd.md` (feature tương tự, cùng auth mechanism) để giữ format/độ chi tiết nhất quán.

Quyết định đã chốt với user (không cần hỏi lại):
- Gộp "Thêm thiết bị" + "Kết nối theo IP" thành 1 API duy nhất.
- Port mặc định 5555 nếu không truyền.
- Check-connection theo 1 serial cụ thể (không phải check tổng quan ADB server).
- Auth tái dùng nguyên trạng `x-api-key` + `LaunchApp:ApiKey` — không tạo cơ chế mới.

## Nhiệm vụ
Viết PRD (`docs/prd/PRD-adb-add-device-api.md`) phạm vi hẹp: mục tiêu, đối tượng dùng (client HTTP nội bộ/đối tác gọi qua API key), Acceptance Criteria tổng quan cho cả 2 API, Non-goals (không có UI, không đổi cơ chế auth, không quản lý danh sách nhiều thiết bị cùng lúc — mỗi API xử lý 1 thiết bị/lần).

## Definition of Done
- [ ] `docs/prd/PRD-adb-add-device-api.md` có đủ mục: Goals, AC tổng quan (cả 2 API), Non-goals, Metric đo lường
- [ ] Xuất DOCX (+ PDF nếu môi trường hỗ trợ) theo R1 CORE.md

## Đã làm
- Đọc reference PRD `docs/prd/PRD-adb-launch-app-api.md` để căn chỉnh format và độ chi tiết.
- Viết `docs/prd/PRD-adb-add-device-api.md` với đầy đủ mục: Tổng quan, Goals, Non-goals, Scope, User Story, AC mức cao (11 AC), Metric, Rủi ro/Câu hỏi mở.
- Chạy `python3 scripts/md_to_docx_kztek.py` — DOCX thành công, PDF fail thiếu LaTeX (chấp nhận).
- Commit hash `359f537`, đã push lên `origin/docker-deploy`.

## Artifact
- `docs/prd/PRD-adb-add-device-api.md` — PRD Markdown chính
- `docs/prd/PRD-adb-add-device-api.docx` — xuất bởi md_to_docx_kztek.py (brand KZTEK)

## Quyết định quan trọng
- Gộp "add device" + "connect" thành 1 API duy nhất (`POST /api/devices/connect-by-ip`) — đã chốt từ PLAN-MASTER, không cần hỏi lại.
- Port mặc định 5555 khi client không truyền `port` — ghi rõ vào AC2.
- AC8 (serial NotFound → HTTP 404 hay HTTP 200 + `status: "NotFound"`) còn mở, giao Tech Lead quyết định ở TDD — recommendation: 404 RESTful hơn.
- Q2 (`ConnectAsync` có trả serial sau connect không?) — giao Tech Lead xác nhận ở TDD; nếu có thì nên trả về trong response để client gọi ngay API 2.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Viết PRD, export DOCX — đã xong và commit `359f537`.
- watch_out: AC8 còn câu hỏi mở (HTTP 404 vs HTTP 200 + NotFound) — BA nên ghi vào user story như 1 assumption cần Tech Lead xác nhận, KHÔNG tự chốt. Q2 về việc `ConnectAsync` có trả serial hay không — tương tự, để ngỏ cho TDD.
- next_inputs: `docs/prd/PRD-adb-add-device-api.md` — source chính để BA chi tiết hóa AC theo Given/When/Then. Đặc biệt mục "Acceptance Criteria (mức cao)" (AC1–AC11) và mục "Rủi ro/Câu hỏi mở" (Q1, Q2).

## Commit
- Hash: 359f537
- Đã push: Yes — origin/docker-deploy

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
