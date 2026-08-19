---
step: "1.5"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-19 13:38
deps: ["1.1", "1.2"]
---

# STEP 1.5 — Tech Lead: Technical Design Document

## Input nhận
- `docs/prd/PRD-api-request-log.md` từ STEP-1.1
- `docs/user-stories/US-api-request-log.md` từ STEP-1.2
- Codebase hiện tại: `src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs`, `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs`, `src/KztekAdbPublishTool.Web/Hubs/DeviceHub.cs`, `src/KztekAdbPublishTool.Web/wwwroot/js/signalr-client.js`, `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js`
- `code-graph/CODE-GRAPH.md` (nếu tồn tại — đọc trước)

## Nhiệm vụ
Viết Technical Design Document chi tiết cho feature API Request Log. TDD phải đủ để Senior Dev và Junior Dev làm song song mà không cần hỏi thêm: định nghĩa schema DB, service interface, cách inject vào endpoint, tên SignalR event + payload schema, và contract cho JS handler.

## Definition of Done
- [ ] `docs/tech-design/TDD-api-request-log.md` đã được tạo
- [ ] Mục "SQL Schema" có định nghĩa đầy đủ bảng `ApiRequestLog` (cột: Id, Timestamp, ApiName, Parameters [JSON hoặc text], Success [bool], ErrorMessage, CallerIp, DurationMs hoặc tương tự)
- [ ] Mục "Service Interface" có `IApiRequestLogService` với ít nhất method `LogAsync(...)` — signature đầy đủ (tên tham số, kiểu trả về)
- [ ] Mục "Endpoint Changes" mô tả rõ inject service vào `DeviceEndpoints.cs` và `LaunchAppEndpoints.cs` tại điểm nào (trước/sau xử lý, try/catch ra sao)
- [ ] Mục "SignalR Event Contract" định nghĩa: tên event (VD: `"ApiRequestLogged"`), payload schema (JSON object với các field), method trên Hub
- [ ] Mục "JS Handler Contract" chỉ rõ: tên event để đăng ký trong `signalr-client.js`, format log string để gọi `appendLog()` (hoặc hàm tương đương)
- [ ] Sequence diagram (text hoặc Mermaid) cho flow: API call → log service → DB write + SignalR broadcast → JS render
- [ ] Chia task breakdown rõ ràng: phần nào Senior Dev làm, phần nào Junior Dev làm
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
1. Đọc `code-graph/CODE-GRAPH.md` để hiểu module map, DI pattern, endpoints hiện có + confidence "CONFIRMED" cho tất cả module động chạm.
2. Đọc source thực tế: `DeviceConnectionEndpoints.cs`, `LaunchAppEndpoints.cs`, `ApiKeyEndpointFilter.cs`, `DeviceRepository.cs` (pattern SQLite ADO.NET), `DeviceHub.cs`, `InstallCoordinator.cs` (pattern `IHubContext<DeviceHub>`), `Program.cs`, `signalr-client.js`, `dashboard.js` (`appendLog()`). Xác nhận project KHÔNG dùng EF Core — dùng raw `Microsoft.Data.Sqlite` + `CREATE TABLE IF NOT EXISTS` idempotent.
3. Viết `docs/tech-design/TDD-api-request-log.md` đầy đủ 12 section: Tham chiếu, Assumptions, Goals/Non-goals, Kiến trúc (sequence + component diagram Mermaid), SQL Schema, Service Interface, Endpoint Changes, SignalR Event Contract, JS Handler Contract, Rủi ro, CODE-GRAPH impact, Task Breakdown Senior/Junior, Rollback Plan, Q&A đã đóng.
4. Đóng cả 5 câu hỏi Q-01..Q-05 (từ PRD/US) với quyết định rõ ràng — không để lửng.
5. Chạy `python3 scripts/md_to_docx_kztek.py docs/tech-design/TDD-api-request-log.md`: DOCX OK (398 KB), PDF fail do env thiếu `xelatex` — không block (§19.4 CLAUDE.md).

## Artifact
- `docs/tech-design/TDD-api-request-log.md` (37 KB, ~600 dòng)
- `docs/tech-design/TDD-api-request-log.docx` (398 KB, brand KZTEK Navy/Cam)
- PDF: KHÔNG có (env thiếu xelatex) — chấp nhận theo §19.4

## Quyết định quan trọng

| # | Quyết định | Chi tiết |
|---|---|---|
| D1 | **Persistence pattern** | Raw ADO.NET SQLite giống `DeviceRepository`, KHÔNG dùng EF Core (project chưa có EF setup) — bảng tạo qua `CREATE TABLE IF NOT EXISTS` idempotent. `PLAN-MASTER` artifact list "EF Migration" được diễn giải là "table creation script"; artifact thật là `ApiRequestLogRepository.Initialize()`. |
| D2 | **Inject point cho 401** | `ApiRequestLoggingEndpointFilter` NEW đặt OUTER (`.AddEndpointFilter` gọi TRƯỚC `.AddEndpointFilter<ApiKeyEndpointFilter>()`). Filter outer chạy đầu, `await next(context)` → chạy vào ApiKeyEndpointFilter → nếu 401 short-circuit trả về, filter outer vẫn nhận được `object?` result và extract `StatusCode` từ `IStatusCodeHttpResult`. KHÔNG sửa `ApiKeyEndpointFilter`. |
| D3 | **Tên SignalR event** | `"ApiRequestLogged"` — hằng số `ApiRequestLogConstants.SignalREvent`. Payload là `ApiRequestLogEntry` object serialize camelCase JSON. |
| D4 | **CallerIp resolution** | XFF first-hop nếu header có, fallback `HttpContext.Connection.RemoteIpAddress`, cuối cùng `"unknown"`. Helper `ResolveCallerIp(HttpContext)`. Không cấu hình `ForwardedHeadersOptions` toàn cục ở phiên bản này (defensive). |
| D5 | **Async pattern** | Filter `await _logService.LogAsync(entry, ct)` — SQLite < 10ms + SignalR < 5ms → tổng < 20ms, xa dưới AC 1s. Đổi latency nhỏ lấy zero-loss on crash. KHÔNG fire-and-forget với `Task.Run`. |
| D6 | **Error handling** | Cả `ApiRequestLogRepository` và `ApiRequestLogService` swallow exception + `ILogger.LogError/LogWarning`, KHÔNG throw ra caller filter → response gốc không bị 500 do side-effect log fail (BR-G9). |
| D7 | **Body buffering** | `TryReadBodyJsonAsync()`: `EnableBuffering()` → `StreamReader.ReadToEndAsync` → rewind `Body.Position = 0`. Cho phép filter đọc body trước khi handler cũng đọc — VÀ giữ Parameters kể cả với request 401 (defensive; PRD cho phép rỗng khi 401). |
| D8 | **Truncation** | `Parameters` max 1024 chars, `ErrorMessage` max 500 chars — clamp ở tầng service TRƯỚC khi ghi DB + broadcast. Không enforce ở SQLite. |
| D9 | **DI lifetime** | Repository + Service: `AddSingleton` (giống `DeviceRepository`). Filter: `AddScoped` (giống pattern implicit của `ApiKeyEndpointFilter`). |
| D10 | **Scope filter attachment** | Chỉ gắn `ApiRequestLoggingEndpointFilter` vào 2 route: `POST /api/devices/connect-by-ip` + `POST /api/launch-app`. Không gắn cho `GET /api/devices/{serial}/status` (PRD Non-goals, AC7). |
| D11 | **JS format** | `[API] {apiName} {paramsSummary} → {result} ({status}, {durationMs}) from {callerIp}` — plain text, không color-coded ở phiên bản này (UX Reviewer 3.1 có thể propose thêm nhưng không bắt buộc). Prefix `[HH:mm:ss]` do `appendLog()` tự thêm. |
| D12 | **JS file split** | Handler + format helper ở `dashboard.js` (theo pattern hiện có `DevicesUpdated`, `Log`, `InstallProgress`, `DeviceInstalled`). KHÔNG sửa `signalr-client.js`. |

## Handoff Payload — bước sau đọc phần này

- **Đã làm:** Viết TDD đầy đủ 12 section trong `docs/tech-design/TDD-api-request-log.md` (37KB). Đã đọc source thực tế và chốt 12 quyết định kỹ thuật (D1–D12) đủ để Senior Dev (2.1) và Junior Dev (2.2) code SONG SONG mà không cần hỏi lẫn nhau. Xuất DOCX OK, PDF fail (thiếu xelatex — chấp nhận theo §19.4).

- **do_not_redo:**
  1. KHÔNG khảo sát lại project để tìm pattern data access — đã xác nhận là **raw ADO.NET `Microsoft.Data.Sqlite`** (mirror `DeviceRepository`), KHÔNG có EF Core setup — chỉ tạo bảng idempotent qua `CREATE TABLE IF NOT EXISTS`.
  2. KHÔNG sửa `ApiKeyEndpointFilter.cs` — filter auth bất khả xâm phạm (PRD Non-goals + BR-G10). Logging 401 giải quyết bằng filter mới đặt outer.
  3. KHÔNG thảo luận lại tên SignalR event — đã chốt `"ApiRequestLogged"` (constant `ApiRequestLogConstants.SignalREvent`). Junior Dev đăng ký chính xác chuỗi này.
  4. KHÔNG thảo luận lại payload schema — đã chốt là `ApiRequestLogEntry` object camelCase (JSON schema chi tiết trong TDD mục "SignalR Event Contract").
  5. KHÔNG tự đổi async pattern sang `Task.Run` fire-and-forget — đã chốt sync-await (D5).
  6. KHÔNG sửa `signalr-client.js` — handler đặt ở `dashboard.js` theo pattern hiện có (D12).

- **watch_out:**
  1. **Order filter đăng ký matters (D2):** Trong `DeviceConnectionEndpoints.cs` và `LaunchAppEndpoints.cs`, PHẢI đặt `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` TRƯỚC `.AddEndpointFilter<ApiKeyEndpointFilter>()`. Nếu đảo ngược → filter logging chạy sau auth → KHÔNG bắt được 401. Verify bằng unit test 401.
  2. **Body double-read gotcha (R1):** PHẢI gọi `http.Request.EnableBuffering()` TRƯỚC read + `Body.Position = 0` SAU read. Nếu quên → handler đọc body sẽ nhận empty stream → validation fail nhầm.
  3. **Không gắn filter vào `GET /api/devices/{serial}/status`** — chỉ POST connect-by-ip. Đọc kỹ D10.
  4. **JSON serializer camelCase:** SignalR mặc định System.Text.Json với camelCase, nên `entry.ApiName` C# → `entry.apiName` JS. Junior Dev đọc `entry.apiName` (KHÔNG `entry.ApiName`) trong `formatApiRequestLog`.
  5. **XSS safety JS (T2.2.5):** `appendLog()` dùng `el.textContent +=` (đã escape sẵn). Junior Dev KHÔNG chuyển sang `innerHTML` khi thêm log entry. Kiểm bằng payload chứa `<script>` — không được alert.
  6. **Truncation ở service, không ở DB (D8):** Nếu Senior Dev chỉ truncate ở SQL param, sẽ có bug khi entry truncate-in-transit sang SignalR mà không truncate ở JS. → clamp ở `ApiRequestLogService.LogAsync` TRƯỚC cả Insert + SendAsync (như spec).
  7. **DurationMs = 0 cho 401 outer** thực tế: filter outer bắt đầu Stopwatch, kết thúc sau `await next()` → 401 rất nhanh cũng cho ~1-5ms. KHÔNG hard-code 0.
  8. **`ExtractStatusCode` fallback:** Nếu handler ghi thẳng `http.Response.StatusCode` mà không dùng `Results.*`, filter đọc `http.Response.StatusCode`. Trong project hiện tại tất cả handler đều dùng `Results.*` → `IStatusCodeHttpResult` sẽ work. Vẫn giữ fallback defensive.
  9. **File `dashboard.js` là ES5** — Junior Dev KHÔNG dùng arrow function / `const` block-scoped khi thêm handler nếu muốn giữ nhất quán style hiện có (dashboard.js dùng `function () {}` xuyên suốt).

- **next_inputs:**
  1. **TDD chính:** `docs/tech-design/TDD-api-request-log.md` — Senior Dev đọc mục "Task Breakdown → Senior Dev (STEP-2.1)" + SQL Schema + Service Interface + Endpoint Changes + SignalR Event Contract. Junior Dev đọc "Task Breakdown → Junior Dev (STEP-2.2)" + JS Handler Contract + SignalR Event Contract (để biết schema payload).
  2. **File mới Senior Dev tạo (11 file):** `Models/ApiRequestLogEntry.cs`, `Services/ApiRequestLogConstants.cs`, `Services/IApiRequestLogService.cs`, `Services/ApiRequestLogService.cs`, `Services/ApiRequestLogRepository.cs`, `Endpoints/ApiRequestLoggingEndpointFilter.cs`, unit tests. Sửa: `Program.cs`, `Endpoints/DeviceConnectionEndpoints.cs`, `Endpoints/LaunchAppEndpoints.cs`.
  3. **File Junior Dev sửa:** CHỈ `wwwroot/js/dashboard.js` — thêm 1 handler + 2 hàm helper `formatApiRequestLog` + `summarizeParams`. KHÔNG đụng `signalr-client.js`.
  4. **Constant tên event:** `"ApiRequestLogged"` — cần khớp chính xác backend `SendAsync("ApiRequestLogged", ...)` và frontend `conn.on('ApiRequestLogged', ...)`.
  5. **Sprint plan (đã có):** `docs/planning/SPRINT-api-request-log.md` — timeline, task board.
  6. **CODE-GRAPH:** Senior Dev PHẢI cập nhật `code-graph/CODE-GRAPH.md` (bảng 2.2 Module Dependencies + 2.3 API Endpoints + 2.4 SignalR Events + lịch sử cập nhật) SAU khi merge STEP-2.1. Danh sách thay đổi bắt buộc trong TDD mục "Đảm bảo tương thích với CODE-GRAPH".

## Commit
- Hash: 1b73bca
- Đã push: không (theo yêu cầu — Tech Lead không push, để Dispatcher/EM quyết định push khi phase phù hợp)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
