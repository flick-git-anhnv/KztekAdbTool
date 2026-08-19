---
step: 4.3
plan: ../PLAN-MASTER.md
agent: DevOps Engineer
status: todo
completed_at:
deps: [4.2]
---

# STEP 4.3 — Deploy lên môi trường tương ứng

## Input nhận
Nhận Handoff Payload từ STEP-4.2 (điều kiện sign-off nếu có). Tham khảo `docs/devops/DEPLOY-adb-launch-app-api.md` (plan mẫu) cho quy trình build container + smoke test qua curl.

## Nhiệm vụ
Deploy container/staging, chạy smoke test cơ bản qua curl (401 khi thiếu key, 400 input sai, response format đúng) cho cả 2 endpoint mới. Viết `docs/devops/DEPLOY-adb-add-device-api.md`.

## Definition of Done
- [ ] Container build OK
- [ ] Smoke test curl PASS cho case validate được mà không cần thiết bị thật
- [ ] Ghi rõ GATE CHƯA ĐÓNG nếu còn case cần thiết bị Android thật (theo điều kiện từ STEP-4.2)
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
