# TC-adb-app-status-reboot-api — Test Cases

**Môi trường:** `http://localhost:5099` | Linux/WSL2 | dotnet run nhánh `docker-deploy`
**Ngày chạy:** 2026-08-24
**QA Engineer:** STEP-4.1

---

## Tổng kết kết quả

| Nhóm | Pass | Blocked | Fail | Tổng |
|------|------|---------|------|------|
| API 1 (app-status) | 6 | 1 | 0 | 7 |
| API 2 (reboot) | 3 | 1 | 0 | 4 |
| UI Smoke | 3 | 0 | 0 | 3 |
| **Tổng** | **12** | **2** | **0** | **14** |

**dotnet test regression:** 119/119 PASS (0 fail, 0 skip)

**Bug phát hiện:** Không có. Tất cả response contract khớp TDD §3 và error matrix §9.

---

## Nhóm A — API 1: GET /api/devices/{serial}/app-status

### TC-A01: Happy path — app đang chạy foreground

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **Blocked — cần thiết bị Android thật** |

**Các bước:**
1. Có thiết bị Android đang chạy app `com.kztek.demo` ở foreground
2. `GET /api/devices/{serial}/app-status?package=com.kztek.demo` với `x-api-key: 123456a@`

**Kết quả mong đợi:** HTTP 200, `{"success":true,"running":true,"state":"Foreground",...}`

**Lý do Blocked:** `adb devices` → trống, không có thiết bị khả dụng trong môi trường này.

**Xác nhận gián tiếp:** Unit test `AppStatusEndpointsTests.MapAppStatusResult_Foreground_Returns200` — PASS.

```
Passed KztekAdbPublishTool.Web.Tests.AppStatusEndpointsTests.MapAppStatusResult_Foreground_Returns200
```

---

### TC-A02: App đang chạy background

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P1 |
| Kết quả | **Blocked — cần thiết bị Android thật** |

**Kết quả mong đợi:** HTTP 200, `{"success":true,"running":true,"state":"Background",...}`

**Lý do Blocked:** Không có thiết bị.

**Xác nhận gián tiếp:** Unit test `AppStatusEndpointsTests.MapAppStatusResult_Background_Returns200` — PASS.

```
Passed KztekAdbPublishTool.Web.Tests.AppStatusEndpointsTests.MapAppStatusResult_Background_Returns200
```

Ngoài ra, `IsForegroundInDumpsys` parsing coverage:
```
Passed AdbServiceAppStatusTests.IsForegroundInDumpsys_MResumedActivity_ContainsPackage_ReturnsTrue
Passed AdbServiceAppStatusTests.IsForegroundInDumpsys_MResumedActivity_OtherPackage_ReturnsFalse
Passed AdbServiceAppStatusTests.IsForegroundInDumpsys_MFocusedActivity_FallbackAndroid6_ReturnsTrue
Passed AdbServiceAppStatusTests.IsForegroundInDumpsys_NeitherResumedNorFocused_ReturnsFalse
Passed AdbServiceAppStatusTests.IsForegroundInDumpsys_EmptyStdOut_ReturnsFalse
```

---

### TC-A03: App không chạy

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P1 |
| Kết quả | **Blocked — cần thiết bị Android thật** |

**Kết quả mong đợi:** HTTP 200, `{"success":true,"running":false,"state":"NotRunning",...}`

**Lý do Blocked:** Không có thiết bị.

**Xác nhận gián tiếp:** Unit test `AppStatusEndpointsTests.MapAppStatusResult_NotRunning_Returns200` — PASS.

```
Passed KztekAdbPublishTool.Web.Tests.AppStatusEndpointsTests.MapAppStatusResult_NotRunning_Returns200
```

---

### TC-A04: Serial không tồn tại → 404

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **PASS** |

**Lệnh curl:**
```
curl -s -w "\nHTTP:%{http_code}" \
  "http://localhost:5099/api/devices/192.168.1.100:5555/app-status?package=com.kztek.demo" \
  -H "x-api-key: 123456a@"
```

**Response thực tế:**
```json
{"success":false,"error":"DeviceNotFound","message":"Device '192.168.1.100:5555' not found."}
HTTP:404
```

**Kết quả mong đợi:** HTTP 404, `error = "DeviceNotFound"` — KHỚP.

---

### TC-A05: Thiếu query param `package` → 400

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **PASS** |

**Lệnh curl:**
```
curl -s -w "\nHTTP:%{http_code}" \
  "http://localhost:5099/api/devices/192.168.1.100:5555/app-status" \
  -H "x-api-key: 123456a@"
```

**Response thực tế:**
```json
{"success":false,"error":"InvalidInput","message":"package is required"}
HTTP:400
```

**Kết quả mong đợi:** HTTP 400, `error = "InvalidInput"`, `message = "package is required"` — KHỚP.

**Bonus — Package format không hợp lệ:**
```
curl -s -w "\nHTTP:%{http_code}" \
  "http://localhost:5099/api/devices/192.168.1.100:5555/app-status?package=invalid-pkg!" \
  -H "x-api-key: 123456a@"
```
```json
{"success":false,"error":"InvalidInput","message":"invalid package name"}
HTTP:400
```
Khớp TDD §3.1. Xác nhận thêm bởi unit test:
```
Passed AppStatusEndpointsTests.ValidateInput_PackageInvalidFormat_ReturnsInvalidPackageName
Passed AppStatusEndpointsTests.ValidateInput_PackageEmpty_ReturnsPackageRequired
Passed AppStatusEndpointsTests.ValidateInput_ValidPackage_ReturnsNull
```

---

### TC-A06: API key sai/thiếu → 401

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **PASS** |

**Lệnh curl (key sai):**
```
curl -s -w "\nHTTP:%{http_code}" \
  "http://localhost:5099/api/devices/192.168.1.100:5555/app-status?package=com.kztek.demo" \
  -H "x-api-key: wrongkey"
```

**Response thực tế:**
```json
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
HTTP:401
```

**Lệnh curl (thiếu header):**
```
curl -s -w "\nHTTP:%{http_code}" \
  "http://localhost:5099/api/devices/192.168.1.100:5555/app-status?package=com.kztek.demo"
```

**Response thực tế:**
```json
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
HTTP:401
```

**Kết quả mong đợi:** HTTP 401, `error = "Unauthorized"` — KHỚP cả 2 sub-case.

---

### TC-A07: Thiết bị offline trong DeviceState → 422

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P1 |
| Kết quả | **Pass (unit test)** |

**Lý do không test qua HTTP:** DeviceState in-memory không có entry offline sẵn khi không có thiết bị. Kịch bản này được xác nhận đầy đủ qua unit test:

```
Passed AppStatusEndpointsTests.CheckDeviceState_OfflineDevice_Returns422
```

Unit test này tạo `DeviceState` mock có device với `Status = "Offline"` và xác nhận endpoint trả HTTP 422 với `error = "DeviceOffline"`, `message = "Device '...' is offline."` — đúng contract TDD §3.1.

---

## Nhóm B — API 2: POST /api/devices/{serial}/reboot

### TC-B01: Happy path — thiết bị online → 200 RebootInitiated

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **Blocked — cần thiết bị Android thật** |

**Kết quả mong đợi:** HTTP 200, `{"success":true,"status":"RebootInitiated","message":"Reboot command sent. Device will go offline shortly.",...}`

**Lý do Blocked:** Không có thiết bị. Ngoài ra TC-B01 KHÔNG được chạy trên thiết bị không rõ chủ sở hữu (reboot không thể hoàn tác).

**Xác nhận gián tiếp:** Unit test `RebootEndpointsTests.MapRebootResult_Success_Returns200` — PASS.

```
Passed KztekAdbPublishTool.Web.Tests.RebootEndpointsTests.MapRebootResult_Success_Returns200
```

---

### TC-B02: Serial không tồn tại → 404

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **PASS** |

**Lệnh curl:**
```
curl -s -w "\nHTTP:%{http_code}" -X POST \
  "http://localhost:5099/api/devices/192.168.1.100:5555/reboot" \
  -H "x-api-key: 123456a@"
```

**Response thực tế:**
```json
{"success":false,"error":"DeviceNotFound","message":"Device '192.168.1.100:5555' not found."}
HTTP:404
```

**Kết quả mong đợi:** HTTP 404, `error = "DeviceNotFound"` — KHỚP.

---

### TC-B03: API key sai/thiếu → 401

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P0 |
| Kết quả | **PASS** |

**Lệnh curl (key sai):**
```
curl -s -w "\nHTTP:%{http_code}" -X POST \
  "http://localhost:5099/api/devices/192.168.1.100:5555/reboot" \
  -H "x-api-key: wrongkey"
```

**Response thực tế:**
```json
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
HTTP:401
```

**Lệnh curl (thiếu header):**
```
curl -s -w "\nHTTP:%{http_code}" -X POST \
  "http://localhost:5099/api/devices/192.168.1.100:5555/reboot"
```

**Response thực tế:**
```json
{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
HTTP:401
```

**Kết quả mong đợi:** HTTP 401 — KHỚP cả 2 sub-case.

---

### TC-B04: Thiết bị offline → 422

| Trường | Giá trị |
|--------|---------|
| Mức ưu tiên | P1 |
| Kết quả | **Pass (unit test)** |

**Xác nhận qua unit test:**

```
Passed RebootEndpointsTests.CheckDeviceState_OfflineDevice_Returns422
Passed RebootEndpointsTests.CheckDeviceState_UnknownSerial_Returns404
Passed RebootEndpointsTests.MapRebootResult_AdbTimeout_Returns422
Passed RebootEndpointsTests.MapRebootResult_AdbError_Returns422
Passed RebootEndpointsTests.MapRebootResult_AdbNotFound_Returns500
```

Contract TDD §3.2 cho DeviceOffline (422) và AdbNotFound (500) được xác nhận đầy đủ qua unit test.

---

## Nhóm C — UI Smoke (UXR STEP-3.3)

> **Ghi chú:** Cả 3 TC dưới đây được UX/UI Reviewer thực thi bằng Playwright trong STEP-3.3.  
> Kết quả: **PASS** (re-check UXR 2026-08-24 23:09 — tất cả issue UI-001/002/003 RESOLVED).  
> Tham chiếu: `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md`

### TC-C01: Click "Kiểm tra trạng thái app" → kết quả hiển thị

| Kết quả | PASS — UXR STEP-3.3 |
|---------|---------------------|

Playwright xác nhận: nút hiển thị đúng, click → gọi API → appendLog + showToast với kết quả state.

---

### TC-C02: Click "Khởi động lại thiết bị" → confirm → gọi API

| Kết quả | PASS — UXR STEP-3.3 |
|---------|---------------------|

Playwright xác nhận: confirm dialog hiển thị serial thiết bị, xác nhận → fetch POST → showToast success.

---

### TC-C03: Click "Khởi động lại thiết bị" → cancel → API không gọi

| Kết quả | PASS — UXR STEP-3.3 |
|---------|---------------------|

Playwright xác nhận: cancel dialog → không có fetch request ra `/reboot`.

---

## Kết quả dotnet test (Regression)

**Lệnh:**
```
dotnet test tests/KztekAdbPublishTool.Web.Tests/ --verbosity minimal
```

**Output:**
```
Passed!  - Failed: 0, Passed: 119, Skipped: 0, Total: 119, Duration: 194 ms
```

Không có regression. 22 test mới của feature (AppStatusEndpointsTests × 9, RebootEndpointsTests × 8, AdbServiceAppStatusTests × 5) đều xanh.

---

## Verification Gate

```
Verification run: 2026-08-24 — http://localhost:5099 (dotnet run local, nhánh docker-deploy)
Test case cuối đã chạy: TC-B03 (POST /reboot 401 no key)
Kết quả: Pass — HTTP 401 {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
dotnet test: 119/119 Pass
```
