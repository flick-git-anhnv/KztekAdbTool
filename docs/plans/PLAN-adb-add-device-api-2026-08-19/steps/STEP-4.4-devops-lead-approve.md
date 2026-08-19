---
step: 4.4
plan: ../PLAN-MASTER.md
agent: DevOps Lead
status: todo
completed_at:
deps: [4.3]
---

# STEP 4.4 — Approve staging, verify smoke test, approve production

## Input nhận
Nhận Handoff Payload từ STEP-4.3.

## Nhiệm vụ
Approve staging nếu evidence đủ. KHÔNG tự approve production nếu còn gate chưa đóng (case cần thiết bị Android thật) — chuyển giao user tự smoke test thủ công trước go-live production, giống quyết định ở plan mẫu `adb-launch-app-api`.

## Definition of Done
- [ ] Quyết định staging approve rõ ràng
- [ ] Nếu còn gate chưa đóng → ghi rõ hành động user cần làm trước khi go-live production
- [ ] Cập nhật PLAN-MASTER.md `status: done` nếu toàn bộ chain hoàn thành (hoặc `status: staging-approved, pending user go-live` nếu còn gate)

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
