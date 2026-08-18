---
id: TEST-PLAN-adb-launch-app-api
feature: adb-launch-app-api
author: QA Engineer
created: 2026-08-18
status: executed
workflow: WF-FEATURE Phase 4
---

# TEST PLAN: API Launch App Android qua ADB

## 1. Mục tiêu

Xác nhận endpoint `POST /api/launch-app` hoạt động đúng theo toàn bộ AC trong `docs/user-stories/US-adb-launch-app-api.md` (SC-01 đến SC-08) và không gây regression trên các route cũ.

## 2. Phạm vi kiểm thử

| Trong phạm vi | Ngoài phạm vi |
|---|---|
| Endpoint `POST /api/launch-app` — auth, validate, DeviceState, ADB mapping | Test load / stress |
| Auth filter `ApiKeyEndpointFilter` | UI / giao diện (không có) |
| Validation input (serial, app/package name) | Kiểm thử ADB trên thiết bị thật (xem §4) |
| Error handling 400/401/404/422/500 | Kiểm thử bảo mật chuyên sâu (đã xử lý ở STEP-3.2 STRIDE) |
| Regression route cũ (SC-07) | Kiểm thử tính năng cũ ngoài route `/api/launch-app` |
| Env var override key (SC-08) | |

## 3. Môi trường

| Mục | Giá trị |
|---|---|
| Môi trường | Local (WSL2, Linux 6.6.87.1) |
| Server | `dotnet run --project src/KztekAdbPublishTool.Web --urls http://localhost:5299` |
| Framework | ASP.NET Core Minimal API, .NET 8 |
| API key test | `123456a@` (từ `appsettings.json` local — KHÔNG commit) |
| Build | 2026-08-18, commit tham chiếu STEP-3.1/3.2 |
| Thiết bị Android | **KHÔNG CÓ** — xem §4 về giới hạn môi trường |

## 4. Giới hạn môi trường — Quan trọng

Môi trường sandbox hiện tại **KHÔNG có thiết bị Android thật hoặc emulator kết nối**. `adb devices` trả về rỗng. `DeviceState` được điền bởi `DevicePollWorker` polling ADB thật — không có cơ chế inject device giả khi chạy server thật.

Do đó test case được phân 2 nhóm:

**NHOM A — Test qua HTTP thật (không cần thiết bị):**
TC-002, TC-003, TC-004, TC-005 (và SC-07, SC-08 partial)

**NHOM B — Verify gián tiếp qua unit test (cần thiết bị thật):**
TC-001, TC-006, TC-007

Nhóm B KHÔNG thể verify qua HTTP thật. Bằng chứng gián tiếp: unit test trong `LaunchAppEndpointTests.cs` gọi trực tiếp `CheckDeviceState`/`MapAdbResult` với `DeviceRecord`/`AdbCommandResult` giả lập đúng schema thật, cover đủ logic 200/422/AppNotInstalled/DeviceOffline. Kết quả 42/42 PASS.

**Cảnh báo:** Phải thực hiện smoke test thủ công trên môi trường có thiết bị Android thật trước khi deploy production.

## 5. Chiến lược test

| Tầng | Phương pháp | Công cụ |
|---|---|---|
| Unit | Gọi trực tiếp public static methods (`ValidateInput`, `CheckDeviceState`, `MapAdbResult`, `ApiKeyEndpointFilter.InvokeAsync`) | xUnit, NullLogger, DefaultHttpContext |
| Integration / Functional | HTTP request thật vào server local chạy `dotnet run` | curl |
| Regression | Gọi route cũ (`GET /api/devices`) không có x-api-key | curl |

## 6. Test Cases

Xem chi tiết tại: `docs/test-cases/TC-adb-launch-app-api.md`

| ID | Mô tả | Nhóm | Trạng thái |
|---|---|---|---|
| TC-001 | Happy path — launch app thành công, device Online | B (cần thiết bị) | Pass (gián tiếp) |
| TC-002 | API key sai → 401 | A (HTTP thật) | PASS |
| TC-003 | Thiếu header x-api-key → 401 | A (HTTP thật) | PASS |
| TC-004 | Serial không tồn tại → 404 | A (HTTP thật) | PASS |
| TC-005 | app trống / invalid package name → 400 | A (HTTP thật) | PASS |
| TC-006 | Device Offline → 422 | B (cần thiết bị) | Pass (gián tiếp) |
| TC-007 | App không cài trên thiết bị → 422 | B (cần thiết bị) | Pass (gián tiếp) |

## 7. Tiêu chí Pass/Fail

| Tiêu chí | Điều kiện PASS |
|---|---|
| NHÓM A — HTTP tests | Tất cả 4 case trả đúng HTTP status code và response body schema |
| NHÓM B — Unit test evidence | 42/42 unit test pass, bao gồm các test cover TC-001/006/007 |
| Regression | `GET /api/devices` không có x-api-key vẫn trả 200 |
| Build integrity | `dotnet test` toàn bộ PASS sau khi test xong |

## 8. Rủi ro và khuyến nghị

| Rủi ro | Khuyến nghị |
|---|---|
| TC-001/006/007 chỉ verify gián tiếp qua unit test | Smoke test thủ công bắt buộc trước production (STEP-4.3/4.4) |
| Text-matching `StdErr` với AdbService là coupling ngầm | Monitor nếu AdbService thay đổi message format |
| `appsettings.json` (local) chứa API key test | TUYỆT ĐỐI KHÔNG commit file này — đã được hook config-protection bảo vệ |
| SC-08 (env var override) không test được đầy đủ bằng HTTP | Verify bằng `LaunchApp__ApiKey` env var khi deploy docker |

## 9. Kết quả tổng

- NHÓM A: 4/4 case PASS qua HTTP thật (timestamp: 2026-08-18 17:00, server local port 5299)
- NHÓM B: 3/3 case Pass gián tiếp — unit test 42/42 PASS
- Regression SC-07: PASS — `GET /api/devices` không bị ảnh hưởng
- Bug phát hiện: Không có
- Dotnet test sau khi hoàn thành: 42/42 PASS

---

## 10. QA Lead Sign-off

**Ngày:** 2026-08-18 17:07
**QA Lead:** trongtv@kztek.vn

### Đối chiếu coverage với 8 AC (SC-01 đến SC-08)

| AC | Mô tả | TC | Phương pháp verify | Kết quả |
|---|---|---|---|---|
| SC-01 | Happy path — launch thành công → 200 | TC-001 | Unit test gián tiếp (`MapAdbResult_ExitCode0_Returns200`, `CheckDeviceState_OnlineDevice_ReturnsNull`) | Pass gián tiếp |
| SC-02 | API key sai hoặc thiếu → 401 | TC-002, TC-003 | HTTP thật | PASS |
| SC-03 | Serial không tồn tại → 404 | TC-004 | HTTP thật | PASS |
| SC-04 | Package name rỗng/invalid → 400 | TC-005a/b/c | HTTP thật (3 sub-case) | PASS |
| SC-05 | Device Offline → 422 | TC-006 | Unit test gián tiếp (`CheckDeviceState_OfflineDevice_Returns422`) | Pass gián tiếp |
| SC-06 | App không cài trên thiết bị → 422 | TC-007 | Unit test gián tiếp (`MapAdbResult_AppNotInstalled_Returns422`) | Pass gián tiếp |
| SC-07 | Route cũ không bị ảnh hưởng | Regression test | HTTP thật (`GET /api/devices` không có key → 200) | PASS |
| SC-08 | API key override bằng env var | Partial | Không test đầy đủ qua HTTP — rủi ro ghi nhận, verify khi DevOps deploy docker với env `LaunchApp__ApiKey` | Conditional |

**Coverage: 8/8 SC đã được cover** (SC-08 partial — điều kiện verify rõ ràng tại STEP-4.3/4.4).

### Trạng thái bug

- P0 bug open: **0**
- P1 bug open: **0**
- Tổng bug phát hiện: **0**

### Quyết định: SIGN-OFF CÓ ĐIỀU KIỆN

**Cho phép tiếp tục sang STEP-4.3 (DevOps Engineer deploy staging).**

**ĐIỀU KIỆN BẮT BUỘC trước khi approve production (STEP-4.4):**

> TC-001 (200 OK happy path), TC-006 (422 device offline), TC-007 (422 app not installed) chỉ được verify gián tiếp qua unit test — CHƯA verify qua HTTP thật với thiết bị Android thật/emulator, do giới hạn môi trường sandbox (không có thiết bị kết nối).
>
> DevOps Engineer (STEP-4.3) và DevOps Lead (STEP-4.4) **PHẢI** thực hiện smoke test thủ công với thiết bị Android thật hoặc emulator tại môi trường staging, bao gồm tối thiểu 3 case sau:
> 1. TC-001: Gọi POST /api/launch-app với thiết bị Online + app đã cài → phải trả HTTP 200
> 2. TC-006: Gọi với thiết bị hiện diện trong DeviceState nhưng Offline → phải trả HTTP 422
> 3. TC-007: Gọi với thiết bị Online nhưng app chưa cài → phải trả HTTP 422
>
> **Nếu bất kỳ case nào trong 3 case trên fail tại staging → KHÔNG được deploy production, phải quay lại fix và thông báo QA Lead.**
>
> Ngoài ra, SC-08 (env var override) cần được verify khi deploy docker bằng cách đặt `LaunchApp__ApiKey=<key>` trong docker-compose/env file và xác nhận endpoint hoạt động đúng với key mới.

**Sign-off:** [ ✅ P0=0 ] [ ✅ P1=0 ] [ ✅ Coverage 8/8 SC ] [ ⚠️ Smoke test thiết bị thật bắt buộc trước production ]

**QA Lead ký:** trongtv@kztek.vn — 2026-08-18 17:07
