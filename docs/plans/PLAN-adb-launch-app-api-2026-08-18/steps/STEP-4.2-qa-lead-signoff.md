---
step: "4.2"
plan: ../PLAN-MASTER.md
agent: qa-lead
status: done
completed_at: 2026-08-18 17:07
deps: ["4.1"]
---

# STEP 4.2 — QA Lead: Sign-off chất lượng (P1)

## Input nhận
Output từ Bước 4.1 (QA Engineer): test plan, test cases, kết quả thực thi, bug reports (nếu có).

## Nhiệm vụ
Review kết quả test của QA Engineer, xác nhận không còn P0/P1 bug chưa fix. Sign-off hoặc VETO nếu còn bug nghiêm trọng. P1 feature — sign-off bắt buộc (CLAUDE.md §4 WF-BUGFIX Bước 5 điều kiện P1).

## Definition of Done
- [ ] Đã đọc toàn bộ test cases + kết quả từ Bước 4.1
- [ ] Xác nhận coverage AC từ User Story đủ (tất cả AC được test)
- [ ] Xác nhận không còn P0/P1 bug open
- [ ] Sign-off ghi nhận trong file `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` (mục QA Lead sign-off) hoặc comment PR
- [ ] Nếu còn P0/P1 bug → VETO, BLOCK bước deploy, yêu cầu fix trước

## Đã làm
- Đọc toàn bộ `docs/test-cases/TC-adb-launch-app-api.md` và `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` từ STEP-4.1.
- Đối chiếu coverage với 8 AC (SC-01 đến SC-08) trong `docs/user-stories/US-adb-launch-app-api.md`: 8/8 SC đã có test case tương ứng — NHÓM A (4 case HTTP thật PASS), NHÓM B (3 case unit test gián tiếp Pass), SC-07 regression PASS, SC-08 partial/conditional.
- Xác nhận không có P0/P1 bug open (0 bug phát hiện trong toàn bộ session test của QA Engineer).
- Ra quyết định SIGN-OFF CÓ ĐIỀU KIỆN và ghi nhận trong `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` mục 10.
- Chạy `md_to_docx_kztek.py` — DOCX xuất thành công, PDF thất bại do thiếu xelatex (đã ghi nhận, không block workflow).

## Artifact
- `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` — đã thêm mục 10 (QA Lead Sign-off với bảng coverage 8 SC, trạng thái bug, quyết định + điều kiện cụ thể)
- `docs/test-plans/TEST-PLAN-adb-launch-app-api.docx` — xuất thành công

## Quyết định quan trọng
**SIGN-OFF CÓ ĐIỀU KIỆN** — KHÔNG VETO, cho phép tiến hành STEP-4.3 (DevOps deploy staging).

Lý do sign-off có điều kiện (không sign-off vô điều kiện): TC-001 (200 OK), TC-006 (422 device offline), TC-007 (422 app not installed) chỉ được verify gián tiếp qua unit test — chưa verify qua HTTP thật với thiết bị Android thật/emulator do giới hạn môi trường sandbox.

**Điều kiện BẮT BUỘC trước khi approve production (STEP-4.4):**
DevOps Engineer (STEP-4.3) và DevOps Lead (STEP-4.4) PHẢI thực hiện smoke test thủ công trên thiết bị Android thật hoặc emulator tại staging cho 3 case tối thiểu: TC-001 (→ HTTP 200), TC-006 (→ HTTP 422 device offline), TC-007 (→ HTTP 422 app not installed). Nếu bất kỳ case nào fail → KHÔNG deploy production, báo QA Lead. SC-08 (env var override) cần verify khi deploy docker với `LaunchApp__ApiKey`.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã sign-off xong — KHÔNG cần đọc lại toàn bộ test case hay chạy lại unit test. KHÔNG cần xin thêm xác nhận QA Lead cho staging.
- watch_out: QUAN TRỌNG — STEP-4.3 và STEP-4.4: BẮT BUỘC smoke test thủ công trên thiết bị Android thật/emulator tại staging cho TC-001 (HTTP 200 happy path), TC-006 (HTTP 422 device offline), TC-007 (HTTP 422 app not installed) TRƯỚC KHI approve production. Nếu fail → dừng deploy production, báo QA Lead. SC-08: verify env var `LaunchApp__ApiKey` override khi docker run.
- next_inputs: `docs/test-plans/TEST-PLAN-adb-launch-app-api.md` mục 10 (điều kiện đầy đủ), `docs/tech-design/TDD-adb-launch-app-api.md` (API contract, cấu hình deploy), `docs/planning/SPRINT-adb-launch-app-api.md` (task board), `docs/prd/PRD-adb-launch-app-api.md` (context feature).

## Commit
- Hash: [điền sau khi commit]
- Đã push: [điền sau khi push]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
