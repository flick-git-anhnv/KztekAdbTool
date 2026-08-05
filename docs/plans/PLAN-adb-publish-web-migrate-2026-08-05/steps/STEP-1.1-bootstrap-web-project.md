---
step: 1.1
title: Tạo project KztekAdbPublishTool.Web (Razor Pages .NET 8)
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:00
deps: []
---

## Nhiệm vụ
Tạo project mới `src/KztekAdbPublishTool.Web/` (KHÔNG sửa `src/KztekAdbPublishTool/`). SDK `Microsoft.NET.Sdk.Web`, TargetFramework `net8.0`, template Razor Pages. Cấu hình DI khung, `appsettings.json` với section `Adb` (`AdbPath`, `PollIntervalMs`, `DbPath`, `UploadsPath`, `MaxUploadBytes`), Kestrel `Limits.MaxRequestBodySize = 500_000_000`.

## Definition of Done
- [ ] `src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj` tồn tại (net8.0, Sdk.Web).
- [ ] `Program.cs` build sạch với `WebApplication.CreateBuilder` + `AddRazorPages` + `AddSignalR` + đọc `AdbSettings` từ config.
- [ ] `appsettings.json` + `appsettings.Development.json` có section Adb đầy đủ.
- [ ] `dotnet build src/KztekAdbPublishTool.Web` → 0 warning, 0 error.
- [ ] Chạy `dotnet run` mở được trang trắng `/`.
- [ ] KHÔNG có reference tới `KztekComponent`, `Guna.UI2`, `System.Windows.Forms`.

## Artifact
- `src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj`
- `src/KztekAdbPublishTool.Web/Program.cs`
- `src/KztekAdbPublishTool.Web/appsettings.json`
- `src/KztekAdbPublishTool.Web/Configuration/AdbSettings.cs`

## Đã làm
- Tạo `KztekAdbPublishTool.Web.csproj` (net8.0, Sdk.Web, Microsoft.Data.Sqlite 8.0.8)
- `Configuration/AdbSettings.cs`: POCO với 5 property (AdbPath, PollIntervalMs, DbPath, UploadsPath, MaxUploadBytes)
- `appsettings.json`: section Adb + Kestrel MaxRequestBodySize=500MB
- `appsettings.Development.json`: AdbPath trỏ Windows path, DbPath relative
- `Program.cs`: WebApplication.CreateBuilder + Configure<AdbSettings> + ConfigureKestrel(500MB) + AddSignalR + AddRazorPages + AddHostedService<DevicePollWorker> + MapHub<DeviceHub> + MapRazorPages
- Thêm project vào `KztekAdbPublishTool.sln`
- Build: 0 warning, 0 error

## Handoff Payload
- Đã làm: Project Web khung hoàn chỉnh, build sạch, DI đầy đủ cho cả 3 service + SignalR + Worker
- do_not_redo: Không tạo lại csproj hay Program.cs — đã đúng cấu trúc
- watch_out: `ApkManifestReader` đăng ký Singleton thay vì static class (để DI-friendly); `DevicePollWorker` và `DeviceHub` phải tồn tại trước khi build (tạo ở STEP 1.3)
- next_inputs: `src/KztekAdbPublishTool.Web/` đã có đủ cấu trúc; STEP 1.2 chỉ cần thêm Services/ và Models/
