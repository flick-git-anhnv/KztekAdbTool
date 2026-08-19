---
id: BUG-adb-reconnect-after-restart
feature: adb-add-device-api
severity: P1
status: open
reporter: user
triaged_by: Senior Developer
created: 2026-08-19
---

# BUG — ADB không tự reconnect WiFi devices sau khi service restart

## Tóm tắt

Sau khi restart service, `GET /api/devices/{serial}/status` luôn trả `404 DeviceNotFound` cho mọi thiết bị, và flow phụ thuộc DeviceState bị ảnh hưởng — cho đến khi user bấm "quét lại" (scan) trên giao diện web. `GET /api/devices` không bị ảnh hưởng vì đọc thẳng từ SQLite.

**Bug này block smoke test nhóm B trước go-live production của feature `adb-add-device-api`.**

---

## Steps to Reproduce

1. Đảm bảo service đang chạy, có ít nhất 1 thiết bị WiFi đã kết nối và hiển thị trong danh sách (SQLite đã có record).
2. Dừng service (docker compose stop / systemctl stop / tắt process).
3. Khởi động lại service (docker compose start / systemctl start).
4. **Không** bấm "quét lại" trên UI.
5. Gọi `GET /api/devices/{serial}/status` với serial của thiết bị đã biết (lấy từ `GET /api/devices`).
6. **Kết quả thực tế:** `404 Not Found` — `{ "success": false, "error": "DeviceNotFound", "message": "Device 'xxx' not found." }`
7. **Kết quả mong đợi:** `200 OK` — `{ "success": true, "serial": "...", "status": "Online" }`

**Verify `GET /api/devices` không bị ảnh hưởng:**
- Gọi `GET /api/devices` ngay sau restart (bước 3–4): trả về danh sách thiết bị đã lưu, **bình thường**.

**Reproduce bằng lệnh (thay thế serial và host thực tế):**
```bash
# Bước 3-4: restart xong, chưa scan
curl -H "x-api-key: <key>" http://localhost:5000/api/devices/192.168.1.100:5555/status
# → 404 DeviceNotFound

curl http://localhost:5000/api/devices
# → 200 OK — trả về danh sách từ SQLite
```

---

## Root Cause (CONFIRMED — trace code)

### Chuỗi nhân quả

```
Service restart
  └─ DevicePollWorker.ExecuteAsync() bắt đầu   [Workers/DevicePollWorker.cs:56]
       └─ manualTrigger = true                  [DevicePollWorker.cs:61]
            └─ PollAsync() chạy ngay lập tức    [DevicePollWorker.cs:72-73]
                 └─ _adb.GetDevicesAsync()       [DevicePollWorker.cs:111]
                      │   = chạy `adb devices -l`
                      │   = chỉ liệt kê thiết bị ĐÃ CÓ kết nối TCP với ADB daemon
                      │   = sau restart, ADB daemon mới → KHÔNG có kết nối TCP nào
                      └─ Trả về danh sách RỖNG
                           └─ DeviceState KHÔNG được populate
                                └─ GET /api/devices/{serial}/status → 404  [DeviceConnectionEndpoints.cs:95]
```

### File và dòng cụ thể

| File | Dòng | Vấn đề |
|------|------|--------|
| `src/KztekAdbPublishTool.Web/Workers/DevicePollWorker.cs` | 56–77 (`ExecuteAsync`) | Không có bước "reconnect persisted WiFi devices" trước khi poll lần đầu |
| `src/KztekAdbPublishTool.Web/Workers/DevicePollWorker.cs` | 107–111 (`PollAsync` đầu) | Chỉ gọi `_adb.GetDevicesAsync()` — là `adb devices -l` — KHÔNG gọi `_adb.ConnectAsync()` |
| `src/KztekAdbPublishTool.Web/Program.cs` | 44 | Chỉ đăng ký `DevicePollWorker` làm hosted service; không có startup task reconnect |
| `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` | 95 | `deviceState.TryGet(s, ...)` — DeviceState trống → 404 |

### Tại sao `GET /api/devices` không bị ảnh hưởng

`DeviceEndpoints.cs` handler cho `GET /api/devices` đọc từ **`DeviceRepository` (SQLite)**, không phụ thuộc `DeviceState`. SQLite persist qua restart nên dữ liệu vẫn còn đầy đủ.

### Tại sao WiFi devices bị mất kết nối sau daemon restart

ADB WiFi là TCP socket. Mỗi lần `adb connect <ip:port>` tạo một kết nối TCP mới từ ADB daemon đến thiết bị. Khi daemon restart (theo service restart), toàn bộ kết nối TCP cũ bị huỷ. Lệnh `adb devices -l` chỉ liệt kê các thiết bị đang có kết nối TCP active — sau restart, danh sách này rỗng cho WiFi devices vì chưa ai gọi `adb connect` lại.

### Tại sao bấm "quét lại" giải quyết được

"Quét lại" (scan) trên UI gọi `POST /api/scan/start` → `ScanCoordinator` TCP probe → tìm thấy devices → gọi `POST /api/devices/connect` hoặc `POST /api/devices/connect-batch` → `AdbService.ConnectAsync()` (`adb connect <ip:port>`) → `PollControlService.TriggerAsync()` → `DevicePollWorker` chạy `PollAsync` → `adb devices -l` lần này thấy device đã kết nối → `DeviceState` được populate → `GET /api/devices/{serial}/status` hoạt động.

---

## Impact

| Chiều | Mức độ |
|-------|--------|
| Severity | **P1** — block production go-live của feature `adb-add-device-api` |
| Người dùng ảnh hưởng | Toàn bộ caller gọi `GET /api/devices/{serial}/status` ngay sau restart mà không scan trước |
| Dữ liệu | Không mất dữ liệu (SQLite nguyên vẹn) |
| Workaround | Bấm "quét lại" trên UI sau mỗi lần restart — tạm chấp nhận được trong staging, **không chấp nhận trong production** |

---

## Hướng fix đề xuất

**Cách tiếp cận: Thêm bước "Warm-up reconnect" trong `DevicePollWorker.ExecuteAsync()` trước vòng lặp chính.**

```csharp
// Trong ExecuteAsync(), TRƯỚC while loop:
await WarmUpReconnectAsync(stoppingToken);

// Thêm method:
private async Task WarmUpReconnectAsync(CancellationToken ct)
{
    var persistedDevices = _repo.GetAll();
    var wifiDevices = persistedDevices
        .Where(d => d.Serial.Contains(':', StringComparison.Ordinal))  // WiFi = "ip:port"
        .ToList();

    if (wifiDevices.Count == 0) return;

    _logger.LogInformation(
        "DevicePollWorker warm-up: reconnecting {Count} persisted WiFi devices...",
        wifiDevices.Count);

    foreach (var device in wifiDevices)
    {
        if (ct.IsCancellationRequested) break;
        var result = await _adb.ConnectAsync(device.Serial, ct: ct);
        _logger.LogInformation(
            "Warm-up connect {Serial}: exitCode={ExitCode} stdOut={StdOut}",
            device.Serial, result.ExitCode, result.StdOut.Trim());
    }
}
```

**Điểm đặt:** Sau `_logger.LogInformation("DevicePollWorker started...")` (dòng 58), trước `var manualTrigger = true` (dòng 61) — hoặc sau khi set `manualTrigger = true` và trước `while`. Reconnect xong thì vòng poll đầu tiên (`manualTrigger=true`) sẽ thấy devices và populate `DeviceState`.

**Xử lý lỗi trong warm-up:**
- Nếu `adb connect` thất bại (device offline, network unreachable) → log warning, tiếp tục device tiếp theo — **KHÔNG throw exception**.
- Nếu ADB binary missing → `ConnectAsync` trả `exitCode=-1` → log error, bỏ qua warm-up.
- Warm-up fail không block vòng poll bình thường — chỉ là best-effort.

**Lý do chọn approach này thay vì alternatives:**
- Thêm logic trong `PollAsync`: `PollAsync` được gọi định kỳ (3s) — thêm reconnect vào đây sẽ retry `adb connect` mỗi 3s cho device offline, tốn tài nguyên và gây noise log.
- Pre-populate `DeviceState` từ SQLite (không gọi ADB): device trong SQLite chưa chắc đang online; status "Offline" từ SQLite cũng hữu ích nhưng thiếu confirmation từ ADB daemon — dễ gây sai trạng thái.
- Warm-up một lần khi start: đơn giản, rõ ràng, không side-effect cho vòng poll thường.

**Cần viết test:**
- Unit test `DevicePollWorker`: mock `DeviceRepository.GetAll()` trả về 1 WiFi device → verify `AdbService.ConnectAsync()` được gọi đúng 1 lần với serial đúng trong warm-up.
- Integration test (nếu có test infra): restart service mock → gọi `GET /api/devices/{serial}/status` → expect 200 (không cần scan).

---

## Liên kết

- Feature gốc: `docs/plans/PLAN-adb-add-device-api-2026-08-19/`
- Fix plan: `docs/plans/PLAN-adb-reconnect-after-restart-2026-08-19/`
- Fix sẽ thực hiện tại: `steps/STEP-2.1-fix-auto-reconnect.md`
- TDD gốc của feature: `docs/tech-design/TDD-adb-add-device-api.md` (nếu có)

---

## Sign-off — QA Lead

**Ngày:** 2026-08-19 12:17
**QA Lead:** trongtv@kztek.vn
**Quyết định:** PASS CÓ ĐIỀU KIỆN

### Tóm tắt đánh giá

Hai bug P1 đã được fix và verify:
- **Bug 1** (warm-up không được gọi sau restart) — fix commit `3a86825`, verified qua log app thật (TC-5) + 9/9 warm-up unit test PASS.
- **Bug 2** (per-device timeout chặn device sau) — fix commit `cff893f`, verified qua log TC-5: cả 2 WiFi device offline đều được thử warm-up độc lập, không bị block lẫn nhau.

Không có P0/P1 bug nào còn mở. 73/73 unit test PASS. Regression TC-3/TC-4 PASS.

### Điều kiện trước khi production deploy được coi là an toàn hoàn toàn

> **ĐIỀU KIỆN BẮT BUỘC:** DevOps Engineer thực hiện smoke test trên staging với ≥1 Android WiFi device thật:
> - **TC-1:** Restart service → KHÔNG scan UI → `GET /api/devices/{serial}/status` → kỳ vọng 200 OK.
> - **TC-2:** Restart service → KHÔNG scan UI → `POST /api/devices/connect-by-ip` → kỳ vọng 200 OK.
> - Nếu không có thiết bị trong cửa sổ deploy → DevOps Lead chấp nhận rủi ro còn lại bằng văn bản tại STEP-3.3.

### Phê duyệt

- [x] P0 = 0
- [x] P1 = 0 (trong phạm vi môi trường verify được)
- [x] Regression sạch (TC-3/TC-4/73 unit tests)
- [ ] TC-1/TC-2 E2E với thiết bị thật — ENV_LIMIT, giao DevOps Engineer verify trên staging

**Approved for staging deploy. Điều kiện TC-1/TC-2 phải hoàn thành hoặc được DevOps Lead chấp nhận rủi ro trước khi production final.**
