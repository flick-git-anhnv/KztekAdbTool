---
step: 4.3
plan: ../PLAN-MASTER.md
agent: DevOps Engineer
status: todo
completed_at: ~
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
(để trống)

## Artifact
(để trống)

## Quyết định quan trọng
(để trống)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
