---
step: 3.1
plan: ../PLAN-MASTER.md
agent: Senior Developer
status: todo
completed_at:
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


## Artifact


## Quyết định quan trọng


## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo:
- watch_out:
- next_inputs:

## Commit
- Hash:
- Đã push:

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
