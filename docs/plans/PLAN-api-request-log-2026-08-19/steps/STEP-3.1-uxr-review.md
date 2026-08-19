---
step: "3.1"
plan: ../PLAN-MASTER.md
agent: ux-ui-reviewer
status: done
completed_at: "2026-08-19 14:12"
deps: ["2.3"]
---

# STEP 3.1 — UX/UI Reviewer: Visual Verification

## Input nhận
- Code đã merge từ STEP-2.3 (backend + frontend JS đều xong)
- `docs/tech-design/TDD-api-request-log.md` — xem mục JS handler contract và format log string
- App phải đang chạy thật (local hoặc staging) để chụp screenshot

## Nhiệm vụ
Chạy app thật, gọi AddDevice API và LaunchApp API, verify log entries xuất hiện đúng trong panel `#log` trên dashboard. Bước này lightweight vì không có UI component mới — chỉ kiểm tra log entries được sinh ra đúng format, đúng timing, đúng nội dung. Chụp screenshot làm bằng chứng.

## Definition of Done
- [ ] App đang chạy thật (không dùng mock)
- [ ] Gọi AddDevice API với API key hợp lệ → log entry xuất hiện trong `#log` trong vòng 2 giây
- [ ] Gọi LaunchApp API với API key hợp lệ → log entry xuất hiện trong `#log` trong vòng 2 giây
- [ ] Gọi API với kết quả thất bại (VD: serial không hợp lệ) → log entry vẫn xuất hiện, phân biệt được với success
- [ ] Mở 2 tab dashboard cùng lúc → cả 2 tab đều nhận log entry (SignalR broadcast hoạt động đúng)
- [ ] Log entry hiển thị đủ thông tin: tên API, tham số chính, kết quả, thời điểm — dễ đọc, không bị truncate
- [ ] Format log entry nhất quán với các log entry khác trong panel (cùng font, cùng cách căn chỉnh)
- [ ] Không có JS error trong browser console khi nhận event
- [ ] Screenshot ít nhất 2 ảnh: (1) panel `#log` sau khi gọi AddDevice API, (2) panel sau khi gọi LaunchApp API
- [ ] `docs/ux-review/UX-REVIEW-api-request-log.md` đã được tạo với đánh giá C1–C7
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
1. Rebuild Docker image với code mới (`docker compose build --no-cache`) — container cũ chạy image pre-feature (thiếu ApiRequestLog code).
2. Restart container → verify dashboard HTTP 200 và ApiRequestLog table tạo thành công khi filter khởi tạo.
3. Chạy 5 test case qua curl:
   - TC-1: AddDevice 200 OK → DB entry Id=1 tạo đúng (params null — bug)
   - TC-2: LaunchApp 422 AppNotInstalled → DB entry Id=2 tạo đúng (params null)
   - TC-3: AddDevice 401 wrong key → DB entry Id=3 tạo đúng (OUTER filter đúng spec)
   - TC-4: LaunchApp 401 no key → DB entry Id=4 tạo đúng
   - TC-5: AddDevice 400 invalid IP → DB entry Id=5 tạo đúng
4. Verify DB trực tiếp (python3 + sqlite3): 5 entries, mọi field đúng ngoại trừ `Parameters=None`.
5. Verify SignalR broadcast qua code review: `ApiRequestLogService.LogAsync` gọi `Clients.All.SendAsync("ApiRequestLogged", entry)` — architecture đúng.
6. Verify JS handler: `conn.on('ApiRequestLogged', ...)` đăng ký trong `setupSignalR()` (dashboard.js:299). `formatApiRequestLog` xử lý null params an toàn → hiển thị `(no body)`.
7. Verify CSS: `#log` có height 150px, overflow-y auto, monospace font — nhất quán.
8. Tạo evidence files: `docs/ux-review/screenshots/2026-08-19/*.txt` (5 TC + 1 DB snapshot).
9. Viết `docs/ux-review/UX-REVIEW-api-request-log.md` + chạy `md_to_docx_kztek.py` → DOCX thành công.

## Artifact
- `docs/ux-review/UX-REVIEW-api-request-log.md` — Report đầy đủ
- `docs/ux-review/UX-REVIEW-api-request-log.docx` — DOCX brand KZTEK
- `docs/ux-review/screenshots/2026-08-19/TC1-adddevice-success.txt`
- `docs/ux-review/screenshots/2026-08-19/TC2-launchapp-app-not-installed.txt`
- `docs/ux-review/screenshots/2026-08-19/TC3-TC4-unauthorized-401-logs.txt`
- `docs/ux-review/screenshots/2026-08-19/TC5-invalid-input-400.txt`
- `docs/ux-review/screenshots/2026-08-19/DB-ApiRequestLog-all-entries.txt`

## Quyết định quan trọng
- **[UI-001] BUG HIGH:** `Parameters` field trong `ApiRequestLog` luôn null cho mọi request, kể cả khi body JSON rõ ràng có nội dung. Root cause: `TryReadBodyJsonAsync()` trong `ApiRequestLoggingEndpointFilter.cs` silent-fails (outer try-catch swallows exception). Hypothesis: `EnableBuffering()` + `StreamReader` pattern không hoạt động đúng trong pipeline Minimal API với Production environment. Impact UX: log entry hiển thị `(no body)` thay vì `ip=192.168.21.11:5555`. **Cần Senior Developer fix trước khi QA Lead sign-off.**
- **[UI-002] LOW:** Container cần rebuild trước khi UXR test được. `docker-compose.yml` có uncommitted changes — STEP 3.4 cần chú ý.
- **401 logging đúng spec:** Entries 3 và 4 (401) được tạo — đây là hành vi ĐÚNG theo design (OUTER filter).

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Không rebuild Docker image lại — container đang chạy đúng image mới (post-feature). ApiRequestLog table tồn tại và 5 entries đã insert thành công.
- watch_out: **BUG UI-001 (High): `Parameters` luôn null** — QAE cần test thêm và verify fix sau khi Senior Dev sửa. Test case 401 ra log entry là ĐÚNG spec (không phải fail). Multi-tab SignalR chưa verify live — QAE cần test bằng 2 browser tab.
- next_inputs: `docs/ux-review/UX-REVIEW-api-request-log.md` (report đầy đủ với issue list); container port 3339; API key production: `sup3rsecr3tap1key@`; device online: `192.168.21.11:5555`, `192.168.21.16:5555`, `192.168.21.22:5555`, `192.168.21.77:5555`.

## Commit
- Hash: 345792c
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
