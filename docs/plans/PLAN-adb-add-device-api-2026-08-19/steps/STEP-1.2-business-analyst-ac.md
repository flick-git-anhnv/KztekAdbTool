---
step: 1.2
plan: ../PLAN-MASTER.md
agent: Business Analyst
status: done
completed_at: 2026-08-19 10:13
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
- Đọc PRD `docs/prd/PRD-adb-add-device-api.md` (AC1–AC11, câu hỏi mở Q1, Q2)
- Tham khảo format US-adb-launch-app-api.md để giữ nhất quán
- Viết `docs/user-stories/US-adb-add-device-api.md` với 14 scenario Given/When/Then (SC-A1–A7, SC-B1–B5, SC-C1–C2), business flow mermaid cho cả 2 API, 6 BR, 8 EC, 7 câu hỏi mở cho Tech Lead
- Xuất DOCX thành công; PDF thất bại do thiếu LaTeX (chấp nhận)

## Artifact
- `docs/user-stories/US-adb-add-device-api.md`
- `docs/user-stories/US-adb-add-device-api.docx`

## Quyết định quan trọng
- SC-B3 (AC8): ghi là assumption mở — BA đề xuất 404, nhưng Tech Lead quyết định ở TDD
- Q1 (serial trong response): để ngỏ cho TDD xác nhận qua đọc source `AdbService.ConnectAsync`
- Chia thành 3 phần rõ ràng: Phần A (connect-by-ip), Phần B (status), Phần C (chung) để dễ trace requirement

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: US đã viết xong tại `docs/user-stories/US-adb-add-device-api.md`, đã commit `b7e9709`, đã push — KHÔNG viết lại.
- watch_out: SC-B3 (mã HTTP cho NotFound) và Q1 (serial trong response) là 2 assumption mở chưa chốt — Tech Lead PHẢI quyết định ở TDD (STEP-2.1) trước khi Dev code. Hành vi ASP.NET Core routing khi serial rỗng (Q6) cũng cần xác nhận vì có thể là GOTCHA.
- next_inputs: Đọc `docs/user-stories/US-adb-add-device-api.md` (toàn bộ — đặc biệt Phần A, Phần B, câu hỏi mở Q1–Q7) + `docs/prd/PRD-adb-add-device-api.md` + source code `AdbService.ConnectAsync` và `DevicePollWorker` để xác nhận hành vi thực và viết TDD.

## Commit
- Hash: b7e9709
- Đã push: có

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
