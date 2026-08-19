---
step: "2.4"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: "2026-08-19 12:08"
deps: ["2.3"]
---

# STEP 2.4 — Tech Lead: Code review lại fix 2.3 (per-device timeout vs service cancellation)

## Input nhận

**Từ STEP-2.3 (Senior Developer):**
- Commit `cff893f` trên `docker-deploy` — sửa `AdbService.RunAsync` (re-throw OCE nếu `ct` gốc cancelled) + `WarmUpReconnectAsync` (phân biệt per-device timeout vs shutdown) + 2 test mới.
- VERIFICATION REPORT: 73/73 test PASS.
- Handoff: xem hàng `AdbService.cs` lines ~76-96 (RunAsync catch block), `DevicePollWorker.cs` lines ~107-160 (foreach body), 2 test mới trong `DevicePollWorkerWarmUpTests.cs`.

## Nhiệm vụ

Verify độc lập fix 2.3: correctness của `ct.ThrowIfCancellationRequested()` đặt đúng chỗ, kiểm tra race edge case, xác nhận test không "fake pass", đảm bảo không regression tests cũ + kiểm tra log message fix.

## Đã làm

### 1. Verify độc lập (không tin report một chiều)

- `dotnet build src/KztekAdbPublishTool.Web/` → **0 error, 0 warning, 2.39s**.
- `dotnet test tests/KztekAdbPublishTool.Web.Tests/` → **73 passed / 0 failed / 246 ms**. Khớp report của Senior Developer.
- Đọc trực tiếp commit `cff893f` bằng `git show cff893f -- <file>` cho từng file thay đổi.

### 2. Review từng điểm

| # | Điểm review | Nhãn | Kết luận |
|---|-------------|------|----------|
| 1 | **Vị trí `ct.ThrowIfCancellationRequested()` trong AdbService.RunAsync (line ~90)** | (Required) | **PASS** — Đặt SAU `process.Kill(true)` (đảm bảo cleanup process trước rồi mới decide throw), TRƯỚC `return AdbCommandResult`. Khi `ct` gốc cancelled → throw OCE để caller phân biệt; khi chỉ `timeoutCts` internal fire → `ct.IsCancellationRequested == false`, không throw, return `ExitCode=-1` + StdErr timeout message như cũ. Logic phân biệt chính xác. |
| 2 | **Edge case race — `ct` cancel cùng lúc `timeoutCts` fire** | (Required) | **PASS** — Nếu cả hai fire cùng lúc: `WaitForExitAsync(timeoutCts.Token)` throws OCE; `ct.ThrowIfCancellationRequested()` re-throw vì `ct.IsCancellationRequested == true`; `WarmUpReconnectAsync` catch → `ct.IsCancellationRequested == true` → `break`. Hành vi đúng (service đang shutdown, không cần thử tiếp). Không có exception rò rỉ ra ngoài `WarmUpReconnectAsync` để crash worker. |
| 3 | **Edge case race 2 — `ct` cancel giữa lúc check và return** | (FYI) | Chấp nhận được — window rất nhỏ. Nếu ct cancel SAU `ThrowIfCancellationRequested` nhưng TRƯỚC return: trả về result timeout; iteration tiếp `if (ct.IsCancellationRequested) break;` sẽ dừng ngay. Có thể tốn 1 iteration thừa nhưng không crash, không gây side effect xấu. |
| 4 | **Binary-not-found detection thay `IsNullOrEmpty(StdOut)` → `StdErr.Contains("Không tìm thấy adb tại")`** | (Required) | **PASS** — Verify tại `AdbService.cs:44-51`: khi `!File.Exists(_adbPath)`, RunAsync trả về `StdErr = $"Không tìm thấy adb tại: {_adbPath}"`. Detection mới precise, khớp chính xác constant string. Phân biệt đúng với timeout (StdErr = `"adb {args} timeout sau {ms}ms"`). Loại bỏ false positive của logic cũ. |
| 5 | **`ExitCode != 0` → log + `continue`** | (Required) | **PASS** — Đặt SAU branch binary-not-found → không "nuốt" case binary missing. Bắt tất cả non-success (timeout, offline, network error) → warning log rõ ràng ("failed/timeout") + `continue` device kế. Đúng thiết kế best-effort. |
| 6 | **Catch OCE phân biệt shutdown vs defensive** | (Required) | **PASS** — `if (ct.IsCancellationRequested) break;` xử lý đúng shutdown thật; nhánh `continue` bên dưới là defensive path (OCE propagate không do ct gốc — hiện tại RunAsync chỉ throw khi ct cancel, nhưng đề phòng future refactor). Log warning riêng cho "timeout" ở defensive path — không gây nhầm lẫn message. |
| 7 | **Test 1 `FirstDeviceTimesOut_ContinuesToRemainingDevices` không "fake pass"** | (Required) | **PASS** — 3 WiFi devices, `TimeoutOnSerial.Add("192.168.1.10:5555")` → FakeAdbService trả về `ExitCode=-1, StdErr="adb connect ... timeout"` (KHÔNG chứa "Không tìm thấy adb tại"). WarmUpReconnectAsync đi qua branch `ExitCode != 0` → log + continue → device 2, 3 vẫn được gọi. Assert `ConnectCalls.Count == 3` verify đúng root cause. Không phải test giả. |
| 8 | **Test 2 `FirstDeviceOcesWithoutCtCancel_ContinuesToNextDevice`** | (Required) | **PASS** — `OceOnSerial` ném `OperationCanceledException` mà ct gốc KHÔNG cancel → catch OCE trong WarmUpReconnectAsync → `ct.IsCancellationRequested == false` → nhánh `continue` defensive. Assert `ConnectCalls.Count == 2` verify chính xác defensive path. |
| 9 | **FakeAdbService mô phỏng thật với `ct.ThrowIfCancellationRequested()` đầu ConnectAsync** | (Required) | **PASS** — Khớp hành vi real AdbService (re-throw khi ct cancel). Điều này bảo vệ test `CancelledBeforeStart_ConnectNotCalled` khỏi rò rỉ nhánh test sai (thực tế test cũ này check `ct` PRE-loop nên FakeAdbService không được gọi, nhưng defensive vẫn đúng đắn). |
| 10 | **Regression — 7 test cũ vẫn PASS + ý nghĩa không đổi** | (Required) | **PASS** — 9/9 warm-up test + toàn bộ 73/73 test project pass. Đặc biệt `OneDeviceConnectFails_ContinuesToNextDevice_DoesNotThrow` (dùng FailOnSerial → `InvalidOperationException`) vẫn đi qua `catch (Exception ex)` branch không đổi → không bị ảnh hưởng bởi 2 catch block mới. |
| 11 | **Log message không còn nhầm giữa "binary not found" và "timeout"** | (Required) | **PASS** — Nhánh binary-not-found chỉ log khi StdErr chứa "Không tìm thấy adb tại"; nhánh `ExitCode != 0` log "failed/timeout"; nhánh OCE defensive log "timeout". Message rõ ràng theo từng case, không còn misleading như QA đã báo ở STEP-3.1. |
| 12 | **Side effect ra ngoài scope** | (Required) | **PASS** — Diff gọn: 3 file production/test + 1 doc CODE-GRAPH. Không lẫn cleanup ngoài scope. `ct.ThrowIfCancellationRequested()` trong RunAsync có ảnh hưởng callers khác (PollAsync, GetDevicesAsync, ...) — Senior đã note: `PollAsync` có `catch (OperationCanceledException)` bao quanh, không bị vỡ. Verify: `grep -rn "await _adb\." src/KztekAdbPublishTool.Web/` cho thấy caller khác đều nằm trong try/catch hoặc chấp nhận propagate lên. Safe. |

### 3. Nhận xét bổ sung (không blocking)

**Nit:** Log warning "failed/timeout" ở nhánh `ExitCode != 0` không phân biệt "offline device" (StdErr rõ) vs "timeout" (StdErr chứa "timeout sau ...ms") — QA khi verify staging có thể quan sát cụ thể StdErr để phân biệt. Không cần sửa trong PR này.

**Optional (Future):** String constant `"Không tìm thấy adb tại"` bị lặp giữa `AdbService.cs:49` và `DevicePollWorker.cs:116`. Có thể trích thành `internal const string AdbBinaryMissingMarker` trong `AdbService` để tránh brittle nếu tương lai thay đổi. Không blocking — Senior đã ghi nhận trong step 2.3.

**FYI:** Fix này lập tức unblock case ≥2 WiFi device offline — chính xác vấn đề Dispatcher phát hiện và nâng severity lên P1. Không cần thêm test race edge case ở level unit (khó reproduce race deterministic trong test); QA verify trên staging sẽ cover.

### 4. Quyết định

**APPROVE — không request changes.** Fix đúng root cause (phân biệt cancel gốc vs internal timeout tại đúng lớp `AdbService.RunAsync`), thiết kế sạch (defensive path trong `WarmUpReconnectAsync`), log message không còn misleading, test coverage đầy đủ cho cả 2 path (timeout ExitCode + OCE defensive), không regression. Không cần escalate CTO (bug fix phạm vi hẹp, không đụng auth/payment/DB schema).

## Artifact

- Review comments: nhúng vào step file này (commit trực tiếp trên `docker-deploy`, không có PR GitHub riêng).
- Kết quả: APPROVE commit `cff893f` — sẵn sàng chuyển QA Engineer re-verify (Bước 3.1b).

## Quyết định quan trọng

1. **APPROVE mà không blocking-comment nào** — các concern (log Nit, string constant trùng) đều Optional/Nit, không phải Required/Critical.
2. **Không escalate CTO / EM** — fix trong scope WF-BUGFIX P1, không thay đổi kiến trúc, không đụng auth/payment/DB.
3. **Không tự sửa code thay Senior** — không có gì cần sửa. Two-Eyes Principle §8: reviewer approve, developer đã tự commit.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: Tech Lead đã verify độc lập build 0 error + 73/73 test PASS + đọc diff commit `cff893f` chi tiết. QA re-verify KHÔNG cần chạy lại `dotnet build`/`dotnet test` (đã verified 2 lần). Tập trung vào verify thật với môi trường có ≥2 WiFi device (kể cả giả lập ADB emulator offline nếu không có 2 device thật).
- watch_out: **Bước 3.1b cần chú ý 3 điều:**
  (a) Test case chính: restart service với ≥ 2 WiFi device trong `devices.db` mà **device đầu offline/không phản hồi** — kỳ vọng: sau ~5-10s (timeout device 1) log xuất hiện `Warm-up connect <serial-1> failed/timeout: ... — skipping, continuing with next device`, và device 2 (nếu online) phải được `Warm-up connect <serial-2>: connected to ...`. TRƯỚC fix này device 2 bị "im lặng" (không có log warm-up). Nếu vẫn thấy im lặng → BLOCK.
  (b) Log message: xác nhận KHÔNG còn thấy log "adb binary not found" cho case timeout (đây là QA finding P2/P3 gốc). Chỉ log này xuất hiện khi ADB binary thực sự missing.
  (c) Startup không crash khi có device offline + không hang > 5s per device (per-device timeout đúng 5s).
- next_inputs: Commit `cff893f` đã APPROVE trên nhánh `docker-deploy`. BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md` + AC gốc trong STEP-1.1 là source of truth. Nếu có ≥2 WiFi device thật để test → dùng; nếu chỉ giả lập được 1 → note ENV_LIMIT trong step 3.1b nhưng vẫn PHẢI verify được log message "failed/timeout ... continuing with next device" xuất hiện đúng bằng cách tạo device offline giả trong DB.

## Commit

- Hash: a1bc684
- Đã push: (sẽ push ngay sau)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
