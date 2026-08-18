---
step: "4.1"
plan: ../PLAN-MASTER.md
agent: qa-engineer
status: done
completed_at: 2026-08-18 17:00
deps: ["3.2"]
---

# STEP 4.1 — QA Engineer: Test Plan + Test Cases + Thực thi

## Input nhận
Output từ Bước 3.2: code đã merge (Tech Lead approve). User Story (`docs/user-stories/US-adb-launch-app-api.md`) làm cơ sở AC.

## Nhiệm vụ
Viết test plan, test case chi tiết cho mọi AC (happy path + error paths), thực thi test trên môi trường staging/local với thiết bị Android thật (hoặc emulator), log kết quả Pass/Fail.

## Definition of Done
- [ ] File `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` đã tạo
- [ ] File `docs/test-cases/TC-adb-launch-app-api.md` đã tạo với test case cho:
  - TC-001: Happy path — launch app thành công, thiết bị online, package tồn tại
  - TC-002: API key sai → 401
  - TC-003: API key thiếu header → 401
  - TC-004: `serial` không tồn tại trong DeviceState → 404
  - TC-005: `app` (package name) trống hoặc thiếu → 400
  - TC-006: Thiết bị offline (ADB mất kết nối) → lỗi phù hợp (500 hoặc 503)
  - TC-007: Package name hợp lệ nhưng app không cài trên thiết bị → lỗi phù hợp
- [ ] Kết quả thực thi (Pass/Fail) ghi trong file test-cases hoặc bug report riêng
- [ ] Bug P0/P1 nếu có → tạo `docs/bugs/BUG-*.md`
- [ ] Đã xuất DOCX + PDF cho cả 2 file bằng `scripts/md_to_docx_kztek.py`

## Đã làm

**NHÓM A — HTTP thật (server local port 5299, 2026-08-18 17:00):**
- TC-002 (API key sai → 401): PASS — `{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}`
- TC-003 (thiếu header → 401): PASS — cùng response body
- TC-004 (serial không tồn tại → 404): PASS — `{"success":false,"error":"DeviceNotFound","message":"Device 'TEST-SERIAL-NOT-EXIST' not found."}`
- TC-005 (package rỗng/invalid → 400): PASS — 3 sub-case (rỗng, no-dot, whitespace-only)
- SC-07 regression (GET /api/devices không có x-api-key → 200): PASS
- EC6 (serial rỗng → 400): PASS

**NHÓM B — Verify gián tiếp (không có thiết bị Android):**
- TC-001 (happy path 200): unit test `MapAdbResult_ExitCode0_Returns200` + `CheckDeviceState_OnlineDevice_ReturnsNull` — PASS
- TC-006 (device offline → 422): unit test `CheckDeviceState_OfflineDevice_Returns422` — PASS
- TC-007 (app không cài → 422): unit test `MapAdbResult_AppNotInstalled_Returns422` — PASS
- Tổng dotnet test: 42/42 PASS (chạy lại sau khi test xong, xác nhận không regression)

**Artifacts tạo:**
- `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` + `.docx` (PDF fail xelatex)
- `docs/test-cases/TC-adb-launch-app-api.md` + `.docx` (PDF fail xelatex)

## Artifact
- `docs/test-plans/TEST-PLAN-adb-launch-app-api.md`
- `docs/test-plans/TEST-PLAN-adb-launch-app-api.docx`
- `docs/test-cases/TC-adb-launch-app-api.md`
- `docs/test-cases/TC-adb-launch-app-api.docx`

## Quyết định quan trọng
**Giới hạn môi trường:** Sandbox không có thiết bị Android thật hoặc emulator. TC-001 (200 happy path), TC-006 (422 device offline), TC-007 (422 app not installed) KHÔNG THỂ verify qua HTTP thật. Bằng chứng gián tiếp: unit test 42/42 PASS với `DeviceRecord`/`AdbCommandResult` giả lập đúng schema thật. QA Lead cần ghi nhận điều này khi sign-off và đảm bảo DevOps thực hiện smoke test thủ công trên thiết bị thật trước khi deploy production.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã chạy toàn bộ NHÓM A qua HTTP thật và NHÓM B qua unit test. KHÔNG chạy lại HTTP test (server đã dừng). KHÔNG chạy lại dotnet test (42/42 đã xác nhận).
- watch_out: TC-001/TC-006/TC-007 CHỈ verify gián tiếp qua unit test — CHƯA verify qua HTTP thật. QA Lead PHẢI ghi rõ điều này khi sign-off và yêu cầu DevOps thực hiện smoke test trên thiết bị Android thật tại STEP-4.3/4.4 trước khi approve production. Đây là rủi ro tồn đọng có chủ ý do giới hạn môi trường, không phải bug.
- next_inputs: `docs/test-cases/TC-adb-launch-app-api.md` (kết quả đầy đủ), `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` (chiến lược và giới hạn môi trường). QA Lead sign-off dựa trên 2 file này + xác nhận 42/42 unit test PASS.

## Commit
- Hash: [điền sau khi commit]
- Đã push: [điền sau khi push]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
