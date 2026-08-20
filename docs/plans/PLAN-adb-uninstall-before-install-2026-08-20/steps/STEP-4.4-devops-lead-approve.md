---
step: 4.4
plan: ../PLAN-MASTER.md
agent: devops-lead
status: todo
completed_at:
deps: [4.3]
---

# STEP 4.4 — DevOps Lead: Approve Staging và Deploy Production

## Input nhận
- Deploy document từ bước 4.3: `docs/devops/DEPLOY-adb-uninstall-before-install.md`
- Smoke test results từ bước 4.3
- QA Lead sign-off từ bước 4.2
- Handoff Payload từ STEP-4.3 (đọc mục "Handoff Payload" trong step file đó — image hash, smoke test results)

## Nhiệm vụ
Review evidence deploy staging (image hash, smoke test checklist) từ DevOps Engineer. Approve staging. Nếu đủ điều kiện, approve và deploy production (hoặc hướng dẫn user tự deploy nếu production cần thao tác ngoài phạm vi agent). Đóng vòng lặp workflow.

## Definition of Done
- [ ] Review DEPLOY document và smoke test evidence từ bước 4.3
- [ ] APPROVE STAGING có hoặc không (ghi rõ lý do nếu không)
- [ ] Quyết định production rõ ràng: tự deploy production, hoặc chuyển giao user với hướng dẫn cụ thể
- [ ] PLAN-MASTER.md status cập nhật → done (toàn bộ task hoàn thành)
- [ ] Cập nhật step file này + PLAN-MASTER.md: bước 4.4 → ✅, status plan → done

## Đã làm
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có (bước cuối cùng)

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
