---
id: DEPLOY-adb-add-device-api
feature: adb-add-device-api
author: DevOps Engineer
reviewed-by: DevOps Lead
created: 2026-08-19
updated: 2026-08-19
status: staging-smoke-tested-nhom-b-pending
tdd: docs/tech-design/TDD-adb-add-device-api.md
---

# DEPLOY — API Thêm Thiết Bị (Connect-by-IP) và Kiểm Tra Kết Nối ADB

## Tóm tắt

Tài liệu này ghi lại quy trình deploy tính năng `POST /api/devices/connect-by-ip` và
`GET /api/devices/{serial}/status` vào môi trường Docker.

Smoke test container đã PASS ở mức 401/400/404 (NHÓM A — 7 case không cần thiết bị thật).
Smoke test NHÓM B (6 case cần thiết bị Android thật) CHƯA thực hiện được — đây là
**GATE BẮT BUỘC** phải hoàn tất trước khi go-live production (xem mục 5).

---

## 1. Cấu hình biến môi trường bắt buộc

API key được đọc qua ASP.NET Core configuration binding từ section `LaunchApp:ApiKey`
(tái dùng nguyên trạng — không tạo section mới, không tạo API key mới).
Khi chạy Docker, inject qua env var theo naming convention `__` (double underscore):

```
LaunchApp__ApiKey=<giá-trị-key-thật-khi-deploy>
```

### 1.1 Kiểm tra `docker-compose.yml`

File `docker-compose.yml` hiện đã có `LaunchApp__ApiKey` trong section `environment:`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://0.0.0.0:8080
  - LaunchApp__ApiKey=sup3rsecr3tap1key@
```

> **Bảo mật:** Không hardcode key thật vào `docker-compose.yml` rồi commit lên git.
> Dùng Docker secrets hoặc `.env` file (đã gitignore) cho môi trường production.
> Giá trị `sup3rsecr3tap1key@` hiện trong file chỉ là placeholder/staging — thay bằng key thật khi deploy production.

### 1.2 Chạy trực tiếp bằng `docker run` (smoke test / staging)

```bash
docker build -t kztek-adb-tool:<version-moi> .

docker run -d \
  --name kztek-adb-tool-staging \
  -p 18080:8080 \
  -e LaunchApp__ApiKey=<key-staging> \
  -v kztek-data:/app/data \
  -v kztek-uploads:/app/uploads \
  kztek-adb-tool:<version-moi>
```

---

## 2. Deploy Checklist (Production)

> Checklist này được DevOps Lead điền và sign-off tại STEP-4.4.

```
[x] PR approved bởi Tech Lead                              ✅ (STEP-3.2 done)
[ ] CI/CD pass toàn bộ                                    — (chưa có CI pipeline; unit test 64/64 PASS local)
[x] QA sign-off trên staging                               ✅ CÓ ĐIỀU KIỆN (xem mục 5)
[ ] DevOps Lead approve STAGING                            ⬜ (STEP-4.4 — chờ DevOps Lead)
[ ] DevOps Lead approve PRODUCTION                         ⬜ CHỜ USER — xem mục 8
[x] EM approve (feature lớn)                               ⏭️ Skipped — P1 feature không yêu cầu EM approve riêng
[x] Rollback plan đã chuẩn bị                              ✅ (xem mục 4)
[ ] Team nhận thông báo (#deploys)                         ⬜ (user thực hiện trước go-live production)
[ ] On-call standby 30 phút sau deploy                     ⬜ (user thực hiện khi deploy production)
[ ] Monitor dashboard đang theo dõi                        ⬜ (user thực hiện khi deploy production)
[ ] *** GATE CHƯA ĐÓNG: Smoke test thiết bị Android thật  ⬜ (user tự thực hiện — xem mục 5)
```

---

## 3. Kết quả smoke test container (2026-08-19)

**Môi trường:** Docker container local, image build từ `Dockerfile` ở root, không qua `docker-compose`.

**Lệnh build:**
```bash
docker build -t kztek-adb-tool-test:add-device-api .
docker run -d --name add-device-api-smoketest -p 18081:8080 \
  -e LaunchApp__ApiKey=smoketest-key-temp \
  kztek-adb-tool-test:add-device-api
```

**Health check:** `GET /health` → `{"ok":true,"adbVersion":"Android Debug Bridge version 1.0.41"}` (healthy)

### NHÓM A — Smoke test HTTP (không cần thiết bị Android thật)

| # | Test Case | Endpoint | Input | Expected | Actual | Status |
|---|-----------|----------|-------|----------|--------|--------|
| TC-A1 | 401 no key | POST /api/devices/connect-by-ip | Không có header x-api-key | 401 | 401 | PASS |
| TC-A2 | 401 wrong key | POST /api/devices/connect-by-ip | x-api-key: wrong-key | 401 | 401 | PASS |
| TC-A3 | 400 missing ip | POST /api/devices/connect-by-ip | key đúng, body `{}` | 400 | 400 | PASS |
| TC-A4 | 400 empty ip | POST /api/devices/connect-by-ip | key đúng, ip="" | 400 | 400 | PASS |
| TC-B1 | 401 no key | GET /api/devices/TEST123/status | Không có header x-api-key | 401 | 401 | PASS |
| TC-B2 | 401 wrong key | GET /api/devices/TEST123/status | x-api-key: wrong-key | 401 | 401 | PASS |
| TC-B3 | 404 serial not found | GET /api/devices/UNKNOWN_SERIAL_9999/status | key đúng, serial giả | 404 | 404 | PASS |

**Kết quả NHÓM A: 7/7 PASS**

**Xác nhận quan trọng:**
- `LaunchApp__ApiKey` được container đọc và áp dụng đúng (TC-A1/TC-A2/TC-B1/TC-B2 xác nhận auth middleware).
- Validation input hoạt động đúng (TC-A3/TC-A4 xác nhận `ValidateConnectInput`).
- Business logic chạy đến bước kiểm tra device (TC-B3 xác nhận auth pass + device lookup).
- Response format JSON `{"success":false,"error":"...","message":"..."}` đúng chuẩn TDD.

**Cleanup:** Container và image test đã xóa sau smoke test. Không để lại rác.

### NHÓM B — Smoke test với thiết bị Android thật

**Trạng thái: GATE CHƯA ĐÓNG** — Xem mục 5.

---

## 4. Rollback Plan

| Tình huống | Hành động |
|---|---|
| Deploy fail (container không start) | Giữ nguyên image version cũ, không thay thế |
| Container start nhưng /health fail | `docker stop` container mới, restart container version cũ |
| API trả sai response sau deploy | `docker stop` container mới, rollback image tag về version trước, `docker start` lại |
| Key bị lộ | Đổi `LaunchApp__ApiKey` ngay, restart container |

**Cách rollback nhanh:**
```bash
# Dừng container hiện tại
docker stop kztek-adb-tool

# Restart với image version cũ (tag trước đó)
docker run -d --name kztek-adb-tool ... <old-image>:<old-tag>
```

---

## 5. CẢNH BÁO — GATE CHƯA ĐÓNG: Smoke test với thiết bị Android thật

> **MỨC ĐỘ: BẮT BUỘC — KHÔNG ĐƯỢC GO-LIVE PRODUCTION KHI CHƯA HOÀN THÀNH**

**Vấn đề:** Sáu (6) scenario dưới đây CHỈ mới được verify gián tiếp qua unit test (64/64 PASS),
CHƯA verify qua HTTP thật với thiết bị Android thật.

| # | Test Case | Scenario | Lý do chưa verify |
|---|-----------|----------|--------------------|
| TC-C01 | connect thành công IP thật | POST /api/devices/connect-by-ip với IP thiết bị Android thật → 200 + serial "ip:port" trong response | Môi trường DevOps sandbox không có thiết bị Android/emulator kết nối |
| TC-C02 | connect port tùy chỉnh | POST với `port` khác 5555 → 200 + serial "ip:port-custom" | Tương tự trên |
| TC-C03 | GET status → Online sau connect | GET /api/devices/ip:port/status sau khi connect thành công → Online | Tương tự trên |
| TC-C04 | GET status → Offline sau disconnect | Ngắt kết nối ADB → GET lại → Offline | Tương tự trên |
| TC-C05 | connect idempotent | Gọi 2 lần cùng IP → response nhất quán, không lỗi | Tương tự trên |
| TC-C06 | connect fail thiết bị không ADB WiFi | POST với IP thiết bị không bật ADB WiFi → 422 | Tương tự trên |

**Hành động bắt buộc trước khi DevOps Lead approve production (STEP-4.4):**

Nếu STEP-4.4 cũng chạy trong môi trường sandbox không có thiết bị thật, đây là NGOẠI LỆ —
user PHẢI tự thực hiện thủ công:

```
1. Chuẩn bị 1 thiết bị Android thật (hoặc emulator) kết nối ADB với máy chạy Docker container
2. Build image từ code mới nhất, chạy container staging với key thật + mount ADB socket (hoặc bridge ADB over TCP)
3. Thực hiện thủ công từng case:
   a. TC-C01: curl POST /api/devices/connect-by-ip với IP thiết bị thật, không truyền port → kiem tra 200 + serial "ip:5555"
   b. TC-C02: curl POST với ip + port tùy chỉnh → kiểm tra 200 + serial "ip:port-custom"
   c. TC-C03: GET /api/devices/ip:5555/status → kiểm tra Online
   d. TC-C04: ngắt ADB WiFi → GET lại → kiểm tra Offline
   e. TC-C05: POST 2 lần cùng IP → kiểm tra cả hai đều 200, không bị lỗi trùng
   f. TC-C06: POST với IP thiết bị không bật ADB WiFi → kiểm tra 422
4. Ghi kết quả vào STEP-4.4 step file trước khi approve production
```

**Nếu bỏ qua bước này → KHÔNG được deploy production.**

---

## 6. Cấu hình tham khảo (TDD)

- Env var: `LaunchApp__ApiKey` (mapping `LaunchApp:ApiKey` trong appsettings)
- Header xác thực: `x-api-key`
- Endpoint 1: `POST /api/devices/connect-by-ip` — body: `{"ip":"...", "port":5555}`
- Endpoint 2: `GET /api/devices/{serial}/status` — response: `{"status":"Online|Offline|NotFound",...}`
- TDD đầy đủ: `docs/tech-design/TDD-adb-add-device-api.md`

---

## 7. Bàn giao cho User — Hành động thủ công còn lại trước khi go-live thật

> **Mức độ: BẮT BUỘC** — Toàn bộ agent chain (11 bước) đã hoàn thành đến STEP-4.3.
> Phần còn lại là hành động chỉ USER mới thực hiện được trên hạ tầng thật của KZTEK.

### Danh sách việc user cần tự làm (theo thứ tự)

**Bước 1 — Xác nhận API key trong `docker-compose.yml` hoặc deployment config**

File hiện có `LaunchApp__ApiKey=sup3rsecr3tap1key@` — xác nhận đây là key staging hợp lệ,
hoặc thay bằng key production thật. KHÔNG hardcode key thật rồi commit.

**Bước 2 — Build lại image trên hạ tầng thật**

```bash
docker build -t kztek/adb-tool:<version-moi> .
# Hoặc cập nhật image tag trong docker-compose.yml và chạy:
docker compose up -d --build
```

**Bước 3 — Chạy smoke test TC-C01–TC-C06 với thiết bị Android thật**

Đây là gate bắt buộc do QA Lead đặt ra. Chạy trên staging/pre-prod trước.

| Test | Hành động | Expected | Kết quả |
|------|-----------|----------|---------|
| TC-C01 | POST /api/devices/connect-by-ip với IP thật, không truyền port | 200 + serial "ip:5555" | [ ] PASS / [ ] FAIL |
| TC-C02 | POST với ip + port tùy chỉnh | 200 + serial "ip:port-custom" | [ ] PASS / [ ] FAIL |
| TC-C03 | GET /api/devices/ip:5555/status sau connect | 200 + status Online | [ ] PASS / [ ] FAIL |
| TC-C04 | Ngắt ADB WiFi → GET lại | status Offline | [ ] PASS / [ ] FAIL |
| TC-C05 | POST 2 lần cùng IP | cả hai 200, không lỗi | [ ] PASS / [ ] FAIL |
| TC-C06 | POST với IP thiết bị không bật ADB WiFi | 422 | [ ] PASS / [ ] FAIL |

**Bước 4 — Quyết định go-live**

- Cả 6 case PASS → chính thức deploy production, thông báo team (#deploys), standby monitor.
- Có case FAIL → BÁO NGAY cho Tech Lead trước khi deploy production. KHÔNG deploy khi biết có bug.

---

## 8. Lịch sử deploy

| Ngày | Môi trường | Người thực hiện | Kết quả | Ghi chú |
|------|-----------|-----------------|---------|---------|
| 2026-08-19 | Container (local smoke test) | DevOps Engineer | PASS (7/7 NHÓM A HTTP case) | Chưa có thiết bị Android thật (NHÓM B pending) |
