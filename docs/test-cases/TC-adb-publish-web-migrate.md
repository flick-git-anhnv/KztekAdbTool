# TC-adb-publish-web-migrate — Smoke Test: KztekAdbPublishTool Web Migrate

**Feature:** WinForms → ASP.NET Core Razor Pages (.NET 8) — Smoke Test  
**Tester:** QA Engineer  
**Ngày chạy:** 2026-08-05 16:32–16:49  
**Môi trường:** Windows 11 Pro, `dotnet run --project src/KztekAdbPublishTool.Web -c Release`, http://localhost:57946  
**Build:** Release — 0 lỗi, 0 warning (xác nhận tại 16:32)  
**Branch:** docker-deploy — Commit: 833972f  

---

## Kết quả tổng quan

| Nhóm | Tổng TC | Pass | Fail | Skip (thiếu device) |
|---|---|---|---|---|
| Server/API | 13 | 13 | 0 | 0 |
| Client-side JS (code analysis) | 4 | 4 | 0 | 0 |
| Device-dependent | 5 | — | — | 5 |
| **Tổng** | **22** | **17** | **0** | **5** |

**Bug P0/P1:** 0  
**Sign-off:** ✅ (các luồng có thể verify đều PASS; luồng thiếu device ghi rõ lý do skip)

---

## Chi tiết test case

### TC-01 — HTTP 200 trang chính
- **Bước:** `GET http://localhost:57946/`  
- **Kết quả mong đợi:** HTTP 200, HTML có title "Dashboard — KZTEK ADB Publish Tool"  
- **Kết quả thực tế:** 200, title đúng, Bootstrap + SignalR client + dashboard.js load đủ  
- **Status: PASS**

---

### TC-02 — GET /api/devices (fallback endpoint, không có device)
- **Bước:** `GET /api/devices`  
- **Kết quả mong đợi:** 200, `{"ok":true,"data":[]}`  
- **Kết quả thực tế:** `{"ok":true,"data":[]}`  
- **Status: PASS**

---

### TC-03 — [BUG C1 VERIFY] Upload APK với field name 'apk' (sau fix 3.2)
- **Mô tả:** Bug C1 (Critical) đã fix: `fd.append('file', file)` → `fd.append('apk', file)`  
- **Bước:** `POST /api/apk/upload` với `-F "apk=@test.apk"`  
- **Kết quả mong đợi:** 200, `{"path":"...","packageName":null}`  
- **Kết quả thực tế:** `{"path":"uploads-dev\\20260805094349-test_app.apk","packageName":null}` — HTTP 200  
- **Regression verify:** `-F "file=@test.apk"` → 400 `"Không tìm thấy trường 'apk' trong form."` ✅  
- **Status: PASS (C1 confirmed fixed)**

---

### TC-04 — [BUG C2 VERIFY] Network scan với field 'rangeText' (sau fix 3.2)
- **Mô tả:** Bug C2 (Critical) đã fix: `ipRange` → `rangeText` trong JSON body  
- **Bước:** `POST /api/scan/start` body `{"rangeText":"192.168.1.1-10","port":5555}`  
- **Kết quả mong đợi:** 202, `{"message":"Bắt đầu quét mạng...","range":"192.168.1.1-10","port":5555}`  
- **Kết quả thực tế:** HTTP 202, response đúng, log: "ScanCoordinator: starting scan 10 IPs on port 5555"  
- **Regression verify:** `{"ipRange":"..."}` → 400 `"Vui lòng nhập dải IP cần quét."` ✅  
- **Status: PASS (C2 confirmed fixed)**

---

### TC-05 — Cancel scan
- **Bước:** `POST /api/scan/cancel`  
- **Kết quả mong đợi:** 200, message hủy  
- **Kết quả thực tế:** 200 `{"message":"Đã gửi tín hiệu hủy quét."}`  
- **Status: PASS**

---

### TC-06 — SignalR negotiate
- **Bước:** `POST /hubs/device/negotiate?negotiateVersion=1`  
- **Kết quả mong đợi:** 200, connectionId + 3 transports (WebSockets, SSE, LongPolling)  
- **Kết quả thực tế:** 200, connectionId trả về, availableTransports: WebSockets/ServerSentEvents/LongPolling  
- **Note:** URL sai `/deviceHub/...` → 404 (đúng, hub map tại `/hubs/device`)  
- **Status: PASS**

---

### TC-07 — Polling toggle off/on
- **Bước:** `POST /api/polling/toggle {"enabled":false}` rồi `{"enabled":true}`  
- **Kết quả mong đợi:** 200, `{ok:true, enabled:<value>}`  
- **Kết quả thực tế:** `{"ok":true,"enabled":false}` 200, `{"ok":true,"enabled":true}` 200  
- **Status: PASS**

---

### TC-08 — Settings package name
- **Bước:** `POST /api/settings/package {"packageName":"com.test.app"}`  
- **Kết quả mong đợi:** 200 `{ok:true}`  
- **Kết quả thực tế:** `{"ok":true}` HTTP 200  
- **Status: PASS**

---

### TC-09 — DELETE /api/apk
- **Bước:** `DELETE /api/apk {"path":"uploads-dev/20260805094349-test_app.apk"}`  
- **Kết quả mong đợi:** 200, message đã xóa  
- **Kết quả thực tế:** `{"message":"Đã xóa APK.","path":"uploads-dev\\..."}` HTTP 200  
- **Status: PASS**

---

### TC-10 — Manual poll trigger
- **Bước:** `POST /api/devices/poll`  
- **Kết quả mong đợi:** 200  
- **Kết quả thực tế:** `{"ok":true,"message":"Đã kích hoạt poll thủ công"}` 200  
- **Status: PASS**

---

### TC-11 — Connect device (IP giả, format ipPort đúng)
- **Bước:** `POST /api/devices/connect {"ipPort":"192.168.99.1:5555"}`  
- **Kết quả mong đợi:** Không crash server; trả lỗi hợp lý (adb không có trong môi trường test)  
- **Kết quả thực tế:** `{"ok":false,"error":"Không tìm thấy adb tại: C:\\platform-tools\\adb.exe"}` 200 — server không crash  
- **Status: PASS** (lỗi adb expected trong môi trường dev thiếu adb.exe)

---

### TC-12 — Connect-batch (IP giả)
- **Bước:** `POST /api/devices/connect-batch {"ipPorts":["192.168.99.1:5555"]}`  
- **Kết quả mong đợi:** Không crash, trả kết quả từng IP  
- **Kết quả thực tế:** `{"ok":true,"data":[{"ipPort":"192.168.99.1:5555","ok":false,"error":"Không tìm thấy adb..."}]}` 200  
- **Status: PASS**

---

### TC-13 — Install với device list rỗng
- **Bước:** `POST /api/install {"serials":[],"selectedOnly":false,"apkPath":"","packageName":"com.test"}`  
- **Kết quả mong đợi:** 400, thông báo không có device  
- **Kết quả thực tế:** `{"error":"Không có thiết bị Online phù hợp."}` 400  
- **Status: PASS**

---

### TC-14 — [C3 VERIFY] Dead code PollingState.cs đã xóa
- **Bước:** Glob `src/KztekAdbPublishTool.Web/**/PollingState*`  
- **Kết quả mong đợi:** Không tìm thấy file  
- **Kết quả thực tế:** Không tìm thấy. State/ chỉ còn `DeviceState.cs`  
- **Status: PASS**

---

### TC-15 đến TC-18 — Client-side JS code analysis

| TC | Luồng | Phân tích | Status |
|---|---|---|---|
| TC-15 | Filter IP/version | `applyFilter()`: `serial.includes(ipVal)` + `ver.includes(verVal)` — contains, case-insensitive. **Parity OK** vs WinForms `String.Contains(..., OrdinalIgnoreCase)` | PASS |
| TC-16 | Select-all (visible rows only) | `toggleSelectAll()`: check `r.style.display !== 'none'` → all-selected? bỏ tick hết : tick hết. **Parity OK** vs WinForms `_grid.Rows` (filtered grid) | PASS |
| TC-17 | Auto-detect toggle | `chkAuto.addEventListener('change')` → `POST /api/polling/toggle {enabled: chkAuto.checked}`. Parity OK | PASS |
| TC-18 | APK browse → upload | `btn-browse-apk` → `inputApk.click()` → `change` → `fd.append('apk', file)`. Parity OK, field name đã fix C1 | PASS |

---

### TC-19 đến TC-22 — SKIP (thiếu thiết bị Android + ADB thật)

| TC | Luồng | Lý do skip |
|---|---|---|
| TC-19 | Poll thật (device Online/Offline detection) | Không có device Android WiFi trong môi trường CI |
| TC-20 | Install APK thật lên device | Thiếu device + APK thật |
| TC-21 | SignalR realtime events (DevicesUpdated, InstallProgress, ScanFound) | Thiếu device để trigger events |
| TC-22 | Launch app sau install | Thiếu device |

**Ghi chú:** Các luồng skip cần verify thủ công khi có thiết bị Android kết nối WiFi. Theo ADR-001, container cần chạy với `--network host` trên Linux để adb thấy LAN.

---

## Server log tổng hợp

Không có 5xx error trong toàn bộ session test. Tất cả request xử lý đúng: 200/202 (success), 400 (validation error expected), 404 (URL sai expected).

```
GET  /                           200  87ms
GET  /api/devices                200  17ms
POST /api/apk/upload (apk)       200  32ms  ← C1 PASS
POST /api/apk/upload (file)      400   1ms  ← C1 regression OK
POST /api/scan/start (rangeText) 202   6ms  ← C2 PASS
POST /api/scan/start (ipRange)   400   1ms  ← C2 regression OK
POST /api/scan/cancel            200   1ms
POST /hubs/device/negotiate      200  12ms
POST /api/polling/toggle         200  11ms  (x2)
POST /api/settings/package       200   5ms
DELETE /api/apk                  200  11ms
POST /api/devices/poll           200   2ms
POST /api/devices/connect        200   2ms
POST /api/devices/connect-batch  200  N/A
POST /api/install                400   3ms
```

---

## Behavior parity WinForms vs Web

| Tính năng | WinForms | Web | Parity |
|---|---|---|---|
| Filter IP/Serial | `Contains(OrdinalIgnoreCase)` | `serial.includes(ipVal)` | ✅ |
| Filter Version | `Contains(OrdinalIgnoreCase)` | `ver.includes(verVal)` | ✅ |
| Select-all toggle | Visible rows trong Grid | `display !== 'none'` rows | ✅ |
| APK upload | `OpenFileDialog` → gửi file | `input[type=file]` → `fd.append('apk')` | ✅ |
| Toggle auto-detect | Timer stop/start | POST /api/polling/toggle | ✅ |
| Network scan | `NetworkScanForm` modal | Bootstrap modal #networkScanModal | ✅ |
| Log area | TextBox append | `appendLog()` | ✅ |

---

_QA Engineer — STEP-3.3 Smoke Test_  
_Verification run: 2026-08-05 16:32–16:49 — http://localhost:57946_
