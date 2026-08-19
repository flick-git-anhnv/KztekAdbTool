---
step: 3.1
plan: ../PLAN-MASTER.md
agent: Senior Developer
status: done
completed_at: 2026-08-19 16:30
deps: [2.1]
---

# STEP 3.1 — Code 2 endpoint mới, unit test

## Input nhận
Nhận Handoff Payload từ STEP-2.1 (TDD đã chốt API contract). Đọc TDD tại `docs/tech-design/TDD-adb-add-device-api.md` để lấy đúng route/DTO/status code đã quyết định — KHÔNG tự đoán lại thiết kế.

**Trước khi code:** chạy `pre-coding-check` skill (`.claude/commands/pre-coding-check.md`) — đọc `code-graph/CODE-GRAPH.md` trước source files.

## Nhiệm vụ
Implement đúng theo TDD:
1. Endpoint `POST /api/devices/connect-by-ip` — tái sử dụng `AdbService.ConnectAsync` + helper `EnsurePort`.
2. Endpoint `GET /api/devices/{serial}/status` — tái sử dụng `DeviceState.TryGet`.
3. Gắn `.AddEndpointFilter<ApiKeyEndpointFilter>()` cho cả 2 route (tái dùng nguyên vẹn, không viết filter mới).
4. Đăng ký route mới trong `Program.cs`.
5. Viết unit test đầy đủ (theo pattern `LaunchAppEndpoints` đã có test trước đó — kiểm tra `tests/` project hiện có để theo đúng convention).
6. Cập nhật `code-graph/CODE-GRAPH.md` + xuất `CODE-GRAPH.pdf` (R2/R3 CORE.md) — thêm route mới, dependency mới nếu có.

## Definition of Done
- [ ] Code 2 endpoint đúng theo TDD, build sạch (`dotnet build`)
- [ ] Unit test cho cả 2 endpoint, tất cả PASS (`dotnet test`)
- [ ] `code-graph/CODE-GRAPH.md` + `.pdf` cập nhật
- [ ] Ghi chú trong step file: nếu cần sửa `appsettings.json` mà bị hook chặn → ghi lại giống plan mẫu (user tự làm ngoài quy trình)

## Đã làm
- Đọc TDD `TDD-adb-add-device-api.md`, CODE-GRAPH.md, LaunchAppEndpoints.cs, Program.cs, DeviceState.cs, AdbService.ConnectAsync, PollControlService.TriggerAsync, LaunchAppEndpointTests.cs để nắm pattern.
- Tạo `DeviceConnectionEndpoints.cs`: implement `POST /api/devices/connect-by-ip` (validate ip/port, gọi AdbService.ConnectAsync với target `ip:port`, trigger PollControlService.TriggerAsync khi success, trả 200/400/422/500) và `GET /api/devices/{serial}/status` (đọc DeviceState.TryGet, trả 200/400/404). Cả 2 gắn ApiKeyEndpointFilter.
- Đổi `ValidateConnectInput` từ `internal` → `public static` để test project truy cập trực tiếp (giống LaunchAppEndpoints.ValidateInput).
- Đăng ký `app.MapDeviceConnectionEndpoints()` trong Program.cs sau `app.MapLaunchAppEndpoints()`.
- Tạo `DeviceConnectionEndpointTests.cs`: 22 test case (validate ip/port edge cases, CheckStatus logic via DeviceState, target build).
- Build sạch (`dotnet build` Web project: 0 lỗi, 0 cảnh báo). Test toàn bộ: 64/64 PASS (42 cũ + 22 mới).
- Cập nhật CODE-GRAPH.md: thêm entry `DeviceConnectionEndpoints`, cập nhật callers của AdbService/PollControlService/DeviceState/ApiKeyEndpointFilter, thêm 2 route mới vào bảng 2.3. Xuất CODE-GRAPH.docx (PDF thất bại do thiếu xelatex — không block workflow per §19.4).

## Artifact
- `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` (mới)
- `src/KztekAdbPublishTool.Web/Program.cs` (sửa — thêm dòng đăng ký)
- `tests/KztekAdbPublishTool.Web.Tests/DeviceConnectionEndpointTests.cs` (mới — 22 test)
- `code-graph/CODE-GRAPH.md` (cập nhật)
- `code-graph/CODE-GRAPH.docx` (cập nhật)

## Quyết định quan trọng
- `ValidateConnectInput` → `public static` (không phải `internal` như TDD gợi ý) để test project assembly khác truy cập trực tiếp. Nhất quán với pattern `LaunchAppEndpoints.ValidateInput`.
- KHÔNG tái dùng `EnsurePort` từ `DeviceEndpoints.cs` — construct inline `$"{ip.Trim()}:{port ?? DefaultAdbPort}"` với hằng số `const int DefaultAdbPort = 5555` trong file mới (theo Handoff Payload từ STEP-2.1).
- PDF xuất thất bại (thiếu xelatex trên môi trường Linux WSL2) — DOCX đã thành công, ghi nhận theo §19.4 không block.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: DeviceConnectionEndpoints.cs đã implement đúng TDD. Program.cs đã đăng ký. Test 64/64 PASS. CODE-GRAPH cập nhật xong. KHÔNG viết lại hay đổi API contract.
- watch_out:
  1. `ValidateConnectInput` là `public static` (không phải `internal`) — đây là quyết định đúng để test project truy cập được.
  2. PDF CODE-GRAPH vẫn chưa xuất được trên môi trường WSL2 (thiếu xelatex). DOCX đã cập nhật — chấp nhận được.
  3. Test mới trong `DeviceConnectionEndpointTests.cs` test logic qua DeviceState trực tiếp (không mock AdbService) — pattern nhất quán với LaunchAppEndpointTests.
  4. 2 route cũ của `DeviceEndpoints.cs` (`/api/devices/connect`, `/api/devices/connect-batch`) không bị đụng chạm — đã xác nhận qua grep + build.
- next_inputs:
  1. File cần review: `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` (file chính, 140 dòng).
  2. Test file: `tests/KztekAdbPublishTool.Web.Tests/DeviceConnectionEndpointTests.cs` (22 test).
  3. TDD checklist đầy đủ tại `docs/tech-design/TDD-adb-add-device-api.md` — mục "Code Review Checklist" cuối file.
  4. Build verification: `dotnet build src/KztekAdbPublishTool.Web/` → 0 lỗi; `dotnet test tests/KztekAdbPublishTool.Web.Tests/` → 64/64 PASS.
  5. Commit hash: `3c5541b`.

## Commit
- Hash: 3c5541b
- Đã push: Yes (docker-deploy → origin/docker-deploy)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
