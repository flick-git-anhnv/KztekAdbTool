---
step: "4.1"
plan: ../PLAN-MASTER.md
agent: qa-engineer
status: todo
completed_at:
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
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
Không có

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
