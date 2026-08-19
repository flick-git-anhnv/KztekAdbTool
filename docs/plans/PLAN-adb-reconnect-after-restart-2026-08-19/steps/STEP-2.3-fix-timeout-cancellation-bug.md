---
step: "2.3"
plan: ../PLAN-MASTER.md
agent: Senior Developer
status: done
completed_at: 2026-08-19 12:03
deps: ["2.2"]
---

# STEP 2.3 — Fix per-device timeout bị nhầm thành service cancellation

## Input nhận
Từ Dispatcher (phát hiện qua verify code trực tiếp sau STEP-3.1 QA report):
- Commit 3a86825 (STEP-2.1) đã có `WarmUpReconnectAsync` với `catch (OperationCanceledException) { break; }`.
- QA báo P2/P3 finding: log message "adb binary not found" xuất hiện nhầm.
- Dispatcher xác nhận severity P1: `AdbService.RunAsync` dùng `CreateLinkedTokenSource(ct)` + `CancelAfter(5000)` → timeout nội bộ ném OCE giống hệt service shutdown → `WarmUpReconnectAsync` catch OCE → `break` → device WiFi còn lại không được thử reconnect.

## Nhiệm vụ
Sửa `WarmUpReconnectAsync` để phân biệt:
1. Per-device timeout (device không phản hồi trong 5s) → log warning + `continue` device kế tiếp.
2. Service shutdown thật (ct gốc bị cancel) → `break` dừng hẳn.
Sửa log message nhầm "adb binary not found" cho case timeout.

## Definition of Done
- [x] `AdbService.RunAsync` re-throw OCE khi `ct.IsCancellationRequested == true` (phân biệt cho caller).
- [x] `WarmUpReconnectAsync` phân biệt: `ExitCode != 0` (per-device timeout/error) → log + continue; OCE + ct cancelled → break; OCE + ct not cancelled → log + continue (defensive).
- [x] Log message "adb binary not found" chỉ xuất hiện khi StdErr thực sự chứa "Không tìm thấy adb tại".
- [x] Unit test mới: 3 WiFi devices, device 1 timeout (TimeoutOnSerial) → cả 3 vẫn được gọi ConnectAsync.
- [x] Unit test mới: 2 WiFi devices, device 1 ném OCE không cancel ct (OceOnSerial) → device 2 vẫn được gọi.
- [x] 73/73 tests PASS, không regression.

## Đã làm
**1. `AdbService.cs` RunAsync** — thêm `ct.ThrowIfCancellationRequested()` trong catch OCE block: nếu ct gốc bị cancel → re-throw (caller phân biệt được); nếu chỉ internal timeout → trả về result ExitCode=-1 như cũ.

**2. `DevicePollWorker.cs` WarmUpReconnectAsync** — sửa 3 điểm:
- Kiểm tra binary not found bằng `result.StdErr.Contains("Không tìm thấy adb tại", StringComparison.Ordinal)` thay vì `string.IsNullOrEmpty(result.StdOut)` (không đặc trưng).
- Thêm `if (result.ExitCode != 0) { log warning + continue; }` — per-device timeout/error → không dừng.
- Sửa `catch (OperationCanceledException)`: `if (ct.IsCancellationRequested) break; else continue;` — phân biệt shutdown vs defensive OCE path.

**3. `DevicePollWorkerWarmUpTests.cs`** — cập nhật FakeAdbService (thêm `TimeoutOnSerial`, `OceOnSerial`, `ct.ThrowIfCancellationRequested()`); thêm 2 test mới; 73/73 PASS.

**4. `code-graph/CODE-GRAPH.md`** — thêm entry lịch sử STEP-2.3.

## Artifact
- `src/KztekAdbPublishTool.Web/Services/AdbService.cs` — thay đổi RunAsync catch block
- `src/KztekAdbPublishTool.Web/Workers/DevicePollWorker.cs` — thay đổi WarmUpReconnectAsync
- `tests/KztekAdbPublishTool.Web.Tests/DevicePollWorkerWarmUpTests.cs` — 2 test mới + FakeAdbService update
- `code-graph/CODE-GRAPH.md` — history entry

## Quyết định quan trọng
- Sửa cả `AdbService.RunAsync` để re-throw OCE khi ct thật sự cancelled (không chỉ internal timeout) — điều này đúng về mặt thiết kế: caller nên biết khi service shutdown thật. Ảnh hưởng tất cả callers của RunAsync, nhưng `PollAsync` đã có `catch (OperationCanceledException)` bao quanh → không bị vỡ.
- Dùng `result.StdErr.Contains("Không tìm thấy adb tại")` thay vì check StdOut.IsEmpty để phân biệt binary not found với timeout — gắn với string constant trong AdbService, có thể brittle nếu message thay đổi, nhưng đây là internal code nên chấp nhận được.

## VERIFICATION REPORT

```
Verification: dotnet test tests/KztekAdbPublishTool.Web.Tests/KztekAdbPublishTool.Web.Tests.csproj --verbosity normal
Output:
  Passed WarmUpReconnect_FirstDeviceTimesOut_ContinuesToRemainingDevices [34 ms]
  Passed WarmUpReconnect_FirstDeviceOcesWithoutCtCancel_ContinuesToNextDevice [15 ms]
  Passed WarmUpReconnect_CancelledBeforeStart_ConnectNotCalled [13 ms]
  Passed WarmUpReconnect_SingleWifiDevice_CallsConnectOnce [63 ms]
  Passed WarmUpReconnect_MultipleWifiDevices_CallsConnectForEach [22 ms]
  Passed WarmUpReconnect_UsbDeviceOnly_ConnectNotCalled [20 ms]
  Passed WarmUpReconnect_MixedDevices_OnlyWifiConnected [21 ms]
  Passed WarmUpReconnect_OneDeviceConnectFails_ContinuesToNextDevice_DoesNotThrow [18 ms]
  Passed WarmUpReconnect_EmptyDatabase_ConnectNotCalled [8 ms]
  Total tests: 73  Passed: 73
Kết luận: Pass
```

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã sửa AdbService.RunAsync (re-throw OCE khi ct cancel) + WarmUpReconnectAsync (logic phân biệt timeout/shutdown) + thêm 2 unit tests. Không cần sửa thêm.
- watch_out: Nếu Tech Lead muốn tách message constant "Không tìm thấy adb tại" ra thành một field/const — có thể làm nhưng không bắt buộc. Fix hiện tại đủ tốt. Cũng lưu ý `PollAsync` có `catch (OperationCanceledException)` bao quanh toàn bộ → re-throw từ RunAsync sẽ được bắt lại ở đó — không gây lỗi regression.
- next_inputs: Commit hash (điền sau commit), branch `docker-deploy`. Tech Lead cần review: `AdbService.cs` lines ~76-96 (RunAsync catch block) + `DevicePollWorker.cs` lines ~107-160 (WarmUpReconnectAsync foreach body) + 2 test mới trong `DevicePollWorkerWarmUpTests.cs`.

## Commit
- Hash: cff893f
- Đã push: có

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
