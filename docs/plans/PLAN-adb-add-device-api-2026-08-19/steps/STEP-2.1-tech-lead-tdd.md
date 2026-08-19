---
step: 2.1
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: done
completed_at: 2026-08-19 10:35
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
- Đọc trực tiếp source: `AdbService.cs` (xác nhận `ConnectAsync` KHÔNG trả serial, chỉ `AdbCommandResult`), `DeviceState.cs` (`TryGet` API), `DeviceEndpoints.cs` (pattern `EnsurePort`, `poll.TriggerAsync`), `LaunchAppEndpoints.cs` (mẫu handler auth-protected, `CheckDeviceState` trả 404), `ApiKeyEndpointFilter.cs` (fail-safe khi ApiKey rỗng), `Program.cs` (chỗ đăng ký `MapXxxEndpoints`), `DevicePollWorker.cs` (xác nhận serial WiFi luôn dạng `ip:port`), `PollControlService.cs`, `DeviceRecord.cs` (field `Status` = "Online"/"Offline"), `AdbSettings.cs` (PollIntervalMs=3000ms).
- Viết `docs/tech-design/TDD-adb-add-device-api.md`: chốt 7 assumption Q1-Q7, API contract 2 endpoint, DTO `ConnectByIpRequest`, bảng status code đầy đủ, pseudocode implement, task breakdown 7 subtask ~6.75h, STRIDE bảng đánh giá, quyết định không escalate CTO có lý do.
- Xuất `TDD-adb-add-device-api.docx` (PDF fail do server thiếu xelatex — chấp nhận theo §19.4).

## Artifact
- `docs/tech-design/TDD-adb-add-device-api.md` (source)
- `docs/tech-design/TDD-adb-add-device-api.docx`
- PDF: SKIP do server thiếu LaTeX engine — DOCX đủ cho review

## Quyết định quan trọng
1. **Q1 (serial trong response API 1):** `ConnectAsync` KHÔNG trả serial explicit; nhưng serial WiFi ADB CHÍNH LÀ `ip:port` vừa gọi (đã xác nhận qua `DevicePollWorker.PollAsync` — `adb.Serial` từ `adb devices -l` với WiFi luôn = `ip:port`). ⇒ Response API 1 echo `serial = "{ip}:{port}"` để client dùng ngay cho API 2.
2. **Q2 (NotFound status code API 2):** CHỐT **404 Not Found** — nhất quán với `LaunchAppEndpoints.CheckDeviceState` đã có; RESTful đúng ngữ nghĩa.
3. **Trigger poll sau connect thành công:** CÓ — gọi `PollControlService.TriggerAsync(ct)` khi `result.Success == true` để giảm delay update `DeviceState` từ tối đa 3s (`PollIntervalMs=3000`) xuống ~200ms; identical pattern với `/api/devices/connect` nội bộ.
4. **File mới:** `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` (tách khỏi `DeviceEndpoints.cs` vì thuộc nhóm public+auth khác nhóm UI nội bộ).
5. **KHÔNG tái dùng `EnsurePort`** — input mới là `(ip, port?)` tách rời không phải chuỗi `ipPort`; construct inline `$"{ip}:{port ?? 5555}"` với hằng số nội bộ `DefaultAdbPort = 5555`. Giữ nguyên `EnsurePort` private static trong `DeviceEndpoints.cs`.
6. **Validate `ip`:** non-empty + trim + không chứa `:` + không chứa whitespace (KHÔNG dùng regex IPv4 strict để cho phép hostname). Range port 1–65535.
7. **KHÔNG escalate CTO** — tái dùng nguyên trạng ApiKeyEndpointFilter + LaunchApp:ApiKey, không có threat rủi ro cao. `security-audit-stride` bắt buộc ở Bước 3.2 đủ để catch nếu xuất hiện.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: TDD đã chốt xong API contract 2 endpoint, DTO, status code, pseudocode, task breakdown. KHÔNG viết lại TDD, không thay đổi assumption Q1-Q7 trừ khi implementation phát sinh vấn đề mới. Đã đọc và xác nhận toàn bộ source liên quan — không cần đọc lại tất cả để suy luận lại.
- watch_out:
  1. Serial API 1 trả về DẠNG `ip:port` — client PHẢI URL-encode `:` thành `%3A` khi dùng làm path param cho API 2 (VD `/api/devices/192.168.1.100%3A5555/status`). Ghi note trong response hoặc README nếu có.
  2. `EnsurePort` của `DeviceEndpoints.cs` KHÔNG dùng ở endpoint mới — KHÔNG refactor thành shared helper, vì input model khác. Construct inline.
  3. Idempotency: `adb connect` trên thiết bị đã kết nối trả `ExitCode == 0` (StdOut: "already connected to ip:port"). Handler map thẳng 200 — KHÔNG cần code check "already".
  4. `ApiKeyEndpointFilter` fail-safe: nếu `LaunchApp:ApiKey` rỗng → 401 mọi request. Test integration cần set env `LaunchApp__ApiKey` trước.
  5. Route SC-B4 (serial rỗng): ASP.NET router tự trả 404 cho `/api/devices//status` — code validation 400 chỉ chạm khi client encode `%20` hoặc khoảng trắng. Cả 2 hành vi đều acceptable.
  6. Cả 2 endpoint PHẢI có `.AddEndpointFilter<ApiKeyEndpointFilter>()` — check kỹ khi review PR.
- next_inputs:
  1. TDD: `docs/tech-design/TDD-adb-add-device-api.md` (đầy đủ pseudocode, có thể paste-adapt trực tiếp).
  2. File cần tạo: `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs`.
  3. File cần sửa: `src/KztekAdbPublishTool.Web/Program.cs` — thêm 1 dòng `app.MapDeviceConnectionEndpoints();` ngay sau `app.MapLaunchAppEndpoints();` (dòng 73).
  4. Task breakdown 7 subtask T-2.1-A → T-2.1-G trong TDD section "Task breakdown" — tổng ~6.75h.
  5. Unit test target: `ValidateConnectInput` (internal static — access qua `InternalsVisibleTo` hoặc test cùng project).
  6. CODE-GRAPH: thêm 2 node endpoint mới + quan hệ với `ApiKeyEndpointFilter`, `AdbService.ConnectAsync`, `PollControlService.TriggerAsync`, `DeviceState.TryGet`.

## Commit
- Hash: d4f14a3
- Đã push: có (origin/docker-deploy)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
