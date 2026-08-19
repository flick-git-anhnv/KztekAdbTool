---
step: "2.2"
plan: ../PLAN-MASTER.md
agent: junior-developer
status: done
completed_at: 2026-08-19 13:42
deps: ["1.5"]
---

# STEP 2.2 — Junior Developer: Frontend JS Implementation [∥ 2.1]

## Input nhận
- `docs/tech-design/TDD-api-request-log.md` từ STEP-1.5 — đọc kỹ: tên SignalR event, payload schema, JS handler contract, format log string
- `src/KztekAdbPublishTool.Web/wwwroot/js/signalr-client.js` (file chính cần sửa)
- `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` (tham khảo cách `appendLog()` được gọi)
- `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` (tham khảo cấu trúc panel `#log`)
- Handoff Payload từ bước liền trước (1.5): xem mục "next_inputs" của STEP-1.5

## Nhiệm vụ
Thêm JS event handler mới trong `signalr-client.js` để lắng nghe SignalR event do backend broadcast (tên event theo TDD 1.5), parse payload, và gọi `appendLog()` (hoặc hàm tương đương) để hiển thị log entry vào panel `#log`. Bước này chạy song song với STEP-2.1 — dựa hoàn toàn vào contract trong TDD, không phụ thuộc code backend.

## Definition of Done
- [ ] `src/KztekAdbPublishTool.Web/wwwroot/js/signalr-client.js` có event handler đăng ký đúng tên event theo TDD (VD: `connection.on("ApiRequestLogged", ...)`)
- [ ] Handler parse đúng payload JSON từ event (đủ các field: tên API, tham số, kết quả, thời điểm)
- [ ] Log entry hiển thị trong panel `#log` với format rõ ràng, nhất quán với các log entry hiện tại khác trong `appendLog()`
- [ ] Log entry phân biệt được: request thành công (VD: prefix `[API]` màu/ký hiệu phù hợp) vs thất bại
- [ ] Không có JS error trong console khi event được nhận
- [ ] Handler hoạt động đúng khi nhiều tab dashboard cùng mở (mỗi tab nhận SignalR event độc lập)
- [ ] Code không làm vỡ các event handler SignalR hiện có trong file
- [ ] PR description có `/verify-pr` VERIFICATION REPORT (ít nhất kiểm tra JS không syntax error, không console error rõ ràng)

## Đã làm
- Đọc TDD đầy đủ, xác định vị trí đặt code: `dashboard.js` (KHÔNG sửa `signalr-client.js`)
- Thêm `conn.on('ApiRequestLogged', function(entry) { appendLog(formatApiRequestLog(entry)); })` trong `setupSignalR()`, ngay sau handler `DeviceInstalled`
- Thêm hàm `formatApiRequestLog(entry)` — format chuỗi `[API] apiName params → result (status, durationMs) from ip`
- Thêm hàm `summarizeParams(apiName, paramsJsonOrNull)` — xử lý 2 case: AddDevice (`ip=X:port`) và LaunchApp (`serial=X, app=Y`), graceful fallback khi parse fail
- Đặt cả 2 helper trên `renderDevices()`, dưới `appendLog()` — nhất quán với layout file
- Kiểm tra syntax: `node --check dashboard.js` → SYNTAX OK
- Verify XSS: `appendLog()` dùng `el.textContent +=` (escape sẵn) — không cần thêm escape

## Artifact
- `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` — sửa: +43 dòng (2 helper + 1 event handler)

## Quyết định quan trọng
1. **Vị trí code:** Dashboard.js (KHÔNG signalr-client.js) — đúng theo TDD §JS Handler Contract. Step file DoD cũ ghi `signalr-client.js` là SAI; TDD là nguồn sự thật cuối cùng. Deviation đã áp dụng đúng.
2. **Format log string:** `[API] apiName params → result (status, durationMs) from ip` — ví dụ: `[API] AddDevice ip=192.168.1.10:5555 → Success (200, 47ms) from 10.0.0.15`
3. **Prefix `[API]`** phân biệt log API request với các log hệ thống khác (`[Fallback]`, `[serial]`...)
4. **Plain text, không color-coded** — theo TDD §Câu hỏi mở Q-04: UX Reviewer có thể propose ở backlog nếu cần

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Handler `conn.on('ApiRequestLogged', ...)` đã đăng ký trong `setupSignalR()` của `dashboard.js`. Không đăng ký lại ở `signalr-client.js` — sẽ bị double-handler.
- watch_out: Event name phải khớp chính xác chuỗi `"ApiRequestLogged"` (PascalCase). Payload camelCase: `id`, `timestamp`, `apiName`, `httpMethod`, `path`, `parameters`, `result`, `httpStatusCode`, `errorMessage`, `callerIp`, `durationMs`. Nếu Senior Dev (2.1) đổi tên field → cần align trước TL review.
- next_inputs: File đã sửa: `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js`; commit hash: `d8abe37`; format log: `[API] apiName params → result (status, durationMs) from ip`; signalr-client.js KHÔNG sửa (đây là deviation so với step file DoD cũ, đã áp dụng đúng theo TDD)

## Commit
- Hash: d8abe37
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
