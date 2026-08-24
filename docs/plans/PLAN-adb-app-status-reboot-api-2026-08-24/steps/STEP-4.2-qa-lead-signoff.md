---
step: 4.2
plan: ../PLAN-MASTER.md
agent: QA Lead
status: done
completed_at: "2026-08-24 23:23"
deps: ["4.1"]
---

# STEP 4.2 — Sign-off chất lượng (P1 — bắt buộc QA Lead)

## Input nhận
Từ Bước 4.1 Handoff Payload — `docs/test-cases/TC-adb-app-status-reboot-api.md` với kết quả Pass/Fail từng TC, danh sách TC cần thiết bị Android thật.

## Nhiệm vụ
Review kết quả test, đưa ra quyết định sign-off:

- Đánh giá coverage: 10 TC API + 3 TC UI đã đủ chưa?
- Kiểm tra: không có P0/P1 bug còn mở
- Đặc biệt: TC reboot (TC-B01) — lệnh destructive, cần được verify thật trước production
- Điều kiện sign-off: nếu TC ADB thật chưa verify → sign-off có điều kiện (DevOps PHẢI smoke test với thiết bị thật tại staging trước khi go-live production)
- Ghi kết quả sign-off inline trong `docs/test-cases/TC-adb-app-status-reboot-api.md` hoặc tạo sign-off section riêng

## Definition of Done
- [ ] Đánh giá đủ coverage + bug status (0 P0/P1 open)
- [ ] SIGN-OFF (đầy đủ hoặc có điều kiện) ghi rõ trong TC doc hoặc step file này
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.2 → ✅

## Đã làm

1. Đọc `docs/test-cases/TC-adb-app-status-reboot-api.md` — 14 TC, 12 Pass / 2 Blocked / 0 Fail.
2. Đánh giá coverage so với TDD error matrix: tất cả status code (200/400/401/404/422/500) đều có TC. 7 TC pass HTTP thực tế, 3 TC pass Playwright (UXR), 4 TC pass unit test.
3. Xác nhận 0 P0/P1 bug open → điều kiện VETO không kích hoạt.
4. Đánh giá đặc biệt TC-B01 (reboot destructive): logic xác nhận qua unit test + confirm dialog UI xác nhận qua Playwright; gap duy nhất là E2E thực tế chưa có thiết bị.
5. Ghi sign-off section đầy đủ vào cuối `docs/test-cases/TC-adb-app-status-reboot-api.md`.
6. Xuất DOCX cho TC doc và step file.

## Artifact

- `docs/test-cases/TC-adb-app-status-reboot-api.md` — cập nhật + sign-off section
- `docs/test-cases/TC-adb-app-status-reboot-api.docx` — xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng

**SIGN-OFF: APPROVED CÓ ĐIỀU KIỆN**

- 14 TC coverage đủ theo TDD error matrix
- 0 P0/P1 bug → không VETO
- Deploy nội bộ/staging: KHÔNG bị block
- Go-live production: YÊU CẦU DevOps smoke test 2 TC với thiết bị thật:
  - Điều kiện 1: TC-A01 (app-status foreground) với thiết bị Android thật — ghi curl output vào DEPLOY doc
  - Điều kiện 2: TC-B01 (reboot) với thiết bị staging Android thật — ghi curl output + xác nhận reconnect vào DEPLOY doc
- Nếu cả 2 điều kiện pass: production go-live được phép, không cần thêm sign-off từ QA Lead
- Nếu một trong 2 fail: escalate lên QA Lead trước khi tiếp tục

## Handoff Payload — bước sau đọc phần này

- do_not_redo: KHÔNG viết lại sign-off section trong TC doc — đã ghi đầy đủ.
- watch_out: Deploy nội bộ OK, nhưng production PHẢI thỏa 2 điều kiện smoke test thiết bị thật (xem mục "Quyết định quan trọng" ở trên). TC-B01 (reboot) là destructive — chỉ chạy trên thiết bị staging, KHÔNG chạy trên thiết bị production đang phục vụ user. Ghi bằng chứng curl output vào DEPLOY doc trước khi đánh dấu điều kiện pass.
- next_inputs: Môi trường deploy: docker-compose trên máy này, image `kztek/adb-tool`, port 3339 theo `docker-compose.yml` (file pending commit từ plan adb-reconnect). Nhánh hiện tại: `docker-deploy`. Sign-off QA Lead: APPROVED CÓ ĐIỀU KIỆN (2 smoke test thiết bị thật phải pass trước production). Artifact tham chiếu: `docs/test-cases/TC-adb-app-status-reboot-api.md` mục "QA Lead Sign-off".

## Commit
- Hash: (điền sau commit)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
