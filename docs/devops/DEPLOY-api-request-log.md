# DEPLOY: API Request Log — Staging Deploy Checklist

**Feature:** API Request Log — Ghi lịch sử & hiển thị real-time cho AddDevice/LaunchApp API  
**Plan:** `docs/plans/PLAN-api-request-log-2026-08-19/`  
**Workflow:** WF-FEATURE — Bước 3.4 (DevOps Engineer)  
**Môi trường:** Staging (Docker local — `kztek-adb-tool`, port `3339:8080`)  
**Ngày deploy:** 2026-08-19  
**Người thực hiện:** DevOps Engineer  

---

## Deploy Checklist

```
[x] PR approved bởi Tech Lead                         — STEP-2.3 (ae2a211 approved 14:50)
[x] CI/CD pass toàn bộ                                — build 0/0 error, 85/85 test pass
[x] QA sign-off trên staging                          — STEP-3.3 QA Lead APPROVED (17:18)
[ ] DevOps Lead approve                               — STEP-3.5 (chờ sau bước này)
[ ] EM approve (nếu feature lớn)                      — N/A (P2, không yêu cầu EM approve riêng)
[x] Rollback plan đã chuẩn bị                         — xem mục Rollback Plan bên dưới
[ ] Team nhận thông báo (#deploys)                    — chờ DevOps Lead approve (STEP-3.5)
[ ] On-call standby 30 phút sau deploy                — chờ DevOps Lead approve (STEP-3.5)
[x] Monitor dashboard đang theo dõi                  — docker logs kztek-adb-tool (streaming)
```

> **Lưu ý:** Checklist production sẽ được điền đầy đủ tại STEP-3.6. Bước này chỉ deploy staging.

---

## Thông tin môi trường Staging

| Thông số | Giá trị |
|---|---|
| Container name | `kztek-adb-tool` |
| Image | `kztek/adb-tool:ver11` |
| Image SHA (build mới) | `sha256:1900e1588f4dea6c759c189dbf61a82db967210359de8d87557aedd826f01365` |
| Port mapping | `3339:8080` (host:container) |
| Staging URL | `http://localhost:3339` |
| SQLite DB path | `/app/data/adbpublishtool.db` (volume `kztekadbtool_adb_data`) |
| API Key | `sup3rsecr3tap1key@` (env `LaunchApp__ApiKey` trong docker-compose.yml) |
| Commits đã include | 79ae3cf, d8abe37, a4a7259, ae2a211, 8a89528 (và các commit docs/plan sau) |

---

## Kết quả từng bước

### Bước 1 — Rebuild Docker image (--no-cache)

**Lệnh:** `docker compose build --no-cache`

**Kết quả:** PASS

```
- dotnet restore: OK (7.68s)
- dotnet publish: 0 error, 0 warning → KztekAdbPublishTool.Web.dll
- Image mới: sha256:1900e1588f4dea6c759c189dbf61a82db967210359de8d87557aedd826f01365
```

**Lý do force `--no-cache`:** QA Lead OBS-02 yêu cầu rebuild bắt buộc vì image cũ được build TRƯỚC fix `ae2a211` (fix UI-001: Parameters null). Build thường (`docker compose build`) trả về toàn bộ CACHED layers — cần `--no-cache` để đảm bảo dotnet publish lại với source mới nhất.

---

### Bước 2 — Restart container

**Lệnh:** `docker compose down && docker compose up -d`

**Kết quả:** PASS

```
Container kztek-adb-tool  Stopped → Removed
Network kztekadbtool_default Removed → Created
Container kztek-adb-tool  Created → Started
```

---

### Bước 3 — Verify startup log (không exception)

**Lệnh:** `docker logs kztek-adb-tool`

**Kết quả:** PASS — không có exception trong startup log

```
info: Now listening on: http://0.0.0.0:8080
info: Application started. Press Ctrl+C to shut down.
info: Hosting environment: Production
info: Content root path: /app
info: DevicePollWorker started. PollInterval=3000ms
info: DevicePollWorker warm-up: reconnecting 10 persisted WiFi device(s)...
```

> warn: DataProtection key sẽ không persist — expected (non-critical, không ảnh hưởng behavior).

---

### Bước 4 — Verify bảng ApiRequestLog tồn tại trong SQLite DB

**Phương pháp:** `docker cp` + host `sqlite3`

**Lệnh:**
```bash
docker cp kztek-adb-tool:/app/data/adbpublishtool.db /tmp/check.db
sqlite3 /tmp/check.db ".tables"
sqlite3 /tmp/check.db ".schema ApiRequestLog"
```

**Kết quả:** PASS

```
Tables: ApiRequestLog  Devices  Settings

Schema:
CREATE TABLE ApiRequestLog (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp      TEXT    NOT NULL,
    ApiName        TEXT    NOT NULL,
    HttpMethod     TEXT    NOT NULL,
    Path           TEXT    NOT NULL,
    Parameters     TEXT    NULL,
    Result         TEXT    NOT NULL,
    HttpStatusCode INTEGER NOT NULL,
    ErrorMessage   TEXT    NULL,
    CallerIp       TEXT    NOT NULL,
    DurationMs     INTEGER NOT NULL
);
CREATE INDEX IX_ApiRequestLog_Timestamp ON ApiRequestLog(Timestamp DESC);
```

> Bảng tự tạo qua `CREATE TABLE IF NOT EXISTS` trong `ApiRequestLogRepository.InitializeAsync()` khi service khởi động lần đầu (raw ADO.NET, không cần EF migration).

---

### Bước 5 — Smoke test: AddDevice API + verify log entry (Parameters NOT null)

**Lệnh:**
```bash
# Call 1: AddDevice với API key hợp lệ
curl -s -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -H "x-api-key: sup3rsecr3tap1key@" \
  -d '{"Ip":"192.168.99.200","Port":5555}'

# Call 2: AddDevice không có API key (expect 401)
curl -s -X POST http://localhost:3339/api/devices/connect-by-ip \
  -H "Content-Type: application/json" \
  -d '{"Ip":"192.168.99.200","Port":5555}'
```

**Kết quả API response:**

| Call | HTTP | Response |
|---|---|---|
| Call 1 (with key) | 422 | `{"success":false,"error":"AdbConnectFailed","message":"...timeout..."}` |
| Call 2 (no key) | 401 | `{"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}` |

**Kết quả DB (query ApiRequestLog):**

```
Id | ApiName   | HttpStatus | Parameters                            | Result      | CallerIp
26 | AddDevice | 422        | {"ip":"192.168.99.200","port":5555}   | Failure     | 192.168.48.1
27 | AddDevice | 401        | {"ip":"192.168.99.200","port":5555}   | Unauthorized| 192.168.48.1
```

**Kết quả:** PASS — `Parameters` KHÔNG null trên cả 2 rows (xác nhận fix UI-001 hoạt động trong image mới)

---

### Bước 6 — Verify các API khác không bị ảnh hưởng

| Endpoint | Method | Key | HTTP | Kết quả |
|---|---|---|---|---|
| `/api/devices` | GET | Không cần | 200 | `ok=True, devices=10` |
| `/health` | GET | Không cần | 200 | `{"ok":true,"adbVersion":"Android Debug Bridge version 1.0.41"}` |
| `/api/launch-app` | POST | Không có | 401 | `{"success":false,"error":"Unauthorized"}` |
| `/api/launch-app` | POST | Có | 422 | `{"success":false,"error":"AppNotInstalled"}` (device có, app không tồn tại — expected) |

**Kết quả:** PASS — không có endpoint nào bị regression

---

## Rollback Plan

Nếu phát hiện vấn đề sau khi deploy staging:

```bash
# 1. Rollback: dùng lại image cũ (rebuid sẽ dùng cache của build trước)
docker compose down

# 2. Nếu cần khôi phục DB (bảng ApiRequestLog chỉ INSERT, không DROP):
#    Bảng ApiRequestLog là ADDITIVE — không ảnh hưởng đến bảng Devices/Settings cũ.
#    Rollback code không cần xử lý DB vì không có DROP TABLE.

# 3. Restart với image cũ:
docker compose up -d
```

> **Lưu ý rollback DB:** Bảng `ApiRequestLog` được tạo bằng `CREATE TABLE IF NOT EXISTS` — nếu rollback code, bảng vẫn tồn tại nhưng không được INSERT thêm. Không gây lỗi cho các bảng khác.

---

## Tổng kết Staging Deploy

| Hạng mục | Kết quả |
|---|---|
| Rebuild image (--no-cache) | PASS — SHA 1900e158 |
| Restart container | PASS |
| App startup không exception | PASS |
| Bảng ApiRequestLog tồn tại | PASS |
| Parameters NOT null (fix UI-001) | PASS — confirmed rows 26, 27 |
| GET /api/devices hoạt động | PASS |
| Health check | PASS |
| LaunchApp API auth | PASS |
| **Staging status** | **READY — chờ DevOps Lead approve (STEP-3.5)** |

---

## Bước tiếp theo

- **STEP-3.5 — DevOps Lead:** Approve staging, verify smoke test độc lập, cấp phép deploy production
- **STEP-3.6 — DevOps Lead:** Deploy production + monitor

---

*Tạo bởi: DevOps Engineer | 2026-08-19 17:25*  
*Plan: `docs/plans/PLAN-api-request-log-2026-08-19/steps/STEP-3.4-doe-deploy-staging.md`*
