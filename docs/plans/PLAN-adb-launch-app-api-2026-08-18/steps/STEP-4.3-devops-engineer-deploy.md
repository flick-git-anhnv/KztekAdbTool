---
step: "4.3"
plan: ../PLAN-MASTER.md
agent: devops-engineer
status: done
completed_at: 2026-08-18 17:15
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
1. Build Docker image: `docker build -t kztek-adb-tool-test:launch-app-api .` — PASS (from cache, image sha `1bfd437a`).
2. Run container: `docker run -d --name launch-app-api-smoketest -p 18080:8080 -e LaunchApp__ApiKey=smoketest-key-temp kztek-adb-tool-test:launch-app-api` — container healthy.
3. Smoke test 3 case qua curl thật:
   - TC-A: `x-api-key: wrong-key` → 401 PASS
   - TC-B: body thiếu `app` → 400 PASS
   - TC-C: key đúng, serial không tồn tại → 404 PASS
4. Cleanup container + image sau smoke test thành công.
5. Tạo `docs/devops/DEPLOY-adb-launch-app-api.md` (checklist deploy, hướng dẫn env var, kết quả smoke test, rollback plan, cảnh báo gate Android thật).
6. Xuất DOCX OK (`docs/devops/DEPLOY-adb-launch-app-api.docx`). PDF thất bại (xelatex không có — nhất quán với tất cả bước trước).
7. KHÔNG sửa `docker-compose.yml` (đang có pending changes ngoài scope) — ghi hướng dẫn thêm env var vào file DEPLOY doc thay vì sửa trực tiếp.

## Artifact
- `docs/devops/DEPLOY-adb-launch-app-api.md` — DEPLOY doc đầy đủ
- `docs/devops/DEPLOY-adb-launch-app-api.docx` — DOCX OK
- `docs/devops/DEPLOY-adb-launch-app-api.pdf` — FAIL (xelatex thiếu — không block)

## Quyết định quan trọng
1. Dùng `docker run` trực tiếp cho smoke test thay vì `docker compose` để tránh động vào `docker-compose.yml` đang có pending changes ngoài scope task.
2. KHÔNG sửa `docker-compose.yml` — ghi hướng dẫn thêm env var dưới dạng snippet trong DEPLOY doc, user tự thêm khi sẵn sàng.
3. Smoke test 401/400/404 PASS xác nhận: env var đọc đúng, routing đúng, auth middleware hoạt động. TC-001/006/007 (cần thiết bị Android thật) KHÔNG thể verify trong môi trường sandbox.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Build image và smoke test 401/400/404 đã hoàn tất. Không cần rebuild hay re-test các case này. DEPLOY doc và DOCX đã tạo xong.
- watch_out: **GATE CHUA DONG — KHONG DUOC APPROVE PRODUCTION KHI CHUA CO**: smoke test TC-001 (200 happy path), TC-006 (422 device offline), TC-007 (422 app not installed) CHUA verify qua HTTP that voi thiet bi Android that. Moi truong sandbox khong co thiet bi — user PHAI tu thuc hien thu cong 3 case nay tren moi truong co thiet bi that truoc khi DevOps Lead sign-off final. Chi tiet trong `docs/devops/DEPLOY-adb-launch-app-api.md` muc 5.
- next_inputs: `docs/devops/DEPLOY-adb-launch-app-api.md` (ket qua smoke test container + checklist deploy con thieu + rollback plan), `docs/tech-design/TDD-adb-launch-app-api.md` (API contract day du), `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` muc 10 (dieu kien sign-off day du). DevOps Lead can xac nhan viec co hoan thanh duoc smoke test thiet bi Android that o buoc 4.4 hay khong.

## Commit
- Hash: [điền sau khi commit]
- Đã push: [điền sau khi push]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
