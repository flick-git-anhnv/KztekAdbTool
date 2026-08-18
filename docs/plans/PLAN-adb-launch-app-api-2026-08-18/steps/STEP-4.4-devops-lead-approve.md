---
step: "4.4"
plan: ../PLAN-MASTER.md
agent: devops-lead
status: done
completed_at: 2026-08-18 17:20
deps: ["4.3"]
---

# STEP 4.4 — DevOps Lead: Approve staging + deploy production

## Input nhận
Output từ Bước 4.3 (DevOps Engineer): deploy checklist, smoke test kết quả, môi trường đang chạy.

## Nhiệm vụ
Verify smoke test kết quả từ DevOps Engineer, approve staging, sau đó approve và deploy production (nếu staging ok). Monitor sau deploy.

## Definition of Done
- [ ] Đã verify smoke test kết quả từ Bước 4.3 (API key đúng → 200, sai → 401)
- [ ] Approve staging: ghi nhận trong `docs/devops/DEPLOY-adb-launch-app-api.md`
- [ ] Deploy production: confirm thành công
- [ ] Monitor post-deploy: không có lỗi bất thường trong log
- [ ] Ghi chú kết quả approve + deploy production vào DEPLOY doc

## Đã làm

1. Đọc `docs/devops/DEPLOY-adb-launch-app-api.md` và PLAN-MASTER để nắm toàn bộ evidence từ STEP-4.3.
2. Verify kết quả smoke test container (TC-A 401 / TC-B 400 / TC-C 404): PASS đủ tiêu chí staging.
3. Cập nhật DEPLOY doc: đổi frontmatter `status` sang `staging-approved-pending-production`; thêm:
   - Mục 6: DevOps Lead Approval (bảng evidence staging + tuyên bố KHÔNG tự duyệt production)
   - Mục 8: Bàn giao cho User — 4 việc thủ công cần làm trước go-live
   - Mục 9: Lịch sử deploy (thêm dòng STEP-4.4)
   - Sửa Deploy Checklist (mục 2): đánh dấu staging APPROVED, production PENDING USER
4. Xuất DOCX thành công (`docs/devops/DEPLOY-adb-launch-app-api.docx`). PDF thất bại do thiếu xelatex (pattern đã biết từ các bước trước, không block).

## Artifact
- `docs/devops/DEPLOY-adb-launch-app-api.md` — cập nhật với DevOps Lead approval + bàn giao user
- `docs/devops/DEPLOY-adb-launch-app-api.docx` — xuất DOCX OK

## Quyết định quan trọng

**APPROVE STAGING (container-level):** Evidence từ STEP-4.3 đủ — build sạch, health check OK, 3 HTTP test case PASS, env var hoạt động đúng, 42/42 unit test PASS, security audit STRIDE 100% Pass.

**KHONG APPROVE PRODUCTION tu dong:** Điều kiện bắt buộc của QA Lead (TC-001/TC-006/TC-007 với thiết bị Android thật) chưa được thỏa mãn vì toàn bộ quá trình agent chạy trong sandbox không có thiết bị thật và không kết nối được tới production KZTEK. Đây là giới hạn môi trường, không phải lỗi kỹ thuật của code. Code hoàn toàn sẵn sàng về mặt kỹ thuật.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đây là bước cuối của agent chain — không có bước tiếp theo trong quy trình agent. KHONG chay lai bat ky docker command nao (da du evidence tu STEP-4.3).
- watch_out: User PHAI tu hoan tat 4 viec truoc khi go-live that: (1) them LaunchApp__ApiKey that vao docker-compose.yml / env — KHONG dung key tam; (2) them section LaunchApp vao appsettings.json thu cong; (3) build lai image tren ha tang that; (4) chay smoke test TC-001/TC-006/TC-007 voi thiet bi Android that — neu PASS moi deploy production.
- next_inputs: Không có — phần còn lại là hành động thủ công của user trên hạ tầng thật, ngoài phạm vi agent.

## Commit
- Hash: [điền sau khi commit]
- Đã push: [điền sau khi push]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
