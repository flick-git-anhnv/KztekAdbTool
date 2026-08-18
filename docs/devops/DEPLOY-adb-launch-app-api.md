---
id: DEPLOY-adb-launch-app-api
feature: adb-launch-app-api
author: DevOps Engineer
created: 2026-08-18
status: container-verified
tdd: docs/tech-design/TDD-adb-launch-app-api.md
---

# DEPLOY — API Launch App Android qua ADB

## Tóm tắt

Tài liệu này ghi lại quy trình deploy tính năng `POST /api/launch-app` vào môi trường Docker.
Smoke test container đã PASS ở mức 401/400/404. Smoke test với thiết bị Android thật CHƯA thực hiện
được (xem cảnh báo phần 5 bên dưới) — đây là gate PHẢI hoàn tất trước khi go-live production.

---

## 1. Cấu hình biến môi trường bắt buộc

API key được đọc qua ASP.NET Core configuration binding từ section `LaunchApp:ApiKey`.
Khi chạy Docker, inject qua env var theo naming convention `__` (double underscore):

```
LaunchApp__ApiKey=<giá-trị-key-thật-khi-deploy>
```

### 1.1 Thêm vào `docker-compose.yml` (thực hiện thủ công)

> **Lưu ý:** File `docker-compose.yml` hiện đang có pending changes ngoài phạm vi task này.
> DevOps Engineer KHÔNG tự sửa file để tránh conflict. User tự thêm dòng dưới vào section
> `environment:` của service tương ứng khi sẵn sàng deploy:

```yaml
environment:
  - LaunchApp__ApiKey=<giá-trị-key-thật-khi-deploy>
```

Ví dụ đầy đủ (fragment):

```yaml
services:
  kztek-adb-tool:
    image: ...
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - LaunchApp__ApiKey=my-secret-production-key-here
    ports:
      - "8080:8080"
```

> **Bảo mật:** TUYỆT ĐỐI KHÔNG hardcode key thật vào `docker-compose.yml` rồi commit lên git.
> Dùng Docker secrets hoặc `.env` file (đã gitignore) cho môi trường production.

### 1.2 Chạy trực tiếp bằng `docker run` (tuỳ chọn / smoke test)

```bash
docker run -d \
  --name kztek-adb-tool \
  -p 8080:8080 \
  -e LaunchApp__ApiKey=<key-thật> \
  -v kztek-data:/app/data \
  -v kztek-uploads:/app/uploads \
  <image-name>:<tag>
```

---

## 2. Deploy Checklist (Production)

> Checklist này phải được DevOps Lead điền và sign-off tại STEP-4.4 trước khi thực sự go-live.

```
[ ] PR approved bởi Tech Lead                              ✅ (STEP-3.2 done)
[ ] CI/CD pass toàn bộ                                    — (chưa có CI pipeline; unit test 42/42 PASS local)
[ ] QA sign-off trên staging                               ✅ CÓ ĐIỀU KIỆN (xem mục 5)
[ ] DevOps Lead approve                                    ⬜ (STEP-4.4 pending)
[ ] EM approve (feature lớn)                               ⏭️ Skipped — P1 feature không yêu cầu EM approve riêng
[ ] Rollback plan đã chuẩn bị                              ✅ (xem mục 4)
[ ] Team nhận thông báo (#deploys)                         ⬜ (trước khi deploy production)
[ ] On-call standby 30 phút sau deploy                     ⬜ (khi deploy production)
[ ] Monitor dashboard đang theo dõi                        ⬜ (khi deploy production)
[ ] *** GATE CHƯA ĐÓNG: Smoke test thiết bị Android thật  ⬜ (xem mục 5 — BẮT BUỘC trước go-live)
```

---

## 3. Kết quả smoke test container (2026-08-18)

**Môi trường:** Docker container local, image build từ `Dockerfile` ở root, không qua `docker-compose`.
**Lệnh build:**
```bash
docker build -t kztek-adb-tool-test:launch-app-api .
docker run -d --name launch-app-api-smoketest -p 18080:8080 \
  -e LaunchApp__ApiKey=smoketest-key-temp \
  kztek-adb-tool-test:launch-app-api
```

**Health check:** `GET /health` → `{"ok":true,"adbVersion":"Android Debug Bridge version 1.0.41"}` (container status: healthy)

**Kết quả smoke test HTTP:**

| # | Test Case | Mô tả | Expected | Actual | Status |
|---|-----------|-------|----------|--------|--------|
| TC-A | 401 wrong key | POST /api/launch-app, `x-api-key: wrong-key`, body hợp lệ | 401 | 401 | PASS |
| TC-B | 400 missing field | POST /api/launch-app, key đúng, body thiếu trường `app` | 400 | 400 | PASS |
| TC-C | 404 unknown serial | POST /api/launch-app, key đúng, serial không tồn tại | 404 | 404 | PASS |

**Xác nhận:** Env var `LaunchApp__ApiKey` được container đọc và áp dụng đúng (TC-A verify key comparison, TC-C verify auth pass + business logic chạy).

**Cleanup:** Container và image test đã xoá sau smoke test. Không để lại rác.

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

## 5. CẢNH BAO — Gate CHUA DONG: Smoke test voi thiet bi Android that

> **MUC DO: BẮT BUOC — KHONG DUOC GO-LIVE PRODUCTION KHI CHUA HOAN THANH**

**Van de:** Ba scenario sau CHI moi duoc verify gian tiep qua unit test (42/42 PASS), CHUA verify qua HTTP that voi thiet bi Android that:

| Test Case | Scenario | Ly do chua verify |
|---|---|---|
| TC-001 | 200 happy path — launch app thanh cong tren thiet bi that | Moi truong QA va DevOps sandbox khong co thiet bi Android/emulator ket noi |
| TC-006 | 422 — device offline | Tuong tu tren |
| TC-007 | 422 — app chua cai (package not found) | Tuong tu tren |

**Hanh dong bat buoc truoc khi DevOps Lead approve production (STEP-4.4):**

Neu STEP-4.4 cung chay trong moi truong sandbox khong co thiet bi that, day la NGOAI LE — user PHAI tu thuc hien thu cong:

```
1. Chuan bi 1 thiet bi Android that (hoac emulator) ket noi ADB voi may chay Docker container
2. Chay container staging voi key that + mount ADB socket (hoac bridge ADB over TCP)
3. Thuc hien thu cong:
   a. TC-001: curl POST /api/launch-app voi serial thiet bi that, app da cai → kiem tra 200 + app mo
   b. TC-006: ngat ket noi thiet bi → curl lai → kiem tra 422 "device offline"
   c. TC-007: curl voi package name chua cai → kiem tra 422 "package not installed"
4. Ghi ket qua vao STEP-4.4 step file truoc khi approve
```

**Neu bo qua buoc nay → KHONG duoc deploy production.**

---

## 6. Cau hinh tham khao (TDD)

- Env var: `LaunchApp__ApiKey` (mapping `LaunchApp:ApiKey` trong appsettings)
- Header xac thuc: `x-api-key`
- Endpoint: `POST /api/launch-app`
- Body: `{"serial": "...", "app": "com.example.package"}`
- TDD day du: `docs/tech-design/TDD-adb-launch-app-api.md`

---

## 7. Lich su deploy

| Ngay | Moi truong | Nguoi thuc hien | Ket qua | Ghi chu |
|------|-----------|-----------------|---------|---------|
| 2026-08-18 | Container (local smoke test) | DevOps Engineer | PASS (3/3 HTTP case) | Chua co thiet bi Android that |
