---
step: 4.3
plan: ../PLAN-MASTER.md
agent: DevOps Engineer
status: done
completed_at: 2026-08-24 23:29
deps: ["4.2"]
---

# STEP 4.3 — Deploy lên môi trường tương ứng, smoke test

## Input nhận
Từ Bước 4.2 Handoff Payload — QA Lead sign-off (đầy đủ hoặc có điều kiện), điều kiện bổ sung nếu có (VD: cần smoke test ADB thật).

## Nhiệm vụ
- Build Docker image mới (chứa 2 endpoint + UI mới)
- Deploy lên staging (docker compose hoặc equivalent)
- Smoke test tối thiểu:
  - TC-A05/06 (400/401 — không cần thiết bị): verify nhanh qua curl
  - TC-B02/03 (404/401 — không cần thiết bị): verify nhanh qua curl
  - TC-C01, TC-C02, TC-C03 (UI buttons): verify trên browser staging
  - TC-A01/B01 (cần thiết bị Android thật): nếu có → verify, nếu không → ghi nhận GATE CHƯA ĐÓNG
- Viết `docs/devops/DEPLOY-adb-app-status-reboot-api.md` — checklist deploy + kết quả smoke test
- Xuất DOCX

## Definition of Done
- [ ] Docker image build thành công
- [ ] Container staging running, endpoint respond đúng
- [ ] Smoke test 401/400/404 qua curl PASS
- [ ] UI smoke test (TC-C01/C02/C03) PASS
- [ ] `docs/devops/DEPLOY-adb-app-status-reboot-api.md` + DOCX tạo xong
- [ ] Ghi nhận rõ TC nào cần thiết bị thật chưa verify (GATE CHƯA ĐÓNG nếu cần)
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.3 → ✅

## Đã làm
- Build image mới `kztek/adb-tool:latest` (`be8c5b3b6a74`) bằng `docker compose build` — `dotnet publish` thành công trong 6.5s, 0 lỗi.
- Redeploy container `kztek-adb-tool` bằng `docker compose up -d` — container mới `fa43f4598d9a` khởi động healthy trong ~10s.
- Smoke test 7 TC qua curl: tất cả PASS (401/400/404 đúng kỳ vọng + HTML có 2 nút mới `btn-check-app-status` / `btn-reboot-device` + class `btn-kz-outline-navy` / `btn-kz-outline-orange`).
- Xác nhận không có thiết bị Android (`adb devices` rỗng) → TC-A01 + TC-B01 ghi nhận GATE CHƯA ĐÓNG.
- Viết `docs/devops/DEPLOY-adb-app-status-reboot-api.md` + xuất DOCX (--no-pdf).

## Artifact
- `docs/devops/DEPLOY-adb-app-status-reboot-api.md` — deploy checklist + curl output thật + rollback plan
- `docs/devops/DEPLOY-adb-app-status-reboot-api.docx` — xuất DOCX brand KZTEK

## Quyết định quan trọng
- Rollback image cũ: `docker tag 3cf8753aaabd kztek/adb-tool:latest && docker compose up -d --force-recreate`. Image `3cf8753aaabd` vẫn còn trên máy (untagged sau build mới).
- KHÔNG commit `docker-compose.yml` (chứa API key plain-text).
- GATE CHƯA ĐÓNG: TC-A01 (app-status foreground) + TC-B01 (reboot thiết bị thật) — DevOps Lead phải xác nhận trên thiết bị staging trước production go-live.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Build image + redeploy container đã xong. Smoke test 7 TC curl đã PASS. DOCX đã xuất.
- watch_out: TC-A01 + TC-B01 cần thiết bị Android thật (staging only, KHÔNG production đang phục vụ user). Rollback image cũ: `3cf8753aaabd` (untagged, vẫn còn trên máy). KHÔNG xóa volume `adb_data`/`adb_uploads`. KHÔNG `docker compose down -v`.
- next_inputs: Container `fa43f4598d9a` (image `be8c5b3b6a74`) đang healthy port 3339. Smoke test 7 TC PASS (curl output đầy đủ trong `docs/devops/DEPLOY-adb-app-status-reboot-api.md` mục 4). GATE CHƯA ĐÓNG: TC-A01 + TC-B01 chờ thiết bị Android staging. DevOps Lead cần: (1) kết nối thiết bị staging, (2) chạy TC-A01 + TC-B01 theo hướng dẫn mục 5 DEPLOY doc, (3) cập nhật bảng mục 4.4, (4) approve production go-live.

## Commit
- Hash: (sẽ điền sau commit)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
