---
step: 4.4
plan: ../PLAN-MASTER.md
agent: DevOps Lead
status: done
completed_at: 2026-08-19
deps: [4.3]
---

# STEP 4.4 — Approve staging, verify smoke test, approve production

## Input nhận
Nhận Handoff Payload từ STEP-4.3.

## Nhiệm vụ
Approve staging nếu evidence đủ. KHÔNG tự approve production nếu còn gate chưa đóng (case cần thiết bị Android thật) — chuyển giao user tự smoke test thủ công trước go-live production, giống quyết định ở plan mẫu `adb-launch-app-api`.

## Definition of Done
- [ ] Quyết định staging approve rõ ràng
- [ ] Nếu còn gate chưa đóng → ghi rõ hành động user cần làm trước khi go-live production
- [ ] Cập nhật PLAN-MASTER.md `status: done` nếu toàn bộ chain hoàn thành (hoặc `status: staging-approved, pending user go-live` nếu còn gate)

## Đã làm

1. Đọc Handoff Payload STEP-4.3: container build OK từ commit 8db0d72, 7/7 NHÓM A PASS, health check PASS. Gate NHÓM B (6 case thiết bị Android thật) chưa đóng.
2. Xét duyệt staging: đối chiếu 7 tiêu chí (PR review, unit test, security-audit-stride, QA sign-off, smoke test NHÓM A, image build, health check) — tất cả PASS hoặc PASS CÓ ĐIỀU KIỆN. Quyết định: APPROVE STAGING.
3. Xét duyệt production: gate NHÓM B chưa đóng, môi trường không có thiết bị Android thật — KHÔNG tự approve production. Ghi rõ checklist bàn giao user.
4. Điền mục 6 (DevOps Lead Approval) vào `docs/devops/DEPLOY-adb-add-device-api.md`: bảng 7 tiêu chí, quyết định staging APPROVED, quyết định production chờ user, hành động bắt buộc trước go-live.
5. Cập nhật Deploy Checklist (mục 2): đánh dấu staging approve ✅.
6. Cập nhật Lịch sử deploy (mục 9): thêm dòng DevOps Lead approve staging.
7. Đánh dấu STEP-4.4 done, cập nhật PLAN-MASTER.md status toàn bộ chain hoàn thành.

## Artifact

- `docs/devops/DEPLOY-adb-add-device-api.md` — mục 6 (DevOps Lead Approval) đã điền, checklist staging ✅, lịch sử deploy cập nhật

## Quyết định quan trọng

1. **STAGING: APPROVED** — evidence đủ (7/7 NHÓM A PASS, QA sign-off P0=0/P1=0, security-audit PASS, image build OK).
2. **PRODUCTION: KHÔNG tự approve** — gate NHÓM B (TC-C01–TC-C06) chưa đóng vì môi trường sandbox không có thiết bị Android thật. Đây là NGOẠI LỆ đã ghi trong DEPLOY doc mục 5.
3. **Checklist bàn giao user** (thực hiện theo thứ tự trước go-live production):
   - Bước 1: Xác nhận/cập nhật API key thật trong docker-compose.yml hoặc deployment config (không hardcode key production vào git).
   - Bước 2: Build lại image chính thức trên hạ tầng thật.
   - Bước 3: Chạy TC-C01–TC-C06 với thiết bị Android thật — cả 6 PASS mới go-live.
   - Bước 4: Go-live production, thông báo team (#deploys), standby monitor 30 phút.
   - Bước 5: Nếu có case FAIL → báo Tech Lead trước khi deploy.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Toàn bộ 10 bước agent chain đã hoàn thành. Không cần chạy lại bất kỳ bước nào trong agent workflow.
- watch_out: Gate NHÓM B (TC-C01–TC-C06) chưa đóng — đây là điều kiện bắt buộc trước go-live production. User KHÔNG được deploy production khi gate chưa đóng.
- next_inputs: User tự thực hiện: (1) cập nhật API key thật, (2) build image, (3) chạy TC-C01–TC-C06 với thiết bị Android thật, (4) nếu PASS mới go-live production. Xem `docs/devops/DEPLOY-adb-add-device-api.md` mục 8 (Bàn giao) cho hướng dẫn chi tiết.

## Commit
- Hash:
- Đã push:

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
