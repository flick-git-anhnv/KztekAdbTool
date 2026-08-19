# RESOURCE PLAN: API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB

**Feature slug:** adb-add-device-api
**Priority:** P1
**Ngày lập:** 2026-08-19
**Engineering Manager:** duongth@kztek.net

---

## 1. Tóm tắt tính năng

Bổ sung 2 endpoint mới cho `KztekAdbPublishTool.Web`:
- `POST /api/devices/connect-by-ip` — nhận `ip` + `port` (mặc định 5555), gọi `AdbService.ConnectAsync` có sẵn, trả JSON kết quả kết nối.
- `GET /api/devices/{serial}/status` — nhận `serial`, đọc `DeviceState` cache từ `DevicePollWorker`, trả trạng thái `Online` / `Offline` / `NotFound`.

Cả 2 endpoint tái sử dụng `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` hiện có — **không tạo cơ chế auth mới**, không tạo section config mới.

---

## 2. Quyết định Priority

| Tiêu chí | Đánh giá |
|---|---|
| Mức độ | **P1** — tính năng quan trọng cho tự động hóa end-to-end: thêm thiết bị → kết nối → kiểm tra online → publish APK |
| Rủi ro | **Thấp hơn plan mẫu (adb-launch-app-api)** — auth tái dùng nguyên trạng `ApiKeyEndpointFilter`, không tạo middleware mới; `AdbService.ConnectAsync` và `DeviceState` đã được kiểm chứng qua các tính năng trước |
| Phụ thuộc | Không block sprint khác; toàn bộ service/filter cần dùng đều đã có sẵn |
| Deadline | Không có deadline cứng; hoàn thành trong 1 ngày làm việc |

**Kết luận: Giữ P1.**

---

## 3. Phân bổ Team

| Agent | Vai trò | Lý do |
|---|---|---|
| **Tech Lead** | Thiết kế TDD, code review, security-audit-stride | Quyết định HTTP response schema, error codes, reuse strategy cho 2 assumption mở (mã HTTP khi serial NotFound, serial trong response connect-by-ip) |
| **Senior Developer** | Code toàn bộ: 2 endpoint + unit test | **KHÔNG giao Junior** — đụng auth (`ApiKeyEndpointFilter`) và kết nối thiết bị ADB; yêu cầu kinh nghiệm xử lý bảo mật đúng cách |
| **QA Engineer** | Test plan + thực thi test case | Verify 14 scenario Given/When/Then từ US đã có sẵn làm test basis |
| **QA Lead** | Sign-off chất lượng | Bắt buộc vì P1 (theo CLAUDE.md WF-FEATURE Bước 12) |
| **DevOps Engineer** | Deploy staging + production | Theo quy trình chuẩn |
| **DevOps Lead** | Approve staging + production | Two-Eyes cho deploy |

**Junior Developer:** Bị skip toàn bộ feature này — đụng auth và kết nối thiết bị, không phù hợp cấp Junior (theo quyết định trong PLAN-MASTER).

---

## 4. Estimate Effort (thực tế, không thổi phồng)

> Feature có 2 endpoint nhưng tái sử dụng nhiều service đã có — effort **thấp hơn plan mẫu** (adb-launch-app-api ước tính ~6–10h vì phải viết auth middleware mới hoàn toàn). Estimate dưới đây là giờ làm việc thực tế.

| Bước | Agent | Estimate | Ghi chú |
|---|---|---|---|
| STEP-2.1: Viết TDD | Tech Lead | **1–2 giờ** | API contract 2 endpoint, routing design, error handling, chốt 2 assumption mở (mã HTTP NotFound, serial trong response connect-by-ip). Tái dùng `ApiKeyEndpointFilter` — không thiết kế auth mới |
| STEP-3.1: Code 2 endpoint + unit test | Senior Developer | **2–3 giờ** | Implement 2 endpoint, tái sử dụng `AdbService.ConnectAsync` + `DeviceState` + `ApiKeyEndpointFilter` — không viết logic ADB mới, không viết filter auth mới. Viết unit test cho cả 2 endpoint |
| STEP-3.2: Code review + security-audit-stride | Tech Lead | **1 giờ** | Review PR + chạy security-audit-stride bắt buộc (route mới — surface attack mới dù tái dùng auth cũ). Block merge nếu Fail rủi ro cao |
| STEP-4.1: Test plan + thực thi | QA Engineer | **1–2 giờ** | 14 scenario Given/When/Then từ US đã có sẵn làm test basis; chủ yếu thực thi + log kết quả |
| STEP-4.2: Sign-off | QA Lead | **30 phút** | Review kết quả test, sign-off nếu không còn P0/P1 bug |
| STEP-4.3 + 4.4: Deploy staging → production | DevOps Engineer + Lead | **30–60 phút** | Restart service, smoke test, approve |
| **Tổng** | | **~5–8 giờ** | **Khoảng 1 ngày làm việc cho toàn chain kỹ thuật — thấp hơn plan mẫu nhờ tái sử dụng auth có sẵn** |

---

## 5. Điều kiện bắt buộc trước khi bắt đầu Phase kỹ thuật

- [ ] Tech Lead đọc `docs/prd/PRD-adb-add-device-api.md` + `docs/user-stories/US-adb-add-device-api.md` trước khi viết TDD
- [ ] Tech Lead chốt 2 assumption mở trong TDD: (1) mã HTTP trả về khi serial NotFound (`404` hay `200` với body `NotFound`?), (2) `serial` thiết bị có được trả về trong response `connect-by-ip` không
- [ ] Senior Developer đọc TDD đầy đủ trước khi code; không tự quyết định các điểm chưa chốt
- [ ] Bước STEP-3.2 BẮT BUỘC chạy `security-audit-stride` — route mới tạo surface attack mới dù tái dùng auth cũ (theo CLAUDE.md §4 WF-FEATURE Bước 10a)

---

## 6. Approve

Engineering Manager xác nhận:
- Priority P1 đã duyệt
- Senior Developer phụ trách toàn bộ code (Junior skip)
- Estimate thực tế ~5–8 giờ (~1 ngày làm việc) — không escalate thêm resource
- Rủi ro thấp hơn plan mẫu (auth tái dùng nguyên trạng) — không cần escalate lên CTO trừ khi Tech Lead phát hiện vấn đề kiến trúc ở TDD
