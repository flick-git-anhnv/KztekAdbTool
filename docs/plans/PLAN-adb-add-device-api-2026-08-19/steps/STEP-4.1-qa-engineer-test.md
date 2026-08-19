---
step: 4.1
plan: ../PLAN-MASTER.md
agent: QA Engineer
status: todo
completed_at:
deps: [3.2]
---

# STEP 4.1 — Viết test plan, thực thi test case

## Input nhận
Nhận Handoff Payload từ STEP-3.2 (code đã merge/approve). TDD tại `docs/tech-design/TDD-adb-add-device-api.md`, US tại `docs/user-stories/US-adb-add-device-api.md`.

Lưu ý giống plan mẫu: môi trường CI/sandbox có thể KHÔNG có thiết bị Android thật để test `connect-by-ip` thật sự (không có mạng LAN với thiết bị) — QA Engineer chia nhóm: case verify được qua HTTP thật (input validation, auth 401, serial not found) vs case cần thiết bị thật (connect thành công, status Online sau khi connect) → verify gián tiếp qua unit test hoặc ghi rõ giới hạn cần DevOps/user smoke test thủ công.

## Nhiệm vụ
Viết `docs/test-plans/TEST-PLAN-adb-add-device-api.md` + `docs/test-cases/TC-adb-add-device-api.md`, thực thi test case, log kết quả.

## Definition of Done
- [ ] Test plan + test case cover đủ scenario từ US (cả 2 API)
- [ ] Case chạy được thật → log kết quả PASS/FAIL cụ thể
- [ ] Case cần thiết bị thật → ghi rõ giới hạn, đề xuất smoke test thủ công trước production
- [ ] Xuất DOCX/PDF theo R1

## Đã làm


## Artifact


## Quyết định quan trọng


## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo:
- watch_out:
- next_inputs:

## Commit
- Hash:
- Đã push:

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
