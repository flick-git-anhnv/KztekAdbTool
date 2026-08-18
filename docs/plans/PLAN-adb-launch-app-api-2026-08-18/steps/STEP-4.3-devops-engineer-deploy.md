---
step: "4.3"
plan: ../PLAN-MASTER.md
agent: devops-engineer
status: todo
completed_at:
deps: ["4.2"]
---

# STEP 4.3 — DevOps Engineer: Deploy

## Input nhận
Output từ Bước 4.2: QA Lead đã sign-off (không còn P0/P1 bug).

## Nhiệm vụ
Deploy bản mới lên môi trường tương ứng (staging hoặc production tuỳ DevOps Lead chỉ định). Đảm bảo biến môi trường `ApiKey` (hoặc tên config Tech Lead chỉ định trong TDD) được set đúng trong docker-compose/env file. Điền checklist deploy.

## Definition of Done
- [ ] File `docs/devops/DEPLOY-adb-launch-app-api.md` đã tạo với checklist deploy
- [ ] Biến môi trường chứa API key đã được set (không hardcode trong image)
- [ ] Docker image build + deploy thành công
- [ ] Smoke test cơ bản: gọi thử API với API key đúng → 200, API key sai → 401
- [ ] Đã xuất DOCX + PDF bằng `scripts/md_to_docx_kztek.py`

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
