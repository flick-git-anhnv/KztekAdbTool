---
step: "2.2"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: "2026-08-19 11:43"
deps: ["2.1"]
---

# STEP 2.2 — Tech Lead: Code review PR fix

## Input nhận

**Từ STEP-2.1 (Senior Developer):**
- Branch: `docker-deploy`, commit `3a86825`
- Files changed: `Services/IAdbService.cs` (new), `Services/AdbService.cs` (implement interface), `Workers/DevicePollWorker.cs` (WarmUpReconnectAsync), `Program.cs` (DI), `KztekAdbPublishTool.Web.csproj` (InternalsVisibleTo), `tests/.../DevicePollWorkerWarmUpTests.cs` (new, 7 tests)
- VERIFICATION REPORT: PASS toàn bộ (build 0 error, 71/71 test, security PASS, diff clean)
- Handoff: WiFi filter dùng `Contains(':')` Ordinal, best-effort error handling (không throw), timeout 5s per device

## Nhiệm vụ

Review PR fix auto-reconnect: correctness, race condition, error handling, filter logic, test coverage, side effects. Xác nhận /verify-pr đã PASS trước khi approve.

## Đã làm

### 1. Xác nhận VERIFICATION REPORT (độc lập, không tin report một chiều)

Chạy lại `dotnet build src/KztekAdbPublishTool.Web/` → PASS 0 error/0 warning, 1.40s. Chạy lại `dotnet test tests/KztekAdbPublishTool.Web.Tests/` → 71 passed / 0 failed / 207ms. Report chính xác.

### 2. Review từng điểm

| # | Điểm review | Nhãn | Kết luận |
|---|-------------|------|----------|
| 1 | **Correctness root cause fix** | (Required) | PASS — `WarmUpReconnectAsync` được gọi trước `while` loop → `ConnectAsync` tạo lại TCP session cho WiFi devices → `PollAsync` đầu tiên (`manualTrigger=true`) sẽ thấy chúng trong `adb devices -l` → `DeviceState` được populate. Sửa đúng gốc, không che triệu chứng. |
| 2 | **Race condition / startup blocking** | (Required) | PASS — `BackgroundService.StartAsync` trong .NET 8 không await `ExecuteAsync`; hit await đầu tiên → return ngay, Kestrel/API vẫn khởi động song song. Warm-up không block web host. Poll loop chỉ chờ warm-up xong, phù hợp thiết kế. |
| 3 | **Error handling / best-effort** | (Required) | PASS — Wrap từng device trong `try/catch`: `OperationCanceledException` → break sạch, `Exception` → log warning + continue. Không throw ra ngoài → worker không crash. `ct.IsCancellationRequested` check ở đầu foreach → tôn trọng shutdown. |
| 4 | **WiFi/USB filter logic** | (Required) | PASS — `Serial.Contains(':', StringComparison.Ordinal)` đúng cho serial ADB WiFi (luôn `ip:port` IPv4). USB serial là alphanumeric (VD `HT7A21234ABC`, `emulator-5554`) — không chứa `:`. ADB không hỗ trợ IPv6 trong stable release — không cần lo edge case IPv6. Test cases đã cover mixed/USB-only/WiFi-only. |
| 5 | **Test coverage** | (Optional) | PASS — 7 test đã cover main paths + edge cases quan trọng (single/multi WiFi, USB-only filter, mixed, fail-continue, empty DB, pre-cancelled). |
| 6 | **Không có side effect ngoài scope** | (Required) | PASS — Diff chỉ chạm 5 file production + 1 file test + 2 doc (CODE-GRAPH, TDD). Không lẫn thay đổi ngoài scope. `AddSingleton<AdbService>` cũ giữ nguyên → không breaking cho caller khác. |
| 7 | **KZTEK convention (§20 CLAUDE.md)** | (FYI) | N/A — Fix thuần backend/hosted service, không đụng UI. Rule KztekComponent không áp dụng. |
| 8 | **Interface Segregation** | (FYI) | Tốt — `IAdbService` chỉ 3 method DevicePollWorker cần (không dồn hết public API của AdbService vào interface). Đúng ISP, testability cao mà không thêm bề mặt. |

### 3. Nhận xét bổ sung (không blocking)

**Optional:** Branch "ADB binary missing" (`result.ExitCode == -1 && string.IsNullOrEmpty(result.StdOut)`) trong `WarmUpReconnectAsync` chưa có test riêng. Đây là early-return path — không gây hại nếu thiếu, nhưng nếu test được sẽ giúp future refactor an toàn. Ghi nhận là kỹ thuật debt nhỏ, không yêu cầu sửa trong PR này.

**Optional (Future):** Warm-up hiện tuần tự (5s/device). Với fleet lớn (> 10 device offline), startup warm-up có thể kéo dài đến 50s+, trong khoảng đó `GET /api/devices/{serial}/status` vẫn 404. Với KZTEK use case hiện tại (few devices) — chấp nhận được. Nếu tương lai fleet lớn hơn, cân nhắc `Task.WhenAll` nhưng cần thận trọng vì `adb server` là single Unix socket, connect song song có thể race. Không phải blocker cho PR này.

**FYI:** Pre-existing format issues ở `DeviceEndpoints.cs` và `InstallCoordinator.cs` KHÔNG do PR này (Senior đã note chính xác). Đúng nguyên tắc scope creep — không mix cleanup vào bug fix P1.

### 4. Quyết định

**APPROVE — không cần request changes.** Fix đúng root cause, thiết kế sạch (IAdbService segregation), best-effort error handling đầy đủ, test coverage đủ cho paths quan trọng, không có side effect. Không cần escalate CTO — bug fix phạm vi hẹp, không đụng auth/payment/DB schema.

## Artifact

- Review comments: nhúng vào step file này (không có PR trên GitHub — commit trực tiếp trên `docker-deploy`)
- Kết quả: APPROVE, cho phép giữ nguyên commit `3a86825` để chuyển sang QA verify.

## Quyết định quan trọng

1. **APPROVE mà không blocking-comment nào** — vì các concern (test coverage branch "binary missing", parallel connect) đều là Optional, không phải Required/Critical.
2. **Không escalate CTO** — bug fix P1 phạm vi hẹp, không thay đổi kiến trúc; escalate không tăng giá trị.
3. **Không tự sửa code thay Senior** — không có gì cần sửa.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: Không cần re-build/re-test — Tech Lead đã verify độc lập (build 0 error, 71/71 test PASS). Không cần đọc lại toàn bộ diff — logic warm-up đã được review kỹ.
- watch_out: QA verify PHẢI thực hiện đúng steps-to-reproduce trong BUG report (restart service, KHÔNG scan trên UI, gọi `GET /api/devices/{serial}/status`). Chú ý: startup có thể mất vài giây warm-up nếu có nhiều WiFi device trong DB — chờ log `DevicePollWorker warm-up: reconnecting X persisted WiFi device(s)...` xong rồi mới test API. Nếu ADB binary missing trên môi trường staging → warm-up sẽ early-return với log warning, poll loop vẫn chạy (không crash) nhưng API vẫn 404 — cần kiểm tra ADB binary có sẵn.
- next_inputs: Commit `3a86825` trên nhánh `docker-deploy` đã APPROVE, sẵn sàng deploy staging. BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md` là source of truth cho AC verification. Regression: chạy toàn bộ WF-BUGFIX QA regression cho feature `adb-add-device-api` (không chỉ smoke test).

## Commit

- Hash: [điền sau commit review]
- Đã push: chưa (sẽ commit sau khi hoàn thành review)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
