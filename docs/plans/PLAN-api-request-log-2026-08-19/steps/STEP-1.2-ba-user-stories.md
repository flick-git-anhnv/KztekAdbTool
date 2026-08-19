---
step: "1.2"
plan: ../PLAN-MASTER.md
agent: business-analyst
status: done
completed_at: "2026-08-19 13:24"
deps: ["1.1"]
---

# STEP 1.2 — Business Analyst: User Stories + AC

## Input nhận
- `docs/prd/PRD-api-request-log.md` từ STEP-1.1 (Product Manager).

## Nhiệm vụ
Chi tiết hóa user stories cho feature API Request Log và viết acceptance criteria dạng Given/When/Then cho từng story, đảm bảo mọi AC đều testable.

## Definition of Done
- [ ] `docs/user-stories/US-api-request-log.md` đã được tạo
- [ ] Có ít nhất 4 user stories bao phủ: (1) log request AddDevice API thành công, (2) log request AddDevice API thất bại, (3) log request LaunchApp API thành công, (4) log request LaunchApp API thất bại
- [ ] Mỗi user story có AC dạng Given/When/Then với ít nhất 1 AC positive + 1 AC edge case
- [ ] Story về xem lại lịch sử sau restart (DB persistence) được ghi rõ
- [ ] Story về real-time display trên multiple dashboard tabs được ghi rõ
- [ ] Business Rules section ghi rõ: thông tin nào được log (thời điểm, tên API, serial/package, kết quả, caller IP nếu có), retention policy (nếu có)
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
- Đọc PRD `docs/prd/PRD-api-request-log.md` (commit e0fa758 từ STEP-1.1).
- Viết `docs/user-stories/US-api-request-log.md` với 6 user stories đầy đủ:
  - US-001: Log AddDevice thành công (3 scenarios Given/When/Then)
  - US-002: Log AddDevice thất bại + AC6 log 401 Unauthorized (3 scenarios)
  - US-003: Log LaunchApp thành công (3 scenarios)
  - US-004: Log LaunchApp thất bại + AC6 log 401 Unauthorized (3 scenarios)
  - US-005: DB persistence sau restart — xác nhận dữ liệu bền vững qua docker restart (3 scenarios)
  - US-006: Real-time multi-tab qua SignalR DeviceHub + appendLog() (4 scenarios)
- Business Rules tổng hợp BR-G1..BR-G10 và câu hỏi mở Q-01..Q-05 cho TL/PM.
- Xuất DOCX thành công; PDF bỏ qua (xelatex chưa cài trên môi trường hiện tại — không block workflow).

## Artifact
- `docs/user-stories/US-api-request-log.md` — nguồn chính
- `docs/user-stories/US-api-request-log.docx` — xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng
- **AC6 (401 logging):** Phản ánh đầy đủ vào US-002 Scenario 2 và US-004 Scenario 2. Kiến trúc inject point (middleware vs. handler) KHÔNG quyết định ở BA — để ngỏ cho Tech Lead ở TDD (Q-01).
- **US-005 (DB persistence):** Story bao gồm cả trường hợp `docker-compose down + up` (xóa container hoàn toàn) — yêu cầu SQLite phải được mount volume, không lưu in-container.
- **US-006 Scenario 3:** Tab mở SAU khi log đã tạo KHÔNG auto-load lại history — consistent với behavior `appendLog()` hiện tại. Đây là quyết định thiết kế có chủ đích.
- **Q-02 (SignalR event name):** Cần Tech Lead định nghĩa tên event rõ ràng trong TDD để Junior Dev code JS handler đúng.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: US đã viết xong 6 stories đủ DoD. Không cần viết lại hoặc thêm story mới trừ khi PM yêu cầu scope thay đổi.
- watch_out: Q-01 (401 inject point) và Q-02 (SignalR event name) là câu hỏi kỹ thuật CHƯA có câu trả lời — Tech Lead PHẢI chốt cả 2 trong TDD (STEP-1.5) trước khi giao 2.1 và 2.2. EM (STEP-1.3) không cần trả lời các câu hỏi kỹ thuật này.
- next_inputs: `docs/user-stories/US-api-request-log.md` — EM dùng làm input cho STEP-1.3 (estimate resource + xác nhận priority P2 + phân bổ Senior/Junior Dev). TL dùng làm input cho STEP-1.5 (TDD: schema, service interface, inject point, SignalR event contract, JS handler contract).

## Commit
- Hash: 6c77fa4
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
