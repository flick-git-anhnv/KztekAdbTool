# SPRINT PLAN: API Launch App Android qua ADB

**Sprint slug:** adb-launch-app-api
**Trạng thái:** Planning
**Ngày lập:** 2026-08-18
**Thời gian dự kiến:** 1–2 ngày làm việc
**Sprint Goal:** Bổ sung endpoint `POST /api/launch-app` với xác thực API key, triển khai lên môi trường staging và production
**Velocity / Ước tính tổng:** ~6–10 giờ
**Team:** Tech Lead, Senior Developer, QA Engineer, QA Lead, DevOps Engineer, DevOps Lead

---

## Task Board

| Task ID | Mô tả | Assignee | Estimate | Priority | Status | Phụ thuộc |
|---------|-------|----------|----------|----------|--------|-----------|
| T-2.1 | Viết TDD: API contract, auth middleware design, error handling, config appsettings; chốt Q1–Q5 (HTTP verb, response schema, config key, validation, behavior am start) | Tech Lead | 1–2h | P1 | Todo | STEP-1.1, STEP-1.2, STEP-1.3 (PRD + US + RESOURCE đã xong) |
| T-3.1 | Code endpoint (`POST /api/launch-app`) + ApiKeyMiddleware + unit test; KHÔNG tự quyết điểm chưa chốt — đọc TDD trước | Senior Developer | 2–3h | P1 | Todo | T-2.1 |
| T-3.2 | Code review PR + chạy `security-audit-stride` bắt buộc (auth mới); quyết định merge; BLOCK merge nếu Fail rủi ro cao | Tech Lead | 1–2h | P1 | Todo | T-3.1 |
| T-4.1 | Viết test plan, thực thi 8 scenario Given/When/Then từ US, log kết quả; tạo `docs/test-plans/` + `docs/test-cases/` | QA Engineer | 1–2h | P1 | Todo | T-3.2 |
| T-4.2 | Review kết quả test, sign-off chất lượng; VETO nếu còn P0/P1 bug | QA Lead | 30 phút | P1 | Todo | T-4.1 |
| T-4.3 | Deploy lên staging; smoke test; tạo `docs/devops/DEPLOY-adb-launch-app-api.md` | DevOps Engineer | 30–60 phút | P1 | Todo | T-4.2 |
| T-4.4 | Approve staging, verify smoke test, approve và deploy production, monitor | DevOps Lead | 30 phút | P1 | Todo | T-4.3 |

---

## Definition of Done (Sprint)

- **P0/P1 Done:** Tất cả 8 scenario Given/When/Then trong US pass — không còn P0/P1 bug
- **QA sign-off:** QA Lead đã sign-off (T-4.2 Done)
- **Deploy staging:** Endpoint `POST /api/launch-app` hoạt động trên môi trường staging (T-4.3 Done)
- **Deploy production:** DevOps Lead approve và deploy thành công (T-4.4 Done)
- **Security:** `security-audit-stride` pass tại T-3.2 — không có Fail nhóm rủi ro cao

---

## Dependencies ngoài sprint

- `AdbService.LaunchAppAsync(serial, packageName, ct)` đã có sẵn — KHÔNG viết logic ADB mới
- `docs/prd/PRD-adb-launch-app-api.md` — input cho T-2.1
- `docs/user-stories/US-adb-launch-app-api.md` — input cho T-2.1 (8 AC Given/When/Then) và T-4.1 (test basis)
- `docs/planning/RESOURCE-adb-launch-app-api.md` — estimate và phân bổ team đã duyệt

---

## Scope bị đẩy ra

- Mở rộng auth cho các route cũ (`/api/install`, ...) — Tech Lead quyết định trong TDD; nếu scope lớn hơn thì tách sprint riêng
- CTO review kiến trúc — chỉ kích hoạt nếu Tech Lead escalate

---

## Rủi ro sprint

| Rủi ro | Xác suất | Ảnh hưởng | Mitigating |
|--------|----------|-----------|------------|
| Q1–Q5 chưa chốt gây rework bước code | Trung bình | Cao | Tech Lead PHẢI chốt toàn bộ trong TDD (T-2.1) trước khi Senior Dev bắt đầu T-3.1 |
| security-audit-stride Fail → block merge | Thấp | Cao | Senior Dev implement theo TDD kỹ thuật; Tech Lead review sớm |
| `am start` behavior khi app đang chạy | Thấp | Trung bình | Tech Lead ghi rõ expected behavior trong TDD |

---

## Phê duyệt

| Vai trò | Người | Trạng thái |
|---------|-------|------------|
| Product Manager | PM | ✅ (PRD đã duyệt tại STEP-1.1) |
| Tech Lead | TL | ⬜ Chờ xác nhận khi bắt đầu T-2.1 |
| QA Lead | QAL | ⬜ Chờ xác nhận testability tại T-4.2 |
| Project Manager | PJM | ✅ Chốt sprint — 2026-08-18 |

---

## Lịch sử cập nhật

| Ngày | Phiên bản | Nội dung | Agent |
|------|-----------|----------|-------|
| 2026-08-18 | v1.0 | Tạo sprint plan ban đầu — 7 task, 4 phase | Project Manager |
