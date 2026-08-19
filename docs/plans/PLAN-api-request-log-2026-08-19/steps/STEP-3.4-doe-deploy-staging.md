---
step: "3.4"
plan: ../PLAN-MASTER.md
agent: devops-engineer
status: done
completed_at: "2026-08-19 17:25"
deps: ["3.3"]
---

# STEP 3.4 — DevOps Engineer: Deploy Staging

## Input nhận
- QA Lead APPROVED từ STEP-3.3 (sign-off 17:18, OBS-02: PHẢI rebuild image trước deploy)
- Commit hash sau merge: ae2a211 (fix UI-001), 8a89528 (TL approve fix), ac817b0, 1e8598a
- `docker-compose.yml` hiện tại của project (working dir: `/home/duonghoang21/docker/KztekAdbTool`)

## Nhiệm vụ
Deploy build mới lên môi trường staging, đảm bảo bảng `ApiRequestLog` được tạo tự động qua `CREATE TABLE IF NOT EXISTS` (raw ADO.NET, không phải EF migration), app khởi động bình thường, và smoke test cơ bản pass.

## Definition of Done
- [x] `docs/devops/DEPLOY-api-request-log.md` đã được tạo (checklist deploy + kết quả)
- [x] Build mới được tạo từ code mới nhất (docker compose build --no-cache)
- [x] Bảng `ApiRequestLog` tồn tại trong DB staging (CREATE TABLE IF NOT EXISTS tự chạy khi service init)
- [x] App khởi động không có exception (kiểm tra startup log)
- [x] Smoke test: gọi AddDevice API → có log entry trong DB staging, Parameters KHÔNG null
- [x] App staging hoạt động bình thường sau deploy (GET /api/devices, /health, /api/launch-app đều pass)
- [x] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm

1. **Rebuild image `--no-cache`**: `docker compose build --no-cache` — dotnet publish 0 error, image mới SHA `sha256:1900e1588f4dea6c759c189dbf61a82db967210359de8d87557aedd826f01365`. Lý do force no-cache: build thường trả toàn CACHED layers do source files trên disk chưa thay đổi kể từ build trước, không đảm bảo OBS-02.

2. **Restart container**: `docker compose down && docker compose up -d` — container mới khởi động với image mới.

3. **Verify startup log**: không có exception. App listening on `http://0.0.0.0:8080`, DevicePollWorker khởi động, warm-up 10 devices.

4. **Verify bảng ApiRequestLog**: `docker cp` + host sqlite3 → xác nhận bảng tồn tại với đúng 11 cột + index `IX_ApiRequestLog_Timestamp`. Bảng tự tạo qua `ApiRequestLogRepository.InitializeAsync()` khi service khởi động.

5. **Smoke test AddDevice API** (`/api/devices/connect-by-ip`):
   - Call với API key `sup3rsecr3tap1key@` → HTTP 422 (device 192.168.99.200 timeout — expected, thiết bị không tồn tại)
   - Call không có key → HTTP 401
   - DB query: rows 26 và 27 có `Parameters = {"ip":"192.168.99.200","port":5555}` KHÔNG null — **fix UI-001 xác nhận hoạt động trong image mới**

6. **Smoke test endpoints khác**:
   - `GET /api/devices` → 200, 10 devices
   - `GET /health` → 200, ADB version OK
   - `POST /api/launch-app` (no key) → 401
   - `POST /api/launch-app` (with key) → 422 (AppNotInstalled — expected)

7. **Tạo DEPLOY doc**: `docs/devops/DEPLOY-api-request-log.md` + chạy `md_to_docx_kztek.py` → DOCX OK, PDF thất bại (xelatex không có — non-blocking).

## Artifact

- `docs/devops/DEPLOY-api-request-log.md` — deploy checklist + kết quả chi tiết
- `docs/devops/DEPLOY-api-request-log.docx` — xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng

1. **Force `--no-cache`**: Theo OBS-02 của QA Lead, PHẢI đảm bảo image mới chứa fix `ae2a211`. Build thường (cached) không đủ tin cậy để xác nhận → dùng `--no-cache` cho chắc chắn.

2. **Bỏ qua "EF migration"** trong step file gốc: Feature thực tế dùng raw ADO.NET `CREATE TABLE IF NOT EXISTS` (không phải EF Core migration). Bảng tự tạo khi service init — không cần bước migration riêng.

3. **API key dùng cho smoke test**: `sup3rsecr3tap1key@` từ env `LaunchApp__ApiKey` trong `docker-compose.yml` (override `appsettings.json` có giá trị `123456a@`).

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Đã rebuild image (sha256:1900e158) và restart container — không rebuild lại. Bảng `ApiRequestLog` đã tạo trong DB staging.
- watch_out: API key staging là `sup3rsecr3tap1key@` (từ env docker-compose, KHÔNG phải giá trị `123456a@` trong appsettings.json). Bảng tự tạo qua code — không cần migration script.
- next_inputs: Staging URL `http://localhost:3339`; API key `sup3rsecr3tap1key@`; `docs/devops/DEPLOY-api-request-log.md` (kết quả smoke test đầy đủ); image SHA `sha256:1900e1588f4dea6c759c189dbf61a82db967210359de8d87557aedd826f01365`.

## Commit
- Hash: 264a9a5
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
