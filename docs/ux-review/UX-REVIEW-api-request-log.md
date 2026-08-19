# UX/UI Review Report — 2026-08-19

**App / Module:** KztekAdbPublishTool — Feature: API Request Log (AddDevice & LaunchApp)
**Reviewer:** UX/UI Reviewer Agent
**Môi trường:** Local Docker (container `kztek-adb-tool`, port 3339) | Build: commit fb26cd1 (sau merge 79ae3cf + d8abe37)
**Tổng số màn hình review:** 1 (dashboard panel `#log`)
**Kết quả tổng quan:** Cần cải thiện — 1 issue High phát hiện (Parameters null), còn lại PASS

> **Ghi chú phương pháp evidence:** Môi trường Linux/WSL2 không có GUI browser. App chạy trong Docker container (headless). Evidence được thu thập bằng: (1) curl để gọi API và capture HTTP response, (2) query SQLite DB từ container để verify DB persistence, (3) docker logs để verify container hoạt động, (4) code review JS handler để verify no JS errors. Tất cả evidence lưu tại `screenshots/2026-08-19/*.txt`.

---

## Tóm tắt phát hiện

| Mức độ | Số lượng |
|--------|---------|
| Critical (chặn release) | 0 |
| High (ảnh hưởng UX đáng kể) | 1 |
| Medium (khó chịu nhưng dùng được) | 0 |
| Low (polish / nice-to-have) | 1 |

---

## Chi tiết từng màn hình

### Dashboard — Panel "Nhật ký hoạt động" (`#log`)

**Evidence:** `screenshots/2026-08-19/TC1-adddevice-success.txt`, `TC2-launchapp-app-not-installed.txt`, `TC3-TC4-unauthorized-401-logs.txt`, `TC5-invalid-input-400.txt`, `DB-ApiRequestLog-all-entries.txt`

| Tiêu chí | Kết quả | Ghi chú |
|---|---|---|
| C1 Layout hợp lý | PASS | Panel `#log` nằm ở cuối dashboard với section title "Nhật ký hoạt động" + icon, rõ ràng và đúng luồng. `appendLog()` append text đúng thứ tự thời gian, auto-scroll. |
| C2 Không chồng chéo | PASS | `<pre id="log">` có `height: 150px; overflow-y: auto` — scroll nội bộ, không tràn khung. Không có overlap với component khác. |
| C3 Hiển thị đầy đủ | FAIL (High) | Parameters field luôn null trong mọi entry DB. Log hiển thị `(no body)` thay vì params thực tế. Xem UI-001. |
| C4 Typography nhất quán | PASS | Font monospace (Consolas/Courier New), 0.78rem — nhất quán với các log entry khác trong panel. Format `[HH:MM:SS] [API] ...` đồng nhất. |
| C5 Màu sắc & Brand | PASS | Section title dùng `kz-navy` đúng brand. Log panel dark terminal style (#1a1a2e / #a0d8ef) — intentional UI choice nhất quán với design hiện tại, không vi phạm brand. |
| C6 Trạng thái đặc biệt | PASS | (1) 401 requests ĐƯỢC log (OUTER filter design đúng — entry Id=3,4 xác nhận); (2) `formatApiRequestLog(null)` trả `[API] (empty log entry)` an toàn; (3) `summarizeParams(apiName, null)` trả `(no body)` an toàn — KHÔNG có JS error. |
| C7 Khoảng cách & Alignment | PASS | padding 10px 12px, border-radius 4px, margin 0 — đều và nhất quán. `white-space: pre-wrap; word-break: break-word` phòng tràn text dài. |

**Phát hiện:**

- [UI-001] High — `Parameters` field null trong mọi entry ApiRequestLog. Log hiển thị `(no body)` thay vì params thực tế (vi phạm AC US-006 BR3 và TDD format spec). Evidence: `DB-ApiRequestLog-all-entries.txt` (5 entries, mọi entry có `params=None`). Root cause nghi ngờ: `TryReadBodyJsonAsync` trong `ApiRequestLoggingEndpointFilter.cs` trả null do body reading không thành công trong pipeline Minimal API — lỗi được swallow silently bởi outer try-catch. Cần debug thêm (`EnableBuffering()` timing vs model binding pipeline).

- [UI-002] Low — Container image cũ (pre-feature) đang chạy khi UXR bắt đầu. Phải rebuild `docker compose build` và restart container trước khi test. STEP 3.4 cần đảm bảo build + deploy đúng image mới. Ghi chú: `docker-compose.yml` có uncommitted changes (`M` trong git status) — confirm trước khi staging deploy.

---

## Test case results

| TC | Mô tả | HTTP Status | DB logged? | params null? | Kết quả |
|----|-------|-------------|------------|--------------|---------|
| TC-1 | AddDevice API — valid key, success | 200 OK | YES (Id=1) | YES (bug) | PARTIAL PASS |
| TC-2 | LaunchApp API — valid key, app not installed | 422 | YES (Id=2) | YES (bug) | PARTIAL PASS |
| TC-3 | AddDevice API — wrong key (401 → logged per design) | 401 | YES (Id=3) | YES (bug) | PASS (log exists) |
| TC-4 | LaunchApp API — no API key (401) | 401 | YES (Id=4) | YES (bug) | PASS (log exists) |
| TC-5 | AddDevice API — invalid IP → 400 | 400 | YES (Id=5) | YES (bug) | PARTIAL PASS |
| TC-Multi | Multi-tab SignalR broadcast | N/A | N/A | N/A | VERIFIED (code) — `Clients.All.SendAsync` broadcasts to all; không verify live do headless |
| TC-JS | Browser console JS error | N/A | N/A | N/A | NO ERROR expected — `formatApiRequestLog` handles null gracefully |

---

## Danh sách issue cần fix

| ID | Màn hình | Mô tả | Mức độ | Tiêu chí | Đề xuất fix |
|---|---|---|---|---|---|
| UI-001 | Dashboard — `#log` | `Parameters` luôn null trong ApiRequestLog DB và SignalR payload. Log hiển thị `(no body)` thay vì `ip=192.168.21.11:5555` (AddDevice) hoặc `serial=..., app=...` (LaunchApp). Vi phạm AC US-006 BR3. Root cause: `TryReadBodyJsonAsync` trong `ApiRequestLoggingEndpointFilter.cs` trả null. Hypothesis: `EnableBuffering()` chạy sau khi body đã bị consume, hoặc exception silently swallowed. | High | C3 | (1) Thêm logging debug tạm thời trong `TryReadBodyJsonAsync` để xác định chính xác điểm fail; (2) Kiểm tra xem body có được read trước khi filter chạy không (Minimal API pipeline); (3) Alternative: thay `StreamReader` bằng `HttpContext.Request.BodyReader` (PipeReader pattern) — ít bị conflict hơn với buffering trong Minimal API. Giao Senior Developer fix. |
| UI-002 | Docker / CI | Container image cũ chạy khi UXR bắt đầu. Cần `docker compose build` trước `docker compose up`. | Low | N/A | STEP 3.4 DevOps Engineer cần: (1) Confirm docker-compose.yml uncommitted changes (`M`) trước khi deploy; (2) Đảm bảo build step là một phần của deploy pipeline. |

---

## Ghi chú đặc biệt cho QA Engineer (STEP 3.2)

1. **Parameters null là BUG xác nhận** — test case TC-1 đến TC-5 đều cho params=null. QA cần verify fix này sau khi Senior Dev sửa UI-001.

2. **401 ĐƯỢC LOG là đúng spec** — Entry Id=3 (AddDevice 401) và Id=4 (LaunchApp 401) là kết quả EXPECTED, không phải lỗi. `ApiRequestLoggingEndpointFilter` đăng ký OUTER (trước `ApiKeyEndpointFilter`), thiết kế có chủ ý để bắt mọi request kể cả unauthorized.

3. **Multi-tab SignalR:** Không verify live được do headless environment. QA cần mở 2 tab browser thật và gọi API để verify cả 2 tab đều nhận event.

4. **Container phải là image MỚI (post-feature):** Verify bằng cách kiểm tra `ApiRequestLog` table tồn tại trong DB trước khi bắt đầu test. Nếu table không tồn tại → container đang chạy image cũ.

---

## Kết luận & Đề xuất

Review phát hiện 1 bug High (UI-001): `Parameters` field luôn null do body reading silently fail trong `ApiRequestLoggingEndpointFilter`. DB persistence, SignalR broadcast architecture, 401 logging, và JS error handling đều hoạt động đúng. Bug này ảnh hưởng trực tiếp đến UX: log entries thiếu thông tin parameters quan trọng cho operator đọc nhật ký.

**Khuyến nghị:** Fix UI-001 (Parameters null) trước khi QA Lead sign-off. Bug không chặn testing cơ bản nhưng vi phạm AC của feature. Sau khi fix cần chạy lại UXR verification ngắn (chỉ verify TC-1 và TC-2 hiển thị đúng params trong log).
