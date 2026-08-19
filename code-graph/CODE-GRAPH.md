---
project: KztekAdbPublishTool (multi-project solution)
last_updated: 2026-08-19
updated_by: Senior Developer (BUG-adb-reconnect-after-restart)
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
│   ├── DeviceConnectionEndpoints.cs← POST /api/devices/connect-by-ip, GET /api/devices/{serial}/status (auth: ApiKeyEndpointFilter) [adb-add-device-api]
│   ├── DeviceEndpoints.cs          ← POST /api/devices/{connect,connect-batch,remove,poll}, GET /api/devices, POST /api/settings/package, /api/polling/toggle
│   ├── HealthEndpoints.cs          ← GET /health
│   ├── InstallEndpoints.cs         ← POST /api/install
│   └── ScanEndpoints.cs            ← POST /api/scan/start, /api/scan/cancel
├── Hubs/
│   └── DeviceHub.cs                ← SignalR Hub, endpoint /hubs/device
├── Models/
│   └── DeviceRecord.cs             ← POCO entity (9 property), dùng làm DB row + SignalR DTO
├── Pages/
│   ├── Index.cshtml                ← Dashboard: toolbar 3 hàng, grid 9 cột, action panel, log area
│   ├── Index.cshtml.cs             ← IndexModel : PageModel
│   └── Shared/
│       └── _Layout.cshtml          ← Bootstrap 5 local, navbar KZTEK brand (Navy/Cam)
├── Services/
│   ├── IAdbService.cs              ← Interface: GetDevicesAsync/ConnectAsync/GetPackageVersionAsync (testability)
│   ├── AdbService.cs               ← Singleton; implement IAdbService; wrap adb binary; GetDevices/Connect/Install/LaunchApp/GetVersion
│   ├── ApkManifestReader.cs        ← Singleton; parse AXML từ .apk; stateless
│   ├── DeviceRepository.cs         ← Singleton; SQLite CRUD (Upsert/Remove/GetAll/GetSetting/SetSetting/UpdateInstallResult)
│   ├── InstallCoordinator.cs       ← Singleton; queue install per-device (SemaphoreSlim(1)), global (SemaphoreSlim(4))
│   ├── PollControlService.cs       ← Singleton; PollingEnabled flag + TriggerAsync (manual poll trigger)
│   ├── ScanCoordinator.cs          ← Singleton; TCP probe scan (SemaphoreSlim(24), max 512 IP, timeout 1200ms)
│   └── ScanRangeParser.cs          ← Static; parse IP range string → List<string> IPs
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
| `Program.cs` | AdbSettings, LaunchAppSettings, AdbService, DeviceRepository, ApkManifestReader, DevicePollWorker, DeviceHub, DeviceState, InstallCoordinator, ScanCoordinator, PollControlService | — (entry point) | CONFIRMED | 2026-08-18 |
| `Configuration/AdbSettings` | — | Program.cs, AdbService, DeviceRepository, DevicePollWorker | CONFIRMED | 2026-08-10 |
| `Configuration/LaunchAppSettings` | — | Program.cs, ApiKeyEndpointFilter | CONFIRMED | 2026-08-18 |
| `Services/IAdbService` | — | DevicePollWorker (interface dep), AdbService (implements) | CONFIRMED | 2026-08-19 |
| `Services/AdbService` | IOptions\<AdbSettings\>, System.Diagnostics.Process, **IAdbService** | DevicePollWorker (via IAdbService), InstallCoordinator, LaunchAppEndpoints, DeviceConnectionEndpoints, HealthEndpoints | CONFIRMED | 2026-08-19 |
| `Services/DeviceRepository` | IOptions\<AdbSettings\>, Microsoft.Data.Sqlite, Models/DeviceRecord | DevicePollWorker, DeviceEndpoints, InstallEndpoints, ApkEndpoints, InstallCoordinator | CONFIRMED | 2026-08-10 |
| `Services/ApkManifestReader` | System.IO.Compression | ApkEndpoints | CONFIRMED | 2026-08-10 |
| `Services/InstallCoordinator` | AdbService, DeviceRepository, DeviceState, IHubContext\<DeviceHub\> | InstallEndpoints | CONFIRMED | 2026-08-10 |
| `Services/PollControlService` | — | DevicePollWorker, DeviceEndpoints, DeviceConnectionEndpoints | CONFIRMED | 2026-08-19 |
| `Services/ScanCoordinator` | IHubContext\<DeviceHub\>, ScanRangeParser | ScanEndpoints | CONFIRMED | 2026-08-10 |
| `Services/ScanRangeParser` | — | ScanCoordinator | CONFIRMED | 2026-08-10 |
| `State/DeviceState` | System.Collections.Concurrent, Models/DeviceRecord | DevicePollWorker, DeviceEndpoints, InstallEndpoints, InstallCoordinator, LaunchAppEndpoints, DeviceConnectionEndpoints | CONFIRMED | 2026-08-19 |
| `Endpoints/ApiKeyEndpointFilter` | IOptions\<LaunchAppSettings\>, ILogger | LaunchAppEndpoints (.AddEndpointFilter), DeviceConnectionEndpoints (.AddEndpointFilter) | CONFIRMED | 2026-08-19 |
| `Endpoints/LaunchAppEndpoints` | DeviceState, AdbService, ApiKeyEndpointFilter, ILoggerFactory | Program.cs (MapLaunchAppEndpoints) | CONFIRMED | 2026-08-18 |
| `Endpoints/DeviceConnectionEndpoints` | AdbService, PollControlService, DeviceState, ApiKeyEndpointFilter, ILoggerFactory | Program.cs (MapDeviceConnectionEndpoints) | CONFIRMED | 2026-08-19 |
| `Hubs/DeviceHub` | Microsoft.AspNetCore.SignalR.Hub | Program.cs (MapHub), DevicePollWorker, InstallCoordinator, ScanCoordinator | CONFIRMED | 2026-08-10 |
| `Workers/DevicePollWorker` | IOptions\<AdbSettings\>, **IAdbService** (không còn phụ thuộc AdbService trực tiếp), DeviceRepository, DeviceState, PollControlService, IHubContext\<DeviceHub\> | Program.cs (AddHostedService) | CONFIRMED | 2026-08-19 |
| `Models/DeviceRecord` | — | DeviceRepository, DeviceState, DevicePollWorker, InstallCoordinator, API JSON response | CONFIRMED | 2026-08-10 |
| `Endpoints/DeviceEndpoints` | DeviceRepository, DeviceState, AdbService, PollControlService | Program.cs (MapDeviceEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/InstallEndpoints` | DeviceState, DeviceRepository, InstallCoordinator | Program.cs (MapInstallEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/ScanEndpoints` | ScanCoordinator | Program.cs (MapScanEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/ApkEndpoints` | IOptions\<AdbSettings\>, ApkManifestReader, DeviceRepository | Program.cs (MapApkEndpoints) | CONFIRMED | 2026-08-10 |
| `Endpoints/HealthEndpoints` | — | Program.cs (MapHealthEndpoints) | CONFIRMED | 2026-08-10 |
| `Pages/Index` | IndexModel : PageModel | Router (/) | CONFIRMED | 2026-08-10 |
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
| POST | `/api/polling/toggle` | DeviceEndpoints → PollControlService + DeviceRepository | ✅ |
| POST | `/api/devices/poll` | DeviceEndpoints → PollControlService.TriggerAsync | ✅ |
| POST | `/api/launch-app` | LaunchAppEndpoints → ApiKeyEndpointFilter → DeviceState → AdbService | ✅ STEP-3.1 2026-08-18 |
| POST | `/api/devices/connect-by-ip` | DeviceConnectionEndpoints → ApiKeyEndpointFilter → AdbService → PollControlService | ✅ STEP-3.1 [adb-add-device-api] 2026-08-19 |
| GET | `/api/devices/{serial}/status` | DeviceConnectionEndpoints → ApiKeyEndpointFilter → DeviceState | ✅ STEP-3.1 [adb-add-device-api] 2026-08-19 |

### 2.4 SignalR Events (server → client)

| Event | Payload | Triggered by | Status |
|---|---|---|---|
| `DevicesUpdated` | `List<DeviceRecord>` | DevicePollWorker (mỗi 3s) | ✅ |
| `InstallProgress` | `string serial, int percent, string message` | InstallCoordinator | ✅ |
| `DeviceInstalled` | `string serial, bool success, string version` | InstallCoordinator | ✅ |
| `ScanProgress` | `int found, int scanned, int total` | ScanCoordinator | ✅ |
| `ScanFound` | `string ipPort` | ScanCoordinator | ✅ |
| `ScanCompleted` | `int total, int found` | ScanCoordinator | ✅ |

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
