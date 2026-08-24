---
step: 4.2
plan: ../PLAN-MASTER.md
agent: QA Lead
status: todo
completed_at: ~
deps: ["4.1"]
---

# STEP 4.2 — Sign-off chất lượng (P1 — bắt buộc QA Lead)

## Input nhận
Từ Bước 4.1 Handoff Payload — `docs/test-cases/TC-adb-app-status-reboot-api.md` với kết quả Pass/Fail từng TC, danh sách TC cần thiết bị Android thật.

## Nhiệm vụ
Review kết quả test, đưa ra quyết định sign-off:

- Đánh giá coverage: 10 TC API + 3 TC UI đã đủ chưa?
- Kiểm tra: không có P0/P1 bug còn mở
- Đặc biệt: TC reboot (TC-B01) — lệnh destructive, cần được verify thật trước production
- Điều kiện sign-off: nếu TC ADB thật chưa verify → sign-off có điều kiện (DevOps PHẢI smoke test với thiết bị thật tại staging trước khi go-live production)
- Ghi kết quả sign-off inline trong `docs/test-cases/TC-adb-app-status-reboot-api.md` hoặc tạo sign-off section riêng

## Definition of Done
- [ ] Đánh giá đủ coverage + bug status (0 P0/P1 open)
- [ ] SIGN-OFF (đầy đủ hoặc có điều kiện) ghi rõ trong TC doc hoặc step file này
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.2 → ✅

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
