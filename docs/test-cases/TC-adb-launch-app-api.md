---
id: TC-adb-launch-app-api
feature: adb-launch-app-api
author: QA Engineer
created: 2026-08-18
executed_at: 2026-08-18 17:00
environment: Local WSL2 — http://localhost:5299
account: x-api-key = 123456a@ (test key local, không commit)
build: 2026-08-18 (STEP-3.1 commit)
---

# Test Cases: API Launch App Android qua ADB

> Test Plan: `docs/test-plans/TEST-PLAN-adb-launch-app-api.md`
> User Story: `docs/user-stories/US-adb-launch-app-api.md`
> Endpoint: `POST /api/launch-app`

---

## Phân nhóm test

- **NHÓM A** — Test qua HTTP thật (server local, curl): TC-002, TC-003, TC-004, TC-005
- **NHÓM B** — Verify gián tiếp qua unit test (cần thiết bị Android/emulator, môi trường này không có): TC-001, TC-006, TC-007

---

## TC-001: Happy path — Launch app thành công (NHÓM B)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-01 |
| Severity | Critical |
| Nhóm | B — cần thiết bị Android thật |

**Điều kiện tiên quyết:**
- Thiết bị Android online, serial đã có trong DeviceState với Status="Online"
- App `com.kztek.abc` đã được cài trên thiết bị
- API key đúng

**Các bước:**
1. POST `http://<server>/api/launch-app` với header `x-api-key: <key>`
2. Body: `{"serial":"<serial-online>","app":"com.kztek.abc"}`

**Kết quả mong đợi:**
- HTTP 200
- Body: `{"success":true,"message":"App launched successfully.","serial":"...","app":"com.kztek.abc","exitCode":0,"stdOut":"Starting: Intent...","stdErr":""}`

**Kết quả thực tế:** KHÔNG THỂ verify qua HTTP thật trong môi trường này do thiếu thiết bị Android/emulator.

**Verify gián tiếp qua unit test:**
- `MapAdbResult_ExitCode0_Returns200` — gọi `LaunchAppEndpoints.MapAdbResult` với `AdbCommandResult { ExitCode=0, Success=true }` → Assert 200 **PASS**
- `CheckDeviceState_OnlineDevice_ReturnsNull` — device Online → null (handler tiếp tục gọi ADB) **PASS**

**Trạng thái:** Pass (gián tiếp qua unit test) — **cần smoke test thủ công trên môi trường có thiết bị thật trước khi DevOps deploy production (STEP-4.3/4.4)**

---

## TC-002: API key sai → 401 (NHÓM A)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-02 |
| Severity | Critical |
| Nhóm | A — HTTP thật |

**Các bước:**
```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: wrong-key-999" \
  -d '{"serial":"192.168.1.100:5555","app":"com.kztek.abc"}'
```

**Kết quả mong đợi:** HTTP 401, body có `"error":"Unauthorized"`

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 401
Body: {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Xác nhận:**
- [x] HTTP status = 401
- [x] Body `error` = "Unauthorized"
- [x] AdbService KHÔNG được gọi (xác nhận bởi unit test `InvokeAsync_WrongKey_Returns401_AndDoesNotCallNext` — PASS)

**Trạng thái:** PASS

---

## TC-003: Thiếu header x-api-key → 401 (NHÓM A)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-02 |
| Severity | Critical |
| Nhóm | A — HTTP thật |

**Các bước:**
```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -d '{"serial":"192.168.1.100:5555","app":"com.kztek.abc"}'
```

**Kết quả mong đợi:** HTTP 401, body có `"error":"Unauthorized"`

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 401
Body: {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**Xác nhận:**
- [x] HTTP status = 401
- [x] Body `error` = "Unauthorized"
- [x] AdbService KHÔNG được gọi (unit test `InvokeAsync_MissingHeader_Returns401_AndDoesNotCallNext` — PASS)

**Trạng thái:** PASS

---

## TC-004: Serial không tồn tại trong DeviceState → 404 (NHÓM A)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-03 |
| Severity | High |
| Nhóm | A — HTTP thật |

**Ghi chú:** Vì môi trường không có thiết bị nào kết nối, `DeviceState` luôn rỗng — mọi serial đều trả 404. Đây là evidence hợp lệ cho TC-004.

**Các bước:**
```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: 123456a@" \
  -d '{"serial":"TEST-SERIAL-NOT-EXIST","app":"com.kztek.abc"}'
```

**Kết quả mong đợi:** HTTP 404, body có `"error":"DeviceNotFound"` và message chứa serial

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 404
Body: {"success":false,"error":"DeviceNotFound","message":"Device 'TEST-SERIAL-NOT-EXIST' not found."}
```

**Xác nhận:**
- [x] HTTP status = 404
- [x] Body `error` = "DeviceNotFound"
- [x] Message chứa serial đã gửi
- [x] AdbService KHÔNG được gọi (unit test `CheckDeviceState_UnknownSerial_Returns404` — PASS)

**Trạng thái:** PASS

---

## TC-005: app trống hoặc package name không hợp lệ → 400 (NHÓM A)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-04, EC2, EC6, BR1 |
| Severity | High |
| Nhóm | A — HTTP thật |

### TC-005a: app rỗng (`""`)

**Các bước:**
```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: 123456a@" \
  -d '{"serial":"TEST-SERIAL","app":""}'
```

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 400
Body: {"success":false,"error":"InvalidInput","message":"app is required"}
```
**Trạng thái:** PASS

### TC-005b: Package name không có dấu chấm (`"invalidpackage"`)

```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: 123456a@" \
  -d '{"serial":"TEST-SERIAL","app":"invalidpackage"}'
```

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 400
Body: {"success":false,"error":"InvalidInput","message":"invalid package name"}
```
**Trạng thái:** PASS

### TC-005c: app chỉ có khoảng trắng (`"   "`) — EC2

```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: 123456a@" \
  -d '{"serial":"TEST-SERIAL","app":"   "}'
```

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 400
Body: {"success":false,"error":"InvalidInput","message":"app is required"}
```
**Trạng thái:** PASS (trim → rỗng → "app is required")

**Trạng thái TC-005 tổng:** PASS (3/3 sub-case)

---

## TC-006: Thiết bị tồn tại nhưng Offline → 422 (NHÓM B)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-05 |
| Severity | High |
| Nhóm | B — cần thiết bị Android thật |

**Kết quả thực tế:** KHÔNG THỂ verify qua HTTP thật.

**Verify gián tiếp qua unit test:**
- `CheckDeviceState_OfflineDevice_Returns422` — device với `Status="Offline"` trong DeviceState → Assert 422 **PASS**

Test inject trực tiếp `DeviceRecord { Serial="192.168.1.100:5555", Status="Offline" }` vào `DeviceState`, gọi `CheckDeviceState` → kết quả `IStatusCodeHttpResult` với StatusCode=422.

**Trạng thái:** Pass (gián tiếp qua unit test) — **cần smoke test thủ công trước production**

---

## TC-007: Package hợp lệ nhưng app không cài trên thiết bị → 422 (NHÓM B)

| Mục | Giá trị |
|---|---|
| AC tham chiếu | SC-06 |
| Severity | High |
| Nhóm | B — cần thiết bị Android thật |

**Kết quả thực tế:** KHÔNG THỂ verify qua HTTP thật.

**Verify gián tiếp qua unit test:**
- `MapAdbResult_AppNotInstalled_Returns422` — `AdbCommandResult { ExitCode=-1, StdErr="No activities found to run" }` → Assert 422 **PASS**

Test gọi `LaunchAppEndpoints.MapAdbResult` với output giả lập đúng schema thật của `AdbService.LaunchAppAsync` khi `resolve-activity` không tìm thấy component.

**Trạng thái:** Pass (gián tiếp qua unit test) — **cần smoke test thủ công trước production**

---

## Kiểm tra bổ sung

### SC-07 Regression: Route cũ không bị ảnh hưởng

```
curl http://localhost:5299/api/devices
```

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 200
Body: {"ok":true,"data":[]}
```

**Trạng thái:** PASS — auth filter KHÔNG áp cho route cũ (BR3 tuân thủ)

### EC6: Serial rỗng → 400

```
curl -X POST http://localhost:5299/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: 123456a@" \
  -d '{"serial":"","app":"com.kztek.abc"}'
```

**Kết quả thực tế (2026-08-18 17:00):**
```
HTTP_STATUS: 400
Body: {"success":false,"error":"InvalidInput","message":"serial is required"}
```

**Trạng thái:** PASS

---

## Tổng kết kết quả

| TC | Mô tả | Nhóm | Kết quả |
|---|---|---|---|
| TC-001 | Happy path 200 | B | Pass (gián tiếp — unit: `MapAdbResult_ExitCode0_Returns200`) |
| TC-002 | API key sai → 401 | A | **PASS** (HTTP thật) |
| TC-003 | Thiếu header → 401 | A | **PASS** (HTTP thật) |
| TC-004 | Serial không tồn tại → 404 | A | **PASS** (HTTP thật) |
| TC-005 | Package invalid/rỗng → 400 | A | **PASS** (HTTP thật, 3 sub-case) |
| TC-006 | Device Offline → 422 | B | Pass (gián tiếp — unit: `CheckDeviceState_OfflineDevice_Returns422`) |
| TC-007 | App không cài → 422 | B | Pass (gián tiếp — unit: `MapAdbResult_AppNotInstalled_Returns422`) |
| SC-07 Regression | Route cũ không trả 401 | A | **PASS** (HTTP thật) |
| EC6 | Serial rỗng → 400 | A | **PASS** (HTTP thật) |

**Bug phát hiện:** Không có

**Dotnet test sau hoàn thành:** 42/42 PASS

---

## Verification run

```
Verification run: 2026-08-18 17:00 — http://localhost:5299 (WSL2 local)
Test case cuối đã chạy: TC-005 + SC-07 regression + EC6 (cùng 1 session)
Kết quả: NHÓM A (4 case chính + regression): PASS — log curl trực tiếp trong file này
          NHÓM B (3 case): Pass gián tiếp — dotnet test 42/42 PASS
```

---

## Cảnh báo cho QA Lead và DevOps

**BẮTBUỘC trước khi deploy production:**
TC-001 (200 OK happy path), TC-006 (422 device offline), TC-007 (422 app not installed) chưa được verify qua HTTP thật do môi trường sandbox không có thiết bị Android. DevOps Lead (STEP-4.4) PHẢI thực hiện smoke test thủ công với thiết bị Android thật hoặc emulator trên môi trường staging trước khi promote production.
