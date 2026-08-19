---
step: 4.3
plan: ../PLAN-MASTER.md
agent: DevOps Engineer
status: done
completed_at: 2026-08-19 10:53
deps: [4.2]
---

# STEP 4.3 — Deploy lên môi trường tương ứng

## Input nhận
Nhận Handoff Payload từ STEP-4.2 (điều kiện sign-off nếu có). Tham khảo `docs/devops/DEPLOY-adb-launch-app-api.md` (plan mẫu) cho quy trình build container + smoke test qua curl.

## Nhiệm vụ
Deploy container/staging, chạy smoke test cơ bản qua curl (401 khi thiếu key, 400 input sai, response format đúng) cho cả 2 endpoint mới. Viết `docs/devops/DEPLOY-adb-add-device-api.md`.

## Definition of Done
- [ ] Container build OK
- [ ] Smoke test curl PASS cho case validate được mà không cần thiết bị thật
- [ ] Ghi rõ GATE CHƯA ĐÓNG nếu còn case cần thiết bị Android thật (theo điều kiện từ STEP-4.2)
- [ ] Xuất DOCX/PDF theo R1

## Đã làm

- Xác nhận container hiện tại (port 3339) KHÔNG có endpoint mới (trả 404 — cần rebuild).
- Build image mới `kztek-adb-tool-test:add-device-api` từ commit 8db0d72 (có `DeviceConnectionEndpoints.cs`). Build SUCCESS.
- Chạy smoke test container port 18081, env `LaunchApp__ApiKey=smoketest-key-temp`.
- Health check: `{"ok":true,"adbVersion":"Android Debug Bridge version 1.0.41"}` — PASS.
- Smoke test NHÓM A: 7/7 PASS (401 no-key, 401 wrong-key, 400 missing-ip, 400 empty-ip cho connect-by-ip; 401 no-key, 401 wrong-key, 404 serial-not-found cho status endpoint).
- Cleanup container + image test — không để rác.
- Viết `docs/devops/DEPLOY-adb-add-device-api.md` + xuất DOCX (PDF skip — thiếu converter, giống các bước trước).
- Commit ea29093 + push lên `docker-deploy`.

## Artifact

- `docs/devops/DEPLOY-adb-add-device-api.md` — Deploy doc đầy đủ
- `docs/devops/DEPLOY-adb-add-device-api.docx` — Xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng

- Rebuild image bắt buộc — container cũ (port 3339) không có DeviceConnectionEndpoints.cs.
- NHÓM B (TC-C01–TC-C06) GATE CHƯA ĐÓNG: môi trường sandbox không có thiết bị Android thật — user PHẢI tự smoke test trước go-live production.
- PDF skip do thiếu converter (xelatex/LibreOffice/docx2pdf) — nhất quán với STEP-4.1/4.2 trước đó, không block workflow.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không rebuild image hay chạy lại NHÓM A — đã PASS 7/7. Không xóa DEPLOY-adb-add-device-api.md.
- watch_out: NHÓM B (TC-C01–TC-C06) là GATE BẮT BUỘC trước production. Nếu môi trường STEP-4.4 cũng không có thiết bị Android thật → ghi rõ điều đó, hướng dẫn user thực hiện thủ công trước khi approve production — KHÔNG tự approve production khi gate chưa đóng.
- next_inputs: `docs/devops/DEPLOY-adb-add-device-api.md` mục 5 (danh sách 6 case NHÓM B + hướng dẫn thủ công), mục 2 (Deploy Checklist Production — điền các ô còn ⬜). DevOps Lead xác nhận staging PASS và điền mục 6 (DevOps Lead Approval) vào DEPLOY doc trước khi approve production.

## Commit
- Hash: ea29093
- Đã push: Yes (docker-deploy → origin)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
