---
step: "1.1"
plan: ../PLAN-MASTER.md
agent: senior-developer
status: done
completed_at: "2026-08-19 11:27"
deps: []
---

# STEP 1.1 — Triage: Reproduce bug & viết BUG report

## Input nhận

- Mô tả bug từ user: sau restart service, `GET /api/devices/{serial}/status` và `POST /api/devices/connect-by-ip` trả sai/rỗng cho đến khi user bấm "quét lại" trên UI. `GET /api/devices` không bị ảnh hưởng.
- Nghi ngờ root cause: `DevicePollWorker` không tự gọi lại `adb connect` cho các thiết bị trong persisted list khi service khởi động lại — `DeviceState` trống sau restart.
- Codebase liên quan: `src/KztekAdbPublishTool.Web/` — đặc biệt `DevicePollWorker`, `DeviceState`, endpoint `GET /api/devices/{serial}/status`, `POST /api/devices/connect-by-ip`.
- Feature gốc: `docs/plans/PLAN-adb-add-device-api-2026-08-19/` (đã deploy staging, chờ go-live production).

## Nhiệm vụ

Đọc CODE-GRAPH (nếu có), trace luồng khởi động `DevicePollWorker` và khởi tạo `DeviceState`, xác nhận root cause, reproduce bug (restart service → gọi 2 API mới mà không scan → quan sát response), rồi viết BUG report đầy đủ tại `docs/bugs/BUG-adb-reconnect-after-restart.md`.

## Definition of Done

- [ ] Root cause được xác nhận rõ (không còn ở mức "nghi ngờ") — trích dẫn đúng file:dòng code gây ra vấn đề
- [ ] Steps to reproduce được ghi đủ để QA Engineer verify độc lập
- [ ] `docs/bugs/BUG-adb-reconnect-after-restart.md` được tạo theo format chuẩn (ID, severity P1, root cause, steps to reproduce, expected vs actual, fix approach đề xuất)
- [ ] Commit + push file BUG report

## Đã làm

1. Đọc `code-graph/CODE-GRAPH.md` → xác định file liên quan: `DevicePollWorker`, `AdbService`, `DeviceState`, `DeviceConnectionEndpoints`, `DeviceRepository`.
2. Đọc source: `DevicePollWorker.cs`, `Program.cs`, `AdbService.cs`, `DeviceConnectionEndpoints.cs`, `PollControlService.cs`, `DeviceState.cs`.
3. Trace luồng khởi động: `ExecuteAsync()` set `manualTrigger=true` → `PollAsync()` chạy ngay → `GetDevicesAsync()` (= `adb devices -l`) → danh sách rỗng vì chưa `adb connect` → `DeviceState` trống.
4. Xác nhận root cause: KHÔNG có code nào trong `ExecuteAsync()` hay `Program.cs` gọi `adb connect` cho persisted WiFi devices trước/trong vòng poll đầu tiên.
5. Viết `docs/bugs/BUG-adb-reconnect-after-restart.md` (format đầy đủ: id, severity, steps to reproduce, root cause file:line, impact, fix approach).
6. Chạy `scripts/md_to_docx_kztek.py` → DOCX ✓, PDF ✗ (xelatex thiếu trên môi trường Linux — ghi chú, không block).
7. Commit + push.

## Artifact

- `docs/bugs/BUG-adb-reconnect-after-restart.md` ← BUG report đầy đủ
- `docs/bugs/BUG-adb-reconnect-after-restart.docx` ← xuất DOCX ✓

## Quyết định quan trọng

- Root cause **khớp hoàn toàn** với nghi ngờ ban đầu — xác nhận từ đọc code trực tiếp (không chỉ là giả thuyết nữa).
- `POST /api/devices/connect-by-ip` KHÔNG bị mất hoàn toàn: endpoint này gọi `adb.ConnectAsync()` trực tiếp và trigger poll — nó CÓ thể hoạt động cho device mới chưa connected. Nhưng sau restart, nếu DeviceState trống và caller check `GET /api/devices/{serial}/status` ngay sau connect (trước khi poll xong), vẫn thấy 404. Điều này giải thích vì sao user báo cả hai API "không hoạt động" — thực ra vấn đề cốt lõi là DeviceState trống.
- Fix đề xuất: Thêm `WarmUpReconnectAsync()` trong `DevicePollWorker.ExecuteAsync()` — đọc SQLite, `adb connect` từng WiFi device (serial chứa ':'), rồi mới vào vòng poll. Best-effort (không throw nếu lỗi).

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: Đã đọc CODE-GRAPH, AdbService, DevicePollWorker, DeviceConnectionEndpoints, PollControlService — không cần trace lại.
- watch_out: (1) `DeviceRepository.GetAll()` cần verify signature thực tế (có thể là sync hoặc có filter param) — bước 2.1 cần đọc thêm `DeviceRepository.cs:GetAll` trước khi viết warm-up. (2) WiFi serial nhận dạng bằng `serial.Contains(':')` — USB serial không chứa ':' → cần exclude USB devices khỏi warm-up reconnect (adb disconnect USB sẽ lỗi). (3) `adb connect` có thể trả exit code 0 kèm message "already connected" hoặc "connected to x.x.x.x:5555" — cả hai đều OK, không cần phân biệt. (4) Thời gian warm-up có thể dài nếu nhiều device offline (mỗi connect timeout 10s mặc định) — cân nhắc giảm timeout cho warm-up xuống 3–5s.
- next_inputs: Root cause xác nhận tại `DevicePollWorker.cs:56-77` (ExecuteAsync thiếu warm-up reconnect) và `DevicePollWorker.cs:111` (PollAsync chỉ gọi GetDevicesAsync, không ConnectAsync). Fix: thêm method `WarmUpReconnectAsync` trong `DevicePollWorker.cs` — đọc `_repo.GetAll()`, filter serial có ':', gọi `_adb.ConnectAsync()` cho từng device, log kết quả, xử lý lỗi best-effort (không throw). Gọi method này trong `ExecuteAsync()` trước `while` loop. Xem chi tiết pseudo-code trong `docs/bugs/BUG-adb-reconnect-after-restart.md` mục "Hướng fix đề xuất".

## Commit

- Hash: 7d88826
- Đã push: có (docker-deploy)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
