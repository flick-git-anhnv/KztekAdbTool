---
id: TC-api-request-log
feature: api-request-log
created: 2026-08-19
author: QA Engineer
prd: docs/prd/PRD-api-request-log.md
user_story: docs/user-stories/US-api-request-log.md
tdd: docs/tech-design/TDD-api-request-log.md
environment: Docker container kztek-adb-tool, port 3339
api_key: sup3rsecr3tap1key@ (via LaunchApp__ApiKey env override in docker-compose.yml)
device_online: "192.168.21.11:5555"
executed: 2026-08-19 17:13
---

# TC — API Request Log (Ghi lịch sử & broadcast real-time)

## Môi trường thực thi

- **App:** Docker container `kztek-adb-tool`, image `kztek/adb-tool:ver11` (rebuilt 2026-08-19, bao gồm fix commit `ae2a211`)
- **Port:** `localhost:3339`
- **DB:** SQLite `/app/data/adbpublishtool.db` (volume `kztekadbtool_adb_data`)
- **API key:** `sup3rsecr3tap1key@` (env override `LaunchApp__ApiKey` trong `docker-compose.yml`)
- **Device online:** `192.168.21.11:5555` (ADB WiFi, đã connected)
- **Verification:** `docker cp kztek-adb-tool:/app/data/adbpublishtool.db <local>` + `sqlite3 <local> "SELECT ..."`

## Ghi chú kiến trúc quan trọng (theo TDD § ASSUMPTIONS + Kiến trúc đề xuất)

> **Request 401 VẪN ĐƯỢC LOG** — `ApiRequestLoggingEndpointFilter` đặt **OUTER** so với `ApiKeyEndpointFilter`. Filter OUTER chạy trước: capture arguments → await next() (bao gồm cả filter INNER 401) → log entry. Đây là hành vi **ĐÚNG THEO DESIGN** (TDD Decision D4). Test case 401 phải verify log xuất hiện với Result=Unauthorized, KHÔNG phải verify không có log.
>
> **Parameters capture:** Sau fix UI-001 (commit `ae2a211`), filter serialize `context.Arguments[0]` (bound DTO) thay vì đọc raw body stream (đã bị consumed bởi model binding). Test case phải verify `Parameters != null` và đúng JSON value.

---

## TC-01 — AddDevice API: Success, Parameters populated

**Mục tiêu:** Verify AddDevice thành công → log entry ghi đúng với Parameters không null

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -H "x-api-key: sup3rsecr3tap1key@" \
  -d '{"ip":"192.168.21.11","port":5555}'
```

**Kết quả mong đợi:**
- HTTP 200, body `{"success":true,...}`
- DB: 1 entry mới, `ApiName=AddDevice`, `Parameters={"ip":"192.168.21.11","port":5555}`, `Result=Success`, `HttpStatusCode=200`

**Kết quả thực tế — PASS:**
- HTTP 200 `{"success":true,"message":"Device connected.","serial":"192.168.21.11:5555","exitCode":0}`
- DB Id 13: `AddDevice|{"ip":"192.168.21.11","port":5555}|Success|200` ✅
- Parameters không null, JSON đúng với request body ✅

**Retest UI-001:** Parameters IS populated — fix `ae2a211` hoạt động đúng ✅

---

## TC-02 — AddDevice API: Failure 400 (invalid input)

**Mục tiêu:** Verify AddDevice lỗi validation → log entry vẫn ghi với Result=Failure

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -H "x-api-key: sup3rsecr3tap1key@" \
  -d '{"ip":"192.168.21.11:5555","port":null}'
```
*(ip field chứa colon — sai format)*

**Kết quả mong đợi:**
- HTTP 400
- DB: 1 entry mới, `ApiName=AddDevice`, `Parameters` có ip kèm colon, `Result=Failure`, `HttpStatusCode=400`

**Kết quả thực tế — PASS:**
- HTTP 400 `{"success":false,"error":"InvalidInput","message":"ip không được chứa ':' — dùng field port riêng"}`
- DB Id 14: `AddDevice|{"ip":"192.168.21.11:5555","port":null}|Failure|400` ✅
- Parameters captured đúng (ip sai format như gửi) ✅

---

## TC-03 — AddDevice API: 401 Wrong Key — VẪN LOG (theo TDD OUTER filter design)

**Mục tiêu:** Verify request với API key sai → log entry ĐƯỢC tạo (không bị chặn) với Result=Unauthorized

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -H "x-api-key: WRONGKEY" \
  -d '{"ip":"192.168.21.11","port":5555}'
```

**Kết quả mong đợi:**
- HTTP 401 (response trả về caller không đổi — BR-G3)
- DB: 1 entry mới, `Result=Unauthorized`, `HttpStatusCode=401`, **Parameters KHÔNG null** (OUTER filter capture trước khi INNER 401)

**Kết quả thực tế — PASS:**
- HTTP 401 `{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}`
- DB Id 15: `AddDevice|{"ip":"192.168.21.11","port":5555}|Unauthorized|401` ✅
- Parameters populated — OUTER filter capture đúng spec ✅

---

## TC-04 — AddDevice API: 401 No Key — VẪN LOG

**Mục tiêu:** Verify request không có API key → log entry ĐƯỢC tạo với Result=Unauthorized

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -d '{"ip":"192.168.21.16","port":5555}'
```
*(không có header x-api-key)*

**Kết quả mong đợi:**
- HTTP 401
- DB: 1 entry mới, `Result=Unauthorized`, `HttpStatusCode=401`, Parameters populated

**Kết quả thực tế — PASS:**
- HTTP 401 `{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}`
- DB Id 16: `AddDevice|{"ip":"192.168.21.16","port":5555}|Unauthorized|401` ✅

---

## TC-05 — LaunchApp API: Failure 422 App Not Installed

**Mục tiêu:** Verify LaunchApp với package không tồn tại → log entry ghi đúng Result=Failure, 422

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: sup3rsecr3tap1key@" \
  -d '{"serial":"192.168.21.11:5555","app":"com.test.notinstalled"}'
```

**Kết quả mong đợi:**
- HTTP 422
- DB: 1 entry mới, `ApiName=LaunchApp`, `Parameters={"serial":"...","app":"com.test.notinstalled"}`, `Result=Failure`, `HttpStatusCode=422`

**Kết quả thực tế — PASS:**
- HTTP 422 `{"success":false,"error":"AppNotInstalled","message":"No activities found to run"}`
- DB Id 19: `LaunchApp|{"serial":"192.168.21.11:5555","app":"com.test.notinstalled"}|Failure|422` ✅
- Parameters: cả `serial` và `app` đều có giá trị ✅

---

## TC-06 — LaunchApp API: 401 Wrong Key — VẪN LOG

**Mục tiêu:** Verify LaunchApp với API key sai → log entry ĐƯỢC tạo với Result=Unauthorized, Parameters populated

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: WRONGKEY" \
  -d '{"serial":"192.168.21.11:5555","app":"com.test.app"}'
```

**Kết quả mong đợi:**
- HTTP 401
- DB: 1 entry mới, `Result=Unauthorized`, `HttpStatusCode=401`, Parameters có cả serial và app

**Kết quả thực tế — PASS:**
- HTTP 401 `{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}`
- DB Id 23: `LaunchApp|{"serial":"192.168.21.11:5555","app":"com.test.app"}|Unauthorized|401` ✅
- Parameters: `{"serial":"192.168.21.11:5555","app":"com.test.app"}` — cả 2 field đúng ✅

---

## TC-06b — LaunchApp API: 401 No Key — VẪN LOG

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/launch-app \
  -H "Content-Type: application/json" \
  -d '{"serial":"192.168.21.11:5555","app":"com.test.app"}'
```

**Kết quả thực tế — PASS:**
- HTTP 401
- DB Id 24: `LaunchApp|{"serial":"192.168.21.11:5555","app":"com.test.app"}|Unauthorized|401` ✅

---

## TC-07 — LaunchApp API: Success

**Mục tiêu:** Verify LaunchApp thành công → log entry ghi đúng Result=Success

**Bước reproduce:**
```bash
curl -X POST http://localhost:3339/api/launch-app \
  -H "Content-Type: application/json" \
  -H "x-api-key: sup3rsecr3tap1key@" \
  -d '{"serial":"192.168.21.11:5555","app":"com.android.settings"}'
```

**Kết quả mong đợi:**
- HTTP 200
- DB: 1 entry mới, `ApiName=LaunchApp`, `Result=Success`, `HttpStatusCode=200`, Parameters populated

**Kết quả thực tế — PASS:**
- HTTP 200 `{"success":true,"message":"App launched successfully.","serial":"192.168.21.11:5555","app":"com.android.settings"}`
- DB Id 20: `LaunchApp|{"serial":"192.168.21.11:5555","app":"com.android.settings"}|Success|200` ✅

---

## TC-08 — Concurrent Requests: Cả 2 Đều Được Log

**Mục tiêu:** Verify 2 request gần như đồng thời → cả 2 đều log đầy đủ, không mất data, không lẫn lộn

**Bước reproduce:**
```bash
# Gửi đồng thời bằng background jobs
curl -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "x-api-key: sup3rsecr3tap1key@" -d '{"ip":"192.168.21.11","port":5555}' &

curl -X POST http://localhost:3339/api/launch-app \
  -H "x-api-key: sup3rsecr3tap1key@" -d '{"serial":"192.168.21.11:5555","app":"com.android.settings"}' &
wait
```

**Kết quả mong đợi:**
- Cả 2 HTTP 200
- DB: 2 entry mới với timestamp gần giống nhau, dữ liệu đúng từng request, không lẫn lộn

**Kết quả thực tế — PASS:**
- Cả 2 request HTTP 200, total elapsed 248ms (chứng tỏ thực sự song song)
- DB Id 21: `AddDevice|{"ip":"192.168.21.11","port":5555}|Success|200|2026-08-19T10:10:35.0778103Z`
- DB Id 22: `LaunchApp|{"serial":"192.168.21.11:5555","app":"com.android.settings"}|Success|200|2026-08-19T10:10:35.0778828Z`
- Timestamp sai nhau 72 microseconds — thực sự đồng thời ✅
- Dữ liệu không lẫn lộn giữa 2 request ✅

---

## TC-09 — DB Persistence: Dữ liệu Còn Sau Restart Container

**Mục tiêu:** Verify log entries tồn tại sau khi restart container (persistent storage qua Docker volume)

**Bước reproduce:**
```bash
# Ghi nhận COUNT trước restart
docker cp kztek-adb-tool:/app/data/adbpublishtool.db /tmp/before.db
sqlite3 /tmp/before.db "SELECT COUNT(*) FROM ApiRequestLog;"
# Restart
docker restart kztek-adb-tool
sleep 8
# Kiểm tra sau restart
docker cp kztek-adb-tool:/app/data/adbpublishtool.db /tmp/after.db
sqlite3 /tmp/after.db "SELECT COUNT(*) FROM ApiRequestLog;"
sqlite3 /tmp/after.db "SELECT Id,ApiName,Result FROM ApiRequestLog WHERE Id IN (13,19,20,21,22);"
```

**Kết quả mong đợi:**
- COUNT sau restart = COUNT trước restart
- Tất cả entry quan trọng vẫn còn đủ

**Kết quả thực tế — PASS:**
- COUNT trước: 24, COUNT sau: 24 ✅
- Id 13 (AddDevice/Success), 19 (LaunchApp/Failure), 20 (LaunchApp/Success), 21 (AddDevice/Success), 22 (LaunchApp/Success) — tất cả vẫn còn ✅

---

## TC-10 — Parameters Field Không Null (Retest UI-001 Fix)

**Mục tiêu:** Xác nhận bug UI-001 đã được fix — Parameters không còn null cho mọi request có body hợp lệ

**Xác minh qua DB:**
```
Id 13: Parameters = {"ip":"192.168.21.11","port":5555}       -- AddDevice success ✅
Id 14: Parameters = {"ip":"192.168.21.11:5555","port":null}  -- AddDevice 400 ✅
Id 15: Parameters = {"ip":"192.168.21.11","port":5555}       -- AddDevice 401 ✅
Id 16: Parameters = {"ip":"192.168.21.16","port":5555}       -- AddDevice 401 no key ✅
Id 19: Parameters = {"serial":"...","app":"com.test.notinstalled"} -- LaunchApp 422 ✅
Id 20: Parameters = {"serial":"...","app":"com.android.settings"}  -- LaunchApp 200 ✅
Id 23: Parameters = {"serial":"...","app":"com.test.app"}    -- LaunchApp 401 ✅
Id 24: Parameters = {"serial":"...","app":"com.test.app"}    -- LaunchApp 401 no key ✅
```

**Kết quả thực tế — PASS:**
- Mọi entry từ Id 13 trở đi (post-fix) có Parameters populated khi request body hợp lệ ✅
- Entries trước fix (Id 1-8, trong DB từ image cũ) có Parameters null — expected (code cũ)
- Fix `ae2a211` hoạt động đúng ✅

---

## TC-11 — SignalR: Handler Đăng Ký Đúng, Không Lỗi

**Mục tiêu:** Verify SignalR handler `ApiRequestLogged` được đăng ký và không gây JS error

**Xác minh (code-level):**
- `dashboard.js:299`: `conn.on('ApiRequestLogged', function (entry) { appendLog(formatApiRequestLog(entry)); });` — handler đăng ký trong `setupSignalR()` ✅
- `formatApiRequestLog`: null-safe (`if (!paramsJsonOrNull) return '(no body)'`) ✅
- Container logs: không có error liên quan đến SignalR, hub, hoặc `ApiRequestLog` service ✅
- `ApiRequestLogService` dùng `Clients.All.SendAsync("ApiRequestLogged", entry)` — fire best-effort (không block persistence nếu broadcast fail) ✅

**Ghi chú:** Multi-tab live broadcast cần browser thật để verify đầy đủ. Verified kiến trúc qua code review (UXR bước 3.1 đã xác nhận architecture). Không có bug phát hiện ở tầng này.

**Kết quả: PASS (code-level + architecture review)** ✅

---

## TC-12 — Panel #log: Không Auto-load History

**Mục tiêu:** Verify panel `#log` chỉ hiển thị entries real-time mới, KHÔNG tự fetch history khi mở trang

**Xác minh (code-level):**
- `dashboard.js`: Tìm kiếm `fetch('/api/request-log'` hoặc `fetch('/api/api-request-log'` → **không có** ✅
- `conn.on('ApiRequestLogged', ...)` chỉ được trigger khi nhận SignalR event mới ✅
- Không có `onopen` / `onconnected` handler nào fetch historical data ✅

**Kết quả: PASS (code-level)** ✅

---

## Tổng hợp kết quả

| TC | Tên | Kết quả | Evidence (DB Id / log) |
|----|-----|---------|------------------------|
| TC-01 | AddDevice success, Parameters populated | **PASS** | Id 13 |
| TC-02 | AddDevice 400 invalid input | **PASS** | Id 14 |
| TC-03 | AddDevice 401 wrong key — vẫn log | **PASS** | Id 15 |
| TC-04 | AddDevice 401 no key — vẫn log | **PASS** | Id 16 |
| TC-05 | LaunchApp 422 AppNotInstalled | **PASS** | Id 19 |
| TC-06 | LaunchApp 401 wrong key — vẫn log | **PASS** | Id 23 |
| TC-06b | LaunchApp 401 no key — vẫn log | **PASS** | Id 24 |
| TC-07 | LaunchApp success | **PASS** | Id 20 |
| TC-08 | Concurrent requests — cả 2 logged | **PASS** | Id 21 & 22 |
| TC-09 | DB Persistence sau restart | **PASS** | COUNT=24 before=after |
| TC-10 | Parameters không null (UI-001 retest) | **PASS** | Ids 13-24 |
| TC-11 | SignalR handler đăng ký, không lỗi | **PASS** | code-level + container logs |
| TC-12 | Panel #log no history auto-load | **PASS** | code-level |

**Tất cả TC: PASS. Không có P0/P1 bug open.**

---

## Bug Report

Không có bug mới phát hiện trong lần test này. UI-001 đã được fix (commit `ae2a211`) và verified PASS tại TC-10.

---

## Observation (non-blocking)

- **OBS-01 (Low):** Entries Id 17-18 trong DB có `app: null` — đây là artifact từ lần test đầu khi dùng field name sai (`"packageName"` thay vì `"app"`). Model binding không ánh xạ unknown field → `App` property null. Behavior này là đúng (garbage-in/garbage-out của test script, không phải code bug). Entry có `serial` vẫn capture đúng.
- **OBS-02 (Info):** Container cần **rebuild image** trước khi QA test — image từ UXR (14:23) được build TRƯỚC fix commit ae2a211 (14:35), nên container khi start lần đầu sẽ có bug Parameters null. Handoff payload UXR nói "không rebuild" nhưng thực tế image cũ. QA đã rebuild thành công. STEP-3.4 DevOps cần rebuild khi deploy staging.

---

*Verification run: 2026-08-19 17:13 — localhost:3339 (Docker kztek-adb-tool)*
*TC cuối đã chạy: TC-09 DB Persistence — docker restart kztek-adb-tool, COUNT=24 same*
*Kết quả: 13/13 PASS — Không có P0/P1 bug open*
