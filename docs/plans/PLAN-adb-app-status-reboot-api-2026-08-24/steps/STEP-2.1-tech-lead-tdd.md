---
step: 2.1
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: done
completed_at: 2026-08-24 22:28
deps: ["1.4"]
---

# STEP 2.1 — Viết TDD: API contract, ADB command strategy, response schema, UI spec

## Input nhận
- Từ MASTER (không có PRD/US/Sprint riêng — Phase 1 SKIPPED, scope đã ghi rõ trong PLAN-MASTER "API Contract dự kiến" + "Quyết định / Ghi chú tổng").
- Context codebase đã đọc: CODE-GRAPH.md, `Endpoints/LaunchAppEndpoints.cs`, `Endpoints/DeviceConnectionEndpoints.cs`, `Services/AdbService.cs`, `Services/IAdbService.cs`, `Endpoints/ApiKeyEndpointFilter.cs`, `Endpoints/ApiRequestLoggingEndpointFilter.cs`, `Services/ApiRequestLogConstants.cs`, `Pages/Index.cshtml`, `wwwroot/js/dashboard.js`, `Program.cs`.

## Nhiệm vụ
Viết `docs/tech-design/TDD-adb-app-status-reboot-api.md` — hoàn tất theo Definition of Done + xuất DOCX.

## Đã làm
- Đọc PLAN-MASTER + step file + CODE-GRAPH + 9 file source làm reference (LaunchAppEndpoints, DeviceConnectionEndpoints, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, ApiRequestLogConstants, IAdbService, AdbService, Index.cshtml, dashboard.js, Program.cs).
- Viết TDD 13 mục:
  1. Tham chiếu; 2. Goals/Non-goals; 3. Kiến trúc (mermaid); 4. API Contract 2 endpoint (response schema đầy đủ 200/400/401/404/422/500); 5. ADB command strategy (pidof + dumpsys mResumedActivity, fallback mFocusedActivity cho Android 6-7, reboot); 6. AdbService thay đổi (2 method mới + model `AppStatusResult` + enum `AppState`, KHÔNG thêm vào IAdbService); 7. Endpoint files code mẫu; 8. Program.cs 2 dòng Map; 9. Cập nhật `ApiRequestLogConstants` + `ResolveApiName`; 10. UI spec 2 nút + confirm dialog + apiHeaders() helper + `window.KZ_API_KEY` từ Razor; 11. Error matrix; 12. Rủi ro & mitigation; 13. Task breakdown 8 task ~7h; 14. Code Review checklist; 15. Q&A đã chốt 10 câu.
- Xuất DOCX bằng `scripts/md_to_docx_kztek.py --no-pdf` → thành công (`docs/tech-design/TDD-adb-app-status-reboot-api.docx`).

## Artifact
- `docs/tech-design/TDD-adb-app-status-reboot-api.md`
- `docs/tech-design/TDD-adb-app-status-reboot-api.docx`

## Quyết định quan trọng
1. **Package name cho nút "Kiểm tra trạng thái app":** dùng ô `#txt-package` HIỆN CÓ ("Gói cần theo dõi") — KHÔNG thêm modal/input riêng. Validate rỗng trước khi gọi API.
2. **`GetAppStatusAsync` + `RebootDeviceAsync` KHÔNG vào `IAdbService`:** follow tiền lệ `LaunchAppAsync`/`UninstallApkAsync` — IAdbService chỉ chứa 3 method DevicePollWorker cần cho testability. Endpoint gọi trực tiếp `AdbService` concrete qua DI như `LaunchAppEndpoints`.
3. **State enum:** trả về STRING `Foreground`/`Background`/`NotRunning` (C# enum `.ToString()`), KHÔNG dùng int.
4. **Reboot response:** `status: "RebootInitiated"` + message rõ "Device will go offline shortly" — NG2 nêu ngoài scope waiting for boot.
5. **Filter chain cả 2 endpoint:** OUTER `ApiRequestLoggingEndpointFilter` + INNER `ApiKeyEndpointFilter` — thống nhất với LaunchApp/AddDevice (khác `GET /api/devices/{serial}/status` cũ chỉ có INNER).
6. **ADB foreground detection:** primary `mResumedActivity` (Android 8+), fallback `mFocusedActivity` (Android 6-7). ROM lạ miss cả 2 → BUG follow-up, KHÔNG blocker merge.
7. **Reboot UI:** BẮT BUỘC confirm dialog + chọn ĐÚNG 1 thiết bị (không multi-select).
8. **API key client-side:** embed `window.KZ_API_KEY` từ Razor `IndexModel.PublicApiKey` (đọc `LaunchApp:ApiKey`) — chấp nhận trong scope internal LAN.
9. **Cập nhật `ApiRequestLogConstants`:** thêm 2 hằng `ApiAppStatus` + `ApiRebootDevice`; `ResolveApiName` dùng `EndsWith` cho route có `{serial}` placeholder.

## Handoff Payload — bước sau đọc phần này
- **Đã làm:** Viết TDD hoàn chỉnh (API contract + ADB command + response mapping + UI spec + task breakdown) + xuất DOCX. Không đụng code. 9 quyết định kỹ thuật đã chốt ở mục "Quyết định quan trọng".
- **do_not_redo:** Đừng đặt lại các câu hỏi Q1–Q10 (mục 13 TDD) — đã chốt. Đừng khảo sát lại pattern `LaunchAppEndpoints` — đã ghi lại đầy đủ trong §6.1/§6.2 TDD. Đừng viết lại regex package name — copy nguyên từ TDD §6.1.
- **watch_out:**
  1. Route mới có `{serial}` placeholder chứa `.`/`:` — PHẢI test URL encoding phía JS (`encodeURIComponent(serial)`) khi serial là dạng `192.168.1.100:5555`.
  2. `ResolveApiName` phải dùng `EndsWith("/app-status")` + `EndsWith("/reboot")` — KHÔNG dùng `.Equals()` vì route có placeholder.
  3. GOTCHA G008: filter đọc `context.Arguments` — với GET endpoint không body, `TryExtractParametersJson` sẽ skip đúng (do arg đầu là string route param, thuộc namespace System → filter bỏ qua). Không cần patch filter.
  4. Không thay đổi 4 file đang modified sẵn từ plan `adb-reconnect`: `docker-compose.yml`, `AdbService.cs` (cả 2 project), `appsettings.json` — KHÔNG revert, KHÔNG conflict.
  5. `window.KZ_API_KEY` — nếu Razor `Model.PublicApiKey` chưa có → phải thêm property vào `IndexModel.cs` (đọc từ `IOptions<LaunchAppSettings>`), khớp DI hiện có.
  6. `Program.cs` line 83 sau `MapDeviceConnectionEndpoints()` — thêm đúng 2 dòng `MapAppStatusEndpoints()` + `MapRebootEndpoints()` (thứ tự không quan trọng).
  7. Unit test tách `MapAppStatusResult`/`MapRebootResult`/`IsForegroundInDumpsys` thành `public static` để test không cần mock AdbService.
- **next_inputs:**
  1. TDD file: `docs/tech-design/TDD-adb-app-status-reboot-api.md` — Senior Developer đọc mục 6 (endpoint code), 5 (AdbService signature), 8 (UI JS), 7 (constants + filter), 11 (task breakdown 8 task).
  2. API contract cuối: xem TDD §3.1 + §3.2 (full JSON response cho từng status code).
  3. Tên file endpoint mới: `src/KztekAdbPublishTool.Web/Endpoints/AppStatusEndpoints.cs` + `RebootEndpoints.cs`.
  4. Signature AdbService:
     - `public async Task<AppStatusResult> GetAppStatusAsync(string serial, string packageName, CancellationToken ct = default)`
     - `public Task<AdbCommandResult> RebootDeviceAsync(string serial, CancellationToken ct = default)`
     - `public static bool IsForegroundInDumpsys(string dumpsysStdOut, string packageName)`
     - Model `AppStatusResult { bool Running; AppState State; AdbCommandResult PidofResult; AdbCommandResult? DumpsysResult }`
     - Enum `AppState { NotRunning, Background, Foreground }`
  5. UI spec: TDD §8 — 2 nút mới thêm vào action panel sau nút "Xóa thiết bị đã chọn"; JS handler ~60 dòng bám vào `bindEvents()`.
  6. Constants mới: `ApiAppStatus = "AppStatus"`, `ApiRebootDevice = "RebootDevice"` trong `ApiRequestLogConstants.cs`; sửa `ApiRequestLoggingEndpointFilter.ResolveApiName` để nhận route có `{serial}`.
  7. Filter chain BẮT BUỘC: `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` (OUTER) + `.AddEndpointFilter<ApiKeyEndpointFilter>()` (INNER) — TDD §3.3.

## Commit
- Hash: (điền sau commit)
- Đã push: (điền sau push)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
