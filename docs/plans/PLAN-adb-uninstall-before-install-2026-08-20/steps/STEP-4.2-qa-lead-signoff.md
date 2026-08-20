---
step: 4.2
plan: ../PLAN-MASTER.md
agent: qa-lead
status: todo
completed_at:
deps: [4.1]
---

# STEP 4.2 — QA Lead: Sign-off Chất lượng (P2)

## Input nhận
- Test results từ bước 4.1: `docs/test-cases/TC-adb-uninstall-before-install.md`
- Test plan: `docs/test-plans/TEST-PLAN-adb-uninstall-before-install.md`
- UXR review: `docs/ux-review/UX-REVIEW-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-4.1 (đọc mục "Handoff Payload" trong step file đó — quan trọng: TC nào PASS/FAIL, có bug P0/P1 nào không)

## Nhiệm vụ
Review kết quả test của QA Engineer, đánh giá coverage, xác nhận không còn bug P0/P1. Quyết định SIGN-OFF (cho phép deploy) hoặc VETO (có bug nghiêm trọng chưa fix). Priority P2 — bắt buộc QA Lead sign-off trước khi deploy.

## Definition of Done
- [ ] Đánh giá coverage AC: tất cả AC từ US-adb-uninstall-before-install đã có TC tương ứng
- [ ] Xác nhận 0 bug P0/P1 còn mở
- [ ] Quyết định SIGN-OFF hoặc VETO ghi rõ với lý do
- [ ] Sign-off note nhúng trong step file này (không cần tạo file riêng)
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
