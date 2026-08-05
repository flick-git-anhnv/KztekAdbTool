---
step: 1.2
title: Port AdbService, DeviceRepository, ApkManifestReader sang project Web
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:00
deps: [1.1]
---

## Nhiệm vụ
Copy 3 file từ `src/KztekAdbPublishTool/Services/` sang `src/KztekAdbPublishTool.Web/Services/`. Sửa constructor `AdbService`/`DeviceRepository` nhận `IOptions<AdbSettings>` (đọc `AdbPath`, `DbPath`). Tạo thư mục cha nếu chưa có trước khi mở SQLite. Đăng ký cả 3 là `Singleton` trong `Program.cs`. Logic method KHÔNG sửa.

## Definition of Done
- [ ] 3 file service compile sạch trong project mới.
- [ ] Có smoke unit test: `DeviceRepository` với path tạm → `Upsert` + `GetAll` trả 1 record.
- [ ] `dotnet build` sạch.

## Artifact
- `src/KztekAdbPublishTool.Web/Services/AdbService.cs`
- `src/KztekAdbPublishTool.Web/Services/DeviceRepository.cs`
- `src/KztekAdbPublishTool.Web/Services/ApkManifestReader.cs`
- `tests/KztekAdbPublishTool.Web.Tests/DeviceRepositoryTests.cs`

## Đã làm
- `Models/DeviceRecord.cs`: copy POCO, đổi namespace, ConnectionType default = "WiFi" (web dùng WiFi-only)
- `Services/AdbService.cs`: constructor đổi `string adbPath` → `IOptions<AdbSettings>`, logic giữ nguyên 100%
- `Services/DeviceRepository.cs`: constructor đổi `string dbPath` → `IOptions<AdbSettings>`, thêm `Directory.CreateDirectory(dir)` trước khi mở DB (critical trong container khi volume mount lần đầu)
- `Services/ApkManifestReader.cs`: chuyển từ `static class` → `sealed class` (instance Singleton), logic giữ nguyên 100%
- `tests/KztekAdbPublishTool.Web.Tests/`: test project xunit, 5 test case DeviceRepository — all pass
- Gotcha fix: thêm `SqliteConnection.ClearAllPools()` trong `Dispose()` của test — thiếu thì IOException "file is being used by another process" trên Windows

## Handoff Payload
- Đã làm: 3 service + 1 model port xong, build sạch, 5/5 test pass
- do_not_redo: KHÔNG sửa logic method của 3 service — đã giữ nguyên 100% từ WinForms
- watch_out: `ApkManifestReader` không còn static — Phase 2 dùng qua DI injection, không gọi `ApkManifestReader.TryGetPackageName()` trực tiếp mà phải inject `ApkManifestReader` instance
- watch_out: `SqliteConnection.ClearAllPools()` bắt buộc trong teardown test SQLite trên Windows — ghi vào GOTCHAS nếu chưa có
- next_inputs: 3 service đã đăng ký Singleton trong Program.cs; STEP 1.3 dùng DeviceHub + DevicePollWorker (chưa tạo)
