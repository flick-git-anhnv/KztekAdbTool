# PRD: API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB

## Tổng quan

- **Vấn đề:** `KztekAdbPublishTool.Web` hiện chưa cung cấp endpoint HTTP cho phép client bên ngoài (1) kết nối một thiết bị Android qua địa chỉ IP:port và (2) truy vấn trạng thái kết nối ADB của một thiết bị cụ thể theo serial. Hai tác vụ này hiện phải thực hiện thủ công qua UI nội bộ hoặc lệnh ADB trực tiếp, gây khó khăn cho hệ thống tự động hóa cần orchestrate nhiều thiết bị.
- **Đối tượng:** Hệ thống tự động hóa (CI/CD pipeline, script triển khai, orchestrator nội bộ) hoặc operator kỹ thuật cần thêm thiết bị vào pool và kiểm tra trạng thái kết nối từ xa mà không cần truy cập UI.
- **Giá trị:** Cho phép tự động hóa end-to-end quy trình: thêm thiết bị → kết nối → kiểm tra online → thực hiện publish/launch APK. Tái sử dụng `AdbService.ConnectAsync` và `DeviceState` đã được kiểm chứng, không phát sinh code ADB mới.

---

## Goals

1. Cung cấp 1 endpoint HTTP để client kết nối thiết bị Android qua IP:port — thực hiện đồng thời "thêm vào danh sách theo dõi" và "kết nối ADB" trong 1 lần gọi.
2. Cung cấp 1 endpoint HTTP để client truy vấn trạng thái kết nối (`Online` / `Offline` / `NotFound`) của 1 thiết bị cụ thể theo serial — đọc từ cache `DeviceState`, không gọi ADB trực tiếp.
3. Bảo vệ cả 2 endpoint bằng API key tĩnh qua header `x-api-key` — tái sử dụng đúng `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` đã có, không tạo cơ chế auth mới.
4. Tách biệt 2 endpoint public mới (`/api/devices/connect-by-ip`, `/api/devices/{serial}/status`) khỏi endpoint nội bộ hiện có (`/api/devices/connect`) — không conflict, không ảnh hưởng route cũ.

## Non-goals (phiên bản này)

- Không có thay đổi UI/giao diện — API backend thuần túy.
- **Không** áp dụng filter auth cho các route cũ (`/api/install`, `/api/launch-app` (đã tự có auth riêng), `/api/devices/connect` nội bộ) — giữ nguyên trạng thái hiện tại.
- Không có multi-key / key rotation — chỉ 1 key tĩnh đọc từ config.
- Không có endpoint quản lý danh sách nhiều thiết bị cùng lúc — mỗi API xử lý 1 thiết bị/lần.
- Không gọi ADB trực tiếp trong endpoint kiểm tra trạng thái — chỉ đọc `DeviceState` cache từ `DevicePollWorker`.
- Không có audit log / request log chi tiết trong phiên bản này.
- Không có rate limiting hay throttle trong phiên bản này.
- **Không** tạo section config mới hay API key mới — tái dùng nguyên trạng `LaunchApp:ApiKey`.

---

## Scope

**Trong scope:**

| Hạng mục | Mô tả |
|---|---|
| Endpoint 1 | `POST /api/devices/connect-by-ip` |
| Input endpoint 1 | `ip` (bắt buộc, string), `port` (tùy chọn, integer, mặc định `5555`) |
| Cơ chế endpoint 1 | Gọi `AdbService.ConnectAsync(ip, port, ct)` — kết nối + đưa vào danh sách theo dõi |
| Output endpoint 1 | JSON trạng thái: thành công (serial thiết bị nếu có) hoặc thất bại kèm thông tin lỗi từ ADB |
| Endpoint 2 | `GET /api/devices/{serial}/status` |
| Input endpoint 2 | `serial` (route param, string) |
| Cơ chế endpoint 2 | Đọc `DeviceState` từ cache (đồng bộ bởi `DevicePollWorker`) — không gọi lệnh ADB trực tiếp |
| Output endpoint 2 | JSON với trường `status`: `"Online"` / `"Offline"` / `"NotFound"` |
| Auth (cả 2 endpoint) | Header `x-api-key` khớp `LaunchApp:ApiKey` trong config; trả 401 nếu thiếu/sai key |
| Config | Tái dùng key đã có ở `appsettings.json`, override bằng biến môi trường `LaunchApp__ApiKey` |

**Ngoài scope:** Xem Non-goals.

---

## User Story (sơ lược)

> "Là hệ thống CI/CD, sau khi chuẩn bị môi trường test, tôi muốn (1) gọi 1 HTTP endpoint để kết nối thiết bị Android theo IP mà không cần thao tác thủ công trên UI, và (2) gọi 1 HTTP endpoint khác để xác nhận thiết bị đó đang online trước khi tiến hành install/launch APK."

Chi tiết Given/When/Then và Acceptance Criteria kỹ thuật do **Business Analyst** viết ở bước tiếp theo (STEP-1.2).

---

## Acceptance Criteria (mức cao)

### API 1 — `POST /api/devices/connect-by-ip`

- [ ] **AC1** — Gọi endpoint với `x-api-key` đúng, `ip` hợp lệ (thiết bị đang lắng nghe ADB) → trả HTTP 200, thiết bị được kết nối và xuất hiện trong danh sách thiết bị theo dõi.
- [ ] **AC2** — Gọi endpoint với `ip` hợp lệ nhưng không truyền `port` → server tự dùng `port = 5555`, hành vi giống truyền `port: 5555` tường minh.
- [ ] **AC3** — Gọi endpoint với `x-api-key` sai hoặc thiếu → trả HTTP 401, không thực thi lệnh ADB.
- [ ] **AC4** — Gọi endpoint với `ip` không thể kết nối (timeout/refused) → trả lỗi ADB tường minh, HTTP 422 hoặc 200 kèm trạng thái thất bại.
- [ ] **AC5** — Gọi endpoint thiếu trường `ip` bắt buộc → trả HTTP 400 với thông báo validation rõ ràng.

### API 2 — `GET /api/devices/{serial}/status`

- [ ] **AC6** — Gọi endpoint với `x-api-key` đúng, `serial` tồn tại trong danh sách và đang online → trả HTTP 200, `status: "Online"`.
- [ ] **AC7** — Gọi endpoint với `serial` tồn tại nhưng đang offline → trả HTTP 200, `status: "Offline"`.
- [ ] **AC8** — Gọi endpoint với `serial` không tồn tại trong danh sách theo dõi → trả HTTP 404 hoặc HTTP 200 với `status: "NotFound"` (Tech Lead quyết định ở TDD).
- [ ] **AC9** — Gọi endpoint với `x-api-key` sai hoặc thiếu → trả HTTP 401.

### Chung

- [ ] **AC10** — Các route cũ (`/api/install`, `/api/launch-app`, `/api/devices/connect` nội bộ) hoạt động bình thường, không bị ảnh hưởng bởi 2 endpoint mới.
- [ ] **AC11** — API key có thể override bằng biến môi trường `LaunchApp__ApiKey` mà không cần rebuild image.

---

## Metric đo lường thành công

| Metric | Mục tiêu | Cách đo |
|---|---|---|
| Tỉ lệ kết nối thành công (khi thiết bị thực sự online, ADB bật) | ≥ 90% | Log `ExitCode == 0` từ `AdbCommandResult` tại endpoint 1 |
| Latency P95 endpoint connect-by-ip | ≤ 10 giây | Đo từ request nhận đến response (bao gồm thời gian ADB connect) |
| Latency P95 endpoint status | ≤ 200 ms | Đọc cache `DeviceState` — không gọi ADB, phải nhanh |
| Tỉ lệ 401 đúng khi key sai | 100% | Test case auth cả 2 endpoint |
| Không có regression trên route cũ | 0 lỗi mới | Smoke test `/api/install`, `/api/launch-app` sau khi deploy |

---

## Rủi ro / Câu hỏi mở

| # | Rủi ro / Câu hỏi | Mức độ | Ghi chú |
|---|---|---|---|
| R1 | `DeviceState` cache cũ — nếu `DevicePollWorker` poll chậm, trạng thái trả về có thể lệch thực tế | Trung bình | Chấp nhận được (eventual consistency); Tech Lead xác nhận chu kỳ poll và ghi vào TDD |
| R2 | Gọi `connect-by-ip` nhiều lần cho cùng IP — hành vi `AdbService.ConnectAsync` khi thiết bị đã connected | Thấp | Tech Lead cần kiểm tra và document hành vi idempotent hay không ở TDD |
| R3 | Port `5555` đang bị block bởi firewall/NAT trên môi trường production | Thấp | Ngoài scope server; document yêu cầu network trong TDD |
| Q1 | HTTP 404 hay HTTP 200 + `status: "NotFound"` cho serial không tồn tại (AC8)? | — | Giao Tech Lead quyết định ở TDD (STEP-2.1); recommendation: 404 rõ ràng hơn về mặt RESTful |
| Q2 | `AdbService.ConnectAsync` trả về serial của thiết bị sau khi kết nối không? Nếu có — nên trả về trong response endpoint 1 để client tự lưu serial | — | Tech Lead xác nhận ở TDD; nếu có → đưa vào response giúp client gọi API 2 ngay sau đó |
