---
step: 3.2
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: todo
completed_at:
deps: [3.1]
---

# STEP 3.2 — Code review, security-audit-stride, quyết định merge

## Input nhận
Nhận Handoff Payload từ STEP-3.1. Code đã push tại commit hash ghi trong STEP-3.1.

## Nhiệm vụ
1. Review code review checklist chuẩn (correctness, security, perf, đúng TDD).
2. Chạy `/verify-pr` (`.claude/commands/verify-pr.md`) — build/lint/test/security note/diff review — chỉ review khi report toàn PASS.
3. Chạy skill `security-audit-stride` (BẮT BUỘC — route mới đụng lại cơ chế kết nối thiết bị + auth, dù tái dùng auth cũ) — OWASP Top 10 + STRIDE.
4. Quyết định APPROVE merge hoặc REQUEST-CHANGES. Nếu Fail nhóm rủi ro cao → BLOCK merge.

## Definition of Done
- [ ] VERIFICATION REPORT toàn PASS
- [ ] security-audit-stride chạy xong, ghi kết quả OWASP/STRIDE
- [ ] Quyết định merge rõ ràng (APPROVE / REQUEST-CHANGES), có escalate CTO hay không

## Đã làm


## Artifact


## Quyết định quan trọng


## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo:
- watch_out:
- next_inputs:

## Commit
- Hash:
- Đã push:

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
