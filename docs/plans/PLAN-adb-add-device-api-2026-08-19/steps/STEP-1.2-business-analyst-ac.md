---
step: 1.2
plan: ../PLAN-MASTER.md
agent: Business Analyst
status: todo
completed_at:
deps: [1.1]
---

# STEP 1.2 — Chi tiết hóa AC theo Given/When/Then

## Input nhận
Nhận Handoff Payload từ STEP-1.1 (đọc mục "Handoff Payload" của step file đó, không đọc lại toàn bộ PRD nếu payload đã đủ). PRD tại `docs/prd/PRD-adb-add-device-api.md`.

Tham khảo cấu trúc scenario của plan mẫu: `docs/plans/PLAN-adb-launch-app-api-2026-08-18/steps/STEP-1.2-business-analyst-ac.md` (US-adb-launch-app-api.md có 8 scenario Given/When/Then cho 1 API — plan này cần scenario tương tự nhưng cho 2 API).

Case cần phủ (tối thiểu, BA có thể bổ sung thêm nếu phát hiện edge case khác):
- **Connect-by-ip**: connect thành công (không truyền port → mặc định 5555); connect thành công (có truyền port khác); IP rỗng/invalid format → 400; adb connect thất bại (thiết bị không phản hồi/refused) → lỗi phù hợp; IP đã kết nối trước đó (idempotent — connect lại không lỗi).
- **Check-connection**: serial Online; serial Offline; serial chưa từng thấy (NotFound); serial rỗng → 400.
- Cả 2: thiếu/sai `x-api-key` → 401 (giống hành vi `/api/launch-app` đã có).

## Nhiệm vụ
Viết `docs/user-stories/US-adb-add-device-api.md` — user story + AC dạng Given/When/Then cho cả 2 API, liệt kê Business Rules (BR) và Edge Cases (EC) tương tự format US-adb-launch-app-api.md.

## Definition of Done
- [ ] `docs/user-stories/US-adb-add-device-api.md` có đủ scenario cho cả 2 API (tối thiểu 8-10 scenario tổng)
- [ ] Business Rules về format IP/port, default port 5555 được ghi rõ
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
