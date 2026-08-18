# PRD: API Launch App Android qua ADB

## Tổng quan

- **Vấn đề:** Hiện tại `KztekAdbPublishTool.Web` chưa cung cấp endpoint HTTP cho phép client bên ngoài ra lệnh bật (launch) một ứng dụng Android trên thiết bị cụ thể qua ADB. Tác vụ này phải thực hiện thủ công hoặc thông qua kênh ngoài, gây bất tiện khi cần tự động hóa quy trình sau khi publish APK.
- **Đối tượng:** Hệ thống tự động hóa (CI/CD pipeline, script triển khai) hoặc operator kỹ thuật cần kích hoạt app từ xa ngay sau khi install mà không cần can thiệp trực tiếp vào thiết bị.
- **Giá trị:** Rút ngắn vòng lặp deploy-launch, cho phép tự động hóa end-to-end: install APK → launch app → kiểm tra app đã khởi động. Tái sử dụng `AdbService.LaunchAppAsync` đã được kiểm chứng, không phát sinh code ADB mới.

---

## Goals

1. Cung cấp 1 endpoint HTTP để client gọi lệnh launch app Android trên thiết bị đích được chỉ định bằng serial.
2. Bảo vệ endpoint bằng API key tĩnh qua header `x-api-key` — áp dụng **riêng** cho endpoint mới này, không ảnh hưởng các route cũ.
3. Trả về kết quả tường minh: thành công (exit code 0 từ ADB) hoặc thất bại kèm thông tin lỗi.
4. Tái sử dụng `AdbService.LaunchAppAsync(serial, packageName, ct)` có sẵn — không viết lại logic ADB.

## Non-goals (phiên bản này)

- Không có thay đổi UI/giao diện — API backend thuần túy.
- **Không** áp dụng API key middleware cho `/api/install` hay bất kỳ route cũ nào khác; các route đó giữ nguyên trạng thái hiện tại.
- Không có multi-key / key rotation — chỉ 1 key tĩnh đọc từ config.
- Không có audit log / request log chi tiết trong phiên bản này.
- Không có rate limiting hay throttle trong phiên bản này.
- Không có quản lý trạng thái app sau launch (không kiểm tra app có thực sự foreground không sau khi gọi).

---

## Scope

**Trong scope:**

| Hạng mục | Mô tả |
|---|---|
| Endpoint mới | `POST /api/launch-app` (hoặc `GET` nếu Tech Lead chọn — quyết định ở TDD) |
| Auth | Header `x-api-key` với giá trị khớp config; trả 401 nếu thiếu/sai key |
| Input | `serial` (serial ADB của thiết bị đích), `app` (package name Android, VD: `com.kztek.abc`) |
| Cơ chế launch | Gọi `AdbService.LaunchAppAsync(serial, packageName, ct)` — dùng `resolve-activity` + `am start -n`, KHÔNG dùng `monkey` |
| Output | JSON với trường trạng thái: thành công / lỗi, thông báo từ ADB (`StdOut`/`StdErr`) |
| Config | API key lưu ở `appsettings.json` và override được bằng biến môi trường |

**Ngoài scope:** Xem Non-goals.

---

## User Story (sơ lược)

> "Là hệ thống CI/CD, sau khi install APK thành công, tôi muốn gọi 1 HTTP endpoint để launch app trên thiết bị đích theo serial — để xác nhận app khởi động được và hoàn thành vòng lặp deploy."

Chi tiết Given/When/Then và Acceptance Criteria kỹ thuật do **Business Analyst** viết ở bước tiếp theo (STEP-1.2).

---

## Acceptance Criteria (mức cao)

- [ ] **AC1** — Gọi endpoint với `x-api-key` đúng, `serial` hợp lệ, `app` là package đã cài trên thiết bị → trả HTTP 200, app khởi động trên thiết bị.
- [ ] **AC2** — Gọi endpoint với `x-api-key` sai hoặc thiếu → trả HTTP 401, không thực thi lệnh ADB.
- [ ] **AC3** — Gọi endpoint với `serial` không có trong danh sách thiết bị đang kết nối → trả HTTP 404 hoặc 422 kèm thông báo rõ ràng.
- [ ] **AC4** — Gọi endpoint với `app` là package chưa được cài trên thiết bị → trả lỗi ADB tường minh (exit code -1, `StdErr: "No activities found to run"`), HTTP 422 hoặc 200 kèm trạng thái thất bại.
- [ ] **AC5** — Các route cũ (`/api/install` và các route khác) hoạt động bình thường, không bị ảnh hưởng bởi cơ chế auth mới.
- [ ] **AC6** — API key có thể override bằng biến môi trường mà không cần rebuild image.

---

## Metric đo lường thành công

| Metric | Mục tiêu | Cách đo |
|---|---|---|
| Tỉ lệ launch thành công (khi thiết bị online, app đã cài) | ≥ 95% | Log `ExitCode == 0` từ `AdbCommandResult` |
| Latency P95 endpoint | ≤ 5 giây | Đo từ request nhận đến response trả về (bao gồm thời gian ADB) |
| Tỉ lệ 401 đúng khi key sai | 100% | Test case auth |
| Không có regression trên route cũ | 0 lỗi mới | Smoke test `/api/install` sau khi deploy |

---

## Rủi ro / Câu hỏi mở

| # | Rủi ro / Câu hỏi | Mức độ | Ghi chú |
|---|---|---|---|
| R1 | API key tĩnh lộ nếu config không được bảo vệ đúng cách (VD: file `.env` commit vào git) | Trung bình | Tech Lead cần chỉ định rõ cách inject key qua env; ghi vào `.gitignore` nếu dùng file riêng |
| R2 | Thiết bị offline hoặc mất kết nối ADB giữa chừng → ADB timeout | Thấp | `LaunchAppAsync` đã có `timeoutMs: 10000` — xử lý thêm ở tầng endpoint nếu cần |
| R3 | Package name trùng namespace với app hệ thống trên một số ROM | Thấp | Ngoài scope phiên bản này; document hành vi thực tế nếu phát sinh |
| Q1 | HTTP verb cho endpoint: POST (body JSON) hay GET (query params)? | — | Giao Tech Lead quyết định ở TDD (STEP-2.1) |
| Q2 | Cần trả về `StdOut` đầy đủ từ ADB trong response không, hay chỉ trả trạng thái? | — | Giao Tech Lead quyết định ở TDD; recommendation: trả đủ để dễ debug |
