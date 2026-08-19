---
id: TEST-PLAN-adb-add-device-api
feature: API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB
author: QA Engineer
created: 2026-08-19
status: executed
related_tdd: docs/tech-design/TDD-adb-add-device-api.md
related_us: docs/user-stories/US-adb-add-device-api.md
---

# TEST PLAN — API Thêm Thiết Bị (Connect-by-IP) và Kiểm Tra Kết Nối ADB

## 1. Phạm vi

Kiểm thử 2 HTTP endpoint mới tại `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs`:

1. `POST /api/devices/connect-by-ip` — kết nối thiết bị WiFi ADB theo IP+port
2. `GET /api/devices/{serial}/status` — kiểm tra trạng thái kết nối ADB của 1 thiết bị

Unit test tầng code: 64/64 PASS (STEP-3.1 — Senior Developer, commit `3c5541b`). Không lặp lại ở đây.
Test plan này tập trung vào tầng **HTTP/integration thật** (chạy app thật + HTTP client).

## 2. Môi trường

| Thành phần | Giá trị |
|---|---|
| App | `KztekAdbPublishTool.Web` — `dotnet run --urls http://localhost:5299 --no-build` |
| Branch | `docker-deploy`, commit `3c5541b` + `5254df7` + `84499e7` |
| API Key test | `test_qa_key_4.1` (env `LaunchApp__ApiKey`) |
| API Key production | `sup3rsecr3tap1key@` (docker-compose) — KHÔNG dùng cho test |
| ADB binary | Không có trong môi trường CI/local dev (path `/opt/platform-tools/adb` chỉ tồn tại trong container) |
| Thiết bị Android | Không có — NHOM B cần thiết bị thật, thực hiện tại staging/production |

## 3. Phân nhóm Test Case

### NHOM A — Verify được qua HTTP thật, không cần thiết bị

Gồm: validation input (ip rỗng/invalid, port ngoài range), xác thực `x-api-key` (thiếu/sai → 401), serial NotFound → 404, connect IP không tồn tại (ADB binary missing → 500, không có thiết bị → 422).

**Có thể chạy tự động trong CI.**

| ID | Mô tả ngắn | Endpoint | Expected |
|----|------------|---------|----------|
| TC-A01 | Thiếu x-api-key → connect-by-ip | POST /api/devices/connect-by-ip | 401 |
| TC-A02 | Sai x-api-key → connect-by-ip | POST /api/devices/connect-by-ip | 401 |
| TC-A03 | ip rỗng → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A04 | ip null (không truyền) → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A05 | ip chứa ':' → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A06 | ip chứa khoảng trắng nội bộ → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A07 | port = 0 → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A08 | port = 65536 → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A09 | port = -1 → 400 | POST /api/devices/connect-by-ip | 400 |
| TC-A10 | IP hợp lệ, connect IP không tồn tại → 500/422 tùy ADB | POST /api/devices/connect-by-ip | 500 (no binary) / 422 (ADB fail) |
| TC-B01 | Thiếu x-api-key → status | GET /api/devices/{serial}/status | 401 |
| TC-B02 | Sai x-api-key → status | GET /api/devices/{serial}/status | 401 |
| TC-B03 | Serial WiFi không tồn tại trong DeviceState → 404 | GET /api/devices/{serial}/status | 404 |
| TC-B04 | Serial USB không tồn tại trong DeviceState → 404 | GET /api/devices/{serial}/status | 404 |
| TC-B05 | Serial rỗng (empty path segment) → router 404 | GET /api/devices//status | 404 (router) |
| TC-B06 | Serial = khoảng trắng URL-encoded (%20) → 400 | GET /api/devices/%20/status | 400 |
| TC-B07 | port = 1 (boundary min hợp lệ) → vượt validation, đến ADB | POST /api/devices/connect-by-ip | 500/422 (ADB layer) |
| TC-B08 | port = 65535 (boundary max hợp lệ) → vượt validation, đến ADB | POST /api/devices/connect-by-ip | 500/422 (ADB layer) |
| TC-B09 | Không truyền port → mặc định 5555, vượt validation, đến ADB | POST /api/devices/connect-by-ip | 500/422 (ADB layer) |

### NHOM B — Cần thiết bị Android thật hoặc emulator

Thực hiện tại staging trước go-live production (STEP-4.3/4.4 — DevOps).

| ID | Mô tả ngắn | Điều kiện | Expected |
|----|------------|-----------|----------|
| TC-C01 | connect-by-ip thành công (SC-A1) | IP thật, thiết bị ADB WiFi on | 200 `{success:true, serial:"ip:5555", ...}` |
| TC-C02 | connect-by-ip với port tùy chỉnh (SC-A2) | IP thật, port ADB khác 5555 | 200 `{success:true, serial:"ip:port", ...}` |
| TC-C03 | status → Online sau khi connect (SC-B1) | Sau TC-C01, gọi GET status | 200 `{status:"Online"}` |
| TC-C04 | status → Offline khi thiết bị disconnect (SC-B2) | Sau TC-C01, rút dây/tắt ADB WiFi | 200 `{status:"Offline"}` |
| TC-C05 | connect idempotent (SC-A7) | Gọi connect-by-ip 2 lần cùng IP | 200 cả 2 lần ("already connected" vẫn success) |

## 4. Chiến lược Test

- **NHOM A:** Thực thi 100% bằng HTTP thật (curl) trong CI/local. Kết quả ghi log đầy đủ request+response.
- **NHOM B:** Thực thi thủ công tại staging (DevOps STEP-4.3) hoặc trước go-live. Ghi kết quả vào `docs/devops/DEPLOY-adb-add-device-api.md`.
- **Regression:** Smoke test endpoint `/api/launch-app` sau khi deploy để đảm bảo route cũ không bị ảnh hưởng.

## 5. Tiêu chí Pass/Fail

| Level | Tiêu chí |
|-------|---------|
| PASS | Status code đúng theo TDD, body JSON đúng schema, error field đúng theo TDD |
| FAIL | Sai status code, sai error field, thiếu field bắt buộc trong response |
| BLOCK | Không chạy được app (lỗi startup), ADB binary cần thiết nhưng không có |

## 6. Giới hạn môi trường đã biết

1. **ADB binary không có trong môi trường dev/CI**: `adb` binary (`/opt/platform-tools/adb`) chỉ tồn tại trong Docker container production. Mọi request hợp lệ vượt qua validation và đến tầng ADB sẽ nhận `500 AdbNotFound`. Đây là hành vi ĐÚNG (code path xử lý ADB missing đã được implement và unit test cover — xem commit `3c5541b`).

2. **Không có thiết bị Android thật**: TC-C01–TC-C05 (NHOM B) cần thiết bị thật. Được verify gián tiếp qua 64/64 unit test PASS (mock ADB + mock DeviceState).

3. **DeviceState trong môi trường local trống**: Không có DevicePollWorker cập nhật cache → mọi serial đều `NotFound`. Đây là điều kiện đúng cho TC-B03, TC-B04.

---
*Tài liệu tạo bởi QA Engineer — 2026-08-19*
