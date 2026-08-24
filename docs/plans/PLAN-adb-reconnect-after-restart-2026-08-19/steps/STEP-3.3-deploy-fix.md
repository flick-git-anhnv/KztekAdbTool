---
step: "3.3"
plan: ../PLAN-MASTER.md
agent: devops-engineer
status: todo
completed_at:
deps: ["3.2"]
---

# STEP 3.3 — DevOps Engineer: Deploy fix lên môi trường tương ứng

## Input nhận

[Nhúng Handoff Payload từ STEP-3.2 vào đây khi giao việc — bao gồm xác nhận QA Lead sign-off "Approved for production deploy", branch/commit hash của fix đã merge.]

## Nhiệm vụ

Deploy fix bug auto-reconnect lên môi trường tương ứng (staging đã verify ở Bước 3.1; nếu QA Lead đã sign-off thì deploy lên production). Thực hiện smoke test nhanh sau deploy để xác nhận không có regression do quá trình deploy.

## Definition of Done

- [ ] QA Lead sign-off "Approved for production deploy" đã có (xác nhận từ Handoff Payload bước trước)
- [ ] Build và deploy thành công — không có lỗi build hoặc container crash
- [ ] Smoke test nhanh sau deploy: restart service → gọi `GET /api/devices/{serial}/status` → response đúng
- [ ] Ghi checklist deploy vào `docs/devops/DEPLOY-adb-reconnect-fix.md` (hoặc nhúng vào BUG report nếu thích hợp)

## Đã làm

[Điền SAU khi hoàn thành]

## Artifact

- Deploy log / checklist trong `docs/devops/DEPLOY-adb-reconnect-fix.md` (hoặc nhúng vào BUG report)

## Quyết định quan trọng

[Điền SAU khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit

- Hash: [điền sau khi commit — nếu có ghi deploy log]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
