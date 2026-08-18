# RESOURCE PLAN: API Launch App Android qua ADB

**Feature slug:** adb-launch-app-api
**Priority:** P1
**Ngày lập:** 2026-08-18
**Engineering Manager:** duongth@kztek.net

---

## 1. Tóm tắt tính năng

Bổ sung endpoint `POST /api/launch-app` cho `KztekAdbPublishTool.Web` — nhận `serial` + `app` (package name), xác thực bằng API key tĩnh qua header `x-api-key`, gọi `AdbService.LaunchAppAsync` có sẵn, trả JSON kết quả. Phạm vi nhỏ: 1 endpoint + 1 middleware auth mới.

---

## 2. Quyết định Priority

| Tiêu chí | Đánh giá |
|---|---|
| Mức độ | **P1** — tính năng quan trọng cho tự động hóa deploy end-to-end |
| Rủi ro | Trung bình — đụng cơ chế auth mới hoàn toàn cho project |
| Phụ thuộc | Không block sprint khác; `AdbService.LaunchAppAsync` đã có sẵn |
| Deadline | Không có deadline cứng; hoàn thành trong 1–2 ngày làm việc |

**Kết luận: Giữ P1.**

---

## 3. Phân bổ Team

| Agent | Vai trò | Lý do |
|---|---|---|
| **Tech Lead** | Thiết kế TDD, code review, security-audit-stride | Chịu trách nhiệm kỹ thuật, quyết định HTTP verb/response schema/config key chưa chốt (Q1–Q5) |
| **Senior Developer** | Code toàn bộ: endpoint + middleware + unit test | **KHÔNG giao Junior** — task đụng auth mechanism mới, cần kinh nghiệm bảo mật. Junior bị skip theo PLAN-MASTER |
| **QA Engineer** | Test plan + thực thi test case | Verify 8 scenario Given/When/Then từ US |
| **QA Lead** | Sign-off chất lượng | Bắt buộc vì P1 (theo CLAUDE.md WF-FEATURE Bước 12) |
| **DevOps Engineer** | Deploy staging + production | Theo quy trình chuẩn |
| **DevOps Lead** | Approve staging + production | Two-Eyes cho deploy |

**Junior Developer:** Bị skip toàn bộ feature này — đụng auth mới, không phù hợp cấp Junior.

---

## 4. Estimate Effort (thực tế, không thổi phồng)

> Feature nhỏ: 1 endpoint + 1 middleware. `AdbService` đã có sẵn. Estimate dưới đây là giờ làm việc thực tế.

| Bước | Agent | Estimate | Ghi chú |
|---|---|---|---|
| STEP-2.1: Viết TDD | Tech Lead | **1–2 giờ** | API contract, auth middleware design, error handling, config schema. Các câu hỏi mở (HTTP verb, response schema, tên config key, behavior am start khi app đang chạy) được chốt tại bước này |
| STEP-3.1: Code endpoint + middleware + unit test | Senior Developer | **2–3 giờ** | Implement + viết unit test cho endpoint và ApiKeyMiddleware. Tái sử dụng `AdbService.LaunchAppAsync` — không viết logic ADB mới |
| STEP-3.2: Code review + security-audit-stride | Tech Lead | **1–2 giờ** | Review PR + chạy security-audit-stride bắt buộc (auth mechanism mới). Block merge nếu Fail rủi ro cao |
| STEP-4.1: Test plan + thực thi test case | QA Engineer | **1–2 giờ** | 8 scenario Given/When/Then từ US đã có sẵn làm test basis; chủ yếu là thực thi + log kết quả |
| STEP-4.2: Sign-off | QA Lead | **30 phút** | Review kết quả test, sign-off nếu không còn P0/P1 bug |
| STEP-4.3 + 4.4: Deploy staging → production | DevOps Engineer + Lead | **30–60 phút** | Restart service, smoke test, approve |
| **Tổng** | | **~6–10 giờ** | **Khoảng 1 ngày làm việc cho toàn chain kỹ thuật** |

---

## 5. Điều kiện bắt buộc trước khi bắt đầu Phase kỹ thuật

- [ ] Tech Lead đọc `docs/prd/PRD-adb-launch-app-api.md` + `docs/user-stories/US-adb-launch-app-api.md` trước khi viết TDD
- [ ] Tech Lead chốt Q1–Q5 (HTTP verb, response schema, config key name, validation level, behavior am start khi app đang chạy) trong TDD — không để mở sang bước code
- [ ] Senior Developer đọc TDD đầy đủ trước khi code; không tự quyết định các điểm chưa chốt
- [ ] Bước STEP-3.2 BẮT BUỘC chạy `security-audit-stride` (auth mechanism mới — không có ngoại lệ)

---

## 6. Approve

Engineering Manager xác nhận:
- Priority P1 đã duyệt
- Senior Developer phụ trách toàn bộ code (Junior skip)
- Estimate thực tế ~1 ngày làm việc — không escalate thêm resource
- Tech Lead có thể escalate lên CTO nếu đánh giá kiến trúc auth key cần quyết định cao hơn
