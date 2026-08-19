---
step: 2.1
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: todo
completed_at:
deps: [1.4]
---

# STEP 2.1 — Viết TDD: API contract, routing, reuse strategy

## Input nhận
Nhận Handoff Payload từ STEP-1.4 (PRD, US, RESOURCE, SPRINT đã có). PRD: `docs/prd/PRD-adb-add-device-api.md`. US: `docs/user-stories/US-adb-add-device-api.md`.

**Context kỹ thuật đã khảo sát sẵn (dùng luôn, không cần đọc lại toàn bộ codebase):**
- `AdbService.ConnectAsync(string ipPort, int timeoutMs = 10000, CancellationToken ct)` — file `src/KztekAdbPublishTool.Web/Services/AdbService.cs` — chạy `adb connect ip:port`, trả `AdbCommandResult { ExitCode, StdOut, StdErr, Success }`.
- `DeviceEndpoints.cs` (`src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs`) đã có `POST /api/devices/connect` nội bộ (không auth, dùng cho UI) — helper `EnsurePort(string ipPort) => ipPort.Contains(':') ? ipPort : $"{ipPort}:5555"` đã có sẵn, TÁI DÙNG được cho API mới thay vì viết lại logic default-port.
- `DeviceState` (`src/KztekAdbPublishTool.Web/State/DeviceState.cs`) — singleton in-memory, `TryGet(string serial, out DeviceRecord? device)` — dùng để check trạng thái Online/Offline mà không cần gọi adb trực tiếp (đã dùng pattern này trong `LaunchAppEndpoints.CheckDeviceState`).
- `ApiKeyEndpointFilter` (`src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs`) — IEndpointFilter có sẵn, đọc `LaunchAppSettings.ApiKey` (config section `LaunchApp:ApiKey`, env `LaunchApp__ApiKey`), so sánh constant-time, trả 401 nếu sai/thiếu. Gắn bằng `.AddEndpointFilter<ApiKeyEndpointFilter>()`. TÁI DÙNG NGUYÊN VẸN cho 2 endpoint mới — không viết filter mới.
- `LaunchAppEndpoints.cs` là ví dụ mẫu đầy đủ nhất về pattern: validate input → check DeviceState → gọi ADB → map kết quả sang IResult theo status code chuẩn `{ success, error, message, ... }`.
- Đăng ký route trong `Program.cs` theo pattern `app.MapXxxEndpoints()` — endpoint mới cần thêm dòng gọi tương ứng.

## Nhiệm vụ
Viết `docs/tech-design/TDD-adb-add-device-api.md` quyết định:
1. Tên file/namespace cho endpoint mới (gợi ý: `DeviceConnectionEndpoints.cs` hoặc mở rộng `DeviceEndpoints.cs` — Tech Lead tự quyết, ưu tiên tách file riêng vì đây là API public khác nhóm với API nội bộ UI).
2. Route chính xác: `POST /api/devices/connect-by-ip` (body: `{ ip: string, port?: int }`) và `GET /api/devices/{serial}/status` (path param serial).
3. Request/Response DTO, mã lỗi (400 invalid input, 401 unauthorized do filter, 404 serial not found cho status API — quyết định: connect-by-ip có cần biết serial trước không, hay chỉ cần ip:port).
4. Xử lý default port 5555 — dùng lại `EnsurePort` helper (cần quyết định: giữ `private static` trong `DeviceEndpoints.cs` hay refactor thành shared helper public dùng chung 2 nơi — tránh trùng lặp code).
5. Idempotency: `adb connect` gọi lại trên thiết bị đã kết nối — xác nhận hành vi (adb trả "already connected" hoặc tương tự) và map sang response thành công.
6. Có cần trigger `PollControlService.TriggerAsync` sau khi connect-by-ip thành công không (giống `/api/devices/connect` nội bộ đã làm) — để DeviceState cập nhật sớm, ảnh hưởng trực tiếp đến độ chính xác của API check-connection ngay sau đó.
7. Quyết định: đăng ký thêm dòng vào `Program.cs` gọi `app.MapXxxEndpoints()`.
8. Đánh giá bảo mật sơ bộ: có cần escalate CTO không (theo PLAN-MASTER, mặc định KHÔNG cần vì tái dùng auth cũ) — ghi rõ quyết định.

## Definition of Done
- [ ] `docs/tech-design/TDD-adb-add-device-api.md` có đủ: API contract 2 endpoint, DTO, status code table, pseudocode, quyết định reuse (EnsurePort, DeviceState, ApiKeyEndpointFilter)
- [ ] Xuất DOCX/PDF theo R1
- [ ] Ghi rõ trong TDD: KHÔNG escalate CTO (hoặc lý do escalate nếu Tech Lead thấy cần)

## Đã làm


## Artifact


## Quyết định quan trọng


## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo:
- watch_out:
- next_inputs:

## Commit
- Hash:
- Đã push:

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
