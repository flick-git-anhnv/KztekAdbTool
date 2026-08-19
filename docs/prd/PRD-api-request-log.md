# PRD: API Request Log — Ghi lịch sử & hiển thị real-time cho AddDevice/LaunchApp API

## Tổng quan

- **Vấn đề:** Hai API mới vừa triển khai — `POST /api/devices/connect-by-ip` (AddDevice) và `POST /api/launch-app` (LaunchApp) — chưa có cơ chế ghi lịch sử request. Khi hệ thống tự động hóa gọi API thất bại hoặc hoạt động bất thường, operator không có dữ liệu để điều tra nguyên nhân (thời điểm gọi, tham số đầu vào, kết quả ADB, IP caller). Dashboard "Nhật ký hoạt động" (`#log`) hiện chỉ phản ánh hoạt động thiết bị nội bộ, không hiển thị activity từ API bên ngoài.
- **Đối tượng:** Operator kỹ thuật cần theo dõi hoạt động API real-time; hệ thống tự động hóa (CI/CD) cần audit trail khi xử lý sự cố.
- **Giá trị:** Cung cấp khả năng observability tối thiểu cho 2 API có ApiKey protection — lịch sử bền vững qua restart, hiển thị real-time trên dashboard, không cần thêm tool monitoring ngoài.

---

## Goals

1. Ghi lịch sử mọi request gọi vào **`POST /api/devices/connect-by-ip` (AddDevice API)** và **`POST /api/launch-app` (LaunchApp API)** — bao gồm cả request thành công và thất bại (kể cả 401 auth fail).
2. Lưu trữ bền vững log vào **SQLite hiện có của project** qua bảng mới `ApiRequestLog` (persist qua restart, không mất dữ liệu).
3. Đẩy mỗi log entry real-time qua **SignalR `DeviceHub`** để hiển thị tức thì vào panel **"Nhật ký hoạt động" (`#log`)** trên mọi tab dashboard đang mở — nhất quán với cơ chế `appendLog()` hiện tại.
4. Không thay đổi hành vi / response của 2 API gốc — logging là side-effect trong suốt với caller.

---

## Non-goals

- **Không log** bất kỳ API nào khác ngoài 2 API nêu trên (`/api/devices/connect`, `/api/install`, `/api/devices/{serial}/status`, ...).
- **Không thay đổi `ApiKeyEndpointFilter`** — logging inject vào handler sau khi auth đã pass, không sửa cơ chế xác thực.
- **Không tạo UI mới** — panel `#log` hiện có được tái sử dụng nguyên vẹn; không thêm trang quản lý log riêng.
- Không có retention policy / tự động xóa log cũ trong phiên bản này.
- Không có log export / download log file trong phiên bản này.
- Không có filter / search log trên dashboard trong phiên bản này.
- Không có rate limiting hay throttle dựa trên log trong phiên bản này.

---

## User Story (sơ lược)

> "Là operator kỹ thuật, khi hệ thống CI/CD gọi AddDevice API hoặc LaunchApp API, tôi muốn thấy ngay trên dashboard (panel Nhật ký hoạt động) thông tin: thời điểm gọi, API nào được gọi, tham số chính, kết quả — để tôi có thể xác nhận hệ thống hoạt động đúng hoặc điều tra sự cố mà không cần đăng nhập server tra log."

Chi tiết Given/When/Then và Acceptance Criteria kỹ thuật do **Business Analyst** viết ở STEP-1.2.

---

## Acceptance Criteria (mức cao)

- [ ] **AC1** — Mỗi request vào AddDevice API (`/api/devices/connect-by-ip`) được ghi 1 bản ghi vào bảng `ApiRequestLog` trong SQLite trong vòng **1 giây** kể từ khi request hoàn thành, bất kể kết quả thành công hay thất bại.
- [ ] **AC2** — Mỗi request vào LaunchApp API (`/api/launch-app`) được ghi 1 bản ghi vào bảng `ApiRequestLog` trong SQLite trong vòng **1 giây** kể từ khi request hoàn thành, bất kể kết quả thành công hay thất bại.
- [ ] **AC3** — Dữ liệu log bền vững qua restart: sau khi khởi động lại ứng dụng (hoặc container), toàn bộ log entries trước đó vẫn còn trong SQLite (KHÔNG bị xóa / reset).
- [ ] **AC4** — Mỗi log entry hiển thị real-time trong panel `#log` của mọi tab dashboard đang mở trong vòng **2 giây** kể từ khi request hoàn thành — thông qua SignalR `DeviceHub`, không cần refresh trang.
- [ ] **AC5** — Mỗi log entry phải chứa tối thiểu: timestamp (UTC), tên API (`AddDevice` / `LaunchApp`), tham số chính (IP:port hoặc serial+package), kết quả (thành công/thất bại + HTTP status code), và caller IP.
- [ ] **AC6** — Các request 401 (sai/thiếu API key) vào 2 API này CŨNG được ghi log với `result = "Unauthorized"` — để phát hiện brute-force hoặc caller dùng key sai.
- [ ] **AC7** — Các API khác (`/api/install`, `/api/devices/connect`, `/api/devices/{serial}/status`, ...) KHÔNG xuất hiện bản ghi mới trong `ApiRequestLog` khi được gọi — logging KHÔNG lan rộng ngoài 2 API đã định.

---

## Metric đo lường thành công

| Metric | Mục tiêu | Cách đo |
|---|---|---|
| Tỉ lệ request được log (so với tổng request vào 2 API) | 100% (không miss) | So sánh đếm request từ log web server với đếm bản ghi trong `ApiRequestLog` sau 10 lần gọi liên tiếp |
| Latency logging (thời gian từ response trả về đến khi bản ghi xuất hiện trong SQLite) | ≤ 1 giây | Đo thủ công: gọi API → query `SELECT * FROM ApiRequestLog ORDER BY CreatedAt DESC LIMIT 1` ngay sau |
| Thời gian hiển thị real-time trên dashboard (từ request hoàn thành đến entry xuất hiện trong `#log`) | ≤ 2 giây | QA quan sát trực tiếp trên dashboard khi gọi API từ Postman/curl |
| Persistence qua restart | 100% (0 bản ghi bị mất) | Gọi 5 request → restart container → đếm lại bản ghi trong `ApiRequestLog` |

---

## Rủi ro / Câu hỏi mở

| # | Rủi ro / Câu hỏi | Mức độ | Ghi chú |
|---|---|---|---|
| R1 | Logging thêm latency vào response của 2 API — nếu ghi DB đồng bộ (sync) trên hot path | Trung bình | Tech Lead quyết định: ghi async (fire-and-forget) hoặc background queue để không ảnh hưởng caller latency |
| R2 | Bảng `ApiRequestLog` tăng trưởng không giới hạn theo thời gian — SQLite file phình to | Thấp (phiên bản này) | Chấp nhận cho P2; retention policy ghi vào Non-goals để track ở backlog tương lai |
| R3 | SignalR broadcast đến tất cả clients khi log volume cao có thể gây noise trên dashboard | Thấp | 2 API này không high-frequency trong use case hiện tại; monitor nếu cần throttle ở phiên bản sau |
| Q1 | 401 auth fail — logging xảy ra trước hay sau `ApiKeyEndpointFilter`? Nếu filter throw 401 trước khi vào handler, cần interceptor/middleware riêng để bắt 401 | — | Tech Lead quyết định kiến trúc inject point ở TDD (STEP-1.5): middleware approach vs. handler-level try-catch vs. ActionFilter |
| Q2 | Format hiển thị log entry trong panel `#log`: dùng plain text hay có color-coding (đỏ/xanh) cho thành công/thất bại? | — | UX/UI Reviewer quyết định ở STEP-3.1; mặc định theo format hiện tại của `appendLog()` nếu không có chỉ định riêng |
