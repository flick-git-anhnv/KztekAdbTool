---
plan: adb-app-status-reboot-api
date: 2026-08-24
agent: DevOps Engineer
environment: docker-compose / localhost:3339
status: staging-deployed
---

# DEPLOY — API App Status + Reboot Device

## 1. Thông tin triển khai

| Mục | Giá trị |
|-----|---------|
| Image mới | `kztek/adb-tool:latest` — `be8c5b3b6a74` |
| Image cũ (rollback ref) | `kztek/adb-tool:latest` trước build — `3cf8753aaabd` |
| Container cũ (dùng image) | `58d249aa1cdb` (untagged, đang chạy trước build) |
| Container mới | `fa43f4598d9a` |
| Port | `3339:8080` |
| Môi trường | `ASPNETCORE_ENVIRONMENT=Production` |
| Branch | `docker-deploy` |
| Thời điểm deploy | 2026-08-24 23:29 |

## 2. Deploy Checklist

```
[x] PR approved bởi Tech Lead (Bước 3.2 — APPROVED)
[x] CI/CD pass toàn bộ (119/119 dotnet test xanh — Bước 3.1)
[x] QA sign-off trên staging (Bước 4.2 — APPROVED CÓ ĐIỀU KIỆN)
[ ] DevOps Lead approve (Bước 4.4 — chờ)
[ ] EM approve (không bắt buộc — feature nhỏ)
[x] Rollback plan đã chuẩn bị (xem mục 6)
[ ] Team nhận thông báo (#deploys) — chờ DevOps Lead
[ ] On-call standby 30 phút sau deploy — chờ DevOps Lead
[x] Monitor dashboard đang theo dõi — health endpoint 200
```

## 3. Lệnh deploy thực thi

```bash
# Build image mới
docker compose build

# Redeploy container (recreate với image mới, giữ volume)
docker compose up -d

# Verify health
curl http://localhost:3339/health
```

**Kết quả build:**
```
#13 [kztek-adb-tool build 7/7] RUN dotnet publish ...
#13 6.314   KztekAdbPublishTool.Web -> /app/publish/
#13 DONE 6.5s
#18 writing image sha256:be8c5b3b6a74c226a768442af20bfd5d1341c69d15d3f1ab69096a2bc3d25943 done
#18 naming to docker.io/kztek/adb-tool:latest done
Built
```

**Kết quả redeploy:**
```
Container kztek-adb-tool  Recreate
Container kztek-adb-tool  Recreated
Container kztek-adb-tool  Starting
Container kztek-adb-tool  Started
```

**Container healthy:**
```
CONTAINER ID   IMAGE                   STATUS                    PORTS
fa43f4598d9a   kztek/adb-tool:latest   Up 10 seconds (healthy)   0.0.0.0:3339->8080/tcp
health → HTTP 200
```

## 4. Kết quả Smoke Test (curl thực tế)

### 4.1 API app-status — TC không cần thiết bị

**TC-1: GET /api/devices/UNKNOWN-SERIAL/app-status?package=com.test → 404 PASS**
```
Request:  GET /api/devices/UNKNOWN-SERIAL/app-status?package=com.test
          x-api-key: sup3rsecr3tap1key@
Response: HTTP 404
Body:     {"success":false,"error":"DeviceNotFound","message":"Device 'UNKNOWN-SERIAL' not found."}
```

**TC-2: GET /api/devices/UNKNOWN-SERIAL/app-status (no package) → 400 PASS**
```
Request:  GET /api/devices/UNKNOWN-SERIAL/app-status
          x-api-key: sup3rsecr3tap1key@
Response: HTTP 400
Body:     {"success":false,"error":"InvalidInput","message":"package is required"}
```

**TC-3: GET app-status wrong key → 401 PASS**
```
Request:  GET /api/devices/UNKNOWN-SERIAL/app-status?package=com.test
          x-api-key: WRONG_KEY
Response: HTTP 401
Body:     {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

**TC-4: GET app-status no key → 401 PASS**
```
Request:  GET /api/devices/UNKNOWN-SERIAL/app-status?package=com.test
          (no x-api-key header)
Response: HTTP 401
Body:     {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

### 4.2 API reboot — TC không cần thiết bị

**TC-5: POST /api/devices/UNKNOWN-SERIAL/reboot → 404 PASS**
```
Request:  POST /api/devices/UNKNOWN-SERIAL/reboot
          x-api-key: sup3rsecr3tap1key@
Response: HTTP 404
Body:     {"success":false,"error":"DeviceNotFound","message":"Device 'UNKNOWN-SERIAL' not found."}
```

**TC-6: POST /api/devices/UNKNOWN-SERIAL/reboot wrong key → 401 PASS**
```
Request:  POST /api/devices/UNKNOWN-SERIAL/reboot
          x-api-key: WRONG_KEY
Response: HTTP 401
Body:     {"success":false,"error":"Unauthorized","message":"Invalid or missing API key."}
```

### 4.3 UI Smoke Test

**TC-7: GET / → 200 + HTML chứa 2 nút mới PASS**
```
Request:  GET /
Response: HTTP 200
Verify:   grep "btn-check-app-status"  → 1 match (PASS)
          grep "btn-reboot-device"     → 1 match (PASS)
          grep "btn-kz-outline-navy"   → 1 match (PASS)
          grep "btn-kz-outline-orange" → 1 match (PASS)
```

### 4.4 Tóm tắt Smoke Test

| TC | Mô tả | Kỳ vọng | Kết quả |
|----|-------|---------|---------|
| TC-1 | GET app-status, serial không tồn tại | 404 | PASS |
| TC-2 | GET app-status, thiếu param package | 400 | PASS |
| TC-3 | GET app-status, key sai | 401 | PASS |
| TC-4 | GET app-status, không có key | 401 | PASS |
| TC-5 | POST reboot, serial không tồn tại | 404 | PASS |
| TC-6 | POST reboot, key sai | 401 | PASS |
| TC-7 | GET /, HTML có 2 nút mới | 200 + 4 element | PASS |
| TC-A01 | GET app-status thiết bị thật — foreground | 200 running=true | **GATE CHƯA ĐÓNG** |
| TC-B01 | POST reboot thiết bị thật (staging only) | 200 RebootInitiated | **GATE CHƯA ĐÓNG** |

## 5. GATE CHƯA ĐÓNG

`adb devices` trả về danh sách rỗng — không có thiết bị Android kết nối tại thời điểm deploy.

**TC-A01** (app-status foreground) và **TC-B01** (reboot thiết bị thật) chưa thực thi được.
- TC-B01 là destructive — CHỈ chạy trên thiết bị staging, KHÔNG chạy trên thiết bị production đang phục vụ user.
- DevOps Lead cần xác nhận 2 TC này trên thiết bị staging trước khi approve production go-live.

**Điều kiện đóng GATE:**
1. Kết nối thiết bị Android staging vào máy (hoặc kết nối qua TCP/IP).
2. Chạy TC-A01: `curl -H "x-api-key: sup3rsecr3tap1key@" "http://localhost:3339/api/devices/{serial}/app-status?package={package}"` — kỳ vọng HTTP 200 với `running=true/false` + `state`.
3. Chạy TC-B01 (staging device only): `curl -X POST -H "x-api-key: sup3rsecr3tap1key@" "http://localhost:3339/api/devices/{serial}/reboot"` — kỳ vọng HTTP 200 `{"status":"RebootInitiated"}`.
4. Ghi kết quả vào `docs/devops/DEPLOY-adb-app-status-reboot-api.md` mục 4.4 (cập nhật 2 dòng GATE CHƯA ĐÓNG → PASS).

## 6. Rollback Plan

### Option A — Rollback bằng image cũ (nhanh, < 1 phút)

Image trước build: `3cf8753aaabd`

```bash
# Tag lại image cũ thành latest
docker tag 3cf8753aaabd kztek/adb-tool:latest

# Redeploy với image cũ
docker compose up -d --force-recreate

# Verify health
curl http://localhost:3339/health
```

### Option B — Rollback bằng source code (nếu image cũ bị xóa)

```bash
# Checkout commit trước (xem git log để lấy commit hash)
git log --oneline -5
git checkout <commit-truoc-feature>

# Build lại và deploy
docker compose build
docker compose up -d
```

### Lưu ý an toàn rollback

- KHÔNG xóa volume `adb_data` hoặc `adb_uploads` khi rollback.
- KHÔNG dùng `docker compose down -v` (xóa volume).
- KHÔNG đụng các container khác (license-manager-*, postgres...).

## 7. Ghi chú bảo mật

- API key `LaunchApp__ApiKey` chỉ tồn tại trong env của container (docker-compose.yml không được commit).
- Key KHÁC với key local dev trong `appsettings.json`.
- Smoke test đã xác nhận 401 trả về đúng khi key sai/thiếu.

---
*Deploy thực hiện bởi DevOps Engineer — 2026-08-24 23:29*
*Chờ DevOps Lead approve (STEP-4.4) trước khi coi là production go-live.*
