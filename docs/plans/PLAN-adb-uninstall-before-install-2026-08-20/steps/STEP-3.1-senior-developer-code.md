---
step: 3.1
plan: ../PLAN-MASTER.md
agent: senior-developer
status: done
completed_at: 2026-08-20 14:17
deps: [2.1]
---

# STEP 3.1 — Code: Uninstall Before Install (AdbService + InstallCoordinator + UI + JS)

## Input nhận
- TDD: `docs/tech-design/TDD-adb-uninstall-before-install.md` — đọc toàn bộ trước khi code
- Handoff Payload từ STEP-2.1 (đọc mục "Handoff Payload" trong step file đó)
- Bối cảnh kỹ thuật (đã khảo sát trước — không cần đọc lại toàn bộ codebase):
  - `IAdbService.cs` + `AdbService.cs`: thêm `UninstallApkAsync(string serial, string packageName, CancellationToken ct)` — chạy `adb -s {serial} uninstall {packageName}`, bắt lỗi exit code khác 0 → log warning, không throw
  - `InstallEndpoints.cs`: thêm `bool UninstallBeforeInstall` vào `InstallRequest` (default false)
  - `InstallCoordinator.cs` `DoInstallAsync`: nếu `request.UninstallBeforeInstall == true` → gọi `UninstallApkAsync` trước các bước hiện tại; push SignalR "Đang gỡ cài đặt..." ở bước mới
  - `Index.cshtml`: thêm checkbox `chk-uninstall-before-install` — vị trí và label theo TDD
  - `dashboard.js`: đọc checkbox state và truyền `uninstallBeforeInstall` vào payload `apiPost('/api/install', {...})`

## Nhiệm vụ
Implement đầy đủ feature theo TDD: thêm `UninstallApkAsync` vào `AdbService`/`IAdbService`, mở rộng `InstallRequest` và `DoInstallAsync`, thêm checkbox UI và cập nhật JS handler. Chạy build sạch và viết/chạy unit test cho `UninstallApkAsync` (bao phủ: thành công, package chưa cài — graceful skip). Cập nhật `code-graph/CODE-GRAPH.md`.

## Definition of Done
- [ ] `IAdbService.cs` có method `UninstallApkAsync`
- [ ] `AdbService.cs` implement `UninstallApkAsync` — xử lý graceful khi package chưa cài (log warning, không throw)
- [ ] `InstallRequest` có field `UninstallBeforeInstall` (bool, default false)
- [ ] `InstallCoordinator.DoInstallAsync` gọi `UninstallApkAsync` đúng thứ tự khi flag bật, push SignalR progress
- [ ] `Index.cshtml` có checkbox mới đúng vị trí/style theo TDD
- [ ] `dashboard.js` truyền flag `uninstallBeforeInstall` trong payload cả 2 handler install
- [ ] Build sạch (0 error, 0 warning mới)
- [ ] Unit test cho `UninstallApkAsync` pass (≥ 2 case: thành công + package chưa cài)
- [ ] `code-graph/CODE-GRAPH.md` cập nhật module mới/sửa
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
- **AdbService.cs**: Thêm `UninstallApkAsync(string serial, string packageName, CancellationToken ct = default)` — gọi `RunAsync($"-s {serial} uninstall {packageName}", timeoutMs: 30000)`. KHÔNG thêm vào `IAdbService.cs`.
- **InstallCoordinator.cs**: Mở rộng `QueueInstalls`/`InstallOneAsync`/`DoInstallAsync` thêm param `bool uninstallBeforeInstall` (KHÔNG default). Block uninstall graceful: kiểm tra `Success && StdOut.Contains("Success", OrdinalIgnoreCase)` để chọn SignalR message; mọi lỗi log WARNING không abort. Shift percent: flag BẬT = 0/15/25/40/55/70/85/100; flag TẮT giữ nguyên 0/20/40/60/80/100 (giá trị mốc thay đổi, được xác nhận lại với TDD: pList=0→25, pInstall=20→40, pVerify=40→55, pVersion=60→70, pLaunch=80→85).
- **InstallEndpoints.cs**: Thêm `InstallRequest.UninstallBeforeInstall` (bool, default false). Truyền vào `QueueInstalls`. Thêm field vào response Accepted.
- **DeviceEndpoints.cs**: Endpoint `POST /api/settings/uninstall-before-install` + DTO `UninstallBeforeInstallSettingRequest(bool Enabled)` — lưu `"true"`/`"false"` vào DB Settings.
- **Index.cshtml.cs**: Property `UninstallBeforeInstall { get; private set; }`. Load trong `OnGet()` bằng `bool.TryParse(...) && v` fallback false (EC5).
- **Index.cshtml**: Checkbox `chk-uninstall-before-install` đặt ngay sau `chk-auto-detect` (hàng 3 toolbar), `form-check form-switch`, label "Gỡ cài đặt app trước khi cài".
- **dashboard.js**: Change handler cho checkbox (gần txt-package blur handler ~dòng 452); 2 install handler (`btn-install-selected`, `btn-install-all`) thêm `uninstallBeforeInstall` vào payload.
- **UninstallBeforeInstallTests.cs** (mới): 12 unit tests — 5 classify cases (real success/exit0-fail/exit1-fail/timeout/case-insensitive), 2 InstallRequest default/set, 2 endpoint repo logic, 3 IndexModel parse EC5.
- **CODE-GRAPH.md + .docx**: Cập nhật entries AdbService, InstallCoordinator, DeviceEndpoints, InstallEndpoints, Pages/Index; thêm endpoint mới vào §2.3.

## Artifact
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Services/AdbService.cs`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Services/InstallCoordinator.cs`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Endpoints/InstallEndpoints.cs`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Pages/Index.cshtml.cs`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/Pages/Index.cshtml`
- `/home/duonghoang21/docker/KztekAdbTool/src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js`
- `/home/duonghoang21/docker/KztekAdbTool/tests/KztekAdbPublishTool.Web.Tests/UninstallBeforeInstallTests.cs` (mới)
- `/home/duonghoang21/docker/KztekAdbTool/code-graph/CODE-GRAPH.md` (updated)
- `/home/duonghoang21/docker/KztekAdbTool/code-graph/CODE-GRAPH.docx` (updated)

## Quyết định quan trọng
- **Theo đúng TDD**: `UninstallApkAsync` KHÔNG vào `IAdbService` — khớp TDD Non-goals. Step file gốc ghi "thêm vào IAdbService" là sai (ghi trước TDD), đã bỏ qua theo hướng dẫn đầu task.
- **Percent mốc xác nhận**: TDD chỉ định pInstall=40 (flag BẬT), xác nhận lại với table §DoInstallAsync — 0/20/40/60/80 → 0/15/25/40/55/70/85/100. Implement đúng table TDD.
- **Build môi trường**: Local WSL thiếu `Microsoft.NETCore.App` runtime — build và test chạy qua Docker SDK container `mcr.microsoft.com/dotnet/sdk:8.0-jammy`. Result: 0 error, 97/97 test pass.
- **CODE-GRAPH PDF**: Thất bại (xelatex không có trong môi trường) — DOCX đã xuất thành công. Ghi nhận theo §19.4 (PDF fail không block).

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: KHÔNG thêm `UninstallApkAsync` vào `IAdbService.cs` (đây là quyết định chốt của TDD §Non-goals). KHÔNG đổi percent mốc khi flag TẮT (phải giữ nguyên 0/20/40/60/80/100 backward-compat).
- watch_out: Build phải chạy qua Docker SDK (môi trường local thiếu `Microsoft.NETCore.App`). Test classify dùng `result.Success && StdOut.Contains("Success", OrdinalIgnoreCase)` — không dùng exit code đơn thuần (watch_out từ TDD §D2: exit code 0 + stdout "Failure [...]" phải là fail). CODE-GRAPH PDF không có (môi trường thiếu pdf engine), DOCX OK.
- next_inputs: Files đã sửa: `AdbService.cs`, `InstallCoordinator.cs`, `InstallEndpoints.cs`, `DeviceEndpoints.cs`, `Index.cshtml.cs`, `Index.cshtml`, `dashboard.js`. Test mới: `UninstallBeforeInstallTests.cs` (12 tests). Build result: 0 error. Test result: 97/97 pass. Commit: c0565c3 (local, chưa push).

## Commit
- Hash: c0565c3
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
