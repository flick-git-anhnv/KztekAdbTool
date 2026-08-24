---
step: 4.4
plan: ../PLAN-MASTER.md
agent: DevOps Lead
status: todo
completed_at: ~
deps: ["4.3"]
---

# STEP 4.4 — Approve staging, verify smoke test, approve + deploy production

## Input nhận
Từ Bước 4.3 Handoff Payload — `docs/devops/DEPLOY-adb-app-status-reboot-api.md`, kết quả smoke test staging, danh sách GATE còn mở (nếu có TC cần thiết bị Android thật).

## Nhiệm vụ
- Review kết quả deploy staging từ DevOps Engineer
- Verify evidence: image build log, curl output, UI test
- Quyết định APPROVE staging hay yêu cầu re-deploy
- Nếu QA Lead sign-off có điều kiện (cần thiết bị thật): KHÔNG tự approve production — chuyển giao user để tự smoke test TC-A01/B01 với thiết bị Android thật
- Nếu tất cả gate đã đóng: approve + deploy production, monitor

Ghi quyết định rõ trong step file này. KHÔNG tự deploy production khi còn GATE CHƯA ĐÓNG — chuyển giao user với hướng dẫn cụ thể.

## Definition of Done
- [ ] Đánh giá evidence staging đầy đủ
- [ ] APPROVE STAGING hoặc yêu cầu re-deploy ghi rõ lý do
- [ ] Nếu deploy production: ghi nhận thời điểm go-live, bắt đầu monitor
- [ ] Nếu không deploy production: hướng dẫn user cụ thể các bước còn lại
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.4 → ✅, PLAN-MASTER status → done

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
