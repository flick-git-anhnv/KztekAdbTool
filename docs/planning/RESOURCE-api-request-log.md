# RESOURCE PLAN: API Request Log — Ghi lịch sử & hiển thị real-time cho AddDevice/LaunchApp API

**Feature slug:** api-request-log
**Priority:** P2
**Ngày lập:** 2026-08-19
**Engineering Manager:** duongth@kztek.net

---

## 1. Tóm tắt tính năng

Thêm cơ chế ghi lịch sử mọi request gọi vào 2 API có ApiKey protection: `POST /api/devices/connect-by-ip` (AddDevice) và `POST /api/launch-app` (LaunchApp). Log được lưu bền vững vào SQLite (bảng mới `ApiRequestLog` qua EF migration) và đẩy real-time qua SignalR `DeviceHub` vào panel "Nhật ký hoạt động" (`#log`) trên mọi tab dashboard đang mở — tái sử dụng cơ chế `appendLog()` hiện tại. Logging là side-effect trong suốt, không thay đổi response trả về caller, kể cả log cả 401 Unauthorized.

---

## 2. Quyết định Priority

| Tiêu chí | Đánh giá |
|---|---|
| Mức độ | **P2** — observability quan trọng nhưng không block production go-live ngay |
| Rủi ro | Thấp — không thay đổi behavior 2 API gốc; logging là side-effect; lỗi ghi DB fail silently |
| Phụ thuộc | Phụ thuộc vào TDD (STEP-1.5) để chốt: inject point 401, tên SignalR event, async strategy, CallerIp source |
| Deadline | Không có deadline cứng; hoàn thành sau khi P1 (adb-reconnect) deploy xong |
| Xung đột P1 | **Không xung đột** — xem Mục 5 chi tiết |

**Kết luận: Xác nhận P2. Không nâng lên P1 — feature này không block production; P1 adb-reconnect-after-restart là ưu tiên cao hơn và đang ở bước cuối (deploy).**

---

## 3. Phân bổ Team

| Agent | Vai trò | Task cụ thể | Lý do |
|---|---|---|---|
| **Tech Lead** | TDD + Code Review + Security Audit (conditional) | STEP-1.5: Chốt schema DB, interface service, inject point 401, tên SignalR event, async strategy. STEP-2.3: Review PR cuối. STEP-2.4: Đánh giá security audit | Kiến trúc inject point 401 (middleware vs response middleware) cần Tech Lead quyết định; DB schema mới cần review |
| **Senior Developer** | Backend C# toàn bộ (STEP-2.1) | Entity `ApiRequestLog` + EF migration, `IApiRequestLogService` + `ApiRequestLogService`, inject vào `DeviceEndpoints.cs` + `LaunchAppEndpoints.cs`, 401 intercept theo thiết kế TDD, SignalR broadcast, unit tests | Logic phức tạp: 401 inject trước handler, async logging, concurrency safety |
| **Junior Developer** | Frontend JS (STEP-2.2) | Thêm SignalR event handler mới trong `signalr-client.js`, format và render log entry vào panel `#log` qua `appendLog()` | Task front-end JS đơn giản, có spec rõ từ TDD, không đụng logic nghiệp vụ phức tạp |
| **UX/UI Reviewer** | Kiểm tra trực quan UI (STEP-3.1) | Chạy app thật, gọi 2 API, xác nhận log entry xuất hiện đúng format và vị trí trong panel `#log` | JS frontend thay đổi — bắt buộc UXR theo CLAUDE.md |
| **QA Engineer** | Test execution (STEP-3.2) | Gọi AddDevice + LaunchApp với các scenario từ US-001 đến US-006; kiểm tra DB persistence; verify SignalR real-time; regression test | 6 User Stories + 18 Acceptance Criteria đã có sẵn làm test basis |
| **QA Lead** | Sign-off (STEP-3.3) | Review kết quả QA, veto nếu còn P0/P1 bug — bắt buộc vì P2 trong WF-FEATURE | Bắt buộc sign-off theo WF-FEATURE |
| **DevOps Engineer** | Deploy staging (STEP-3.4) | `docker-compose up --build`, verify migration apply thành công (bảng `ApiRequestLog` xuất hiện) | |
| **DevOps Lead** | Approve staging + production (STEP-3.5, 3.6) | Smoke test, approve deploy production, monitor | Two-Eyes cho deploy production |

**Junior Developer:** Được giao đúng phần frontend JS phù hợp cấp Junior — đụng `signalr-client.js`, không đụng logic C# backend, không đụng auth.

---

## 4. Estimate Effort (thực tế, không thổi phồng)

> Feature quy mô vừa: thêm 1 bảng DB + 1 service mới + inject vào 2 endpoint có sẵn + 1 JS handler. Phần phức tạp nhất là 401 inject point (cần quyết định kiến trúc ở TDD).

| Bước | Agent | Estimate | Ghi chú |
|---|---|---|---|
| STEP-1.5: Viết TDD | Tech Lead | **2–3 giờ** | Chốt: schema bảng `ApiRequestLog`, interface service, inject point 401 (middleware hay response middleware), tên SignalR event, async strategy (fire-and-forget vs queue), CallerIp source. Đây là bước dài nhất ở Phase 1 vì phải quyết định kiến trúc cho 401 |
| STEP-2.1: Backend C# | Senior Developer | **5–7 giờ** | Entity + Migration (~1h) + IService/Service (~2h) + inject vào 2 endpoint (~1h) + implement 401 intercept theo TDD (~1h) + unit tests (~1–2h) |
| STEP-2.2: Frontend JS | Junior Developer | **2–3 giờ** | Đọc TDD lấy tên event + format entry (~0.5h) + viết event handler trong `signalr-client.js` (~1h) + test trên browser (~0.5–1h) |
| STEP-2.3: Code review + merge | Tech Lead | **1–2 giờ** | Review cả backend (2.1) lẫn frontend (2.2); yêu cầu /verify-pr report trước khi mở review |
| STEP-2.4: Security audit (conditional) | Tech Lead | **30–60 phút** | DB schema mới nhưng dữ liệu không nhạy cảm (IP + timestamp + status code). Tech Lead tự quyết có chạy stride không; recommend chạy nhẹ để cover CallerIp logging |
| STEP-3.1: UXR review | UX/UI Reviewer | **30–60 phút** | Chạy app thật, gọi API thực, xác nhận log entry format và real-time |
| STEP-3.2: QA test execution | QA Engineer | **2–3 giờ** | 6 US × 3 scenario mỗi US = ~18 test case; gồm DB persistence check + SignalR real-time verify |
| STEP-3.3: Sign-off | QA Lead | **30 phút** | Review kết quả QA |
| STEP-3.4 + 3.5 + 3.6: Deploy | DevOps Engineer + Lead | **1–2 giờ** | Build + deploy + smoke test staging → approve → production |
| **Tổng** | | **~15–22 giờ** | **Khoảng 2–3 ngày làm việc cho toàn chain kỹ thuật** |

---

## 5. Phân tích Resource Conflict với P1 (adb-reconnect-after-restart)

| Tiêu chí | P1: adb-reconnect-after-restart | P2: api-request-log | Kết luận |
|---|---|---|---|
| **Status hiện tại** | Active — chỉ còn STEP-3.3 (DevOps deploy) | Phase 1 đang lập plan | P1 gần xong — chỉ DevOps còn việc |
| **Code area** | `DevicePollWorker.cs` (warm-up reconnect logic), `CancellationToken` handling | `ApiRequestLog.cs` (entity mới), `ApiRequestLogService.cs` (mới), `DeviceEndpoints.cs` + `LaunchAppEndpoints.cs` (inject logging — không đụng reconnect logic), `signalr-client.js` | **Không overlap code** |
| **Agent bận** | DevOps Engineer (deploy 3.3 — bước cuối) | Senior Dev, Junior Dev, Tech Lead | **Không conflict agent** — DevOps bận 3.3 nhưng tech team free |
| **File có thể conflict** | `DeviceEndpoints.cs` (P1 đã sửa reconnect endpoint) | `DeviceEndpoints.cs` (P2 inject logging vào endpoint này) | **Conflict tiềm năng duy nhất**: cùng sửa `DeviceEndpoints.cs` |
| **Mức độ conflict** | P1 đã merge `DeviceEndpoints.cs` (STEP-2.1/2.2 done ✅). P2 Senior Dev sẽ inject logging vào version đã merge của P1 | Thấp — P2 sẽ có code P1 merged sẵn làm base; không có parallel editing cùng file |

**Quyết định EM: P2 có thể bắt đầu song song với P1 deploy (STEP-3.3).** Lý do:
- Tech Lead, Senior Dev, Junior Dev đều free (P1 không còn dùng họ).
- `DeviceEndpoints.cs` đã được merge từ P1 — P2 Senior Dev code trên base đó, không có merge conflict.
- DevOps Engineer bận deploy P1 nhưng không block P2 Phase 1 (lập plan) và Phase 2 (code).
- P2 deploy (STEP-3.4) sẽ chờ P1 deploy (STEP-3.3) hoàn thành trước để staging sạch — DevOps Lead xác nhận thời điểm.

---

## 6. Điều kiện bắt buộc trước khi bắt đầu Phase kỹ thuật (Phase 2)

- [ ] TDD (STEP-1.5) đã hoàn thành và Tech Lead đã chốt: inject point 401, tên SignalR event, async strategy, CallerIp source
- [ ] P1 deploy (STEP-3.3) đã hoàn thành — Senior Dev verify `DeviceEndpoints.cs` trên nhánh main là bản P1 đã merge trước khi code P2
- [ ] Senior Developer đọc TDD đầy đủ trước khi code; không tự quyết định Q-01 đến Q-05 còn mở trong US
- [ ] Junior Developer đọc phần "JS handler contract" trong TDD trước khi code — cần tên event SignalR chính xác và format tham số
- [ ] Tech Lead review /verify-pr report trước khi mở review STEP-2.3

---

## 7. Approve

Engineering Manager xác nhận:
- **Priority P2** xác nhận — không block production; thấp hơn P1 (adb-reconnect) đang active
- **Không có resource conflict** với P1 — tech team (Senior Dev, Junior Dev, Tech Lead) free ngay; DevOps queue sau P1 deploy xong
- **Senior Developer** phụ trách toàn bộ backend C# — phần 401 inject point cần kinh nghiệp kiến trúc
- **Junior Developer** phụ trách frontend JS — phù hợp cấp, scope rõ ràng sau khi có TDD
- **Estimate thực tế ~2–3 ngày làm việc** — không escalate thêm resource, không cần CTO review (P2, quy mô nhỏ-vừa)
- DevOps Lead xác nhận thời điểm deploy staging P2 sau khi P1 deploy xong
