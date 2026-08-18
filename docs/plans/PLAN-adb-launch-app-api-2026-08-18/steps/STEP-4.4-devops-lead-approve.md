---
step: "4.4"
plan: ../PLAN-MASTER.md
agent: devops-lead
status: todo
completed_at:
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
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
Không có

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
