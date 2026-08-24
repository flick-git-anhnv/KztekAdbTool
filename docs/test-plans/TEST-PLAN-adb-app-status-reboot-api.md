# TEST-PLAN-adb-app-status-reboot-api

## 1. Thông tin chung

| Trường | Giá trị |
|--------|---------|
| Feature | API Kiểm tra trạng thái ứng dụng + Reboot thiết bị Android |
| Plan liên quan | `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/PLAN-MASTER.md` |
| TDD tham chiếu | `docs/tech-design/TDD-adb-app-status-reboot-api.md` |
| Người thực hiện | QA Engineer |
| Ngày | 2026-08-24 |
| Bước | STEP-4.1 (Phase 4 — Kiểm thử) |

---

## 2. Scope

### 2.1 Trong scope

| Hạng mục | Mô tả |
|----------|-------|
| API 1 | `GET /api/devices/{serial}/app-status?package={pkg}` — 7 TC |
| API 2 | `POST /api/devices/{serial}/reboot` — 4 TC |
| UI smoke | 2 nút bấm mới trên dashboard (đã cover bởi UXR STEP-3.3 Playwright) — 3 TC |
| Unit test | 22 test mới trong 3 class (`AppStatusEndpointsTests`, `RebootEndpointsTests`, `AdbServiceAppStatusTests`) |
| Regression | 119/119 test tổng (bao gồm tất cả test hiện có) |

### 2.2 Ngoài scope

- Test trên thiết bị Android thật (xem §5 Giới hạn môi trường)
- Test `mResumedActivity` fallback trên ROM Android < 8 cụ thể
- Test WebSocket / SignalR log streaming khi API được gọi
- Test performance / load
- Test `adb reboot bootloader` / `adb reboot recovery` (NG3)

---

## 3. Chiến lược test (Test Strategy)

### 3.1 Lớp 1 — HTTP Integration (curl)

Chạy thật app (`dotnet run --urls http://localhost:5099`) + gọi API bằng `curl`.

**TC thực thi bằng curl** (không cần thiết bị Android):
- TC-A04 (404 serial không tồn tại)
- TC-A05 (400 thiếu query param `package`)
- TC-A06 (401 API key sai/thiếu)
- TC-B02 (404 serial không tồn tại)
- TC-B03 (401 API key sai/thiếu)

### 3.2 Lớp 2 — Unit Test (dotnet test)

Xác nhận coverage contract theo error matrix TDD §9 cho các kịch bản không thể kiểm qua HTTP thật vì không có thiết bị:
- TC-A01 (Foreground): `MapAppStatusResult_Foreground_Returns200`
- TC-A02 (Background): `MapAppStatusResult_Background_Returns200`
- TC-A03 (NotRunning): `MapAppStatusResult_NotRunning_Returns200`
- TC-A07 (DeviceOffline): `CheckDeviceState_OfflineDevice_Returns422` (AppStatusEndpointsTests)
- TC-B01 (RebootInitiated): `MapRebootResult_Success_Returns200`
- TC-B04 (DeviceOffline): `CheckDeviceState_OfflineDevice_Returns422` (RebootEndpointsTests)
- Cộng thêm: `IsForegroundInDumpsys` (5 case: mResumedActivity match, không match, mFocusedActivity fallback Android 6, cả hai không có, empty stdout)

### 3.3 Lớp 3 — UI Smoke (UXR STEP-3.3)

Kịch bản UI đã được UX/UI Reviewer thực thi bằng Playwright (xem `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md`):
- TC-C01: Click "Kiểm tra trạng thái app" → kết quả hiển thị
- TC-C02: Click "Khởi động lại thiết bị" → confirm → gọi API
- TC-C03: Click "Khởi động lại thiết bị" → cancel → API không gọi

---

## 4. Môi trường test

| Tham số | Giá trị |
|---------|---------|
| OS | Linux 6.6.87.1-microsoft-standard-WSL2 |
| Runtime | .NET 8.0, ASP.NET Core 8 |
| App URL | `http://localhost:5099` |
| API Key | `x-api-key: 123456a@` (từ `appsettings.json`) |
| Test tool (HTTP) | `curl` |
| Test tool (Unit) | `dotnet test` |
| Browser | N/A (UI smoke delegate UXR STEP-3.3) |
| Build | Nhánh `docker-deploy`, commit sau STEP-3.3 fix |

---

## 5. Giới hạn môi trường (QUAN TRỌNG)

**Không có thiết bị Android thật hoặc emulator** trong môi trường test hiện tại (`adb devices` → `List of devices attached` — trống).

Hệ quả:
- TC-A01, TC-A02, TC-A03 (happy path app Foreground/Background/NotRunning thật) **không thể thực thi qua HTTP** — xác nhận gián tiếp qua unit test.
- TC-A07, TC-B04 (DeviceOffline qua HTTP) **không thể dựng scenario** vì DeviceState in-memory trống — xác nhận gián tiếp qua unit test.
- TC-B01 (reboot thật trả `RebootInitiated`) **không thể thực thi qua HTTP** — xác nhận gián tiếp qua unit test.
- **TC-B01 cần thiết bị thật**: KHÔNG reboot bất kỳ thiết bị nào không rõ chủ sở hữu.

---

## 6. Definition of Done

- [x] Tất cả TC có kết quả rõ ràng: Pass / Blocked (với lý do) / Fail
- [x] TC HTTP integration: có curl output thật (body + HTTP code)
- [x] TC unit test: có tên test cụ thể đã pass từ `dotnet test`
- [x] Regression: 119/119 pass — xác nhận không có regression
- [x] TC UI smoke: reference UXR STEP-3.3 PASS
- [x] Không phát hiện bug hợp đồng API (response contract khớp TDD §3 và error matrix §9)
- [x] Xuất DOCX

---

## 7. Tiêu chí Pass / Fail

| Mức | Điều kiện |
|-----|-----------|
| Pass | HTTP code đúng + response body khớp contract TDD; HOẶC unit test xanh cho case đó |
| Blocked | TC cần thiết bị Android thật và không có thiết bị khả dụng |
| Fail | HTTP code sai / response body sai / unit test đỏ |

---

## 8. Bug severity reference

| Severity | Ví dụ |
|----------|-------|
| P0/Critical | API trả sai HTTP code so với contract (404 nhưng expect 200) |
| P1/High | API trả đúng code nhưng schema thiếu field bắt buộc |
| P2/Medium | Lỗi corner case không ảnh hưởng happy path |
| P3/Low | Typo trong message, log format nhỏ |
