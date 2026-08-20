---
step: 3.1
plan: ../PLAN-MASTER.md
agent: senior-developer
status: todo
completed_at:
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
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
