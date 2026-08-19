---
step: "2.1"
plan: ../PLAN-MASTER.md
agent: senior-developer
status: done
completed_at: "2026-08-19 13:49"
deps: ["1.5"]
---

# STEP 2.1 — Senior Developer: Backend Implementation [∥ 2.2]

## Input nhận
- `docs/tech-design/TDD-api-request-log.md` từ STEP-1.5 — đọc kỹ: schema, service interface, endpoint changes, SignalR event contract, task breakdown
- `code-graph/CODE-GRAPH.md` (nếu tồn tại — đọc trước khi mở source files)
- Handoff Payload từ bước liền trước (1.5): xem mục "next_inputs" của STEP-1.5

## Nhiệm vụ
Implement toàn bộ backend của feature API Request Log theo TDD (1.5): tạo entity `ApiRequestLog`, EF migration, `IApiRequestLogService` + `ApiRequestLogService`, inject service vào `DeviceEndpoints.cs` và `LaunchAppEndpoints.cs`, broadcast qua `DeviceHub`. Bước này chạy song song với STEP-2.2 (Junior Dev làm JS) — KHÔNG phụ thuộc nhau về code, chỉ cần cùng dùng contract trong TDD.

## Definition of Done
- [ ] `src/KztekAdbPublishTool.Web/Data/ApiRequestLog.cs` entity có đủ properties theo TDD schema
- [ ] EF Migration mới được tạo (`dotnet ef migrations add AddApiRequestLog`) và đã verify `dotnet ef database update` chạy thành công
- [ ] `IApiRequestLogService` interface tạo tại đúng namespace theo convention project
- [ ] `ApiRequestLogService` implement đầy đủ `LogAsync(...)`: ghi DB (async, không block request pipeline) + broadcast SignalR event qua `DeviceHub`
- [ ] `ApiRequestLogService` được đăng ký trong DI container (`Program.cs` hoặc tương đương)
- [ ] `DeviceEndpoints.cs` inject service và gọi `LogAsync(...)` đúng điểm (sau xử lý, capture cả success và failure, KHÔNG sửa ApiKeyEndpointFilter)
- [ ] `LaunchAppEndpoints.cs` inject service và gọi `LogAsync(...)` tương tự
- [ ] Error trong logging (VD: DB write fail) KHÔNG propagate ra thành HTTP error response — log error nội bộ, tiếp tục trả kết quả API bình thường
- [ ] Unit test (hoặc integration test) tối thiểu cho `ApiRequestLogService.LogAsync()`: verify ghi DB và gọi SignalR hub
- [ ] `code-graph/CODE-GRAPH.md` cập nhật: thêm entity `ApiRequestLog`, service `IApiRequestLogService`/`ApiRequestLogService`, ghi nhận thay đổi trong 2 endpoint files
- [ ] PR description có `/verify-pr` VERIFICATION REPORT toàn PASS (chạy trước khi giao TL review)
- [ ] Script `scripts/md_to_docx_kztek.py` KHÔNG cần chạy (không tạo file .md mới, chỉ code)

## Đã làm
1. Tạo `Models/ApiRequestLogEntry.cs` — POCO 11 property (Id, Timestamp, ApiName, HttpMethod, Path, Parameters, Result, HttpStatusCode, ErrorMessage, CallerIp, DurationMs).
2. Tạo `Services/ApiRequestLogConstants.cs` — hằng số ApiName ("AddDevice"/"LaunchApp"), Result ("Success"/"Failure"/"Unauthorized"), SignalREvent ("ApiRequestLogged"), length caps (1024/500).
3. Tạo `Services/ApiRequestLogRepository.cs` — raw ADO.NET SQLite; `Initialize()` chạy `CREATE TABLE IF NOT EXISTS ApiRequestLog` + index idempotent; `InsertAsync` dùng `RETURNING Id`; catch mọi exception → return -1 (isolation).
4. Tạo `Services/IApiRequestLogService.cs` + `ApiRequestLogService.cs` — Singleton; truncate Parameters/ErrorMessage trước khi ghi; try `InsertAsync` catch swallow; try `hub.Clients.All.SendAsync("ApiRequestLogged", entry, ct)` catch swallow; không dùng fire-and-forget (await sync < 20ms).
5. Tạo `Endpoints/ApiRequestLoggingEndpointFilter.cs` — OUTER IEndpointFilter; `EnableBuffering()` + rewind `Body.Position = 0`; XFF first-hop → RemoteIpAddress → "unknown"; `ExtractStatusCode` từ `IStatusCodeHttpResult`; outcome switch 401→Unauthorized, 2xx→Success, else→Failure; `await LogAsync` trong `finally` (đảm bảo log kể cả khi handler crash + rethrow).
6. Sửa `DeviceConnectionEndpoints.cs` — `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` OUTER trước `ApiKeyEndpointFilter` trên POST route. GET route KHÔNG đụng (Non-goal PRD).
7. Sửa `LaunchAppEndpoints.cs` — tương tự, POST `/api/launch-app`.
8. Sửa `Program.cs` — `AddSingleton<ApiRequestLogRepository>()`, `AddSingleton<IApiRequestLogService, ApiRequestLogService>()`, `AddScoped<ApiRequestLoggingEndpointFilter>()`.
9. Tạo 2 test file: `ApiRequestLogServiceTests.cs` (4 test: happy path ghi DB, truncate params, truncate errMsg, hub throw swallow) + `ApiRequestLoggingEndpointFilterTests.cs` (7 test: 200/401/422, body buffering, handler throw → log, XFF, durationMs). Tổng 84/84 pass.
10. Cập nhật `code-graph/CODE-GRAPH.md` — 6 module mới, 2 endpoint modified, 1 SignalR event mới, Confidence CONFIRMED.

## VERIFICATION REPORT
Generated: 2026-08-19 13:49 | Branch: docker-deploy | By: Senior Developer (api-request-log STEP-2.1)

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| 1 | Build | PASS | `dotnet build KztekAdbPublishTool.Web.csproj` — 0 error, 0 warning |
| 2 | Lint / Type-check | SKIP | Project không có `dotnet format` config; không có ESLint/ruff |
| 3 | Test | PASS | `dotnet test` — 84/84 passed (11 test mới: 7 filter + 4 service), 0.9s |
| 4 | Security Quick Note | PASS | Không có hard-code credential; SQL hoàn toàn parameterized (`AddWithValue`); không có file nhạy cảm; không đụng auth/payment logic |
| 5 | Diff Self-review | PASS | Filter chain order đúng (OUTER trước INNER); `EnableBuffering` + rewind body; `finally` block đảm bảo log luôn được ghi; exception swallow đúng cả repo lẫn hub; GET route `/api/devices/{serial}/status` KHÔNG bị đụng (đúng Non-goal) |

**Tổng kết:** READY FOR REVIEW

## Artifact
- `src/KztekAdbPublishTool.Web/Models/ApiRequestLogEntry.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Services/ApiRequestLogConstants.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Services/ApiRequestLogRepository.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Services/IApiRequestLogService.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Services/ApiRequestLogService.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Endpoints/ApiRequestLoggingEndpointFilter.cs` (MỚI)
- `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` (sửa — 1 dòng filter)
- `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` (sửa — 1 dòng filter)
- `src/KztekAdbPublishTool.Web/Program.cs` (sửa — 3 dòng DI)
- `tests/KztekAdbPublishTool.Web.Tests/ApiRequestLogServiceTests.cs` (MỚI — 4 test)
- `tests/KztekAdbPublishTool.Web.Tests/ApiRequestLoggingEndpointFilterTests.cs` (MỚI — 7 test)
- `code-graph/CODE-GRAPH.md` (cập nhật)

## Quyết định quan trọng
- **Deviation EF Core → raw ADO.NET:** PLAN-MASTER (viết trước TDD) ghi "EF Migration". TDD đã chốt dùng raw ADO.NET mirror `DeviceRepository.cs`. Đã theo TDD — không tạo migration file, không thêm EF Core package. Bảng `ApiRequestLog` tạo idempotent qua `CREATE TABLE IF NOT EXISTS` tại constructor `ApiRequestLogRepository`.
- **Filter lifetime = Scoped** (như `ApiKeyEndpointFilter` pattern) — dependencies là Singleton/Logger, không có captive dependency issue.
- **Async await trong `finally`** — C# cho phép từ C# 5+. `LogAsync` luôn swallow exception nên không có exception từ finally làm mất exception gốc.
- **GET route KHÔNG log** — chỉ POST `/api/devices/connect-by-ip` được gắn filter. GET `/api/devices/{serial}/status` giữ nguyên (đúng Non-goal TDD).
- **SignalR event name thực tế:** `"ApiRequestLogged"` — đúng như TDD constant `ApiRequestLogConstants.SignalREvent`.
- **Body truncate limit:** Parameters = 1024 ký tự, ErrorMessage = 500 ký tự — truncate tại `ApiRequestLogService.LogAsync()` trước khi ghi.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Không tạo EF migration, không thêm EF Core package, không sửa ApiKeyEndpointFilter, không gắn filter vào GET route.
- watch_out: `ApiRequestLoggingEndpointFilter` phải được đăng ký SCOPED (không Singleton) — nếu refactor DI sau này cần giữ đúng lifetime. SignalR event name là `"ApiRequestLogged"` (không phải `"ApiRequestLog"` hay `"LogApiRequest"`). Junior Dev (STEP-2.2) đăng ký handler JS cần dùng đúng tên này.
- next_inputs: Commit hash `79ae3cf`. SignalR event: `"ApiRequestLogged"`. Payload camelCase: `{id, timestamp, apiName, httpMethod, path, parameters, result, httpStatusCode, errorMessage, callerIp, durationMs}`. File bị sửa: `DeviceConnectionEndpoints.cs`, `LaunchAppEndpoints.cs`, `Program.cs`. 11 test mới, 84/84 pass.

## Commit
- Hash: 79ae3cf
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
