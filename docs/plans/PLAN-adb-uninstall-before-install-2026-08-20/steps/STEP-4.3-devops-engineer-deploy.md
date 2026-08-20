---
step: 4.3
plan: ../PLAN-MASTER.md
agent: devops-engineer
status: todo
completed_at:
deps: [4.2]
---

# STEP 4.3 — DevOps Engineer: Deploy

## Input nhận
- QA Lead sign-off từ bước 4.2
- Handoff Payload từ STEP-4.2 (đọc mục "Handoff Payload" trong step file đó — xác nhận SIGN-OFF trước khi deploy)

## Nhiệm vụ
Build và deploy ứng dụng lên môi trường staging (Docker container theo `docker-compose.yml` hiện tại). Thực hiện smoke test cơ bản để xác nhận UI checkbox hiển thị và luồng install hoạt động. Lập DEPLOY document.

## Definition of Done
- [ ] Build Docker image thành công (0 error)
- [ ] Container start lên bình thường, Dashboard truy cập được
- [ ] Smoke test: checkbox "Uninstall Before Install" hiển thị đúng trên UI
- [ ] Smoke test: gọi `POST /api/install` với `uninstallBeforeInstall: false` → hoạt động như cũ
- [ ] `docs/devops/DEPLOY-adb-uninstall-before-install.md` đã tạo với: image hash, smoke test checklist, env/config notes
- [ ] `docs/devops/DEPLOY-adb-uninstall-before-install.docx` xuất thành công
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
