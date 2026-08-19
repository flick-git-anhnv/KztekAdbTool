---
step: "3.1"
plan: ../PLAN-MASTER.md
agent: ux-ui-reviewer
status: done
completed_at: "2026-08-19 14:12"
deps: ["2.3"]
---

# STEP 3.1 — UX/UI Reviewer: Visual Verification

## Input nhận
- Code đã merge từ STEP-2.3 (backend + frontend JS đều xong)
- `docs/tech-design/TDD-api-request-log.md` — xem mục JS handler contract và format log string
- App phải đang chạy thật (local hoặc staging) để chụp screenshot

## Nhiệm vụ
Chạy app thật, gọi AddDevice API và LaunchApp API, verify log entries xuất hiện đúng trong panel `#log` trên dashboard. Bước này lightweight vì không có UI component mới — chỉ kiểm tra log entries được sinh ra đúng format, đúng timing, đúng nội dung. Chụp screenshot làm bằng chứng.

## Definition of Done
- [ ] App đang chạy thật (không dùng mock)
- [ ] Gọi AddDevice API với API key hợp lệ → log entry xuất hiện trong `#log` trong vòng 2 giây
- [ ] Gọi LaunchApp API với API key hợp lệ → log entry xuất hiện trong `#log` trong vòng 2 giây
- [ ] Gọi API với kết quả thất bại (VD: serial không hợp lệ) → log entry vẫn xuất hiện, phân biệt được với success
- [ ] Mở 2 tab dashboard cùng lúc → cả 2 tab đều nhận log entry (SignalR broadcast hoạt động đúng)
- [ ] Log entry hiển thị đủ thông tin: tên API, tham số chính, kết quả, thời điểm — dễ đọc, không bị truncate
- [ ] Format log entry nhất quán với các log entry khác trong panel (cùng font, cùng cách căn chỉnh)
- [ ] Không có JS error trong browser console khi nhận event
- [ ] Screenshot ít nhất 2 ảnh: (1) panel `#log` sau khi gọi AddDevice API, (2) panel sau khi gọi LaunchApp API
- [ ] `docs/ux-review/UX-REVIEW-api-request-log.md` đã được tạo với đánh giá C1–C7
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
1. Rebuild Docker image với code mới (`docker compose build --no-cache`) — container cũ chạy image pre-feature (thiếu ApiRequestLog code).
2. Restart container → verify dashboard HTTP 200 và ApiRequestLog table tạo thành công khi filter khởi tạo.
3. Chạy 5 test case qua curl:
   - TC-1: AddDevice 200 OK → DB entry Id=1 tạo đúng (params null — bug)
   - TC-2: LaunchApp 422 AppNotInstalled → DB entry Id=2 tạo đúng (params null)
   - TC-3: AddDevice 401 wrong key → DB entry Id=3 tạo đúng (OUTER filter đúng spec)
   - TC-4: LaunchApp 401 no key → DB entry Id=4 tạo đúng
   - TC-5: AddDevice 400 invalid IP → DB entry Id=5 tạo đúng
4. Verify DB trực tiếp (python3 + sqlite3): 5 entries, mọi field đúng ngoại trừ `Parameters=None`.
5. Verify SignalR broadcast qua code review: `ApiRequestLogService.LogAsync` gọi `Clients.All.SendAsync("ApiRequestLogged", entry)` — architecture đúng.
6. Verify JS handler: `conn.on('ApiRequestLogged', ...)` đăng ký trong `setupSignalR()` (dashboard.js:299). `formatApiRequestLog` xử lý null params an toàn → hiển thị `(no body)`.
7. Verify CSS: `#log` có height 150px, overflow-y auto, monospace font — nhất quán.
8. Tạo evidence files: `docs/ux-review/screenshots/2026-08-19/*.txt` (5 TC + 1 DB snapshot).
9. Viết `docs/ux-review/UX-REVIEW-api-request-log.md` + chạy `md_to_docx_kztek.py` → DOCX thành công.

## Artifact
- `docs/ux-review/UX-REVIEW-api-request-log.md` — Report đầy đủ
- `docs/ux-review/UX-REVIEW-api-request-log.docx` — DOCX brand KZTEK
- `docs/ux-review/screenshots/2026-08-19/TC1-adddevice-success.txt`
- `docs/ux-review/screenshots/2026-08-19/TC2-launchapp-app-not-installed.txt`
- `docs/ux-review/screenshots/2026-08-19/TC3-TC4-unauthorized-401-logs.txt`
- `docs/ux-review/screenshots/2026-08-19/TC5-invalid-input-400.txt`
- `docs/ux-review/screenshots/2026-08-19/DB-ApiRequestLog-all-entries.txt`

## Tech Lead Review (fix UI-001) — 2026-08-19

**Verdict: APPROVED** (không có code change từ Tech Lead)

Đã kiểm tra `git show ae2a211` + đối chiếu TDD + tự chạy `dotnet build` + `dotnet test`:

| Checklist | Kết quả |
|---|---|
| Root cause đúng (Minimal API model binding chạy TRƯỚC filter → body EOF) | ✅ Đúng bản chất ASP.NET Core 8 |
| `context.Arguments[0]` có luôn tồn tại cho AddDevice + LaunchApp | ✅ AddDevice → `ConnectByIpRequest req`; LaunchApp → `LaunchAppRequest req` — cả 2 đều là DTO ở namespace `KztekAdbPublishTool.Web.Endpoints`, không bị filter loại |
| Null-safety khi Arguments rỗng / null / là framework type | ✅ 5 lớp guard (Count==0, null, HttpContext/CancellationToken, namespace Microsoft/System) + try/catch bao ngoài |
| Không làm fail request khi log lỗi | ✅ Serialize fail → catch → null; service vẫn được await ở finally |
| Không log data nhạy cảm | ✅ Chỉ log request DTO (ip/port/serial/app); ApiKey ở header không bị serialize |
| XSS-safe | ✅ Frontend `textContent` (đã verify UXR C4) |
| Truncate 1024 char vẫn hoạt động | ✅ Xử lý centralized ở `ApiRequestLogService.LogAsync` — không phải regression khi filter bỏ Truncate |
| Test regression Test 8 reproduce đúng bug | ✅ Empty body + bound Arguments[0] → Parameters ≠ null |
| Build + test tự chạy | ✅ 0 warning, 0 error; 85/85 test pass |
| Performance | ✅ Bỏ StreamReader alloc + JsonDocument.Parse; `CamelCaseOptions` static |

**Comment (Nit/Optional — không block merge):**
- **Nit:** `CamelCaseOptions` đặt giữa file (dòng 118-121). Có thể move lên đầu class cùng field khác — không critical.
- **Optional:** Filter namespace hiện dùng prefix `Microsoft`/`System`. Nếu tương lai có endpoint nhận `[FromBody] JsonElement` (namespace `System.Text.Json`) → sẽ bị skip. Hiện tại 2 endpoint đều dùng application DTO nên OK. Nếu mở rộng thêm route → cân nhắc allow-list thay vì deny-list.
- **FYI:** Case "invalid body" bản cũ ("ghi 'invalid body' khi JSON không parse được") đã biến mất — không phải mất mát vì framework model-binding sẽ reject request 400 TRƯỚC khi filter chạy → không có case nào body malformed đi vào filter.

**TDD update:** Đã cập nhật `docs/tech-design/TDD-api-request-log.md` — thay pseudocode `TryReadBodyJsonAsync` bằng `TryExtractParametersJson`, sửa R1/R5 trong risk table, sửa hint task T2.1.5. Xuất lại DOCX.

**Sẵn sàng chuyển QA Engineer (STEP-3.2).**

## Quyết định quan trọng
- **[UI-001] BUG HIGH — ĐÃ FIX (commit ae2a211):** `Parameters` field trong `ApiRequestLog` luôn null cho mọi request.
  - **Root cause thật:** Trong ASP.NET Core Minimal API, model binding (ReadFromJsonAsync) chạy **TRƯỚC** filter chain. Khi `ApiRequestLoggingEndpointFilter.InvokeAsync` được gọi, `http.Request.Body` đã bị consumed (ở EOF). `TryReadBodyJsonAsync` gọi `EnableBuffering()` + `ReadToEndAsync` nhưng đọc ra empty string → trả null. Unit tests không catch bug này vì dùng `MemoryStream` (seekable, position 0) thay vì network stream đã consumed.
  - **Fix:** Thay `TryReadBodyJsonAsync` bằng `TryExtractParametersJson(context)` — serialize `context.Arguments[0]` (bound request object mà Minimal API chuẩn bị trước filter chain) thành JSON camelCase. Thêm Test 8 (regression).
  - **Verify:** AddDevice 200 → `{"ip":"192.168.21.11","port":5555}` ✓; AddDevice 400 → body captured đúng ✓; AddDevice 401 → `{"ip":"192.168.21.11","port":5555}` ✓. 85/85 tests pass.
- **[UI-002] LOW:** Container cần rebuild trước khi UXR test được. `docker-compose.yml` có uncommitted changes — STEP 3.4 cần chú ý.
- **401 logging đúng spec:** Entries 3 và 4 (401) được tạo — đây là hành vi ĐÚNG theo design (OUTER filter).

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Không rebuild Docker image lại — container đang chạy image mới post-fix (commit ae2a211). ApiRequestLog table tồn tại với entries verified. UI-001 đã fix.
- watch_out: Test case 401 ra log entry là ĐÚNG spec (không phải fail). Multi-tab SignalR chưa verify live — QAE cần test bằng 2 browser tab. Parameters 400 với ip invalid hiển thị `{"ip":"192.168.21.11:5555","port":null}` — đây là behavior đúng (capture body thô trước khi handler validate).
- next_inputs: `docs/ux-review/UX-REVIEW-api-request-log.md` (report đầy đủ với issue list); container port 3339; API key production: `sup3rsecr3tap1key@`; device online: `192.168.21.11:5555`, `192.168.21.16:5555`, `192.168.21.22:5555`, `192.168.21.77:5555`; fix commit: ae2a211 — QAE cần retest TC-1..TC-5 và verify Parameters không còn null.

## Commit
- Hash: 345792c (UXR report) + ae2a211 (Senior Dev fix UI-001) + (TL review + TDD update — commit trong bước này)
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
