# SPRINT PLAN: API Thêm Thiết Bị (Connect-by-IP) và Kiểm Tra Kết Nối ADB

**Sprint slug:** adb-add-device-api
**Trạng thái:** Active
**Ngày lập:** 2026-08-19
**Thời gian dự kiến:** 1 ngày làm việc
**Sprint Goal:** Bổ sung 2 endpoint (`POST /api/devices/connect-by-ip` và `GET /api/devices/{serial}/status`) tái dùng auth đã có, triển khai lên staging và production
**Velocity / Ước tính tổng:** ~5–8 giờ
**Team:** Tech Lead, Senior Developer, QA Engineer, QA Lead, DevOps Engineer, DevOps Lead

---

## Phase 1: Phân tích & Lên kế hoạch (ĐÃ HOÀN THÀNH)

| Task ID | Mô tả | Assignee | Estimate | Priority | Status | Hoàn thành lúc |
|---------|-------|----------|----------|----------|--------|----------------|
| T-1.1 | Viết PRD phạm vi hẹp: 2 API mới, mục tiêu, AC tổng quan, non-goals | Product Manager | ~1h | P1 | ✅ Done | 2026-08-19 10:09 |
| T-1.2 | Chi tiết hóa AC theo Given/When/Then (14 scenario), user story cho 2 API | Business Analyst | ~1h | P1 | ✅ Done | 2026-08-19 10:13 |
| T-1.3 | Estimate resource, xác nhận priority P1, phân bổ team | Engineering Manager | ~30 phút | P1 | ✅ Done | 2026-08-19 10:16 |
| T-1.4 | Lên task board, sprint plan cho feature này | Project Manager | ~30 phút | P1 | ✅ Done | — |

---

## Task Board (Phase 2–4)

| Task ID | Mô tả | Assignee | Estimate | Priority | Status | Phụ thuộc |
|---------|-------|----------|----------|----------|--------|-----------|
| T-2.1 | Viết TDD: API contract 2 endpoint, routing design, reuse strategy (`AdbService.ConnectAsync` + `DeviceState`), error handling; chốt 2 assumption mở (mã HTTP khi serial NotFound, serial có trong response connect-by-ip) | Tech Lead | 1–2h | P1 | Todo | T-1.1, T-1.2, T-1.3 (PRD + US + RESOURCE đã xong) |
| T-3.1 | Code 2 endpoint mới + unit test + cập nhật CODE-GRAPH; KHÔNG tự quyết điểm chưa chốt — đọc TDD trước; tái dùng `AdbService.ConnectAsync`, `DeviceState`, `ApiKeyEndpointFilter` | Senior Developer | 2–3h | P1 | Todo | T-2.1 |
| T-3.2 | Code review PR + chạy `security-audit-stride` BẮT BUỘC (route mới — surface attack mới dù tái dùng auth cũ); quyết định merge; BLOCK merge nếu Fail rủi ro cao | Tech Lead | 1h | P1 | Todo | T-3.1 |
| T-4.1 | Viết test plan, thực thi 14 scenario Given/When/Then từ US, log kết quả; tạo `docs/test-plans/TEST-PLAN-adb-add-device-api.md` + `docs/test-cases/TC-adb-add-device-api.md` | QA Engineer | 1–2h | P1 | Todo | T-3.2 |
| T-4.2 | Review kết quả test, sign-off chất lượng; VETO nếu còn P0/P1 bug | QA Lead | 30 phút | P1 | Todo | T-4.1 |
| T-4.3 | Deploy lên staging; smoke test; tạo `docs/devops/DEPLOY-adb-add-device-api.md` | DevOps Engineer | 30–60 phút | P1 | Todo | T-4.2 |
| T-4.4 | Approve staging, verify smoke test, approve và deploy production, monitor | DevOps Lead | 30 phút | P1 | Todo | T-4.3 |

---

## Definition of Done (Sprint)

- **P0/P1 Done:** Tất cả 14 scenario Given/When/Then trong US pass — không còn P0/P1 bug
- **QA sign-off:** QA Lead đã sign-off (T-4.2 Done)
- **Deploy staging:** Cả 2 endpoint hoạt động trên staging (T-4.3 Done)
- **Deploy production:** DevOps Lead approve và deploy thành công (T-4.4 Done)
- **Security:** `security-audit-stride` pass tại T-3.2 — không có Fail nhóm rủi ro cao

---

## Dependencies ngoài sprint

- `AdbService.ConnectAsync` đã có sẵn — KHÔNG viết logic ADB mới
- `DeviceState` + `DevicePollWorker` đã có sẵn — API chỉ đọc cache
- `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` đã có sẵn — KHÔNG tạo auth mới
- `docs/prd/PRD-adb-add-device-api.md` — input cho T-2.1
- `docs/user-stories/US-adb-add-device-api.md` — input cho T-2.1 (14 AC) và T-4.1 (test basis)
- `docs/planning/RESOURCE-adb-add-device-api.md` — estimate và phân bổ team đã duyệt

---

## Scope bị đẩy ra

- CTO review kiến trúc — chỉ kích hoạt nếu Tech Lead escalate khi phát hiện vấn đề kiến trúc tại TDD
- Đổi tên section config `LaunchApp:ApiKey` thành tên khác rõ nghĩa hơn — Tech Lead có thể đề xuất nhưng ưu tiên tái dùng nguyên trạng tránh breaking change

---

## Rủi ro sprint

| Rủi ro | Xác suất | Ảnh hưởng | Mitigating |
|--------|----------|-----------|------------|
| 2 assumption mở chưa chốt (mã HTTP NotFound, serial trong response) gây rework code | Trung bình | Trung bình | Tech Lead PHẢI chốt trong TDD (T-2.1) trước khi Senior Dev bắt đầu T-3.1 |
| `security-audit-stride` Fail → block merge | Thấp | Cao | Senior Dev implement theo TDD; Tech Lead review sớm |
| `DeviceState` không đủ thông tin cho serial NotFound | Thấp | Trung bình | Tech Lead verify tại TDD; nếu thiếu thì escalate lên EM trước khi code |

---

## Phê duyệt

| Vai trò | Người | Trạng thái |
|---------|-------|------------|
| Product Manager | PM | ✅ (PRD đã duyệt tại T-1.1) |
| Tech Lead | TL | ⬜ Chờ xác nhận khi bắt đầu T-2.1 |
| QA Lead | QAL | ⬜ Chờ xác nhận testability tại T-4.2 |
| Project Manager | PJM | ✅ Chốt sprint — 2026-08-19 |

---

## Lịch sử cập nhật

| Ngày | Phiên bản | Nội dung | Agent |
|------|-----------|----------|-------|
| 2026-08-19 | v1.0 | Tạo sprint plan — Phase 1 hoàn thành (T-1.1 đến T-1.4 Done); 7 task Phase 2–4 Todo | Project Manager |
