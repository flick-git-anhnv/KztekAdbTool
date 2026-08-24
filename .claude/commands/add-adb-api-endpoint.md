---
description: "PHẢI dùng khi cần THÊM MỚI 1 external API endpoint (REST, có x-api-key) vào project KztekAdbPublishTool.Web — kèm hoặc không kèm nút UI trên dashboard — VD: 'thêm API check pin + nút trên view', 'thêm endpoint POST clear-app-data'. Skill trả về checklist 7 lớp file + quyết định phải chốt + gotchas đã trả giá, dùng ở bước TDD (Tech Lead) và bước code (Senior Dev) thay cho việc khảo sát lại pattern. KHÔNG dùng khi: sửa/fix endpoint CÓ SẴN (→ WF-BUGFIX), thay đổi UI thuần không có endpoint mới (→ WF-FASTTRACK), thêm SignalR event/middleware cross-cutting (→ WF-REFACTOR/ARCH), hoặc project KHÁC KztekAdbPublishTool.Web."
---

# Skill: add-adb-api-endpoint — Thêm external API + nút UI vào KztekAdbPublishTool.Web

> Đúc kết từ 4 feature cùng khuôn: `launch-app` (2026-08-18), `add-device` (2026-08-19), `uninstall-before-install` (2026-08-20), `app-status + reboot` (2026-08-24). File mẫu chuẩn nhất (mới nhất, đã qua UXR fix): `Endpoints/AppStatusEndpoints.cs` + `RebootEndpoints.cs`.

## Phạm vi áp dụng

- CHỈ cho `src/KztekAdbPublishTool.Web` (ASP.NET Core 8 Minimal API). Project WinForms gốc `src/KztekAdbPublishTool` bất khả xâm phạm.
- "External API" = endpoint được client ngoài gọi, bảo vệ bằng header `x-api-key`. Endpoint nội bộ dashboard (không key) → vẫn dùng được checklist nhưng bỏ lớp auth filter, theo mẫu `DeviceEndpoints.cs`.
- Skill KHÔNG thay thế workflow — nó là input cho bước TDD (Tech Lead) và bước code (Senior Dev) trong plan WF-FEATURE. Plan file vẫn bắt buộc theo §16 CLAUDE.md.

## Checklist 7 lớp (mỗi lớp = file phải tạo/sửa)

| # | Lớp | File | Ghi chú bắt buộc |
|---|-----|------|------------------|
| 1 | TDD doc | `docs/tech-design/TDD-<slug>.md` | Contract + error matrix theo khuôn §Lớp-3; ADB command + cách parse; UI spec nếu có nút |
| 2 | Service | `Services/AdbService.cs` | Method mới `XxxAsync(string serial, ..., CancellationToken ct)` dùng `RunAsync` (timeout gợi ý: 10000ms cho lệnh shell thường — theo tiền lệ LaunchApp/AppStatus; TDD chốt giá trị cụ thể). KHÔNG thêm vào `IAdbService` trừ khi `DevicePollWorker`/worker cần mock (tiền lệ: `LaunchAppAsync`, `UninstallApkAsync`, `GetAppStatusAsync` đều KHÔNG vào interface). Logic parse output → tách `public static` để unit test không cần mock |
| 3 | Endpoint | `Endpoints/<Ten>Endpoints.cs` (MỚI — 1 file/nhóm endpoint) + đăng ký `app.Map<Ten>Endpoints()` trong `Program.cs` | Filter chain ĐÚNG THỨ TỰ, gắn TRỰC TIẾP trên từng route trong file endpoint (không gắn group — theo mẫu AppStatusEndpoints): `.AddEndpointFilter<ApiRequestLoggingEndpointFilter>()` (OUTER) rồi `.AddEndpointFilter<ApiKeyEndpointFilter>()` (INNER). Error matrix chuẩn: 400 input thiếu/sai → `InvalidInput`; 401 key sai/thiếu (filter tự trả); 404 serial không có trong `DeviceState` → `DeviceNotFound`; 422 device offline (xem `CheckDeviceState` mẫu); 500 lỗi ADB |
| 4 | Logging | `Services/ApiRequestLogConstants.cs` + `Endpoints/ApiRequestLoggingEndpointFilter.cs` | Thêm hằng `ApiXxx = "Xxx"` — tên PascalCase theo nghiệp vụ, khớp tiền lệ (`AppStatus`, `RebootDevice`, `LaunchApp`; VD battery → `ApiBatteryStatus = "BatteryStatus"`); `ResolveApiName` phải match bằng `EndsWith("/route-cuối")` — TUYỆT ĐỐI KHÔNG `.Equals()` vì route chứa `{serial}` |
| 5 | UI (nếu có nút) | `Pages/Index.cshtml` + `wwwroot/js/dashboard.js` (+ `dashboard.css` nếu cần class mới) | Xem mục "Chuẩn UI button" dưới |
| 6 | Tests | `tests/KztekAdbPublishTool.Web.Tests/<Ten>EndpointsTests.cs` (+ test parse trong AdbService tests) | Cover: happy path (map result), 400/404, parse edge cases. Chạy `dotnet test` — TOÀN BỘ bộ cũ + mới phải xanh |
| 7 | CODE-GRAPH | `code-graph/CODE-GRAPH.md` (+ export theo §17.4) | Thêm module mới vào bảng Dependencies + bảng API Endpoints + "Thay đổi gần đây", Confidence CONFIRMED + Last verified |

## Quyết định phải chốt ở TDD (không được để mơ hồ sang bước code)

1. Route + method + query/body params; response schema JSON chính xác từng field (camelCase).
2. ADB command chính xác (`adb -s {serial} ...`) + timeout + cách parse stdout (fallback cho Android version cũ nếu parse dumpsys).
3. Method có vào `IAdbService` không (mặc định: KHÔNG).
4. Có nút UI không; nếu có → lấy input từ đâu (ưu tiên tái dùng ô có sẵn như `#txt-package`, KHÔNG thêm modal khi chưa cần).
5. API có destructive không (xem mục dưới).

## Chuẩn UI button (đúc từ UXR NEEDS-FIX UI-001/002/003 — làm ngay từ đầu, đừng để UXR bắt lại)

- Class màu brand: `btn-kz-outline-navy` (hành động đọc/kiểm tra — Navy #251C53) hoặc `btn-kz-outline-orange` (hành động tác động thiết bị — Cam #F05922) trong `dashboard.css`. KHÔNG dùng `btn-outline-primary`/`btn-outline-warning` Bootstrap mặc định.
- Icon `<i class="bi bi-..." aria-hidden="true">`.
- Handler JS: `button.disabled = true` + spinner `spinner-border-sm` TRƯỚC `fetch()`, restore trong `finally` (chống double-click).
- Serial trên URL: LUÔN `encodeURIComponent(serial)` (serial WiFi dạng `192.168.1.100:5555` chứa `.`/`:`).
- API key phía JS: dùng `window.KZ_API_KEY` (embed từ Razor `IndexModel.PublicApiKey`) — KHÔNG hardcode, KHÔNG `console.log` key.
- Kết quả: `showToast(msg, type)` + ghi log area; nút thao tác 1-thiết-bị đặt dưới `<hr>` section "Thao tác thiết bị (1 thiết bị đã chọn)".

## API destructive (reboot, wipe, clear-data, force-stop...)

**Tiêu chí xác định destructive (đủ 1 trong 3):** lệnh làm device/app NGỪNG PHỤC VỤ tạm thời hoặc vĩnh viễn; MẤT DỮ LIỆU; hoặc KHÔNG HOÀN TÁC được từ phía server. Lệnh ghép nhiều adb command (VD force-stop rồi start lại app): nếu bước cuối đưa hệ về trạng thái phục vụ và endpoint chờ được kết quả → response trả kết quả thật, không cần wording "initiated"; nếu endpoint trả về trước khi hệ ổn định → vẫn dùng "initiated".

- UI: confirm dialog (native `confirm()`) hiển thị serial + ghi rõ "không thể hoàn tác" TRƯỚC khi fetch. Cancel → không gửi request.
- Response wording: `"XxxInitiated"` — KHÔNG "completed" (device sẽ offline sau lệnh).
- Review: bước Tech Lead review PHẢI chạy `security-audit-stride` (STRIDE cho destructive: Tampering serial, Spoofing key, DoS spam lệnh — cân nhắc ghi khuyến nghị rate-limit).
- QA: TC destructive CHỈ chạy trên thiết bị staging, KHÔNG trên thiết bị production đang phục vụ user; không có thiết bị → đánh Blocked + gate production chờ verify thật.

## Gotchas bắt buộc nhớ (đã trả giá)

| Gotcha | Nội dung |
|--------|----------|
| G008 | Filter KHÔNG đọc raw `Request.Body` (model binding đã đọc hết trước filter) — dùng `context.Arguments` |
| Route match | `ResolveApiName` dùng `EndsWith`, không `Equals` (route có `{serial}`) |
| Test giả | Unit test đọc body bằng `MemoryStream` tự tạo sẽ pass giả — hành vi body phải test qua HTTP thật |
| adb treo | Lệnh `adb` có thể treo khi khởi động daemon — mọi lệnh adb trong test/script bọc `timeout` |
| Serial 404 | Check tồn tại qua `DeviceState` (in-memory), không phải DB |

## Verification (done gate)

- [ ] Đủ 7 lớp trong bảng (lớp 5 bỏ qua nếu API không có UI — ghi rõ lý do trong TDD mục UI spec + step file của bước code)
- [ ] `dotnet build -c Release` 0 error + `dotnet test` toàn bộ xanh
- [ ] Curl thật 400/401/404 đúng contract (không cần thiết bị)
- [ ] Nếu destructive: confirm dialog + security-audit-stride + wording "initiated" đủ cả 3
- [ ] CODE-GRAPH cập nhật cùng session

## Red Flags (lý do hay bỏ qua — dừng lại khi thấy)

| Thought | Reality |
|---------|---------|
| "API này nhỏ, bỏ qua lớp 4 (logging) cho nhanh" | Cả 4 feature trước đều gắn logging filter — API không có audit log là lỗ hổng vận hành, reviewer sẽ REQUEST-CHANGES |
| "Dùng btn-outline-primary Bootstrap cho nhanh, màu tính sau" | UXR đã chấm NEEDS-FIX đúng lỗi này (UI-001, 2026-08-24) — sửa sau tốn thêm 1 vòng fix + re-check |
| "Thêm method vào IAdbService cho 'sạch'" | Tiền lệ 3 method đều KHÔNG vào interface; thêm vào là phình mock của mọi worker test — chỉ thêm khi worker thật sự gọi |
| "Endpoint GET đơn giản, khỏi viết TDD" | TDD là nơi chốt error matrix + adb command; bỏ qua → bước code tự quyết → review trả về |
| "Sửa endpoint cũ cũng na ná, dùng luôn skill này" | Skill này CHỈ cho endpoint THÊM MỚI — sửa endpoint có sẵn đi WF-BUGFIX/REFACTOR, checklist này sẽ dẫn sai (tạo file mới không cần thiết) |

## Keywords

thêm API mới, external API, x-api-key, endpoint mới, nút bấm dashboard, adb command, KztekAdbPublishTool.Web, filter chain, ApiKeyEndpointFilter, ApiRequestLoggingEndpointFilter, destructive API, reboot, confirm dialog
