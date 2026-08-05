---
project: KztekAdbPublishTool (multi-project solution)
last_updated: 2026-08-05
updated_by: Senior Developer
---

# CODE-GRAPH — KztekAdbPublishTool Solution

## Lịch sử cập nhật

| Ngày | Người | Thay đổi |
|---|---|---|
| 2026-08-05 | Senior Developer | Tạo mới — Phase 1 Foundation (STEP 1.1–1.3) |

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
│   └── AdbSettings.cs          ← POCO config (AdbPath, PollIntervalMs, DbPath, UploadsPath, MaxUploadBytes)
├── Hubs/
│   └── DeviceHub.cs            ← SignalR Hub, endpoint /hubs/device
├── Models/
│   └── DeviceRecord.cs         ← POCO entity (9 property), dùng làm DB row + SignalR DTO
├── Pages/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Index.cshtml             ← Dashboard (Phase 2 sẽ hoàn thiện)
│   ├── Index.cshtml.cs          ← IndexModel : PageModel
│   └── Shared/
│       └── _Layout.cshtml       ← Bootstrap 5, navbar KZTEK brand (Navy/Cam)
├── Services/
│   ├── AdbService.cs            ← Singleton; wrap adb binary; constructor: IOptions<AdbSettings>
│   ├── ApkManifestReader.cs     ← Singleton; parse AXML từ .apk; stateless
│   └── DeviceRepository.cs      ← Singleton; SQLite CRUD; constructor: IOptions<AdbSettings>
├── Workers/
│   └── DevicePollWorker.cs      ← BackgroundService; poll 3s heartbeat (Phase 2: logic thực)
├── wwwroot/
│   └── js/
│       └── signalr-client.js    ← SignalR client (auto-reconnect, expose window.kzHubConnection)
├── Program.cs                   ← Entry point, DI registration, Kestrel 500MB limit
├── appsettings.json
└── appsettings.Development.json
```

### 2.2 Module Dependencies

| Module | Phụ thuộc vào | Được gọi bởi (Callers) | Confidence | Last verified |
|---|---|---|---|---|
| `Program.cs` | AdbSettings, AdbService, DeviceRepository, ApkManifestReader, DevicePollWorker, DeviceHub | — (entry point) | CONFIRMED | 2026-08-05 |
| `Configuration/AdbSettings` | — | Program.cs, AdbService, DeviceRepository, DevicePollWorker | CONFIRMED | 2026-08-05 |
| `Services/AdbService` | IOptions\<AdbSettings\>, System.Diagnostics.Process | DevicePollWorker (Phase 2), InstallCoordinator (Phase 2) | CONFIRMED | 2026-08-05 |
| `Services/DeviceRepository` | IOptions\<AdbSettings\>, Microsoft.Data.Sqlite, Models/DeviceRecord | DevicePollWorker (Phase 2), API endpoints (Phase 2) | CONFIRMED | 2026-08-05 |
| `Services/ApkManifestReader` | System.IO.Compression | APK upload endpoint (Phase 2) | CONFIRMED | 2026-08-05 |
| `Hubs/DeviceHub` | Microsoft.AspNetCore.SignalR.Hub | Program.cs (MapHub), Workers (Phase 2) | CONFIRMED | 2026-08-05 |
| `Workers/DevicePollWorker` | IOptions\<AdbSettings\>, ILogger | Program.cs (AddHostedService) | CONFIRMED | 2026-08-05 |
| `Models/DeviceRecord` | — | DeviceRepository, SignalR push (Phase 2), API JSON response (Phase 2) | CONFIRMED | 2026-08-05 |
| `Pages/Index` | IndexModel : PageModel | Router (/) | CONFIRMED | 2026-08-05 |
| `wwwroot/js/signalr-client.js` | signalr.min.js (CDN) | _Layout.cshtml | CONFIRMED | 2026-08-05 |

### 2.3 API Endpoints (Phase 1 khung — Phase 2 sẽ bổ sung)

| Method | Path | Handler | Status |
|---|---|---|---|
| GET | `/` | Pages/Index | ✅ Phase 1 |
| WS/GET | `/hubs/device/negotiate` | DeviceHub (SignalR) | ✅ Phase 1 |
| POST | `/api/install` | InstallCoordinator | ⬜ Phase 2 (STEP-2.2) |
| POST | `/api/scan/start` | ScanCoordinator | ⬜ Phase 2 (STEP-2.3) |
| POST | `/api/scan/cancel` | ScanCoordinator | ⬜ Phase 2 (STEP-2.3) |
| POST | `/api/apk/upload` | APK handler | ⬜ Phase 2 (STEP-2.4) |
| DELETE | `/api/apk` | APK handler | ⬜ Phase 2 (STEP-2.4) |
| POST | `/api/devices/connect` | Device endpoint | ⬜ Phase 2 (STEP-2.5) |
| POST | `/api/devices/connect-batch` | Device endpoint | ⬜ Phase 2 (STEP-2.5) |
| POST | `/api/devices/remove` | Device endpoint | ⬜ Phase 2 (STEP-2.5) |
| GET | `/api/devices` | Device endpoint | ⬜ Phase 2 (STEP-2.5) |
| POST | `/api/settings/package` | Settings endpoint | ⬜ Phase 2 (STEP-2.5) |
| POST | `/api/polling/toggle` | Polling endpoint | ⬜ Phase 2 (STEP-2.5) |

### 2.4 SignalR Events (server → client)

| Event | Payload | Triggered by | Status |
|---|---|---|---|
| `DevicesUpdated` | `List<DeviceRecord>` | DevicePollWorker | ⬜ Phase 2 (STEP-2.1) |
| `InstallProgress` | `serial, percent, message` | InstallCoordinator | ⬜ Phase 2 (STEP-2.2) |
| `DeviceInstalled` | `serial, success, version` | InstallCoordinator | ⬜ Phase 2 (STEP-2.2) |
| `ScanProgress` | `found, scanned, total` | ScanCoordinator | ⬜ Phase 2 (STEP-2.3) |
| `ScanFound` | `ipPort` | ScanCoordinator | ⬜ Phase 2 (STEP-2.3) |

### 2.5 Configuration (appsettings.json — section "Adb")

| Key | Default (production) | Default (development) | Ghi chú |
|---|---|---|---|
| `AdbPath` | `/opt/platform-tools/adb` | `C:\platform-tools\adb.exe` | Path tới adb binary |
| `PollIntervalMs` | `3000` | `3000` | Chu kỳ poll (ms) |
| `DbPath` | `/app/data/adbpublishtool.db` | `adbpublishtool-dev.db` | SQLite DB |
| `UploadsPath` | `/app/uploads` | `uploads-dev` | Thư mục APK tạm |
| `MaxUploadBytes` | `500000000` | `500000000` | 500 MB |
| Kestrel `MaxRequestBodySize` | `500000000` | inherited | Set trong Program.cs |

---

## 3. Project: KztekAdbPublishTool (WinForms nguồn — KHÔNG SỬA)

> Không cập nhật graph của project này — bất khả xâm phạm theo §1A WF-MIGRATE.

---

## 4. Thay đổi gần đây

| Ngày | File | Loại thay đổi |
|---|---|---|
| 2026-08-05 | `src/KztekAdbPublishTool.Web/**` | Tạo mới toàn bộ project (Phase 1 — STEP 1.1–1.3) |
| 2026-08-05 | `tests/KztekAdbPublishTool.Web.Tests/**` | Tạo mới test project + 5 test cases DeviceRepository |
| 2026-08-05 | `KztekAdbPublishTool.sln` | Thêm 2 project mới vào solution |
