---
step: "3.1"
plan: ../PLAN-MASTER.md
agent: qa-engineer
status: done
completed_at: "2026-08-19 11:50"
deps: ["2.2"]
---

# STEP 3.1 — QA Engineer: Verify fix trên staging + regression test

## Input nhận

**Từ STEP-2.2 (Tech Lead — Handoff Payload):**
- do_not_redo: Không cần re-build/re-test — Tech Lead đã verify độc lập (build 0 error, 71/71 test PASS). Không cần đọc lại toàn bộ diff.
- watch_out: QA verify PHẢI thực hiện đúng steps-to-reproduce trong BUG report (restart service, KHÔNG scan trên UI, gọi `GET /api/devices/{serial}/status`). Startup có thể mất vài giây warm-up — chờ log `DevicePollWorker warm-up: reconnecting X persisted WiFi device(s)...` xong rồi mới test API. Nếu ADB binary missing trên môi trường staging → warm-up sẽ early-return với log warning, poll loop vẫn chạy (không crash) nhưng API vẫn 404 — cần kiểm tra ADB binary có sẵn.
- next_inputs: Commit `3a86825` trên nhánh `docker-deploy` đã APPROVE, sẵn sàng deploy staging. BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md` là source of truth cho AC verification. Regression: chạy toàn bộ WF-BUGFIX QA regression cho feature `adb-add-device-api`.

## Nhiệm vụ

Deploy fix lên staging (hoặc xác nhận DevOps đã deploy), thực hiện: (1) verify fix — restart service rồi gọi ngay 2 API mới mà không scan, kiểm tra response đúng; (2) regression test — `GET /api/devices` vẫn hoạt động bình thường, `POST /api/devices/connect-by-ip` và `GET /api/devices/{serial}/status` hoạt động đúng cả trước và sau restart.

## Definition of Done

- [x] Warm-up reconnect được gọi khi startup (log evidence `reconnecting 1 persisted WiFi device(s)`)
- [x] USB device không bị đưa vào warm-up (log: chỉ 1 WiFi device, không phải 2)
- [x] `GET /api/devices` vẫn hoạt động bình thường sau fix (regression — 200 OK)
- [x] Smoke test log ghi đủ: endpoint, request, response, pass/fail cho từng case
- [x] Không phát sinh P0/P1 bug mới từ fix
- [ ] [ENVIRONMENT LIMITATION] `GET /api/devices/{serial}/status` → 200 ngay sau restart cần thiết bị Android thật — xác nhận cần smoke test thủ công bổ sung

## Đã làm

### Môi trường thực thi

- Môi trường: LOCAL (WSL2, Linux 6.6.87.1-microsoft-standard-WSL2)
- Commit được verify: `3a86825` trên branch `docker-deploy`
- ADB binary: `/home/duonghoang21/platform-tools/adb` (v1.0.41, 37.0.0-14910828) — CÓ SẴN
- Thiết bị Android thật: KHÔNG CÓ — giới hạn môi trường sandbox/WSL2
- API port: `http://127.0.0.1:57946`
- API key: `123456a@` (từ `appsettings.json`)
- Test DB: SQLite temp (seeded 1 WiFi device `192.168.1.100:5555` + 1 USB device `HT7A21USBTEST`)

### Bước 1 — Build (xác nhận độc lập)

```
$ dotnet build src/KztekAdbPublishTool.Web/
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.76
```
Kết quả: PASS

### Bước 2 — Unit test DevicePollWorkerWarmUpTests (7 tests)

```
$ dotnet test tests/KztekAdbPublishTool.Web.Tests/ --filter "DevicePollWorkerWarmUpTests"
  Passed WarmUpReconnect_SingleWifiDevice_CallsConnectOnce [66 ms]
  Passed WarmUpReconnect_MixedDevices_OnlyWifiConnected [26 ms]
  Passed WarmUpReconnect_CancelledBeforeStart_ConnectNotCalled [29 ms]
  Passed WarmUpReconnect_MultipleWifiDevices_CallsConnectForEach [35 ms]
  Passed WarmUpReconnect_EmptyDatabase_ConnectNotCalled [10 ms]
  Passed WarmUpReconnect_UsbDeviceOnly_ConnectNotCalled [17 ms]
  Passed WarmUpReconnect_OneDeviceConnectFails_ContinuesToNextDevice_DoesNotThrow [20 ms]
Total tests: 7   Passed: 7   Total time: 1.1721 Seconds
```
Kết quả: PASS (7/7)

### Bước 3 — Full regression test suite (71 tests)

```
$ dotnet test tests/KztekAdbPublishTool.Web.Tests/
Test Run Successful.
Total tests: 71   Passed: 71   Total time: 1.0242 Seconds
```
Kết quả: PASS (71/71, không có regression nào bị vỡ)

### Bước 4 — Chạy app local với test DB có seeded data

**Setup test data:**
- WiFi device: serial=`192.168.1.100:5555`, model=`[TEST] WifiDevice01`
- USB device: serial=`HT7A21USBTEST`, model=`[TEST] UsbDevice01`
- Seeded vào SQLite temp DB (không đụng DB thật)

**Khởi động app:**
```bash
Adb__AdbPath=/home/duonghoang21/platform-tools/adb \
Adb__DbPath=<test-db> \
ASPNETCORE_URLS=http://127.0.0.1:57946 \
dotnet run --project src/KztekAdbPublishTool.Web/
```

**Log startup (trích):**
```
info: KztekAdbPublishTool.Web.Workers.DevicePollWorker[0]
      DevicePollWorker started. PollInterval=3000ms
info: KztekAdbPublishTool.Web.Workers.DevicePollWorker[0]
      DevicePollWorker warm-up: reconnecting 1 persisted WiFi device(s)...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://127.0.0.1:57946
info: Microsoft.Hosting.Lifetime[0]
      Application started.
warn: KztekAdbPublishTool.Web.Workers.DevicePollWorker[0]
      DevicePollWorker warm-up skipped — adb binary not found:
      adb connect 192.168.1.100:5555 timeout sau 5000ms
```

Log KEY: `reconnecting 1 persisted WiFi device(s)` — xác nhận `WarmUpReconnectAsync` ĐƯỢC GỌI và chỉ xử lý WiFi device (không bao gồm USB). Kestrel khởi động SONG SONG với warm-up (không bị block).

### Bước 5 — Test cases

#### TC-1: GET /api/devices/{serial}/status sau restart (không scan)

```
Request:  GET http://127.0.0.1:57946/api/devices/192.168.1.100:5555/status
          Header: x-api-key: 123456a@
Response: 404
Body:     {"success":false,"error":"DeviceNotFound","message":"Device '192.168.1.100:5555' not found."}
```

**Kết quả: ENVIRONMENT LIMITATION — 404 do không có thiết bị Android thật**

Phân tích: Warm-up ĐƯỢC gọi và cố gắng `adb connect 192.168.1.100:5555` (log: `reconnecting 1 persisted WiFi device(s)`). Tuy nhiên device không tồn tại nên adb connect timeout → ExitCode=-1, StdOut="" → warm-up dừng sớm → DeviceState không populate → 404. Đây là hành vi ĐÚNG trong môi trường không có thiết bị Android — không phải bug mới. Với thiết bị Android thật online, adb connect sẽ thành công → DeviceState sẽ được populate → status sẽ trả về 200.

Unit test `WarmUpReconnect_SingleWifiDevice_CallsConnectOnce` xác nhận `ConnectAsync` được gọi với đúng serial khi startup.

#### TC-2: POST /api/devices/connect-by-ip sau restart (không scan)

```
Request:  POST http://127.0.0.1:57946/api/devices/connect-by-ip
          Header: x-api-key: 123456a@
          Body: {"ip":"192.168.1.100","port":5555}
Response: 422
Body:     {"success":false,"error":"AdbConnectFailed","message":"adb connect 192.168.1.100:5555 timeout sau 10000ms","exitCode":-1,"stdOut":"","stdErr":"adb connect 192.168.1.100:5555 timeout sau 10000ms"}
```

**Kết quả: ENVIRONMENT LIMITATION — 422 do không có thiết bị Android thật**

Phân tích: API hoạt động đúng — gọi `adb connect` thật, nhận timeout vì không có device. Với thiết bị thật, kết quả sẽ là 200 OK.

#### TC-3: GET /api/devices sau restart (regression — đọc SQLite)

```
Request:  GET http://127.0.0.1:57946/api/devices
          Header: x-api-key: 123456a@
Response: 200 OK
Body:     {"ok":true,"data":[
  {"serial":"192.168.1.100:5555","model":"[TEST] WifiDevice01","connectionType":"WiFi","status":"Online",...},
  {"serial":"HT7A21USBTEST","model":"[TEST] UsbDevice01","connectionType":"USB","status":"Online",...}
]}
```

**Kết quả: PASS** — `GET /api/devices` đọc từ SQLite, không phụ thuộc DeviceState, trả về đúng 2 seeded records sau restart. Không bị ảnh hưởng bởi fix.

#### TC-4: USB device KHÔNG vào warm-up reconnect

```
Log startup: "DevicePollWorker warm-up: reconnecting 1 persisted WiFi device(s)..."
DB có: 1 WiFi device + 1 USB device = 2 records
Warm-up count = 1 (chỉ WiFi, không có USB)
```

**Kết quả: PASS** — USB device `HT7A21USBTEST` bị lọc ra khỏi warm-up (Contains(':') = false). Xác nhận bằng:
- Log count: `1 persisted WiFi device(s)` (không phải 2)
- Unit test `WarmUpReconnect_UsbDeviceOnly_ConnectNotCalled` và `WarmUpReconnect_MixedDevices_OnlyWifiConnected` — cả 2 PASS

### Bước 6 — Tổng kết test cases

| TC | Mô tả | Kết quả | Ghi chú |
|----|-------|---------|---------|
| TC-1 | GET status sau restart, không scan | ENV_LIMIT | Warm-up được gọi (log), device không thật nên 404 — đúng behavior |
| TC-2 | POST connect-by-ip sau restart | ENV_LIMIT | adb connect chạy, timeout — đúng behavior khi không có device |
| TC-3 | GET /api/devices regression | PASS | 200, 2 records từ SQLite |
| TC-4 | USB không vào warm-up | PASS | Log "1 persisted WiFi device(s)", unit test PASS |
| Regression suite | 71 unit tests tổng | PASS | 71/71 |

### Phát hiện bổ sung (P2/P3 — không blocking)

**Vấn đề:** Log message `DevicePollWorker warm-up skipped — adb binary not found: ...` bị dùng cho cả trường hợp adb timeout (không chỉ khi adb binary thực sự missing). ExitCode=-1 với StdOut="" có thể xảy ra cả khi binary missing LẪN khi connect timeout. Hệ quả: nếu có 2+ WiFi devices và device đầu timeout, device thứ 2 sẽ không được thử warm-up.

**Mức độ:** P2/P3 — không blocking cho KZTEK use case hiện tại (few devices). Tech Lead đã ghi nhận là Optional concern.

**Không tự sign-off** — chuyển QA Lead review.

## Artifact

- Test case log: nhúng trực tiếp vào step file này (xem Bước 5)
- Build output: 0 error, 0 warning (xem Bước 1)
- Unit test results: 7/7 warm-up tests PASS, 71/71 total PASS (xem Bước 2-3)
- Startup log evidence: `reconnecting 1 persisted WiFi device(s)` xác nhận WarmUpReconnectAsync được gọi (xem Bước 4)

## Quyết định quan trọng

1. **TC-1/TC-2 không thể verify E2E thật** trong môi trường này vì không có Android device thật. Đã verify intent của fix qua: (a) log startup, (b) unit tests 7/7 PASS. Cần smoke test thủ công bổ sung khi deploy staging thật có adb + device.
2. **Không có P0/P1 bug mới** phát sinh từ fix.
3. **Phát hiện P2/P3**: log message misleading + potential early-return khi 2+ WiFi devices, device đầu timeout. Đã ghi nhận, chuyển QA Lead quyết định.
4. **Không tự sign-off** — đây là P1 bug, phải qua QA Lead (Bước 3.2).

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: Không cần re-run build/unit test — 71/71 PASS đã được QA Engineer chạy và xác nhận độc lập. Không cần đọc lại toàn bộ log startup.
- watch_out: (1) TC-1/TC-2 có ENVIRONMENT LIMITATION — log evidence đã confirm warm-up được gọi nhưng cần smoke test thủ công với thiết bị Android thật trên staging thật trước khi sign-off P1 hoàn toàn. (2) Phát hiện P2/P3: log "adb binary not found" misleading khi thực ra là timeout — QA Lead quyết định có cần thêm fix nhỏ hay không. (3) Warm-up log key để check: `DevicePollWorker warm-up: reconnecting N persisted WiFi device(s)...`.
- next_inputs: Step file này + BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md` + unit test results (71/71 PASS) là input đầy đủ cho QA Lead sign-off. QA Lead cần xác nhận: (a) P2/P3 finding có cần fix trong PR này hay không, (b) có chấp nhận ENVIRONMENT LIMITATION cho TC-1/TC-2 hay yêu cầu thêm smoke test thủ công trước khi sign-off.

## Commit

- Hash: [điền sau khi commit]
- Đã push: không (chưa commit — sẽ commit sau khi điền xong)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
