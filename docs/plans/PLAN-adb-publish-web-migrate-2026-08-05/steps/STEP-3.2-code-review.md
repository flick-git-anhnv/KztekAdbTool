---
step: 3.2
title: Code Migrator review (Opus)
assignee: code-migrator
status: todo
completed_at: —
deps: [3.1]
---

## Nhiệm vụ
Review `src/KztekAdbPublishTool.Web/`:
1. **Correctness:** logic Install (before/install/after/launch), Filter, Select-All, Toggle timer còn đúng semantics gốc?
2. **Behavior parity:** đối chiếu ADR-001 §4.3–§4.4 — mỗi event có handler tương đương? Message tiếng Việt giữ nguyên?
3. **Security:** APK upload validate extension + không path traversal; adb command không nhận input chưa validate (serial regex); SignalR không leak nhạy cảm.
4. **Style:** DI đúng scope, async không `.Result`/`.Wait()`, không nuốt exception.
5. Rà csproj + using → KHÔNG còn `System.Windows.Forms`, `KztekComponent`, `Guna.UI2`.

## Definition of Done
- [ ] `_workspace/04_code_review.md` liệt kê phát hiện + severity (Blocker/Major/Minor).
- [ ] Blocker/Major fix trước khi sang QA.

## Artifact
- `_workspace/04_code_review.md`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
