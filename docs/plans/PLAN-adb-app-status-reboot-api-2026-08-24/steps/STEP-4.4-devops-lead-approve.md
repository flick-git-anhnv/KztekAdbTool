---
step: 4.4
plan: ../PLAN-MASTER.md
agent: DevOps Lead
status: done
completed_at: 2026-08-24 23:33
deps: ["4.3"]
---

# STEP 4.4 — Approve staging, verify smoke test, approve + deploy production

## Input nhận
Từ Bước 4.3 Handoff Payload — `docs/devops/DEPLOY-adb-app-status-reboot-api.md`, kết quả smoke test staging, danh sách GATE còn mở (nếu có TC cần thiết bị Android thật).

## Nhiệm vụ
- Review kết quả deploy staging từ DevOps Engineer
- Verify evidence: image build log, curl output, UI test
- Quyết định APPROVE staging hay yêu cầu re-deploy
- Nếu QA Lead sign-off có điều kiện (cần thiết bị thật): KHÔNG tự approve production — chuyển giao user để tự smoke test TC-A01/B01 với thiết bị Android thật
- Nếu tất cả gate đã đóng: approve + deploy production, monitor

Ghi quyết định rõ trong step file này. KHÔNG tự deploy production khi còn GATE CHƯA ĐÓNG — chuyển giao user với hướng dẫn cụ thể.

## Definition of Done
- [ ] Đánh giá evidence staging đầy đủ
- [ ] APPROVE STAGING hoặc yêu cầu re-deploy ghi rõ lý do
- [ ] Nếu deploy production: ghi nhận thời điểm go-live, bắt đầu monitor
- [ ] Nếu không deploy production: hướng dẫn user cụ thể các bước còn lại
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 4.4 → ✅, PLAN-MASTER status → done

## Đã làm
- Verify độc lập container `fa43f4598d9a` (image `be8c5b3b6a74`) — Up 4 phút (healthy), port `0.0.0.0:3339->8080/tcp`.
- `curl http://localhost:3339/health` → HTTP 200.
- Spot-check 3 TC: app-status no key → 401, reboot wrong key → 401, app-status missing package → 400. Tất cả PASS đúng kỳ vọng.
- Thêm section "8. DevOps Lead Approval" vào `docs/devops/DEPLOY-adb-app-status-reboot-api.md`: bảng verify độc lập, quyết định APPROVE STAGING + CONDITIONAL HOLD production, hướng dẫn đóng gate TC-A01/TC-B01 với lệnh curl cụ thể, monitor note.
- Cập nhật Deploy Checklist mục 2 (DevOps Lead approve → `[x]`).
- Xuất lại DOCX: `docs/devops/DEPLOY-adb-app-status-reboot-api.docx`.

## Artifact
- `docs/devops/DEPLOY-adb-app-status-reboot-api.md` — đã thêm section 8 (DevOps Lead Approval)
- `docs/devops/DEPLOY-adb-app-status-reboot-api.docx` — re-exported

## Quyết định quan trọng
1. **APPROVE STAGING**: container healthy, smoke test 7/7 pass, spot-check verify pass — môi trường staging docker nội bộ được coi là ổn định.
2. **PRODUCTION GO-LIVE: CONDITIONAL HOLD**: 2 gate chưa đóng (TC-A01 app-status thiết bị thật, TC-B01 reboot thiết bị staging thật). Giữ đúng điều kiện QA Lead đặt ra ở Bước 4.2. KHÔNG deploy production khi chưa pass 2 TC này.
3. Không rollback — không có vấn đề gì phát hiện trong quá trình verify.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Không verify lại các TC 1-7 đã pass ở Bước 4.3. Không redeploy container — image hiện tại đã ổn định.
- watch_out: TC-B01 là destructive (reboot thiết bị) — CHỈ chạy trên thiết bị staging, KHÔNG chạy trên thiết bị đang phục vụ production. Sau khi thiết bị reboot, `adb devices` sẽ cần vài giây để thiết bị reconnect.
- next_inputs: Khi có thiết bị Android staging, chạy TC-A01 và TC-B01 theo lệnh cụ thể trong `docs/devops/DEPLOY-adb-app-status-reboot-api.md` mục 8.3. Sau khi 2 TC pass: cập nhật bảng mục 4.4 (TC-A01/TC-B01 → PASS), thông báo DevOps Lead, cập nhật Deploy Checklist mục 2 (gate đóng) — khi đó production go-live có thể tiến hành.

## Commit
- Hash: (sẽ điền sau commit)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
