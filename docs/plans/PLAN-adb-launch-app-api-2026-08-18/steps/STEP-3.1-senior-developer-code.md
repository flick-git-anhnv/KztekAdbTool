---
step: "3.1"
plan: ../PLAN-MASTER.md
agent: senior-developer
status: done
completed_at: 2026-08-18 16:38
deps: ["2.1"]
---

# STEP 3.1 — Senior Developer: Code endpoint + auth + unit test

## Input nhận
Output từ Bước 2.1 (Tech Lead): `docs/tech-design/TDD-adb-launch-app-api.md` — bao gồm API contract, vị trí file, phạm vi auth filter, cấu hình appsettings, Code Review Checklist 14 mục.

## Nhiệm vụ
Implement endpoint `POST /api/launch-app` theo đúng TDD, thêm `ApiKeyEndpointFilter` (IEndpointFilter, constant-time compare), cấu hình `LaunchAppSettings`, viết unit test. Tái sử dụng `AdbService.LaunchAppAsync`. Chạy `/verify-pr` trước khi handoff cho Tech Lead review.

## Definition of Done
- [x] Đã đọc `code-graph/CODE-GRAPH.md` và chạy pre-coding-check trước khi code
- [x] File endpoint mới: `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs`
- [x] File filter: `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs`
- [x] POCO config: `src/KztekAdbPublishTool.Web/Configuration/LaunchAppSettings.cs`
- [x] `Program.cs` đã đăng ký `Configure<LaunchAppSettings>` + `MapLaunchAppEndpoints()`
- [x] `appsettings.json` — BLOCKED bởi config-protection hook (xem Quyết định quan trọng); section mặc định từ POCO = `""` → fail-safe 401 đúng
- [x] Unit test: 42 tests / 42 pass (bao gồm 22 test mới cho filter + endpoint)
- [x] Code KHÔNG dùng `monkey` — tái sử dụng `AdbService.LaunchAppAsync`
- [x] graphify không cài → cập nhật thủ công `code-graph/CODE-GRAPH.md` (Confidence: CONFIRMED)
- [x] VERIFICATION REPORT đính kèm bên dưới (toàn PASS)
- [x] Commit + push lên nhánh hiện tại

## Đã làm

### File mới tạo
1. `src/KztekAdbPublishTool.Web/Configuration/LaunchAppSettings.cs` — POCO `LaunchAppSettings { ApiKey = "" }`, `SectionName = "LaunchApp"`
2. `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs` — `IEndpointFilter` với `CryptographicOperations.FixedTimeEquals`, fail-safe: `ApiKey` rỗng → 401, không log key
3. `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` — static class với:
   - `MapLaunchAppEndpoints()` extension — đăng ký `POST /api/launch-app` + `.AddEndpointFilter<ApiKeyEndpointFilter>()`
   - `ValidateInput(serial, app) → string?` — public static (testable trực tiếp)
   - `CheckDeviceState(serial, deviceState, logger) → IResult?` — public static (testable trực tiếp)
   - `MapAdbResult(serial, app, result, logger) → IResult` — public static (testable trực tiếp)
   - DTO `LaunchAppRequest { Serial, App }`
4. `tests/KztekAdbPublishTool.Web.Tests/ApiKeyEndpointFilterTests.cs` — 5 test cases
5. `tests/KztekAdbPublishTool.Web.Tests/LaunchAppEndpointTests.cs` — 17 test cases

### File sửa
- `src/KztekAdbPublishTool.Web/Program.cs` — thêm `Configure<LaunchAppSettings>` + `app.MapLaunchAppEndpoints()`
- `tests/KztekAdbPublishTool.Web.Tests/KztekAdbPublishTool.Web.Tests.csproj` — thêm `FrameworkReference Include="Microsoft.AspNetCore.App"` để test dùng ASP.NET Core types
- `code-graph/CODE-GRAPH.md` — thêm `LaunchAppSettings`, `ApiKeyEndpointFilter`, `LaunchAppEndpoints`; thêm `POST /api/launch-app`; cập nhật callers của `AdbService`, `DeviceState`, `Program.cs`

### VERIFICATION REPORT

```
## VERIFICATION REPORT
Generated: 2026-08-18 16:38 | Branch: docker-deploy | By: Senior Developer (STEP-3.1)

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| 1 | Build | PASS | `dotnet build src/KztekAdbPublishTool.Web/` — 0 error, 0 warning mới |
| 2 | Lint / Type-check | PASS* | `dotnet format --verify-no-changes --include [3 file mới]` — 0 thay đổi. Lỗi lint ở file cũ (DeviceEndpoints.cs, InstallCoordinator.cs) là pre-existing, không phải tôi tạo ra |
| 3 | Test | PASS | `dotnet test` — 42/42 passed (22 test mới + 20 test cũ), 0.68s |
| 4 | Security Quick Note | PASS | Không hard-code key (default rỗng). Constant-time compare dùng FixedTimeEquals. Không injection risk (serial/app là input đến AdbService đã có sẵn). Đụng auth → Tech Lead PHẢI chạy security-audit-stride ở STEP-3.2 |
| 5 | Diff Self-review | PASS | Route đúng `/api/launch-app` (flat). Filter gắn qua `.AddEndpointFilter<>()` trên 1 endpoint. Không global middleware. Không log key. `StringComparison.Ordinal` cho "Online". `RequestAborted` truyền vào LaunchAppAsync |

**Tổng kết:** READY FOR REVIEW
```

## Artifact

| File | Loại | Ghi chú |
|------|------|---------|
| `src/KztekAdbPublishTool.Web/Configuration/LaunchAppSettings.cs` | Mới | POCO config |
| `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs` | Mới | IEndpointFilter auth |
| `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` | Mới | Handler + DTO + helper methods |
| `src/KztekAdbPublishTool.Web/Program.cs` | Sửa | Thêm 2 dòng DI + endpoint |
| `tests/KztekAdbPublishTool.Web.Tests/ApiKeyEndpointFilterTests.cs` | Mới | 5 unit tests filter |
| `tests/KztekAdbPublishTool.Web.Tests/LaunchAppEndpointTests.cs` | Mới | 17 unit tests endpoint |
| `tests/KztekAdbPublishTool.Web.Tests/KztekAdbPublishTool.Web.Tests.csproj` | Sửa | FrameworkReference |
| `code-graph/CODE-GRAPH.md` | Sửa | 3 module mới, 1 endpoint mới |
| `appsettings.json` | KHÔNG SỬA | Hook config-protection chặn — xem Quyết định quan trọng |

## Quyết định quan trọng

### 1. Không sửa được `appsettings.json` (hook config-protection)
Hook `.claude/hooks/config-protection.js` chặn việc sửa `appsettings.json`. Tuy nhiên không có vấn đề gì: `LaunchAppSettings.ApiKey` mặc định `string.Empty` từ POCO → fail-safe 401 đúng thiết kế khi chưa cấu hình env var. Giá trị thật inject qua `LaunchApp__ApiKey` env var như TDD mô tả.

**Tech Lead cần**: Nếu muốn document section này trong `appsettings.json`, thêm thủ công:
```json
"LaunchApp": {
  "ApiKey": ""
}
```

### 2. Static class không dùng làm type argument (`ILogger<LaunchAppEndpoints>`)
C# không cho phép static class làm type argument. Thay vì tạo marker class hoặc thêm `ILogger<LaunchAppRequest>`, dùng `ILoggerFactory` trong handler lambda và tạo logger với `loggerFactory.CreateLogger(nameof(LaunchAppEndpoints))`. Logger name trong log output sẽ là `"LaunchAppEndpoints"` — đúng như TDD mô tả.

### 3. Helper methods public static để testable
`ValidateInput`, `CheckDeviceState`, `MapAdbResult` được khai báo `public static` để unit test gọi trực tiếp mà không cần `WebApplicationFactory` hay Moq (không thể mock `AdbService` vì sealed class, không có interface). Pattern này đảm bảo coverage đầy đủ 17 cases bao gồm 200/422/500 mapping logic.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Route `/api/launch-app` (flat, không phải `/api/devices/{serial}/launch-app`). `IEndpointFilter` (không phải global middleware). Constant-time compare đã đúng. Fail-safe ApiKey rỗng → 401. Static class dùng `ILoggerFactory`. Helper methods public static để test.
- watch_out:
  1. Text-matching StdErr tiếng Việt — xem bảng mapping trong `LaunchAppEndpoints.MapAdbResult`. Đây là coupling với `AdbService` (R3 trong TDD Rủi ro). Kiểm tra TL kỹ 4 case: "Không tìm thấy adb tại:" (500), "No activities found to run" (422), "timeout sau" (422), else (422).
  2. Filter gắn qua `.AddEndpointFilter<ApiKeyEndpointFilter>()` trong `MapLaunchAppEndpoints` — verify route cũ `/api/install` KHÔNG bị áp filter (kiểm tra test hoặc curl).
  3. `appsettings.json` chưa được sửa (hook blocked) — không ảnh hưởng runtime nhưng Tech Lead có thể muốn ghi chú.
  4. `security-audit-stride` là BẮT BUỘC ở STEP-3.2 (đụng auth mới) — không bỏ qua.
- next_inputs: File cần review: `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs` (constant-time compare, fail-safe), `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` (filter gắn đúng chỗ, mapping đủ 8 status code), `src/KztekAdbPublishTool.Web/Program.cs` (DI config), test files để verify coverage.

## Commit
- Hash: f0581a8
- Đã push: có (docker-deploy → origin/docker-deploy)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
