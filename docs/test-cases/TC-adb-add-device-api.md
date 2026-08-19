---
id: TC-adb-add-device-api
feature: API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB
author: QA Engineer
created: 2026-08-19
status: executed
environment: http://localhost:5299 (dotnet run, no-build)
api_key_test: test_qa_key_4.1
branch: docker-deploy
commits: 3c5541b + 5254df7 + 84499e7
test_plan: docs/test-plans/TEST-PLAN-adb-add-device-api.md
---

# TEST CASES — API Thêm Thiết Bị (Connect-by-IP) và Kiểm Tra Kết Nối ADB

## Môi trường thực thi

- URL: `http://localhost:5299`
- Lệnh khởi động: `LaunchApp__ApiKey=test_qa_key_4.1 dotnet run --project src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj --no-build --urls http://localhost:5299`
- Thời gian thực thi: 2026-08-19 10:42:56 – 10:43:22
- Xác nhận startup: `curl http://localhost:5299/` → HTTP 200
- Unit test đã có: 64/64 PASS (commit `3c5541b`)

---

## TEST DATA

| Loại | Giá trị | Ghi chú |
|------|---------|---------|
| API Key hợp lệ | `test_qa_key_4.1` | Env var `LaunchApp__ApiKey` |
| API Key sai | `wrong_key` / `wrongkey123` | Dùng để test 401 |
| IP hợp lệ (test) | `192.168.1.100` | IP không tồn tại trong mạng test — chỉ verify validation |
| Serial WiFi test | `192.168.1.100:5555` (URL-encoded: `192.168.1.100%3A5555`) | NotFound trong DeviceState |
| Serial USB test | `emulator-5554` | NotFound trong DeviceState |

---

## NHOM A — Verify qua HTTP thật (không cần thiết bị)

### TC-A01: Thiếu x-api-key — connect-by-ip → 401

**Severity:** Critical | **Priority:** P0

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json

{"ip":"192.168.1.100"}
```

**Response thực tế:**
```
HTTP/1.1 401 Unauthorized
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Kết quả: PASS** — status 401, error="Unauthorized" khớp TDD.

---

### TC-A02: Sai x-api-key — connect-by-ip → 401

**Severity:** Critical | **Priority:** P0

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: wrong_key

{"ip":"192.168.1.100"}
```

**Response thực tế:**
```
HTTP/1.1 401 Unauthorized
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Kết quả: PASS** — status 401 khớp TDD.

---

### TC-A03: ip rỗng (empty string) → 400

**Severity:** High | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":""}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"ip là bắt buộc"}
```

**Kết quả: PASS** — status 400, message "ip là bắt buộc" khớp TDD.

---

### TC-A04: ip null (không truyền field) → 400

**Severity:** High | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"port":5555}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"ip là bắt buộc"}
```

**Kết quả: PASS** — null ip treated as empty, trả 400 đúng.

---

### TC-A05: ip chứa ':' (client nhầm truyền ipPort) → 400

**Severity:** High | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100:5555"}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"ip không được chứa ':' — dùng field port riêng"}
```

**Kết quả: PASS** — đúng message theo TDD/SC-A4.

---

### TC-A06: ip chứa khoảng trắng nội bộ → 400

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1. 100"}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"ip không được chứa khoảng trắng"}
```

**Kết quả: PASS** — whitespace nội bộ bị chặn đúng.

---

### TC-A07: port = 0 (dưới range) → 400

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100","port":0}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"port phải nằm trong khoảng 1–65535"}
```

**Kết quả: PASS** — boundary 0 bị từ chối.

---

### TC-A08: port = 65536 (trên range) → 400

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100","port":65536}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"port phải nằm trong khoảng 1–65535"}
```

**Kết quả: PASS** — boundary 65536 bị từ chối.

---

### TC-A09: port = -1 (âm) → 400

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100","port":-1}
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"port phải nằm trong khoảng 1–65535"}
```

**Kết quả: PASS** — port âm bị từ chối.

---

### TC-A10: IP hợp lệ nhưng ADB binary không có → 500

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"10.255.255.1","port":5555}
```

**Response thực tế:**
```
HTTP/1.1 500 Internal Server Error
{"success":false,"error":"AdbNotFound","message":"adb binary not found on server."}
```

**Kết quả: PASS** — validation pass → ADB layer → AdbNotFound 500 (môi trường không có binary `/opt/platform-tools/adb`). Trong môi trường production có ADB binary, case này với IP không tồn tại sẽ trả 422 AdbConnectFailed sau timeout ~10s.

---

### TC-B01: Thiếu x-api-key — status → 401

**Severity:** Critical | **Priority:** P0

**Request:**
```
GET /api/devices/192.168.1.100%3A5555/status HTTP/1.1
```

**Response thực tế:**
```
HTTP/1.1 401 Unauthorized
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Kết quả: PASS** — status 401 khớp TDD.

---

### TC-B02: Sai x-api-key — status → 401

**Severity:** Critical | **Priority:** P0

**Request:**
```
GET /api/devices/192.168.1.100%3A5555/status HTTP/1.1
x-api-key: wrongkey123
```

**Response thực tế:**
```
HTTP/1.1 401 Unauthorized
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Kết quả: PASS** — status 401 khớp TDD.

---

### TC-B03: Serial WiFi không tồn tại trong DeviceState → 404

**Severity:** High | **Priority:** P1

**Request:**
```
GET /api/devices/192.168.1.100%3A5555/status HTTP/1.1
x-api-key: test_qa_key_4.1
```

**Response thực tế:**
```
HTTP/1.1 404 Not Found
{"success":false,"error":"DeviceNotFound","message":"Device '192.168.1.100:5555' not found."}
```

**Kết quả: PASS** — URL-decode `:` hoạt động đúng, serial được decode thành `192.168.1.100:5555`, DeviceState trả NotFound → 404 đúng. Message chứa đúng serial.

---

### TC-B04: Serial USB không tồn tại → 404

**Severity:** High | **Priority:** P1

**Request:**
```
GET /api/devices/emulator-5554/status HTTP/1.1
x-api-key: test_qa_key_4.1
```

**Response thực tế:**
```
HTTP/1.1 404 Not Found
{"success":false,"error":"DeviceNotFound","message":"Device 'emulator-5554' not found."}
```

**Kết quả: PASS** — 404 đúng với serial USB không tồn tại.

---

### TC-B05: Serial rỗng (empty path segment) → router 404

**Severity:** Low | **Priority:** P2

**Request:**
```
GET /api/devices//status HTTP/1.1
x-api-key: test_qa_key_4.1
```

**Response thực tế:**
```
HTTP/1.1 404 Not Found
(empty body — ASP.NET Core router default 404)
```

**Kết quả: PASS** — Router 404 đúng theo TDD Q6: route `{serial}` yêu cầu non-empty segment, router không match → app layer không chạy tới.

---

### TC-B06: Serial = khoảng trắng URL-encoded (%20) → 400

**Severity:** Low | **Priority:** P2

**Request:**
```
GET /api/devices/%20/status HTTP/1.1
x-api-key: test_qa_key_4.1
```

**Response thực tế:**
```
HTTP/1.1 400 Bad Request
{"success":false,"error":"InvalidInput","message":"serial không được rỗng"}
```

**Kết quả: PASS** — %20 decode thành khoảng trắng, sau trim() → empty → 400 đúng theo TDD Q6 edge case.

---

### TC-B07: port = 1 (boundary min hợp lệ) → vượt validation, đến ADB

**Severity:** Low | **Priority:** P2

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100","port":1}
```

**Response thực tế:**
```
HTTP/1.1 500 Internal Server Error
{"success":false,"error":"AdbNotFound","message":"adb binary not found on server."}
```

**Kết quả: PASS** — port=1 hợp lệ (trong range 1-65535), vượt qua validation, đến ADB layer → AdbNotFound (môi trường không có binary).

---

### TC-B08: port = 65535 (boundary max hợp lệ) → vượt validation, đến ADB

**Severity:** Low | **Priority:** P2

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100","port":65535}
```

**Response thực tế:**
```
HTTP/1.1 500 Internal Server Error
{"success":false,"error":"AdbNotFound","message":"adb binary not found on server."}
```

**Kết quả: PASS** — port=65535 hợp lệ, vượt validation, đến ADB layer.

---

### TC-B09: Không truyền port → mặc định 5555

**Severity:** Medium | **Priority:** P1

**Request:**
```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: test_qa_key_4.1

{"ip":"192.168.1.100"}
```

**Response thực tế:**
```
HTTP/1.1 500 Internal Server Error
{"success":false,"error":"AdbNotFound","message":"adb binary not found on server."}
```

**Kết quả: PASS** — ip hợp lệ, port null → default 5555, target = "192.168.1.100:5555", validation PASS, đến ADB → AdbNotFound. Port defaulting xác nhận qua unit test TC-3.1 (64/64 PASS).

---

## NHOM B — Cần thiết bị thật (smoke test thủ công tại staging/production)

Các case này KHÔNG thể tự động hóa trong môi trường CI do cần thiết bị Android WiFi thật.

| ID | Mô tả | Điều kiện yêu cầu | Verify gián tiếp qua |
|----|-------|------------------|---------------------|
| TC-C01 | Connect thành công, nhận 200 + serial | Thiết bị ADB WiFi on | Unit test mock ADB (64/64 PASS) |
| TC-C02 | Connect với port tùy chỉnh | Thiết bị ADB port ≠ 5555 | Unit test mock ADB (64/64 PASS) |
| TC-C03 | Status Online sau khi connect | TC-C01 trước, đợi poll (~200ms sau trigger) | Unit test mock DeviceState (64/64 PASS) |
| TC-C04 | Status Offline khi thiết bị disconnect | Sau TC-C01, ngắt WiFi | Unit test mock DeviceState (64/64 PASS) |
| TC-C05 | Idempotent: connect 2 lần cùng IP → 200 cả 2 lần | TC-C01 trước | Unit test SC-A7 (64/64 PASS) |
| TC-C06 | 422 AdbConnectFailed khi IP không phản hồi (có ADB binary) | Container có binary, IP không tồn tại | Unit test mock ADB ExitCode≠0 (64/64 PASS) |

**Hành động yêu cầu từ DevOps (STEP-4.3/4.4):** Chạy TC-C01, TC-C03, TC-C05 tối thiểu trước khi go-live production. Ghi kết quả vào `docs/devops/DEPLOY-adb-add-device-api.md`.

---

## Tóm tắt kết quả

| Nhóm | Tổng case | PASS (HTTP thật) | Cần smoke test thủ công |
|------|-----------|-----------------|------------------------|
| NHOM A (Auth 401) | 4 | 4 | 0 |
| NHOM A (Validation 400) | 7 | 7 | 0 |
| NHOM A (ADB layer 422/500) | 4 | 4 | 0 |
| NHOM A (Status 404/400) | 4 | 4 | 0 |
| **NHOM A Tổng** | **19** | **19** | **0** |
| NHOM B (cần thiết bị) | 6 | 0 (verify gián tiếp) | 6 |
| **Tổng cộng** | **25** | **19** | **6** |

**Không có bug P0/P1 được phát hiện.** Code path tầng HTTP khớp 100% TDD specification.

---

## Bugs phát hiện

Không có bug. Tất cả case NHOM A PASS đúng expected behavior theo TDD.

---

## Verification Gate (CLAUDE.md §QA Engineer)

```
Verification run: 2026-08-19 10:42:56 — http://localhost:5299
  (LaunchApp__ApiKey=test_qa_key_4.1, dotnet run --no-build, branch docker-deploy commit 3c5541b+5254df7+84499e7)
Test case cuối đã chạy: TC-B09 — port default 5555
Kết quả: 19/19 PASS (NHOM A) | 6 case NHOM B cần smoke test thủ công tại staging
```

---
*Tài liệu tạo bởi QA Engineer — 2026-08-19*
