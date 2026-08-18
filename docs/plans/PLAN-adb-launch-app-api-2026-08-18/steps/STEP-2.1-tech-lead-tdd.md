---
step: "2.1"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-18 16:19
deps: ["1.4"]
---

# STEP 2.1 — Tech Lead: Technical Design Document (TDD)

## Input nhận
Output từ Phase 1: PRD (`docs/prd/PRD-adb-launch-app-api.md`), User Story (`docs/user-stories/US-adb-launch-app-api.md`), Resource plan, Sprint plan.

## Nhiệm vụ
Viết TDD đầy đủ cho tính năng API Launch App. Tech Lead quyết định thiết kế API contract, cơ chế auth API key, vị trí đặt code trong project hiện tại, và phạm vi áp dụng middleware.

## Definition of Done
- [x] File `docs/tech-design/TDD-adb-launch-app-api.md` đã tạo
- [x] TDD có đủ các mục sau:

### Mục bắt buộc trong TDD

**1. API Contract:**
- Route: `POST /api/launch-app` (đổi khỏi gợi ý `/api/devices/{serial}/launch-app` — lý do: nhất quán với `POST /api/install` hiện có, flat pattern; đã ghi rõ trong TDD)
- Request: header `x-api-key: <key>`, body JSON `{ "serial": "...", "app": "..." }`
- Response schema đầy đủ cho 200/400/401/404/422/500 (bám sát SC-01..SC-08 + EC1/EC4/EC6)

**2. Auth Middleware / API Key:**
- Cơ chế: `IEndpointFilter` (KHÔNG middleware toàn cục) — scope cứng 1 endpoint qua `.AddEndpointFilter<ApiKeyEndpointFilter>()`
- Config key: section `LaunchApp`, property `ApiKey`
- Env var override: `LaunchApp__ApiKey` (chuẩn ASP.NET Core `__` separator)
- Phạm vi: CHỈ endpoint `/api/launch-app` — quyết định đã chốt cứng với user

**3. Vị trí đặt code:**
- `Endpoints/LaunchAppEndpoints.cs` (theo pattern `InstallEndpoints.cs`)
- `Endpoints/ApiKeyEndpointFilter.cs` (đặt cùng thư mục `Endpoints/`, KHÔNG tạo `Middleware/` để tránh gợi ý sai)
- `Configuration/LaunchAppSettings.cs`
- Đăng ký DI + `MapLaunchAppEndpoints()` trong `Program.cs`

**4. Tái sử dụng AdbService:**
- Xác nhận signature `LaunchAppAsync(string serial, string packageName, CancellationToken ct = default)` (đọc từ `Services/AdbService.cs:171-188`)
- KHÔNG dùng `monkey` — comment source dòng 168-170 xác nhận lý do

**5. Error handling:**
- Mapping đủ 4 case StdErr → HTTP status: `"Không tìm thấy adb tại:"` → 500, `"No activities found to run"` → 422 AppNotInstalled, `"timeout sau"` → 422 AdbTimeout, else → 422 AdbError
- Log level theo bảng: Info (success/validation), Warning (401/timeout/error), Error (500/unhandled)

**6. Sequence diagram:** Đã có mermaid chi tiết đầy đủ nhánh 200/400/401/404/422/500

- [x] TDD được xuất DOCX bằng `scripts/md_to_docx_kztek.py` (PDF fail do thiếu xelatex — ghi nhận, không block)

## Đã làm
1. Đọc kỹ US-adb-launch-app-api (8 scenario SC-01..SC-08, BR1-BR6, EC1-EC7, Q1-Q5) + PRD gốc.
2. Đọc source: `Services/AdbService.cs` (LaunchAppAsync, AdbCommandResult, RunAsync — biết được text hardcoded "Không tìm thấy adb tại:", "No activities found to run", "timeout sau" để mapping error), `State/DeviceState.cs` (TryGet + Status property), `Endpoints/InstallEndpoints.cs` (pattern static class MapXxxEndpoints), `Program.cs` (DI/endpoint registration hiện tại), `Configuration/AdbSettings.cs` (pattern POCO config), `Models/DeviceRecord.cs` (Status string field), `appsettings.json`, `docker-compose.yml`.
3. Quyết định Q1–Q5:
   - Q1: POST (side-effect, body JSON, khớp `/api/install` pattern).
   - Q2: Trả `success`, `message`, `exitCode`, `stdOut`, `stdErr` (ADB output không nhạy cảm, dễ debug CI/CD).
   - Q3: Section `LaunchApp`, property `ApiKey`, env var `LaunchApp__ApiKey`.
   - Q4: Regex Android chuẩn `^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$` (chặt hơn BR1 tối thiểu).
   - Q5: KHÔNG thêm flag `-S` — `am start` mặc định bring-to-foreground; không mất state, cả 2 trường hợp ExitCode=0 → 200 nhất quán.
4. Chọn `IEndpointFilter` thay vì `IMiddleware` (idiomatic Minimal API + scope cứng 1 endpoint) — đảm bảo SC-07/BR3.
5. Thiết kế fail-safe: config `ApiKey` rỗng → mọi request 401 (không cho phép mode bypass).
6. Thiết kế constant-time compare với `CryptographicOperations.FixedTimeEquals` chống timing attack.
7. Viết TDD 12 section: Tham chiếu, Assumptions, Goals/Non-goals, Kiến trúc mermaid, API Contract chi tiết, Auth Filter, Vị trí code, Tái sử dụng AdbService, Validation package name, Phân biệt SC-03/SC-05, Map lỗi ADB, Q5, Error handling + log level, Sequence diagram, Rủi ro (R1-R7), Task breakdown (T-3.1a..T-4.4), Code Review Checklist 15 mục, Non-goals nhấn mạnh.
8. Xuất DOCX qua `scripts/md_to_docx_kztek.py` — DOCX OK, PDF fail xelatex (đã ghi nhận).

## Artifact
- `docs/tech-design/TDD-adb-launch-app-api.md` (mới)
- `docs/tech-design/TDD-adb-launch-app-api.docx` (xuất từ script KZTEK brand)
- PDF: không tạo được do thiếu xelatex (chấp nhận)

## Quyết định quan trọng
- **Route/verb chọn:** `POST /api/launch-app` (đổi khỏi gợi ý ban đầu `/api/devices/{serial}/launch-app` — lý do: nhất quán với `POST /api/install`, flat pattern).
- **Tên config key:** section `LaunchApp`, property `ApiKey`. Env var override: `LaunchApp__ApiKey`.
- **Vị trí auth check:** `IEndpointFilter` tại `Endpoints/ApiKeyEndpointFilter.cs` (KHÔNG tạo thư mục `Middleware/`).
- **Phạm vi middleware:** CHỈ áp cho endpoint `/api/launch-app` qua `.AddEndpointFilter<ApiKeyEndpointFilter>()` — KHÔNG dùng `app.Use()` toàn cục. Quyết định cứng đã chốt với user, KHÔNG cân nhắc lại phương án áp toàn bộ.
- **Fail-safe:** `ApiKey` rỗng → mọi request 401 (không cho phép bypass khi thiếu config).
- **Constant-time compare:** `CryptographicOperations.FixedTimeEquals` chống timing attack.
- **Validation package name:** regex Android chuẩn `^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$`.
- **Mapping SC-03 vs SC-05:** `DeviceState.TryGet == false` → 404 DeviceNotFound; `TryGet == true && Status != "Online"` → 422 DeviceOffline.
- **Mapping SC-06:** Kiểm `result.StdErr.Contains("No activities found to run")` → 422 AppNotInstalled.
- **Không sửa `AdbService`:** chỉ tái sử dụng `LaunchAppAsync` hiện có; không dùng `monkey`.
- **File tên chuẩn hoá:** `LaunchAppEndpoints.cs` (số nhiều, khớp `InstallEndpoints.cs`), `ApiKeyEndpointFilter.cs` (không phải `ApiKeyMiddleware.cs`).

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- **Đã làm:** TDD đầy đủ tại `docs/tech-design/TDD-adb-launch-app-api.md` — chốt POST /api/launch-app + IEndpointFilter + config LaunchApp:ApiKey + env var LaunchApp__ApiKey. Q1-Q5 đã quyết định hết.
- **do_not_redo:** KHÔNG cần bàn lại route/verb, không cần thay đổi cơ chế auth sang middleware toàn cục (user đã chốt cứng RIÊNG endpoint mới), không cần thảo luận lại Q1-Q5. KHÔNG sửa `AdbService.LaunchAppAsync` (chỉ tái sử dụng). KHÔNG dùng `monkey`.
- **watch_out:**
  1. Text-matching `StdErr` tiếng Việt (`"Không tìm thấy adb tại:"`, `"No activities found to run"`, `"timeout sau"`) là interface không chính thức của `AdbService` — nếu tương lai đổi text sẽ vỡ mapping error, ĐỪNG sửa `AdbService`.
  2. `IEndpointFilter` PHẢI gắn qua `.AddEndpointFilter<ApiKeyEndpointFilter>()` chỉ trên endpoint launch-app — nếu quên gắn = auth bypass (rủi ro R4).
  3. So sánh API key PHẢI dùng `CryptographicOperations.FixedTimeEquals` (constant-time), KHÔNG dùng `==`/`string.Equals` — chống timing attack (R2).
  4. Fail-safe: `ApiKey` rỗng → 401 (không bypass) — nếu dev để `appsettings.json` có key thật, tức đã leak vào git.
  5. `DeviceState.Status` case-sensitive `"Online"` — dùng `StringComparison.Ordinal` khớp convention codebase.
  6. `HttpContext.RequestAborted` truyền vào `LaunchAppAsync` để hỗ trợ client cancel.
  7. `LaunchAppEndpoints.cs` (số nhiều, khớp pattern) — không phải `LaunchAppEndpoint.cs` số ít.
  8. KHÔNG tạo thư mục `Middleware/` (dễ gợi ý sai global middleware) — để filter trong `Endpoints/`.
- **next_inputs:**
  - `docs/tech-design/TDD-adb-launch-app-api.md` (nguồn sự thật — Senior Dev đọc TOÀN BỘ trước khi code).
  - File cần tạo mới: `src/KztekAdbPublishTool.Web/Configuration/LaunchAppSettings.cs`, `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs`, `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs`.
  - File cần sửa: `src/KztekAdbPublishTool.Web/Program.cs` (thêm `Configure<LaunchAppSettings>` + `MapLaunchAppEndpoints`), `src/KztekAdbPublishTool.Web/appsettings.json` (thêm section `"LaunchApp": { "ApiKey": "" }`).
  - Route: `POST /api/launch-app`. DTO: `LaunchAppRequest { Serial, App }`.
  - Regex validation: `^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$`.
  - Tham khảo pattern static class từ `src/KztekAdbPublishTool.Web/Endpoints/InstallEndpoints.cs`.
  - Test unit BẮT BUỘC: filter (thiếu header → 401, sai key → 401, đúng key → next), endpoint (SC-01..SC-06 + EC1 + EC4 → 200/400/404/422/500 đúng schema).
  - CODE-GRAPH.md cần cập nhật ở STEP-3.1 (không phải bước này) — thêm mục "API endpoints: POST /api/launch-app" và "Dependencies: LaunchAppSettings, ApiKeyEndpointFilter".
  - Docker Compose (STEP-4.3 DevOps): thêm `LaunchApp__ApiKey=<value>` vào `docker-compose.yml`, KHÔNG commit key thật.

## Commit
- Hash: cd50c59 (initial), <sẽ có commit chốt tiếp theo cho hash + push log này>
- Đã push: có — `docker-deploy 7a3eb7b..cd50c59` lên `origin/docker-deploy`

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
