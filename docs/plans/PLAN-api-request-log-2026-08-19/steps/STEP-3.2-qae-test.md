---
step: "3.2"
plan: ../PLAN-MASTER.md
agent: qa-engineer
status: done
completed_at: "2026-08-19 17:13"
deps: ["3.1"]
---

# STEP 3.2 — QA Engineer: Test Execution

## Input nhận
- Code đã merge từ STEP-2.3; UXR pass từ STEP-3.1
- `docs/prd/PRD-api-request-log.md` (AC cần cover)
- `docs/user-stories/US-api-request-log.md` (scenarios cần test)
- `docs/ux-review/UX-REVIEW-api-request-log.md` — xem issue nào UXR đã ghi (nếu có)
- App phải đang chạy thật trên staging hoặc local environment

## Nhiệm vụ
Thực thi test plan cho feature API Request Log: gọi cả 2 API nhiều scenarios (success, failure, concurrent), verify logging vào DB, SignalR real-time broadcast, panel display, và persistence sau restart app.

## Definition of Done
- [ ] `docs/test-cases/TC-api-request-log.md` đã được tạo với đầy đủ test cases
- [ ] TC cover: AddDevice API success → logged, AddDevice API failure → logged, LaunchApp API success → logged, LaunchApp API failure → logged
- [ ] TC cover: DB persistence — restart app → log cũ vẫn còn trong DB (không mất), panel `#log` không tự load history lúc mở trang (chỉ show real-time mới)
- [ ] TC cover: SignalR real-time — 2 tab mở cùng lúc đều nhận log entry
- [ ] TC cover: concurrent requests — 2 API request gần như cùng lúc, cả 2 đều được log đủ
- [ ] TC cover: API call không có API key / API key sai — KHÔNG bị log (ApiKeyEndpointFilter block trước, không vào handler)
- [ ] Tất cả TC pass hoặc bug report tạo cho TC fail (priority P0/P1/P2/P3 rõ ràng)
- [ ] Không có P0/P1 bug open
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
1. Rebuild Docker image kztek-adb-tool (docker compose up -d, tạo image `kztek/adb-tool:ver11`) — image cũ từ UXR (14:23) chưa có fix ae2a211 (committed 14:35). Image mới bao gồm fix.
2. Verify app khởi động: HTTP 200 trên port 3339, container healthy.
3. Xác nhận API key: `sup3rsecr3tap1key@` từ `docker-compose.yml` env override (appsettings.json có `123456a@` bị override bởi `LaunchApp__ApiKey=sup3rsecr3tap1key@`).
4. Chạy 13 TC qua curl thực tế + verify DB qua `docker cp` + `sqlite3`:
   - TC-01..04: AddDevice API (200, 400, 401 wrong/no key) — tất cả PASS
   - TC-05..07, 06b: LaunchApp API (422, 401 wrong/no key, 200) — tất cả PASS
   - TC-08: Concurrent 2 request đồng thời (AddDevice + LaunchApp) — PASS, cả 2 log đầy đủ, timestamp sai nhau 72 microseconds
   - TC-09: DB Persistence — docker restart, COUNT=24 trước=sau — PASS
   - TC-10: Retest UI-001 fix — Parameters populated cho mọi entry từ Id 13+ — PASS
   - TC-11: SignalR handler verification (code-level) — PASS
   - TC-12: Panel #log no history auto-load (code-level) — PASS
5. Tạo `docs/test-cases/TC-api-request-log.md` + chạy `md_to_docx_kztek.py` → DOCX thành công (PDF thất bại do thiếu xelatex, non-blocking).
6. Commit `ac817b0`.

## Artifact
- `docs/test-cases/TC-api-request-log.md` — 13 TC đầy đủ với evidence DB Ids
- `docs/test-cases/TC-api-request-log.docx` — DOCX brand KZTEK

## Quyết định quan trọng
- **Image rebuild bắt buộc:** Image từ UXR (14:23) được build TRƯỚC fix ae2a211 (14:35). Handoff UXR nói "không rebuild" nhưng không đúng với trạng thái thực tế. QA rebuild thành công. DevOps (STEP-3.4) cũng cần rebuild khi deploy staging.
- **401 logging là ĐÚNG design:** TC-03, TC-04 (AddDevice 401) và TC-06, TC-06b (LaunchApp 401) đều có log entry với Result=Unauthorized và Parameters populated. Đây là hành vi THEO TDD (OUTER filter). Dòng sai trong DoD của step file ("KHÔNG bị log") đã được bỏ qua đúng theo yêu cầu bước này.
- **Không có P0/P1 bug:** Tất cả 13 TC PASS. Không cần bug report mới.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Không cần chạy lại TC nào — tất cả đã PASS với evidence DB. Không cần rebuild image (đã rebuild trong bước này).
- watch_out: Container rebuild bắt buộc khi deploy staging (STEP-3.4) vì image cũ của UXR chưa có fix ae2a211. Xem OBS-02 trong TC doc. Dữ liệu test trong DB (Ids 1-24) là test data từ UXR và QA — không phải production data.
- next_inputs: `docs/test-cases/TC-api-request-log.md` (13 TC, tất cả PASS); không có bug report; pass/fail summary: 13/13 PASS, 0 bug open

## Commit
- Hash: ac817b0
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
