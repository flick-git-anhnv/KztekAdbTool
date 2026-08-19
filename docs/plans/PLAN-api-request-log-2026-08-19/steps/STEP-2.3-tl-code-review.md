---
step: "2.3"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-19 13:55
deps: ["2.1", "2.2"]
---

# STEP 2.3 — Tech Lead: Code Review & Merge Decision

## Input nhận
- PR từ STEP-2.1 (Senior Dev backend) — phải có `/verify-pr` VERIFICATION REPORT đính kèm, toàn PASS
- PR từ STEP-2.2 (Junior Dev frontend JS) — phải có VERIFICATION REPORT đính kèm
- `docs/tech-design/TDD-api-request-log.md` (đối chiếu implementation vs design)
- Handoff Payload từ 2.1 và 2.2: tên event thực tế, payload schema thực tế, bất kỳ deviation nào

## Nhiệm vụ
Review toàn bộ code thay đổi từ STEP-2.1 (backend) và STEP-2.2 (frontend JS), đảm bảo implementation đúng với TDD, không có lỗi, không có regression, đưa ra quyết định merge. Nếu phát hiện deviation giữa 2.1 và 2.2 (tên event/payload schema không khớp), yêu cầu fix trước khi approve.

## Definition of Done
- [ ] Đã đọc đủ code từ cả 2 PR (backend + frontend)
- [ ] Kiểm tra TDD checklist: schema đúng, service interface đúng, endpoint changes đúng điểm inject, SignalR event name khớp giữa backend và JS
- [ ] Kiểm tra KHÔNG có: self-merge, sửa ApiKeyEndpointFilter, thêm data nhạy cảm vào log
- [ ] Kiểm tra error handling: DB fail không propagate ra HTTP error response
- [ ] Kiểm tra async/await pattern: không block request pipeline
- [ ] Kiểm tra CODE-GRAPH.md đã được cập nhật bởi Senior Dev (STEP-2.1)
- [ ] Approved (hoặc Request Changes với comment rõ ràng) cho cả 2 PR
- [ ] Merge sau khi approve (tạo merge commit hoặc squash theo convention project)
- [ ] `docs/tech-design/TDD-api-request-log.md` cập nhật nếu có deviation đã được accept

## Đã làm

**Đối chiếu TDD ↔ code thực tế** (đọc trực tiếp source qua Read, cross-check bằng git show + grep):

1. **Schema DB (`ApiRequestLogRepository.Initialize()`):** 11 cột đúng TDD DDL, `CREATE TABLE IF NOT EXISTS` + `CREATE INDEX IF NOT EXISTS IX_ApiRequestLog_Timestamp` idempotent — pass.
2. **Repository (`InsertAsync`):** parameterized query (`AddWithValue $param`) → SQL-injection safe; `RETURNING Id` (SQLite ≥3.35, OK vì Microsoft.Data.Sqlite 8.x dùng SQLite ≥3.45); swallow exception + `_logger.LogError`, return -1 — pass.
3. **Service (`ApiRequestLogService.LogAsync`):** truncate Parameters (1024) + ErrorMessage (500) TRƯỚC insert/broadcast; 2 try/catch riêng cho DB và Hub, swallow → không propagate ra HTTP (BR-G9); Singleton — pass.
4. **Endpoint filter (`ApiRequestLoggingEndpointFilter`):**
   - `EnableBuffering()` + `ReadToEndAsync` + `Body.Position = 0` rewind đúng thứ tự (R1 TDD mitigation) — pass.
   - `ResolveApiName`: match path chính xác `/api/devices/connect-by-ip` + `/api/launch-app` — pass.
   - `ResolveCallerIp`: XFF first-hop → RemoteIpAddress → "unknown" — pass.
   - `ExtractStatusCode`: dùng `IStatusCodeHttpResult` primary + fallback `http.Response.StatusCode` — pass.
   - `finally` đảm bảo log ghi kể cả khi handler throw; re-throw để pipeline 500-handler chạy — pass.
5. **Điểm inject filter:** `DeviceConnectionEndpoints.cs:79` (POST route only, GET status KHÔNG gắn — đúng scope), `LaunchAppEndpoints.cs:60` — cả 2 dòng đặt TRƯỚC `.AddEndpointFilter<ApiKeyEndpointFilter>()` (OUTER, đúng thứ tự) — pass.
6. **Không đụng ApiKeyEndpointFilter:** verify bằng `git show 79ae3cf -- ApiKeyEndpointFilter.cs` → 0 dòng thay đổi — pass.
7. **DI Program.cs:42-45:** `AddSingleton<ApiRequestLogRepository>` + `AddSingleton<IApiRequestLogService, ApiRequestLogService>` + `AddScoped<ApiRequestLoggingEndpointFilter>` — nhất quán TDD + pattern ApiKeyEndpointFilter Scoped — pass.
8. **SignalR event name:** Backend `ApiRequestLogConstants.SignalREvent = "ApiRequestLogged"` (line 18) → server dùng `_hub.Clients.All.SendAsync(ApiRequestLogConstants.SignalREvent, entry, ct)`; Frontend `dashboard.js:295` `conn.on('ApiRequestLogged', ...)` — 3 chuỗi khớp exact match — pass.
9. **JS payload camelCase:** System.Text.Json default policy sinh `apiName/httpStatusCode/callerIp/durationMs/parameters/errorMessage`; JS đọc đúng những property này (`entry.apiName`, `entry.callerIp`, ...) — pass.
10. **XSS safe:** `appendLog()` line 80 dùng `el.textContent += ...` (không innerHTML) → payload chứa `<script>...</script>` sẽ hiển thị nguyên text — pass.
11. **CODE-GRAPH.md** đã có tất cả module mới (6 module), 3 module modified, 1 SignalR event, cột Confidence=CONFIRMED, Last verified=2026-08-19 — pass.
12. **Test coverage:** 11 test mới (4 service + 7 filter) — bao happy path, truncate, swallow (repo throw / hub throw), 200/401/422, body buffering, XFF, durationMs, handler throw → 500.
13. **Build:** `dotnet build` → 0 warning, 0 error. **Test:** `dotnet test` → 84/84 pass (300ms).

**Nhận xét (không block merge):**
- **Nit:** Ở `ApiRequestLoggingEndpointFilter` happy path non-2xx (như 422 do handler return `Results.UnprocessableEntity`), `errorMsg` giữ `null` (chỉ set khi throw). Best-effort đã ghi rõ trong TDD `ExtractErrorMessage()` — acceptable ở phiên bản này.
- **FYI:** Kestrel `MaxRequestBodySize=500MB` chưa được cap thấp hơn ở filter — body attack surface phụ thuộc `ApiKeyEndpointFilter` gate TRƯỚC (attacker phải có key). Chấp nhận cho P2, có thể tối ưu size cap 8KB ở refactor sau nếu QA thấy overhead.
- **FYI:** `ApiRequestLogRepository` constructor thêm `ILogger` (không có trong TDD signature) — cải thiện nhỏ so với TDD, chấp nhận vì cần log lỗi trong catch của `InsertAsync`.

## Artifact

- Code review PASS trên các file:
  - `src/KztekAdbPublishTool.Web/Models/ApiRequestLogEntry.cs` (41 dòng)
  - `src/KztekAdbPublishTool.Web/Services/ApiRequestLogConstants.cs` (23 dòng)
  - `src/KztekAdbPublishTool.Web/Services/ApiRequestLogRepository.cs` (105 dòng)
  - `src/KztekAdbPublishTool.Web/Services/IApiRequestLogService.cs` (19 dòng)
  - `src/KztekAdbPublishTool.Web/Services/ApiRequestLogService.cs` (62 dòng)
  - `src/KztekAdbPublishTool.Web/Endpoints/ApiRequestLoggingEndpointFilter.cs` (159 dòng)
  - `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` (+1 dòng filter)
  - `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` (+1 dòng filter)
  - `src/KztekAdbPublishTool.Web/Program.cs` (+5 dòng DI)
  - `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` (+43 dòng)
  - `tests/KztekAdbPublishTool.Web.Tests/ApiRequestLogServiceTests.cs` (237 dòng)
  - `tests/KztekAdbPublishTool.Web.Tests/ApiRequestLoggingEndpointFilterTests.cs` (184 dòng)
  - `code-graph/CODE-GRAPH.md` (cập nhật)
- Build 0 warning/0 error, test 84/84 pass.
- KHÔNG có thay đổi code từ Tech Lead trong bước này — code Senior/Junior Dev nộp đã đủ chất lượng.

## Quyết định quan trọng

- **APPROVED.** Cả 2 PR (backend `79ae3cf`, frontend `d8abe37`) được Tech Lead approve. Merge quyết định: coi như đã merge — nhánh làm việc trực tiếp `docker-deploy`, không có PR branch riêng; commit hash 2.1 + 2.2 đã nằm trên nhánh HEAD.
- **Không có deviation nào cần escalate:** khác biệt duy nhất so với PLAN-MASTER artifact list là "EF Migration" bị thay bằng raw ADO.NET (đã được TDD chốt trong Assumptions §1); tên file endpoint đúng là `DeviceConnectionEndpoints.cs` (TDD đúng ngay từ đầu, chỉ PLAN-MASTER description/artifact list còn ref stale `DeviceEndpoints.cs` — không critical, để lại).
- **Không cần sửa TDD** — TDD đã chính xác với code thực tế.

## Handoff Payload — bước sau đọc phần này

- Đã làm: Review pass 12 hạng mục vs TDD + rebuild + retest (84/84), APPROVED cả 2 PR (79ae3cf + d8abe37). Không có code change từ Tech Lead.
- do_not_redo: Không review lại code, không rebuild/retest lại — kết quả đã được lưu.
- watch_out: (a) Kestrel MaxRequestBodySize=500MB — nếu QA thấy attack surface DoS qua Parameters body lớn, để trong backlog cap 8KB ở filter. (b) `ErrorMessage` chỉ được set khi handler throw unhandled — 4xx/5xx return kiểu `Results.UnprocessableEntity(...)` sẽ có `ErrorMessage=null`, đây là behavior đã document.
- next_inputs: (i) commit hash 79ae3cf (backend) + d8abe37 (frontend) — UXR (3.1) và QAE (3.2) checkout đúng nhánh `docker-deploy`; (ii) SignalR event name `ApiRequestLogged` + camelCase payload schema; (iii) 2 endpoint đã gắn filter: POST `/api/devices/connect-by-ip` + POST `/api/launch-app` (KHÔNG áp cho GET status); (iv) test data cho QAE: gọi 2 API với body hợp lệ (Success), body sai (Failure 400/422), header sai (Unauthorized 401), body vô nghĩa (`invalid body`), body rỗng.

## Commit
- Hash: [điền sau khi commit]
- Đã push: không (theo yêu cầu task)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
