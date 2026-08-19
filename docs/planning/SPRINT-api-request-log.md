# SPRINT Plan: API Request Log | Status: Active

**Feature slug:** api-request-log
**Priority:** P2
**Project Manager:** duongth@kztek.net
**Ngày tạo:** 2026-08-19

---

## Thông tin Sprint

| Trường | Giá trị |
|---|---|
| Sprint Goal | Deliver tính năng API Request Log: ghi bền vững SQLite + real-time SignalR vào panel `#log` cho 2 API có ApiKey protection |
| Ngày bắt đầu | 2026-08-19 |
| Ngày hoàn thành dự kiến | 2026-08-21 |
| Velocity target | ~15–22 giờ (2–3 ngày làm việc) |
| Team | Tech Lead, Senior Developer, Junior Developer, UX/UI Reviewer, QA Engineer, QA Lead, DevOps Engineer, DevOps Lead |

---

## Sprint Backlog

| Task ID | Mô tả | Assignee | Estimate | Story Points | Priority | Status | Phụ thuộc |
|---------|-------|----------|----------|--------------|----------|--------|-----------|
| S1-T01 | TDD: chốt schema `ApiRequestLog`, interface `IApiRequestLogService`, inject point 401, tên SignalR event, async strategy, CallerIp source, JS handler contract | Tech Lead | 2–3h | 3 | P0 | In Progress | 1.3 Done |
| S1-T02 | Backend C#: Entity `ApiRequestLog` + EF migration, `IApiRequestLogService` / `ApiRequestLogService`, inject vào `DeviceEndpoints.cs` + `LaunchAppEndpoints.cs`, 401 intercept, SignalR broadcast, unit tests | Senior Developer | 5–7h | 8 | P0 | Todo | S1-T01 |
| S1-T03 | Frontend JS: event handler mới trong `signalr-client.js`, format và render log entry vào panel `#log` qua `appendLog()` | Junior Developer | 2–3h | 3 | P0 | Todo | S1-T01 |
| S1-T04 | Code review cuối + merge decision (verify-pr report bắt buộc trước khi mở review; cần S1-T02 + S1-T03 cùng xong) | Tech Lead | 1–2h | 2 | P0 | Todo | S1-T02, S1-T03 |
| S1-T05 | Security audit (conditional): cover CallerIp logging; Tech Lead tự quyết có chạy STRIDE không — ghi rõ lý do nếu skip | Tech Lead | 0.5–1h | 1 | P1 | Todo | S1-T04 |
| S1-T06 | UXR: chạy app thật, gọi AddDevice + LaunchApp, xác nhận log entries xuất hiện đúng format/nội dung/timing trong panel `#log` | UX/UI Reviewer | 0.5–1h | 1 | P1 | Todo | S1-T04 |
| S1-T07 | QA test execution: gọi 2 API theo 6 US × 3 scenario, kiểm tra DB persistence, SignalR real-time (≤2s), AC6 (401 log), AC7 (không log API khác) | QA Engineer | 2–3h | 3 | P1 | Todo | S1-T06 |
| S1-T08 | QA Lead sign-off: review kết quả, veto nếu còn P0/P1 bug | QA Lead | 0.5h | 1 | P1 | Todo | S1-T07 |
| S1-T09 | Deploy staging: `docker-compose up --build`, verify migration `ApiRequestLog` apply thành công, smoke test staging | DevOps Engineer | 1–2h | 2 | P2 | Todo | S1-T08, P1-deploy-xong |
| S1-T10 | Approve staging + cấp phép deploy production: smoke test, verify log entries trên staging | DevOps Lead | 0.5h | 1 | P2 | Todo | S1-T09 |
| S1-T11 | Deploy production + monitor | DevOps Lead | 0.5h | 1 | P2 | Todo | S1-T10 |

**Tổng Story Points:** 26 SP | **Tổng Estimate:** ~15–22 giờ

---

## Task Board

### TODO
- S1-T02 — Backend C# (Senior Developer)
- S1-T03 — Frontend JS (Junior Developer)
- S1-T04 — Code review + merge (Tech Lead)
- S1-T05 — Security audit (conditional) (Tech Lead)
- S1-T06 — UXR review (UX/UI Reviewer)
- S1-T07 — QA test execution (QA Engineer)
- S1-T08 — QA Lead sign-off (QA Lead)
- S1-T09 — Deploy staging (DevOps Engineer)
- S1-T10 — Approve staging (DevOps Lead)
- S1-T11 — Deploy production (DevOps Lead)

### IN PROGRESS
- S1-T01 — TDD (Tech Lead) — STEP-1.5

### REVIEW
_(trống)_

### DONE
- Sprint Plan — Project Manager — STEP-1.4

---

## Timeline

| Ngày | Phase | Tasks | Ghi chú |
|------|-------|-------|---------|
| 2026-08-19 | Phase 1 — Thiết kế | S1-T01 (TDD) | TDD phải xong trước khi giao S1-T02 + S1-T03 |
| 2026-08-20 | Phase 2 — Code | S1-T02 ∥ S1-T03 → S1-T04 → S1-T05 | S1-T02 và S1-T03 chạy song song sau khi TDD done; S1-T04 chỉ mở review khi cả hai xong |
| 2026-08-21 | Phase 3 — QA + Deploy | S1-T06 → S1-T07 → S1-T08 → S1-T09 → S1-T10 → S1-T11 | Deploy staging (S1-T09) BẮT BUỘC chờ P1 (adb-reconnect) deploy xong |

---

## Dependencies

| Task | Phụ thuộc vào | Ghi chú |
|------|---------------|---------|
| S1-T02 (Backend) | S1-T01 (TDD) | Senior Dev không code khi inject point 401 + tên SignalR event chưa được Tech Lead chốt |
| S1-T03 (Frontend JS) | S1-T01 (TDD) | Junior Dev cần tên event SignalR chính xác và payload schema từ TDD |
| S1-T04 (Code review) | S1-T02 + S1-T03 | Tech Lead chỉ mở review khi CẢ HAI backend và frontend đều xong + verify-pr report đính kèm |
| S1-T05 (Security audit) | S1-T04 | Chạy sau merge; Tech Lead tự quyết skip (ghi rõ lý do) hay chạy |
| S1-T06 (UXR) | S1-T04 | UXR sau khi code merged — chạy app thật |
| S1-T07 (QA) | S1-T06 | QA sau UXR xác nhận UI OK |
| S1-T08 (Sign-off) | S1-T07 | QA Lead ký duyệt sau QA done |
| S1-T09 (Deploy staging) | S1-T08 + **P1 deploy production xong** | WATCH OUT: staging P2 không deploy trước khi P1 (adb-reconnect) đã production — DevOps Lead xác nhận thời điểm |
| S1-T10 (Approve staging) | S1-T09 | Two-Eyes |
| S1-T11 (Deploy production) | S1-T10 | Two-Eyes |

---

## Scope bị đẩy ra (Out of Scope phiên bản này)

| Item | Lý do |
|------|-------|
| Retention policy / tự động xóa log cũ | PRD Non-goal — track ở backlog tương lai |
| Log export / download log file | PRD Non-goal |
| Filter / search log trên dashboard | PRD Non-goal |
| Rate limiting dựa trên log | PRD Non-goal |
| Log cho các API khác ngoài AddDevice + LaunchApp | PRD Non-goal — AC7 |

---

## Rủi ro Sprint

| # | Rủi ro | Xác suất | Tác động | Mitigat |
|---|--------|----------|----------|---------|
| R1 | TDD (S1-T01) mất hơn 3h vì 401 inject point phức tạp → trễ S1-T02/T03 | Thấp | Trung bình | Tech Lead ưu tiên chốt Q-01 (inject point) trước, các quyết định còn lại ghi là assumption có thể refine |
| R2 | P1 deploy chậm → S1-T09 (staging) bị block | Trung bình | Thấp | S1-T09 block là có kế hoạch — tracking P1 deploy qua kênh DevOps riêng; không ảnh hưởng Phase 2 |
| R3 | Senior Dev gặp vấn đề 401 intercept ngoài TDD spec → cần Tech Lead hỗ trợ giữa sprint | Thấp | Trung bình | Escalate ngay qua Tech Lead; không tự quyết kiến trúc |

---

## Definition of Done

- **P0 Done:** S1-T01 (TDD approved), S1-T02 (backend merged), S1-T03 (frontend merged), S1-T04 (code review OK)
- **QA sign-off:** S1-T07 (QA execution pass — AC1–AC7), S1-T08 (QA Lead ký duyệt)
- **Deploy staging xong:** S1-T09 + S1-T10 (smoke test staging pass)
- **Demo xong:** Demo panel `#log` hiển thị log entry real-time khi gọi AddDevice/LaunchApp từ Postman/curl
- **Deploy production xong:** S1-T11 (monitor ổn định)

---

## Phê duyệt

| Vai trò | Người | Trạng thái |
|---------|-------|-----------|
| Product Manager | duongth@kztek.net | ✅ (PRD STEP-1.1) |
| Engineering Manager | duongth@kztek.net | ✅ (RESOURCE STEP-1.3) |
| Tech Lead | duongth@kztek.net | ⬜ Chờ TDD (STEP-1.5) |
| QA Lead | duongth@kztek.net | ⬜ Chờ test plan |
| Project Manager | duongth@kztek.net | ✅ Sprint plan chốt |

---

## Lịch sử cập nhật

| Ngày | Phiên bản | Cập nhật | Agent |
|------|-----------|----------|-------|
| 2026-08-19 | v1.0 | Sprint plan tạo mới — backlog 11 task, timeline 3 ngày, dependencies rõ | Project Manager |
