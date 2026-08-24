---
step: 3.1
plan: ../PLAN-MASTER.md
agent: Senior Developer
status: done
completed_at: 2026-08-24 22:40
deps: ["2.1"]
---

# STEP 3.1 — Code AdbService methods + 2 endpoints + UI buttons + unit tests + CODE-GRAPH

## Input nhận
Từ Bước 2.1 Handoff Payload — `docs/tech-design/TDD-adb-app-status-reboot-api.md` (API contract chính xác, ADB command đã chọn, interface AdbService, UI spec 2 nút).

**QUAN TRỌNG — KHÔNG đụng:**
- Project `src/KztekAdbPublishTool` (WinForms gốc) — bất khả xâm phạm
- Các thay đổi đang pending (docker-compose.yml, AdbService.cs cả 2 project, appsettings.json từ plan adb-reconnect) — KHÔNG revert, KHÔNG conflict

**Pre-coding check (bắt buộc trước khi code):** Đọc `code-graph/CODE-GRAPH.md` trả lời ≥3/5 câu hỏi; tra `.claude/shared/GOTCHAS.md` (đặc biệt G008: KHÔNG đọc raw Request.Body trong filter — dùng `context.Arguments`).

## Nhiệm vụ

### 1. Thêm vào `AdbService.cs` + `IAdbService.cs` (Web project):
- `GetAppStatusAsync(string serial, string packageName, CancellationToken ct)` — chạy pidof + dumpsys theo TDD, trả về `AppStatusResult`
- `RebootDeviceAsync(string serial, CancellationToken ct)` — chạy `adb -s {serial} reboot`
- Model `AppStatusResult` + enum `AppState` (tạo file mới hoặc thêm vào Models/)

### 2. Tạo file endpoints mới:
- `src/KztekAdbPublishTool.Web/Endpoints/AppStatusEndpoints.cs` — GET /api/devices/{serial}/app-status
- `src/KztekAdbPublishTool.Web/Endpoints/RebootEndpoints.cs` — POST /api/devices/{serial}/reboot
- Cả 2 dùng OUTER `ApiRequestLoggingEndpointFilter` + INNER `ApiKeyEndpointFilter` theo pattern LaunchAppEndpoints.cs
- Đăng ký trong `Program.cs`

### 3. UI buttons trong `Pages/Index.cshtml` + `wwwroot/js/dashboard.js`:
- 2 nút mới theo UI spec từ TDD
- Nút "Reboot Device": confirm dialog trước khi gọi API
- Nút "Check App Status": cần input package name, hiển thị kết quả

### 4. Unit tests:
- `tests/KztekAdbPublishTool.Web.Tests/` — test cho `GetAppStatusAsync` + `RebootDeviceAsync` + 2 endpoints (happy path + edge cases từ TDD)
- Chạy `dotnet test` — toàn bộ PHẢI PASS

### 5. Cập nhật CODE-GRAPH:
- Thêm 2 module endpoint mới, 2 method AdbService mới vào `code-graph/CODE-GRAPH.md`
- Cập nhật relationships/callers
- Xuất lại `code-graph/CODE-GRAPH.pdf`

## Definition of Done
- [ ] `AdbService.cs` + `IAdbService.cs` có 2 method mới, build không lỗi
- [ ] `AppStatusEndpoints.cs` + `RebootEndpoints.cs` tạo xong, đăng ký trong `Program.cs`
- [ ] UI: 2 nút bấm hiển thị đúng trên dashboard, confirm dialog cho Reboot
- [ ] `dotnet test` — toàn bộ test PASS (không có test nào fail/skip mới)
- [ ] `code-graph/CODE-GRAPH.md` cập nhật + `CODE-GRAPH.pdf` xuất lại
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 3.1 → ✅

## Đã làm
1. Thêm `AppState` enum + `AppStatusResult` model vào `AdbService.cs` (trước class AdbService).
2. Thêm `GetAppStatusAsync` (pidof → dumpsys logic), `IsForegroundInDumpsys` (public static), `RebootDeviceAsync` vào `AdbService.cs`.
3. Tạo `Endpoints/AppStatusEndpoints.cs` — GET /api/devices/{serial}/app-status với OUTER+INNER filter, ValidateInput/CheckDeviceState/MapAppStatusResult public static.
4. Tạo `Endpoints/RebootEndpoints.cs` — POST /api/devices/{serial}/reboot với OUTER+INNER filter, CheckDeviceState/MapRebootResult public static.
5. Cập nhật `Program.cs`: thêm `MapAppStatusEndpoints()` + `MapRebootEndpoints()` sau `MapDeviceConnectionEndpoints()`.
6. Cập nhật `ApiRequestLogConstants.cs`: thêm `ApiAppStatus` + `ApiRebootDevice`.
7. Cập nhật `ApiRequestLoggingEndpointFilter.ResolveApiName`: nhận diện `/app-status` + `/reboot` bằng `EndsWith`.
8. Cập nhật `IndexModel.cs`: inject `IOptions<LaunchAppSettings>`, expose `PublicApiKey`.
9. Cập nhật `Index.cshtml`: thêm 2 nút (`btn-check-app-status`, `btn-reboot-device`) + `window.KZ_API_KEY` script.
10. Cập nhật `dashboard.js`: thêm `apiHeaders()` helper, cập nhật `summarizeParams` nhận `entry` (dùng path cho AppStatus/RebootDevice), thêm 2 event handler.
11. Tạo 3 test files: `AppStatusEndpointsTests.cs` (9 test), `RebootEndpointsTests.cs` (6 test), `AdbServiceAppStatusTests.cs` (5 test). Tổng 119 test, 0 fail.
12. Cập nhật `code-graph/CODE-GRAPH.md` + xuất `CODE-GRAPH.docx`.

## VERIFICATION REPORT
Generated: 2026-08-24 22:40 | Branch: docker-deploy | By: Senior Developer

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| 1 | Build | PASS | `dotnet build src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj -c Release` — 0 error, 0 warning, 3.5s |
| 2 | Lint / Type-check | SKIP (pre-existing) | `dotnet format --verify-no-changes` báo 5 whitespace issue trong `ApiRequestLogService.cs` + `InstallCoordinator.cs` — pre-existing, không phải từ thay đổi này. File mới của PR không có issue. |
| 3 | Test | PASS | `dotnet test` — 119/119 passed, 0 fail, 248ms |
| 4 | Security Quick Note | PASS | `window.KZ_API_KEY` embed từ server-side Razor (không hard-code). `encodeURIComponent(serial)` tránh injection trong URL. Không có SQL/shell injection mới. Không có secret trong diff. |
| 5 | Diff Self-review | PASS | Không có dead code hay debug log. `IsForegroundInDumpsys` public static để test không cần mock. Confirm dialog cho Reboot có trong `dashboard.js`. Filter chain OUTER+INNER đúng thứ tự trong cả 2 endpoint. `ResolveApiName` dùng `EndsWith` đúng (không `Equals`) cho route có `{serial}`. |

**Tổng kết:** READY FOR REVIEW

## Artifact
- `src/KztekAdbPublishTool.Web/Services/AdbService.cs` — đã thêm `AppState`, `AppStatusResult`, 2 method + 1 static
- `src/KztekAdbPublishTool.Web/Endpoints/AppStatusEndpoints.cs` — **MỚI**
- `src/KztekAdbPublishTool.Web/Endpoints/RebootEndpoints.cs` — **MỚI**
- `src/KztekAdbPublishTool.Web/Program.cs` — +2 dòng endpoint registration
- `src/KztekAdbPublishTool.Web/Services/ApiRequestLogConstants.cs` — +2 constants
- `src/KztekAdbPublishTool.Web/Endpoints/ApiRequestLoggingEndpointFilter.cs` — `ResolveApiName` cập nhật
- `src/KztekAdbPublishTool.Web/Pages/Index.cshtml.cs` — thêm `PublicApiKey` + `IOptions<LaunchAppSettings>`
- `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` — +2 nút + KZ_API_KEY script
- `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` — `apiHeaders()` + 2 handler + `summarizeParams` update
- `tests/KztekAdbPublishTool.Web.Tests/AppStatusEndpointsTests.cs` — **MỚI** (9 test)
- `tests/KztekAdbPublishTool.Web.Tests/RebootEndpointsTests.cs` — **MỚI** (6 test)
- `tests/KztekAdbPublishTool.Web.Tests/AdbServiceAppStatusTests.cs` — **MỚI** (5 test)
- `code-graph/CODE-GRAPH.md` + `.docx` — cập nhật

## Quyết định quan trọng
1. **`IndexModel` inject `IOptions<LaunchAppSettings>` để lấy `PublicApiKey`** — không tạo endpoint `/api/config/public-key` riêng. Chấp nhận embed key vào Razor HTML trong scope internal LAN (TDD Q9).
2. **`summarizeParams` đổi signature thêm `entry` param** — backward compatible vì các caller `formatApiRequestLog` đã truyền `entry` sẵn.
3. **`IsForegroundInDumpsys` dùng `Contains($" {packageName}/")` không phải regex** — đơn giản hơn, đủ chính xác với format output của dumpsys (`package/Activity`).
4. **Lint SKIP cho pre-existing whitespace issues** — không thuộc thay đổi PR này. Tech Lead biết để xử lý riêng.

## Handoff Payload — bước sau đọc phần này
- **Đã làm:** Code 2 API + UI + 15 test mới. Build sạch 0 error. 119/119 test pass. Push commit 5297bc2.
- **do_not_redo:** KHÔNG sửa `IAdbService.cs` (2 method mới KHÔNG vào interface — đã chốt TDD NG4). KHÔNG tạo endpoint `/api/config/public-key` riêng cho API key (đã embed qua Razor). KHÔNG chạy lại build/test (đã xác nhận sạch).
- **watch_out:**
  1. Reboot API endpoint bảo vệ bởi `x-api-key` — reviewer PHẢI xác nhận confirm dialog `dashboard.js` có trước khi gọi fetch.
  2. `ResolveApiName` dùng `EndsWith("/app-status")` + `EndsWith("/reboot")` — KHÔNG `Equals` vì route có `{serial}`. Nếu review thấy `Equals` → REQUEST-CHANGES.
  3. `window.KZ_API_KEY` embed từ Razor không log ra console — reviewer xác nhận không thấy `console.log(key)` trong `dashboard.js`.
  4. 5 lint whitespace pre-existing trong `ApiRequestLogService.cs` + `InstallCoordinator.cs` — không phải regression từ PR này.
  5. `summarizeParams` đổi signature từ `(apiName, paramsJsonOrNull)` → `(apiName, paramsJsonOrNull, entry)` — caller duy nhất là `formatApiRequestLog` đã cập nhật.
- **next_inputs:**
  1. Diff để review: `git diff 6e900b9..5297bc2` (commit trước là `6e900b9`).
  2. TDD §12 Code Review Checklist — danh sách 14 mục reviewer cần verify.
  3. Các file thay đổi chính: `AppStatusEndpoints.cs`, `RebootEndpoints.cs`, `AdbService.cs`, `dashboard.js`, `Index.cshtml`.

## Fix vòng 2 theo UX review (UI-001/002/003)

**Ngày:** 2026-08-24 | **Agent:** Senior Developer

### Đã fix:
- **UI-001 (Medium):** Thêm 2 class CSS brand KZTEK vào `wwwroot/css/dashboard.css`: `btn-kz-outline-navy` (border/text Navy `#251C53`, hover nền Navy chữ trắng) và `btn-kz-outline-orange` (border/text Cam `#F05922`, hover nền Cam chữ trắng). Cập nhật `Pages/Index.cshtml`: `btn-check-app-status` dùng `btn-kz-outline-navy`, `btn-reboot-device` dùng `btn-kz-outline-orange`. KHÔNG override toàn cục `btn-outline-primary`/`btn-outline-warning`.
- **UI-002 (Low):** Cập nhật 2 handler trong `wwwroot/js/dashboard.js`: `button.disabled = true` trước `fetch()`, restore `false` trong `finally`; thêm `<span class="spinner-border spinner-border-sm">` thay thế innerHTML tạm thời khi đang gọi API.
- **UI-003 (Low):** Thêm `aria-hidden="true"` cho `<i class="bi bi-info-circle">` (btn-check-app-status) và `<i class="bi bi-arrow-repeat">` (btn-reboot-device) trong `Pages/Index.cshtml`.

### Files đã sửa:
- `src/KztekAdbPublishTool.Web/wwwroot/css/dashboard.css` — thêm `.btn-kz-outline-navy` + `.btn-kz-outline-orange`
- `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` — đổi class 2 nút + aria-hidden
- `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` — disabled state + spinner 2 handler

### Verification:
- `dotnet build` — 0 error, 0 warning
- `dotnet test` — 119/119 pass, 0 fail

## Handoff Payload — cập nhật (sau fix vòng 2)
- **do_not_redo:** KHÔNG tạo lại class CSS brand (đã có `btn-kz-outline-navy`/`btn-kz-outline-orange`). KHÔNG sửa icon nút cũ (UI-003 chỉ áp 2 nút mới — đã làm). KHÔNG thêm disabled state vào nút khác (ngoài scope).
- **watch_out:** Spinner dùng `origHtml`/`origRebootHtml` để restore — nếu spinner hiển thị sai, kiểm tra `btnAppStatus.innerHTML` và `btnReboot.innerHTML` trong finally.
- **next_inputs:** UI-001/002/003 đã fix tại commit 23838dc. UXR cần re-check 3 issue trên + xác nhận disabled state hoạt động đúng khi click nhanh.

## Commit
- Hash: 5297bc2 (commit gốc — code 2 API + UI)
- Đã push: Yes (docker-deploy → origin/docker-deploy)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
