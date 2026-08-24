---
step: 3.1
plan: ../PLAN-MASTER.md
agent: Senior Developer
status: todo
completed_at: ~
deps: ["2.1"]
---

# STEP 3.1 — Code AdbService methods + 2 endpoints + UI buttons + unit tests + CODE-GRAPH

## Input nhận
Từ Bước 2.1 Handoff Payload — `docs/tech-design/TDD-adb-app-status-reboot-api.md` (API contract chính xác, ADB command đã chọn, interface AdbService, UI spec 2 nút).

**QUAN TRỌNG — KHÔNG đụng:**
- Project `src/KztekAdbPublishTool` (WinForms gốc) — bất khả xâm phạm
- Các thay đổi đang pending (docker-compose.yml, AdbService.cs cả 2 project, appsettings.json từ plan adb-reconnect) — KHÔNG revert, KHÔNG conflict

**Pre-coding check (bắt buộc trước khi code):** Đọc `code-graph/CODE-GRAPH.md` trả lời ≥3/5 câu hỏi; tra `.claude/shared/GOTCHAS.md` (đặc biệt G008: KHÔNG đọc raw Request.Body trong filter — dùng `context.Arguments`).

## Nhiệm vụ

### 1. Thêm vào `AdbService.cs` + `IAdbService.cs` (Web project):
- `GetAppStatusAsync(string serial, string packageName, CancellationToken ct)` — chạy pidof + dumpsys theo TDD, trả về `AppStatusResult`
- `RebootDeviceAsync(string serial, CancellationToken ct)` — chạy `adb -s {serial} reboot`
- Model `AppStatusResult` + enum `AppState` (tạo file mới hoặc thêm vào Models/)

### 2. Tạo file endpoints mới:
- `src/KztekAdbPublishTool.Web/Endpoints/AppStatusEndpoints.cs` — GET /api/devices/{serial}/app-status
- `src/KztekAdbPublishTool.Web/Endpoints/RebootEndpoints.cs` — POST /api/devices/{serial}/reboot
- Cả 2 dùng OUTER `ApiRequestLoggingEndpointFilter` + INNER `ApiKeyEndpointFilter` theo pattern LaunchAppEndpoints.cs
- Đăng ký trong `Program.cs`

### 3. UI buttons trong `Pages/Index.cshtml` + `wwwroot/js/dashboard.js`:
- 2 nút mới theo UI spec từ TDD
- Nút "Reboot Device": confirm dialog trước khi gọi API
- Nút "Check App Status": cần input package name, hiển thị kết quả

### 4. Unit tests:
- `tests/KztekAdbPublishTool.Web.Tests/` — test cho `GetAppStatusAsync` + `RebootDeviceAsync` + 2 endpoints (happy path + edge cases từ TDD)
- Chạy `dotnet test` — toàn bộ PHẢI PASS

### 5. Cập nhật CODE-GRAPH:
- Thêm 2 module endpoint mới, 2 method AdbService mới vào `code-graph/CODE-GRAPH.md`
- Cập nhật relationships/callers
- Xuất lại `code-graph/CODE-GRAPH.pdf`

## Definition of Done
- [ ] `AdbService.cs` + `IAdbService.cs` có 2 method mới, build không lỗi
- [ ] `AppStatusEndpoints.cs` + `RebootEndpoints.cs` tạo xong, đăng ký trong `Program.cs`
- [ ] UI: 2 nút bấm hiển thị đúng trên dashboard, confirm dialog cho Reboot
- [ ] `dotnet test` — toàn bộ test PASS (không có test nào fail/skip mới)
- [ ] `code-graph/CODE-GRAPH.md` cập nhật + `CODE-GRAPH.pdf` xuất lại
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 3.1 → ✅

## Đã làm
(để trống)

## Artifact
(để trống)

## Quyết định quan trọng
(để trống)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
