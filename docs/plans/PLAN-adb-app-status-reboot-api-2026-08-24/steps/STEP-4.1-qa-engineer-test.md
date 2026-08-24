---
step: 4.1
plan: ../PLAN-MASTER.md
agent: QA Engineer
status: todo
completed_at: ~
deps: ["3.3"]
---

# STEP 4.1 — Viết test plan, thực thi test case, log kết quả

## Input nhận
Từ Bước 3.3 Handoff Payload — UX/UI Reviewer PASS (hoặc ghi nhận issues đã fix), commit hash code đã review, `docs/tech-design/TDD-adb-app-status-reboot-api.md` (AC + error matrix).

## Nhiệm vụ

### Test Plan (`docs/test-plans/TEST-PLAN-adb-app-status-reboot-api.md`):
- Scope: 2 API endpoint + 2 nút UI + AdbService methods
- Strategy: HTTP integration test (curl/Postman) + unit test đã có từ 3.1 (verify) + UI smoke test
- Test environment: local docker hoặc staging
- GIỚI HẠN ghi rõ: nếu không có thiết bị Android thật/emulator → các TC liên quan đến ADB thật cần note

### Test Cases (`docs/test-cases/TC-adb-app-status-reboot-api.md`):

**API 1 — GET /api/devices/{serial}/app-status:**
- TC-A01: Happy path — app đang chạy foreground → 200 `{ running: true, state: "Foreground" }`
- TC-A02: App đang chạy background → 200 `{ running: true, state: "Background" }`
- TC-A03: App không chạy → 200 `{ running: false, state: "NotRunning" }`
- TC-A04: Serial không tồn tại → 404
- TC-A05: Thiếu query param `package` → 400
- TC-A06: API key sai/thiếu → 401
- TC-A07: Thiết bị offline → 500 hoặc 503

**API 2 — POST /api/devices/{serial}/reboot:**
- TC-B01: Happy path — thiết bị online → 200 `{ status: "RebootInitiated" }`
- TC-B02: Serial không tồn tại → 404
- TC-B03: API key sai/thiếu → 401
- TC-B04: Thiết bị offline → 500 hoặc 503

**UI smoke test:**
- TC-C01: Click "Check App Status" → input dialog hiện → nhập package → gọi API → kết quả hiển thị
- TC-C02: Click "Reboot Device" → confirm dialog hiện → xác nhận → gọi API → thông báo hiển thị
- TC-C03: Click "Reboot Device" → confirm dialog → cancel → API KHÔNG được gọi

Xuất DOCX sau khi viết.

## Definition of Done
- [ ] `docs/test-plans/TEST-PLAN-adb-app-status-reboot-api.md` + DOCX
- [ ] `docs/test-cases/TC-adb-app-status-reboot-api.md` + DOCX với kết quả Pass/Fail từng TC
- [ ] Ghi rõ TC nào cần thiết bị Android thật (không thể verify trong môi trường hiện tại)
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.1 → ✅

## Đã làm
(để trống)

## Artifact
(để trống)

## Quyết định quan trọng
(để trống)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
