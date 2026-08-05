---
id: ADR-001
title: Migration inventory + mapping — WinForms KztekAdbPublishTool → ASP.NET Core MVC/Razor Pages (.NET 8)
status: Proposed
date: 2026-08-05
author: Code Migrator (Opus)
supersedes: —
---

# ADR-001 — Migration inventory + mapping: WinForms → ASP.NET Core (.NET 8) cho KztekAdbPublishTool

> Mục tiêu: chuyển đổi toàn bộ tính năng WinForms của `src/KztekAdbPublishTool` sang ứng dụng **ASP.NET Core Razor Pages (.NET 8)** chạy được trong Docker Linux container, GIỮ NGUYÊN 100% tính năng. Project nguồn TUYỆT ĐỐI không sửa (§1A code-migrator). Code mới nằm trong project MỚI `src/KztekAdbPublishTool.Web`.

---

## 1. ASSUMPTIONS đang đặt ra

1. **Framework đích:** ASP.NET Core **Razor Pages** (không MVC controllers thuần, không Blazor, không SPA tách rời). Chọn Razor Pages vì UI dạng 1 dashboard đơn, không nhiều route phức tạp.
2. **ADB WiFi-only** — không cần USB passthrough → container Linux chạy adb tới `<ip>:5555` là đủ. **Container yêu cầu `--network host` trên Linux host** để adb connect thấy được subnet LAN của thiết bị.
3. **Single-tenant / admin nội bộ** — mọi phiên trình duyệt cùng thấy 1 danh sách device chung (state toàn cục ở server, không session per-user).
4. **Real-time UI** dùng **SignalR** để push cập nhật device list, install progress, scan progress — thay thế `System.Windows.Forms.Timer` + UI thread invoke.
5. **Persistent state:** giữ **SQLite** (đã cross-platform); DB + uploads mount qua Docker volume.
6. **Bootstrap 5** cho UI (không Guna.UI2 / không KztekComponent — 2 lib này Windows-only).
7. **Bước viết Dockerfile / build image** KHÔNG thuộc plan này — chỉ ghi chú bàn giao DevOps sau khi code migrate xong (§5).

→ **Xác nhận lại ngay nếu sai** trước khi thực thi plan.

---

## 2. Cấp 0 — Đếm file nguồn (chống bỏ sót)

| Loại | Số file | Đường dẫn |
|---|---|---|
| Entry point | 1 | `Program.cs` |
| Form (UI) | 2 | `Forms/MainForm.cs` (716 LOC), `Forms/NetworkScanForm.cs` (300 LOC) |
| Model | 1 | `Models/DeviceRecord.cs` |
| Service | 3 | `Services/AdbService.cs`, `Services/DeviceRepository.cs`, `Services/ApkManifestReader.cs` |
| csproj | 1 | `KztekAdbPublishTool.csproj` |
| Asset external | — | `../platform-tools/**` (adb.exe + phụ trợ, được `<Link>` copy sang output khi build WinForms) |
| **Tổng file production** | **7** | (không tính `obj/`) |

Grep signals: `System.Windows.Forms.Timer` = 1 (MainForm), `event Click` handler count = 12 (MainForm 8 + NetworkScanForm 4), `MessageBox.Show` = 6.
→ Số dòng bảng inventory Cấp 2 dưới đây PHẢI ≥ 7 file + 12 event + 1 timer.

---

## 3. Cấp 1 — Inventory project

| Project nguồn | Loại | Stack | Cần migrate? | Ghi chú |
|---|---|---|---|---|
| `KztekAdbPublishTool` | WinExe (UI) | net8.0-windows7.0 + WinForms + Guna.UI2 + KztekComponent | ✅ Toàn bộ | Tạo project mới `KztekAdbPublishTool.Web` (net8.0, ASP.NET Core Razor Pages) |
| `KztekComponent` (ref) | Library WinForms controls | net8.0-windows + Guna.UI2 | ❌ Không dùng | UI web thay bằng Bootstrap 5 |
| Package `Microsoft.Data.Sqlite 8.0.8` | NuGet | Cross-platform | ✅ Giữ nguyên | OK trên Linux |
| Asset `platform-tools/*` | native binary | Windows-only (adb.exe) | ⚠️ Thay | Docker image COPY adb Linux binary vào `/opt/platform-tools/` |

---

## 4. Cấp 2 — Inventory chi tiết

### 4.1 File `Models/DeviceRecord.cs`
1 class POCO, 9 property (Serial, Model, ConnectionType, Status, InstalledVersion, LastInstallStatus, LastInstallTime, FirstSeen, LastSeen). **Giữ nguyên 100%** — dùng đồng thời làm entity DB + DTO gửi qua SignalR/JSON. Không sửa.

### 4.2 File `Services/AdbService.cs` (198 LOC)

| Method | Chức năng | Migrate |
|---|---|---|
| `.ctor(string adbPath)` | Nhận path adb | ⚠️ Đổi: nhận `IOptions<AdbSettings>`, default path đọc từ appsettings (`/opt/platform-tools/adb` trong container) |
| `RunAsync(args, timeout, ct)` | Process.Start adb, đọc stdout/stderr, timeout | ✅ Giữ nguyên |
| `GetDevicesAsync` | Parse `adb devices -l` | ✅ Giữ nguyên |
| `ConnectAsync(ipPort)` | `adb connect ip:port` | ✅ Giữ nguyên |
| `InstallApkAsync(serial, apk)` | `adb -s serial install -r apk` | ✅ Giữ nguyên |
| `ListThirdPartyPackagesAsync` | `pm list packages -3` | ✅ Giữ nguyên |
| `LaunchAppAsync` | resolve-activity + am start | ✅ Giữ nguyên |
| `GetPackageVersionAsync` | dumpsys package | ✅ Giữ nguyên |

**File `Services/DeviceRepository.cs` (154 LOC):** SQLite CRUD (Upsert/UpdateInstallResult/Remove/GetAll/GetSetting/SetSetting) + Initialize. **Giữ nguyên 100%**, chỉ đổi constructor để đọc `dbPath` từ config (mặc định `/app/data/adbpublishtool.db`).

**File `Services/ApkManifestReader.cs` (173 LOC):** static, parse AXML thuần managed code. **Giữ nguyên 100%.**

### 4.3 File `Forms/MainForm.cs` (716 LOC)

**b1. Controls (Kz* + gốc):**

| Control | Tên | Chức năng | HTML/Razor tương đương |
|---|---|---|---|
| `KzTextBox` | `_txtPackage` | Ô package name | `<input class="form-control">` |
| `KzTextBox` | `_txtApkPath` | Hiển thị path APK (ReadOnly) | `<span>` hoặc `<input readonly>` (thay bằng "tên file đã upload") |
| `KzTextBox` | `_txtConnect` | Ô IP:port | `<input>` |
| `KzTextBox` | `_txtLog` | Log multi-line ReadOnly | `<pre id="log">` cuộn |
| `KzTextBox` (×2) | `_txtFilterIp`, `_txtFilterVersion` | Filter | `<input>` + JS filter table client-side |
| `KzButton` (×8) | `_btnBrowseApk`, `_btnInstallSelected`, `_btnInstallAll`, `_btnConnect`, `_btnNetworkScan`, `_btnRefresh`, `_btnRemoveSelected`, `_btnSelectAll` | Action | `<button class="btn btn-primary\|secondary">` |
| `KzCheckBox` | `_chkAutoDetect` | Toggle polling | `<input type="checkbox" role="switch">` (Bootstrap switch) |
| `KzDataGrid` | `_grid` | 9-column device grid + checkbox column + edit checkbox | `<table class="table">` render server-side lần đầu, update cell qua SignalR |
| `KzProgressBar` | `_progress` | Tiến độ cài | `<div class="progress"><div class="progress-bar">` |
| `Label` | `_lblStatus` | Text trạng thái | `<span>` |
| `OpenFileDialog` | — | Chọn file APK | `<input type="file" accept=".apk">` |
| `MessageBox.Show` (×6) | — | Alert/confirm | Bootstrap Modal + toast (SweetAlert2 tuỳ chọn) |
| `TableLayoutPanel`/`FlowLayoutPanel` | — | Layout | Bootstrap Grid (row/col) |

**b2. Timer / task ngầm:**

| Loại | Tên | Chu kỳ | Việc làm | Đích |
|---|---|---|---|---|
| `System.Windows.Forms.Timer` | `_pollTimer` | 3000ms | `PollDevicesAsync` (GetDevicesAsync → Upsert → RefreshVersion → RenderGrid) | `IHostedService` `DevicePollWorker` (Task.Delay 3s), push kết quả qua SignalR `DeviceHub.SendAsync("DevicesUpdated", devices)` |
| `Task.Run` (implicit ×N) | Song song install (SemaphoreSlim 4) | On-demand | Install nhiều thiết bị đồng thời | Giữ nguyên logic — chạy trong endpoint POST `/api/install`; push progress từng device qua SignalR (`InstallProgress`, `DeviceInstalled`) |
| `Task.Run` (implicit) | RefreshVersion sau poll | On-demand | Update version từng device online | Giữ nguyên trong DevicePollWorker |

**b3. Event / luồng tương tác chính (12 event):**

| Sự kiện gốc | Hành vi | Handler đích |
|---|---|---|
| `Form.Load` | Poll lần đầu + start timer | `Index.OnGetAsync` (render DB) + SignalR client join hub |
| `_pollTimer.Tick` | `PollDevicesAsync()` | `DevicePollWorker.ExecuteAsync` loop |
| `_txtPackage.Leave` | Save PackageName vào DB | POST `/api/settings/package` khi blur ô input |
| `_btnBrowseApk.Click` | OpenFileDialog + Load APK + parse manifest | `<input type=file>` change → POST `/api/apk/upload` (multipart) → server lưu upload + `ApkManifestReader.TryGetPackageName` → JSON trả `{ path, packageName }` |
| `_btnConnect.Click` | `adb connect` | POST `/api/devices/connect` `{ipPort}` |
| `_btnNetworkScan.Click` | Mở NetworkScanForm | Bootstrap Modal `#networkScanModal` + JS gọi `/api/scan/start` (SSE hoặc SignalR channel) |
| `_btnRefresh.Click` | `PollDevicesAsync(force:true)` | POST `/api/devices/poll` (trigger poll ngoài chu kỳ) |
| `_chkAutoDetect.CheckedChanged` | Start/Stop timer | POST `/api/polling/toggle` `{enabled}` → BackgroundService đọc cờ; hoặc client-only: ngắt SignalR listener |
| `_btnInstallSelected.Click` / `_btnInstallAll.Click` | Install song song 4 device | POST `/api/install` `{serials[], selectedOnly}` → chạy song song → push progress SignalR |
| `_btnRemoveSelected.Click` | Xóa thiết bị + confirm | POST `/api/devices/remove` `{serials[]}` (confirm bằng Bootstrap modal client-side) |
| `_btnSelectAll.Click` | Toggle chọn tất cả (theo filter) | JS thuần: iterate `<input type=checkbox>` visible |
| `_grid.CellClick` / `CellValueChanged` | Toggle chọn dòng | JS: click row bất kỳ → toggle checkbox; state lưu Set<serial> client-side |
| `_txtFilterIp.TextChanged` / `_txtFilterVersion.TextChanged` | Re-render grid theo filter | JS thuần: hide/show row |

### 4.4 File `Forms/NetworkScanForm.cs` (300 LOC)

**b1. Controls:** `_txtRange`, `_txtPort`, `_btnScan`, `_btnStop`, `_btnAdd`, `_btnCancel`, `_resultGrid` (2 col: checkbox + address), `_progress`, `_lblStatus`. Mapping tương tự MainForm — đặt trong **Bootstrap Modal** `#networkScanModal`.

**b2. Task ngầm:** Quét dải IP song song `SemaphoreSlim(24)`, timeout 1200ms/địa chỉ, max 512 địa chỉ, có `CancellationTokenSource` cho nút Dừng. → Endpoint POST `/api/scan/start` `{range, port}` chạy trong Task, push progress qua SignalR `ScanProgress`/`ScanFound`, hủy qua POST `/api/scan/cancel`.

**b3. Event (4):**

| Event | Đích |
|---|---|
| `_btnScan.Click` | POST `/api/scan/start` |
| `_btnStop.Click` | POST `/api/scan/cancel` |
| `_btnAdd.Click` | Đóng modal + POST `/api/devices/connect-batch` `{ipPorts[]}` |
| `_btnCancel.Click` | Đóng modal (JS) |

### 4.5 File `Program.cs` (entry)
WinForms bootstrap → **thay bằng** `WebApplication.CreateBuilder(args)` + DI (`AddRazorPages`, `AddSignalR`, `AddSingleton<AdbService>`, `AddSingleton<DeviceRepository>`, `AddHostedService<DevicePollWorker>`, `AddSingleton<InstallCoordinator>`, `AddSingleton<ScanCoordinator>`) + `MapRazorPages` + `MapHub<DeviceHub>("/hubs/device")` + `MapPost` cho API endpoints (hoặc dùng Razor Pages handlers).

### 4.6 csproj

| Trường | Nguồn | Đích |
|---|---|---|
| SDK | `Microsoft.NET.Sdk` | `Microsoft.NET.Sdk.Web` |
| TargetFramework | `net8.0-windows7.0` | `net8.0` (trung lập, chạy Linux) |
| OutputType | `WinExe` | (default `Exe` cho web) |
| UseWindowsForms | `true` | ❌ Bỏ |
| ProjectReference | `KztekComponent` | ❌ Bỏ |
| Reference | `Guna.UI2` DLL | ❌ Bỏ |
| PackageReference | `Microsoft.Data.Sqlite 8.0.8` | ✅ Giữ + thêm `Microsoft.AspNetCore.SignalR` (built-in .NET 8, không cần thêm package) |
| Asset | `<None Include="../../platform-tools/**">` copy adb.exe | ❌ Bỏ khỏi csproj; adb Linux binary sẽ được COPY trong Dockerfile giai đoạn DevOps |

### 4.7 Dependency inventory (Cấp 2c — kiểm cross-platform Linux)

| Thư viện | Dùng | Linux OK? | Hành động | Task |
|---|---|---|---|---|
| `System.Windows.Forms` (implicit qua UseWindowsForms) | UI | ❌ | Bỏ, thay Razor Pages + Bootstrap | T-01 |
| `KztekComponent` (Guna.UI2 WinForms) | Kz* controls | ❌ | Bỏ, thay Bootstrap 5 | T-01, T-07 |
| `Guna.UI2` | Style | ❌ | Bỏ | T-01 |
| `Microsoft.Data.Sqlite 8.0.8` | Lưu SQLite | ✅ | Giữ | — |
| `platform-tools/adb.exe` | Native binary | ❌ (Windows exe) | Thay bằng adb Linux (Google android-tools), COPY trong Dockerfile | T-13 (DevOps) |
| `Microsoft.AspNetCore.SignalR` (built-in) | Realtime push | ✅ | Thêm | T-03 |

---

## 5. Ghi chú bàn giao DevOps (ngoài scope plan này)

Sau khi code migrate xong (Phase 3), DevOps Engineer viết:
1. **Dockerfile** multi-stage:
   ```
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   COPY . /src && cd /src && dotnet publish src/KztekAdbPublishTool.Web -c Release -o /out
   FROM mcr.microsoft.com/dotnet/aspnet:8.0
   RUN apt-get update && apt-get install -y --no-install-recommends android-tools-adb && rm -rf /var/lib/apt/lists/*
   COPY --from=build /out /app
   VOLUME ["/app/data", "/app/uploads"]
   EXPOSE 8080
   ENTRYPOINT ["dotnet", "/app/KztekAdbPublishTool.Web.dll"]
   ```
2. **docker-compose.yml** với `network_mode: host` (để adb connect thấy được LAN 192.168.x.y) + volume mount cho `data/` (SQLite) và `uploads/` (APK).
3. Health check `/health` endpoint.

→ Ghi rõ: **container BẮT BUỘC chạy `--network host` trên Linux host**, nếu không adb connect ra ngoài LAN sẽ fail.

---

## 6. Rủi ro & mitigation

| # | Rủi ro | Mức | Mitigation |
|---|---|---|---|
| R1 | Multi-user cùng bấm Install trên cùng device → 2 process `adb install` chạy song song | Cao | `InstallCoordinator` singleton giữ `ConcurrentDictionary<serial, SemaphoreSlim(1)>` — queue per-device |
| R2 | Real-time UX kém nếu SignalR mất kết nối | Trung | Auto-reconnect (SignalR JS client mặc định) + client fallback: polling `/api/devices` mỗi 5s khi disconnected |
| R3 | APK upload > default limit Kestrel (28MB) | Cao | Cấu hình `Kestrel.Limits.MaxRequestBodySize = 500MB` + `MultipartBodyLengthLimit` |
| R4 | Container không thấy LAN → adb connect timeout | Cao | Ghi rõ yêu cầu `--network host` + healthcheck kiểm `adb version` |
| R5 | adb server state trong container ephemeral | Trung | `adb start-server` trong entrypoint; DB & uploads mount volume để bền vững qua restart |
| R6 | Behavior parity: filter/select-all hiện WinForms là server-side (RenderGrid); nếu chuyển client-side JS có thể lệch | Thấp | Test parity: chọn tất cả sau khi filter → chỉ chọn dòng đang hiển thị (giữ đúng logic OnToggleSelectAll) |
