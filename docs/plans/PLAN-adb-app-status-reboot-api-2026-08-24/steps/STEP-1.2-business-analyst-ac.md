---
step: 1.2
plan: ../PLAN-MASTER.md
agent: Business Analyst
status: todo
completed_at: ~
deps: ["1.1"]
---

# STEP 1.2 — Chi tiết hóa AC theo Given/When/Then, user story 2 API

## Input nhận
Từ Bước 1.1 Handoff Payload — `docs/prd/PRD-adb-app-status-reboot-api.md` (PRD phạm vi hẹp với AC tổng quan + Non-goals).

## Nhiệm vụ
Viết User Story (`docs/user-stories/US-adb-app-status-reboot-api.md`) với:
- User story cho API kiểm tra trạng thái app (happy path + edge cases: app không tồn tại, thiết bị offline, serial NotFound, API key sai)
- User story cho API reboot (happy path + edge cases: thiết bị offline, serial NotFound, API key sai)
- User story cho 2 nút UI (operator click → API gọi → hiển thị kết quả trên dashboard)
- AC theo format Given/When/Then đầy đủ
- Business Rules: reboot là lệnh không thể hoàn tác → cần confirm dialog trên UI? (để ngỏ cho Tech Lead / UX Reviewer quyết)

Xuất DOCX sau khi viết.

## Definition of Done
- [ ] `docs/user-stories/US-adb-app-status-reboot-api.md` có scenario Given/When/Then cho cả 2 API + 2 nút UI, bao gồm edge cases
- [ ] Xuất DOCX bằng `python3 scripts/md_to_docx_kztek.py`
- [ ] Commit + push lên `origin/docker-deploy`
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 1.2 → ✅

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
