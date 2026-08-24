---
project: KztekAdbPublishTool (multi-project solution)
last_updated: 2026-08-24
updated_by: Senior Developer (adb-app-status-reboot-api STEP-3.1)
---

# CODE-GRAPH — KztekAdbPublishTool Solution

## Lịch sử cập nhật

| Ngày | Người | Thay đổi |
|---|---|---|
| 2026-08-05 | Senior Developer | Tạo mới — Phase 1 Foundation (STEP 1.1–1.3) |
| 2026-08-10 | Senior Developer | Cập nhật đầy đủ sau Phase 2+3 hoàn tất; thêm `DeviceState.Remove()` (bug fix R9); bổ sung toàn bộ module Services/Endpoints/State còn thiếu |
| 2026-08-18 | Senior Developer | STEP-3.1: Thêm `POST /api/launch-app` — `LaunchAppSettings`, `ApiKeyEndpointFilter`, `LaunchAppEndpoints`; cập nhật callers của `AdbService`, `DeviceState`, `Program.cs` |
| 2026-08-19 | Senior Developer | STEP-3.1 [adb-add-device-api]: Thêm `POST /api/devices/connect-by-ip` + `GET /api/devices/{serial}/status` — `DeviceConnectionEndpoints`; cập nhật callers của `AdbService`, `DeviceState`, `PollControlService`, `ApiKeyEndpointFilter`, `Program.cs` |
| 2026-08-19 | Senior Developer | BUG-adb-reconnect: Thêm `IAdbService` interface; `AdbService : IAdbService`; `DevicePollWorker` dùng `IAdbService` + thêm `WarmUpReconnectAsync()` (internal); `Program.cs` đăng ký `IAdbService`; thêm 6 unit tests `DevicePollWorkerWarmUpTests` |
| 2026-08-19 | Senior Developer | BUG-adb-reconnect STEP-2.3: `AdbService.RunAsync` — re-throw OCE khi `ct.IsCancellationRequested` (phân biệt per-device timeout vs service shutdown); `WarmUpReconnectAsync` — fix logic ExitCode!= 0 → continue (không return/break), fix catch OCE → check ct, fix log message; thêm 2 unit tests (TimeoutOnSerial + OceOnSerial) |
| 2026-08-19 | Senior Developer | api-request-log STEP-2.1: Thêm `Models/ApiRequestLogEntry`, `Services/ApiRequestLogConstants`, `Services/ApiRequestLogRepository` (raw ADO.NET SQLite), `Services/IApiRequestLogService` + `ApiRequestLogService` (ghi DB + broadcast SignalR `ApiRequestLogged`), `Endpoints/ApiRequestLoggingEndpointFilter` (OUTER filter bắt 401); gắn filter vào `LaunchAppEndpoints` + `DeviceConnectionEndpoints` POST route; đăng ký DI trong `Program.cs`; thêm 11 unit tests |
| 2026-08-20 | Senior Developer | adb-uninstall-before-install STEP-3.1: Thêm `AdbService.UninstallApkAsync` (KHÔNG vào IAdbService); mở rộng `InstallCoordinator.QueueInstalls`/`InstallOneAsync`/`DoInstallAsync` (+param `uninstallBeforeInstall`, shift percent SignalR 0/15/25/40/55/70/85/100); thêm `InstallRequest.UninstallBeforeInstall`; thêm endpoint `POST /api/settings/uninstall-before-install` + DTO `UninstallBeforeInstallSettingRequest` vào `DeviceEndpoints.cs`; `IndexModel` load `UninstallBeforeInstall` từ DB; checkbox UI `chk-uninstall-before-install` (Index.cshtml hàng 3); dashboard.js change handler + 2 install handler truyền flag. Thêm 12 unit tests (`UninstallBeforeInstallTests`). |
| 2026-08-24 | Senior Developer | adb-app-status-reboot-api STEP-3.1: Thêm `AppState` enum + `AppStatusResult` model + `AdbService.GetAppStatusAsync` (pidof + dumpsys) + `AdbService.IsForegroundInDumpsys` (static, testable) + `AdbService.RebootDeviceAsync`; tạo `Endpoints/AppStatusEndpoints.cs` (GET /api/devices/{serial}/app-status) + `Endpoints/RebootEndpoints.cs` (POST /api/devices/{serial}/reboot); thêm 2 constants vào `ApiRequestLogConstants`; cập nhật `ResolveApiName` trong `ApiRequestLoggingEndpointFilter`; `IndexModel` thêm `PublicApiKey` (từ `LaunchApp:ApiKey`); `Index.cshtml` thêm 2 nút + `window.KZ_API_KEY` script; `dashboard.js` thêm `apiHeaders()` + 2 event handler + cập nhật `summarizeParams`; thêm 16 unit tests (AppStatusEndpointsTests x9 + RebootEndpointsTests x6 + AdbServiceAppStatusTests x5 → thực tế 15 test mới + bộ cũ = 119 total). |

---

## 1. Tổng quan solution

| Project | SDK | TargetFramework | Vai trò |
|---|---|---|---|
| `KztekAdbPublishTool` | Sdk.WinExe | net8.0-windows7.0 | Bản gốc WinForms — **KHÔNG SỬA** |
| `KztekAdbPublishTool.Web` | Sdk.Web | net8.0 | Bản migrate mới — ASP.NET Core Razor Pages |
| `KztekAdbPublishTool.Web.Tests` | Sdk | net8.0 | Unit tests cho project Web |

---

## 2. Project: KztekAdbPublishTool.Web

### 2.1 Cấu trúc thư mục

```
src/KztekAdbPublishTool.Web/
├── Configuration/
│   └── AdbSettings.cs              ← POCO config (AdbPath, PollIntervalMs, DbPath, UploadsPath, MaxUploadBytes)
├── Endpoints/
│   ├── ApkEndpoints.cs             ← POST /api/apk/upload, DELETE /api/apk
│   ├── AppStatusEndpoints.cs       ← GET /api/devices/{serial}/app-status (filter: ApiRequestLoggingEndpointFilter OUTER + ApiKeyEndpointFilter) [adb-app-status-reboot-api STEP-3.1]
│   ├── DeviceConnectionEndpoints.cs← POST /api/devices/connect-by-ip (filter: ApiRequestLoggingEndpointFilter OUTER + ApiKeyEndpointFilter), GET /api/devices/{serial}/status [adb-add-device-api; STEP-2.1]
│   ├── DeviceEndpoints.cs          ← POST /api/devices/{connect,connect-batch,remove,poll}, GET /api/devices, POST /api/settings/package, /api/settings/uninstall-before-install, /api/polling/toggle [+endpoint STEP-3.1 adb-uninstall-before-install]
│   ├── HealthEndpoints.cs          ← GET /health
│   ├── InstallEndpoints.cs         ← POST /api/install
│   ├── LaunchAppEndpoints.cs       ← POST /api/launch-app (filter: ApiRequestLoggingEndpointFilter OUTER + ApiKeyEndpointFilter) [STEP-2.1]
│   ├── RebootEndpoints.cs          ← POST /api/devices/{serial}/reboot (filter: ApiRequestLoggingEndpointFilter OUTER + ApiKeyEndpointFilter) [adb-app-status-reboot-api STEP-3.1]
│   ├── ApiKeyEndpointFilter.cs     ← INNER auth filter; BẤT KHẢ XÂM PHẠM
│   ├── ApiRequestLoggingEndpointFilter.cs ← OUTER logging filter; capture body + callerIp + result → gọi IApiRequestLogService.LogAsync [STEP-2.1]; ResolveApiName cập nhật nhận AppStatus/RebootDevice route [adb-app-status-reboot-api]
│   └── ScanEndpoints.cs            ← POST /api/scan/start, /api/scan/cancel
├── Hubs/
│   └── DeviceHub.cs                ← SignalR Hub, endpoint /hubs/device
├── Models/
│   ├── DeviceRecord.cs             ← POCO entity (9 property), dùng làm DB row + SignalR DTO
│   └── ApiRequestLogEntry.cs       ← POCO entity (11 property) — DB row + SignalR payload cho feature api-request-log [STEP-2.1]
├── Pages/
│   ├── Index.cshtml                ← Dashboard: toolbar 3 hàng, grid 9 cột, action panel, log area
│   ├── Index.cshtml.cs             ← IndexModel : PageModel
│   └── Shared/
│       └── _Layout.cshtml          ← Bootstrap 5 local, navbar KZTEK brand (Navy/Cam)
├── Services/
│   ├── IAdbService.cs              ← Interface: GetDevicesAsync/ConnectAsync/GetPackageVersionAsync (testability)
│   ├── AdbService.cs               ← Singleton; implement IAdbService; wrap adb binary; GetDevices/Connect/Install/LaunchApp/GetVersion/UninstallApkAsync/GetAppStatusAsync/IsForegroundInDumpsys(static)/RebootDeviceAsync [+adb-app-status-reboot-api STEP-3.1]
│   ├── ApkManifestReader.cs        ← Singleton; parse AXML từ .apk; stateless
│   ├── DeviceRepository.cs         ← Singleton; SQLite CRUD (Upsert/Remove/GetAll/GetSetting/SetSetting/UpdateInstallResult)
│   ├── InstallCoordinator.cs       ← Singleton; queue install per-device (SemaphoreSlim(1)), global (SemaphoreSlim(4)); QueueInstalls+param `uninstallBeforeInstall` [STEP-3.1 adb-uninstall-before-install]
│   ├── PollControlService.cs       ← Singleton; PollingEnabled flag + TriggerAsync (manual poll trigger)
│   ├── ScanCoordinator.cs          ← Singleton; TCP probe scan (SemaphoreSlim(24), max 512 IP, timeout 1200ms)
│   ├── ScanRangeParser.cs          ← Static; parse IP range string → List<string> IPs
│   ├── ApiRequestLogConstants.cs   ← Static; hằng số ApiName/Result enum-string, SignalR event name, length caps [STEP-2.1]
│   ├── ApiRequestLogRepository.cs  ← Singleton; raw ADO.NET SQLite; INSERT ApiRequestLog; CREATE TABLE IF NOT EXISTS idempotent [STEP-2.1]
│   ├── IApiRequestLogService.cs    ← Interface: LogAsync(entry, ct) — ghi DB + broadcast SignalR [STEP-2.1]
│   └── ApiRequestLogService.cs     ← Singleton; impl IApiRequestLogService; truncate + InsertAsync + SendAsync("ApiRequestLogged"); swallow exception [STEP-2.1]
├── State/
│   └── DeviceState.cs              ← Singleton; ConcurrentDictionary in-memory snapshot; AddOrUpdate/TryGet/GetAll/GetSerials/Remove
├── Workers/
│   └── DevicePollWorker.cs         ← BackgroundService; WarmUpReconnectAsync (internal) → gọi ConnectAsync cho WiFi device persisted → poll 3s → Upsert → RefreshVersion → push SignalR DevicesUpdated
├── wwwroot/
│   ├── js/
│   │   ├── dashboard.js            ← Main UI logic: SignalR handlers, device grid, install/remove/connect buttons
│   │   ├── scan-modal.js           ← Network scan modal JS
│   │   └── signalr-client.js       ← SignalR client (auto-reconnect, expose window.kzHubConnection)
│   └── lib/
│       ├── bootstrap.bundle.min.js ← Bootstrap 5 local (tránh CDN fail trong Docker LAN)
│       └── signalr.min.js          ← SignalR client lib local
├── Program.cs                      ← Entry point, DI registration, Kestrel+FormOptions 500MB limit
├── appsettings.json
└── appsettings.Development.json
```

### 2.2 Module Dependencies

| Module | Phụ thuộc vào | Được gọi bởi (Callers) | Confidence | Last verified |
|---|---|---|---|---|
| `Program.cs` | AdbSettings, LaunchAppSettings, AdbService, DeviceRepository, ApkManifestReader, DevicePollWorker, DeviceHub, DeviceState, InstallCoordinator, ScanCoordinator, PollControlService, ApiRequestLogRepository, IApiRequestLogService, ApiRequestLoggingEndpointFilter, AppStatusEndpoints, RebootEndpoints | — (entry point) | CONFIRMED | 2026-08-24 |
| `Configuration/AdbSettings` | — | Program.cs, AdbService, DeviceRepository, DevicePollWorker | CONFIRMED | 2026-08-10 |
| `Configuration/LaunchAppSettings` | — | Program.cs, ApiKeyEndpointFilter | CONFIRMED | 2026-08-18 |
| `Services/IAdbService` | — | DevicePollWorker (interface dep), AdbService (implements) | CONFIRMED | 2026-08-19 |
| `Services/AdbService` | IOptions\<AdbSettings\>, System.Diagnostics.Process, **IAdbService** | DevicePollWorker (via IAdbService), InstallCoordinator, LaunchAppEndpoints, DeviceConnectionEndpoints, HealthEndpoints, AppStatusEndpoints, RebootEndpoints | CONFIRMED | 2026-08-24 |
| `Services/DeviceRepository` | IOptions\<AdbSettings\>, Microsoft.Data.Sqlite, Models/DeviceRecord | DevicePollWorker, DeviceEndpoints, InstallEndpoints, ApkEndpoints, InstallCoordinator | CONFIRMED | 2026-08-10 |
| `Services/ApkManifestReader` | System.IO.Compression | ApkEndpoints | CONFIRMED | 2026-08-10 |
| `Services/InstallCoordinator` | AdbService, DeviceRepository, DeviceState, IHubContext\<DeviceHub\> | InstallEndpoints | CONFIRMED | 2026-08-20 |
| `Services/PollControlService` | — | DevicePollWorker, DeviceEndpoints, DeviceConnectionEndpoints | CONFIRMED | 2026-08-19 |
| `Services/ScanCoordinator` | IHubContext\<DeviceHub\>, ScanRangeParser | ScanEndpoints | CONFIRMED | 2026-08-10 |
| `Services/ScanRangeParser` | — | ScanCoordinator | CONFIRMED | 2026-08-10 |
| `State/DeviceState` | System.Collections.Concurrent, Models/DeviceRecord | DevicePollWorker, DeviceEndpoints, InstallEndpoints, InstallCoordinator, LaunchAppEndpoints, DeviceConnectionEndpoints | CONFIRMED | 2026-08-19 |
| `Endpoints/ApiKeyEndpointFilter` | IOptions\<LaunchAppSettings\>, ILogger | LaunchAppEndpoints (.AddEndpointFilter INNER), DeviceConnectionEndpoints (.AddEndpointFilter INNER) | CONFIRMED | 2026-08-19 |
| `Endpoints/ApiRequestLoggingEndpointFilter` | IApiRequestLogService, ILogger | LaunchAppEndpoints (.AddEndpointFilter OUTER), DeviceConnectionEndpoints (.AddEndpointFilter OUTER, POST route only), AppStatusEndpoints (.AddEndpointFilter OUTER), RebootEndpoints (.AddEndpointFilter OUTER) | CONFIRMED | 2026-08-24 |
| `Endpoints/AppStatusEndpoints` | DeviceState, AdbService, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, ILoggerFactory | Program.cs (MapAppStatusEndpoints) | CONFIRMED | 2026-08-24 |
| `Endpoints/RebootEndpoints` | DeviceState, AdbService, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, ILoggerFactory | Program.cs (MapRebootEndpoints) | CONFIRMED | 2026-08-24 |
| `Endpoints/LaunchAppEndpoints` | DeviceState, AdbService, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, ILoggerFactory | Program.cs (MapLaunchAppEndpoints) | CONFIRMED | 2026-08-19 |
| `Endpoints/DeviceConnectionEndpoints` | AdbService, PollControlService, DeviceState, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, ILoggerFactory | Program.cs (MapDeviceConnectionEndpoints) | CONFIRMED | 2026-08-19 |
| `Hubs/DeviceHub` | Microsoft.AspNetCore.SignalR.Hub | Program.cs (MapHub), DevicePollWorker, InstallCoordinator, ScanCoordinator | CONFIRMED | 2026-08-10 |
| `Workers/DevicePollWorker` | IOptions\<AdbSettings\>, **IAdbService** (không còn phụ thuộc AdbService trực tiếp), DeviceRepository, DeviceState, PollControlService, IHubContext\<DeviceHub\> | Program.cs (AddHostedService) | CONFIRMED | 2026-08-19 |
| `Models/DeviceRecord` | — | DeviceRepository, DeviceState, DevicePollWorker, InstallCoordinator, API JSON response | CONFIRMED | 2026-08-10 |
| `Models/ApiRequestLogEntry` | — | ApiRequestLogRepository, ApiRequestLogService, ApiRequestLoggingEndpointFilter, SignalR broadcast payload | CONFIRMED | 2026-08-19 |
| `Services/ApiRequestLogConstants` | — | ApiRequestLogService, ApiRequestLoggingEndpointFilter, tests | CONFIRMED | 2026-08-19 |
| `Services/ApiRequestLogRepository` | IOptions\<AdbSettings\>, Microsoft.Data.Sqlite, Models/ApiRequestLogEntry | ApiRequestLogService | CONFIRMED | 2026-08-19 |
| `Services/IApiRequestLogService` | — | ApiRequestLoggingEndpointFilter (dep), ApiRequestLogService (implements) | CONFIRMED | 2026-08-19 |
| `Services/ApiRequestLogService` | ApiRequestLogRepository, IHubContext\<DeviceHub\>, IApiRequestLogService, ILogger | ApiRequestLoggingEndpointFilter (via IApiRequestLogService) | CONFIRMED | 2026-08-19 |
| `Endpoints/DeviceEndpoints` | DeviceRepository, DeviceState, AdbService, PollControlService | Program.cs (MapDeviceEndpoints) | CONFIRMED | 2026-08-20 |
| `Endpoints/InstallEndpoints` | DeviceState, DeviceRepository, InstallCoordinator | Program.cs (MapInstallEndpoints) | CONFIRMED | 2026-08-20 |
| `Endpoints/ScanEndpoints` | ScanCoordinator | Program.cs (MapScanEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/ApkEndpoints` | IOptions\<AdbSettings\>, ApkManifestReader, DeviceRepository | Program.cs (MapApkEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/HealthEndpoints` | — | Program.cs (MapHealthEndpoints) | CONFIRMED | 2026-08-10 |
| `Pages/Index` | IndexModel : PageModel, IOptions\<LaunchAppSettings\> (PublicApiKey) | Router (/) | CONFIRMED | 2026-08-24 |
| `wwwroot/js/signalr-client.js` | signalr.min.js (local lib) | _Layout.cshtml | CONFIRMED | 2026-08-10 |
| `wwwroot/js/dashboard.js` | signalr-client.js, bootstrap.bundle.min.js (local) | _Layout.cshtml | CONFIRMED | 2026-08-10 |
| `wwwroot/js/scan-modal.js` | signalr-client.js | _Layout.cshtml | CONFIRMED | 2026-08-10 |

### 2.3 API Endpoints

| Method | Path | Handler | Status |
|---|---|---|---|
| GET | `/` | Pages/Index | ✅ |
| GET | `/health` | HealthEndpoints | ✅ |
| WS/GET | `/hubs/device/negotiate` | DeviceHub (SignalR) | ✅ |
| POST | `/api/install` | InstallEndpoints → InstallCoordinator | ✅ |
| POST | `/api/scan/start` | ScanEndpoints → ScanCoordinator | ✅ |
| POST | `/api/scan/cancel` | ScanEndpoints → ScanCoordinator | ✅ |
| POST | `/api/apk/upload` | ApkEndpoints | ✅ |
| DELETE | `/api/apk` | ApkEndpoints | ✅ |
| POST | `/api/devices/connect` | DeviceEndpoints → AdbService + PollControlService | ✅ |
| POST | `/api/devices/connect-batch` | DeviceEndpoints → AdbService + PollControlService | ✅ |
| POST | `/api/devices/remove` | DeviceEndpoints → **DeviceState** + DeviceRepository | ✅ Bug R9 fixed 2026-08-10 |
| GET | `/api/devices` | DeviceEndpoints → DeviceRepository | ✅ |
| POST | `/api/settings/package` | DeviceEndpoints → DeviceRepository | ✅ |
| POST | `/api/settings/uninstall-before-install` | DeviceEndpoints → DeviceRepository | ✅ STEP-3.1 2026-08-20 |
| POST | `/api/polling/toggle` | DeviceEndpoints → PollControlService + DeviceRepository | ✅ |
| POST | `/api/devices/poll` | DeviceEndpoints → PollControlService.TriggerAsync | ✅ |
| POST | `/api/launch-app` | LaunchAppEndpoints → ApiKeyEndpointFilter → DeviceState → AdbService | ✅ STEP-3.1 2026-08-18 |
| POST | `/api/devices/connect-by-ip` | DeviceConnectionEndpoints → ApiKeyEndpointFilter → AdbService → PollControlService | ✅ STEP-3.1 [adb-add-device-api] 2026-08-19 |
| GET | `/api/devices/{serial}/status` | DeviceConnectionEndpoints → ApiKeyEndpointFilter → DeviceState | ✅ STEP-3.1 [adb-add-device-api] 2026-08-19 |
| GET | `/api/devices/{serial}/app-status` | AppStatusEndpoints → ApiRequestLoggingEndpointFilter → ApiKeyEndpointFilter → DeviceState → AdbService.GetAppStatusAsync | ✅ STEP-3.1 [adb-app-status-reboot-api] 2026-08-24 |
| POST | `/api/devices/{serial}/reboot` | RebootEndpoints → ApiRequestLoggingEndpointFilter → ApiKeyEndpointFilter → DeviceState → AdbService.RebootDeviceAsync | ✅ STEP-3.1 [adb-app-status-reboot-api] 2026-08-24 |

### 2.4 SignalR Events (server → client)

| Event | Payload | Triggered by | Status |
|---|---|---|---|
| `DevicesUpdated` | `List<DeviceRecord>` | DevicePollWorker (mỗi 3s) | ✅ |
| `InstallProgress` | `string serial, int percent, string message` | InstallCoordinator | ✅ |
| `DeviceInstalled` | `string serial, bool success, string version` | InstallCoordinator | ✅ |
| `ScanProgress` | `int found, int scanned, int total` | ScanCoordinator | ✅ |
| `ScanFound` | `string ipPort` | ScanCoordinator | ✅ |
| `ScanCompleted` | `int total, int found` | ScanCoordinator | ✅ |
| `ApiRequestLogged` | `ApiRequestLogEntry` (camelCase JSON: id, timestamp, apiName, httpMethod, path, parameters, result, httpStatusCode, errorMessage, callerIp, durationMs) | ApiRequestLogService (via IApiRequestLogService) | ✅ STEP-2.1 2026-08-19 |

### 2.5 Configuration (appsettings.json — section "Adb" và "LaunchApp")

| Key | Default (production) | Default (development) | Ghi chú |
|---|---|---|---|
| `AdbPath` | `/opt/platform-tools/adb` | `C:\platform-tools\adb.exe` | Path tới adb binary |
| `PollIntervalMs` | `3000` | `3000` | Chu kỳ poll (ms) |
| `DbPath` | `/app/data/adbpublishtool.db` | `adbpublishtool-dev.db` | SQLite DB |
| `UploadsPath` | `/app/uploads` | `uploads-dev` | Thư mục APK tạm |
| `MaxUploadBytes` | `500000000` | `500000000` | 500 MB |
| `LaunchApp:ApiKey` | `""` (fail-safe) | `""` | Override bằng env var `LaunchApp__ApiKey`; rỗng = 401 mọi request (STEP-3.1) |
| Kestrel `MaxRequestBodySize` | `500000000` | inherited | Set trong Program.cs |

---

## 3. Project: KztekAdbPublishTool (WinForms nguồn — KHÔNG SỬA)

> Không cập nhật graph của project này — bất khả xâm phạm theo §1A WF-MIGRATE.

---

## 4. Thay đổi gần đây

| Ngày | File | Loại thay đổi |
|---|---|---|
| 2026-08-05 | `src/KztekAdbPublishTool.Web/**` | Tạo mới toàn bộ project (Phase 1–3) |
| 2026-08-05 | `tests/KztekAdbPublishTool.Web.Tests/**` | Tạo mới test project + 5 test cases DeviceRepository |
| 2026-08-05 | `KztekAdbPublishTool.sln` | Thêm 2 project mới vào solution |
| 2026-08-10 | `State/DeviceState.cs` | Thêm method `Remove(string serial)` — fix bug R9: thiết bị không biến mất sau khi xóa |
| 2026-08-10 | `Endpoints/DeviceEndpoints.cs` | Handler `/api/devices/remove` nhận thêm `DeviceState` qua DI, gọi `deviceState.Remove()` trước `repo.Remove()` — parity WinForms MainForm.cs:572-577 |
| 2026-08-19 | `Services/IAdbService.cs` | **MỚI** — interface cho AdbService (GetDevicesAsync, ConnectAsync, GetPackageVersionAsync) — phục vụ testability của DevicePollWorker |
| 2026-08-19 | `Services/AdbService.cs` | Implement `IAdbService` (thêm `: IAdbService` vào class declaration) |
| 2026-08-19 | `Workers/DevicePollWorker.cs` | **BUG FIX** `BUG-adb-reconnect-after-restart`: (1) Đổi dependency `AdbService` → `IAdbService`; (2) Thêm `WarmUpReconnectAsync(ct)` (internal) — gọi `ConnectAsync` cho WiFi devices persist trước khi poll lần đầu |
| 2026-08-19 | `Program.cs` | Thêm `AddSingleton<IAdbService>` → delegate sang `AdbService` singleton |
| 2026-08-19 | `KztekAdbPublishTool.Web.csproj` | Thêm `InternalsVisibleTo` cho test project |
| 2026-08-19 | `tests/.../DevicePollWorkerWarmUpTests.cs` | **MỚI** — 6 unit tests cho `WarmUpReconnectAsync` (WiFi call, USB filter, mixed, continue-on-fail, empty DB, pre-cancelled) |
| 2026-08-19 | `src/.../Models/ApiRequestLogEntry.cs` | **MỚI** — POCO entity (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Services/ApiRequestLogConstants.cs` | **MỚI** — hằng số feature (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Services/ApiRequestLogRepository.cs` | **MỚI** — raw ADO.NET SQLite repository (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Services/IApiRequestLogService.cs` | **MỚI** — interface LogAsync (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Services/ApiRequestLogService.cs` | **MỚI** — singleton impl: truncate + InsertAsync + SignalR broadcast (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Endpoints/ApiRequestLoggingEndpointFilter.cs` | **MỚI** — OUTER logging filter, EnableBuffering + rewind body, ExtractStatusCode (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Endpoints/DeviceConnectionEndpoints.cs` | Thêm `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` OUTER cho POST route (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Endpoints/LaunchAppEndpoints.cs` | Thêm `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` OUTER (api-request-log STEP-2.1) |
| 2026-08-19 | `src/.../Program.cs` | Thêm 3 dòng DI: ApiRequestLogRepository (Singleton), IApiRequestLogService (Singleton), ApiRequestLoggingEndpointFilter (Scoped) |
| 2026-08-19 | `tests/.../ApiRequestLogServiceTests.cs` | **MỚI** — 4 unit tests: happy path, truncate params, truncate errMsg, hub throw swallow |
| 2026-08-19 | `tests/.../ApiRequestLoggingEndpointFilterTests.cs` | **MỚI** — 7 unit tests: 200/401/422, body buffering, handler throw, XFF, durationMs |
| 2026-08-24 | `src/.../Services/AdbService.cs` | **THÊM** — `AppState` enum, `AppStatusResult` model, `GetAppStatusAsync` (pidof+dumpsys), `IsForegroundInDumpsys` (public static), `RebootDeviceAsync` [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Endpoints/AppStatusEndpoints.cs` | **MỚI** — GET /api/devices/{serial}/app-status; Validate/CheckDeviceState/MapAppStatusResult public static [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Endpoints/RebootEndpoints.cs` | **MỚI** — POST /api/devices/{serial}/reboot; CheckDeviceState/MapRebootResult public static [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Services/ApiRequestLogConstants.cs` | Thêm `ApiAppStatus` + `ApiRebootDevice` constants [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Endpoints/ApiRequestLoggingEndpointFilter.cs` | `ResolveApiName` nhận diện 2 route mới dùng EndsWith (có {serial} placeholder) [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Pages/Index.cshtml.cs` | `IndexModel` thêm `PublicApiKey` từ `LaunchApp:ApiKey` (inject `IOptions<LaunchAppSettings>`) [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Pages/Index.cshtml` | Thêm 2 nút `btn-check-app-status` + `btn-reboot-device`; thêm `window.KZ_API_KEY` script [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../wwwroot/js/dashboard.js` | Thêm `apiHeaders()` helper; cập nhật `summarizeParams` nhận `entry` param (dùng path cho AppStatus/RebootDevice); thêm 2 event handler [adb-app-status-reboot-api] |
| 2026-08-24 | `src/.../Program.cs` | Thêm `MapAppStatusEndpoints()` + `MapRebootEndpoints()` [adb-app-status-reboot-api] |
| 2026-08-24 | `tests/.../AppStatusEndpointsTests.cs` | **MỚI** — 9 unit tests: ValidateInput x3, CheckDeviceState x2, MapAppStatusResult x4 (3 states + AdbNotFound + AdbTimeout + AdbError) |
| 2026-08-24 | `tests/.../RebootEndpointsTests.cs` | **MỚI** — 6 unit tests: CheckDeviceState x2, MapRebootResult x4 (Success + AdbNotFound + AdbTimeout + AdbError) |
| 2026-08-24 | `tests/.../AdbServiceAppStatusTests.cs` | **MỚI** — 5 unit tests: IsForegroundInDumpsys (mResumedActivity found, other package, empty, mFocusedActivity fallback, neither) |
