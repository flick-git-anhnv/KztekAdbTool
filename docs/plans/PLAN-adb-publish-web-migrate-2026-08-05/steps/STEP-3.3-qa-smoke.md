---
step: 3.3
title: QA smoke test + verify behavior parity với WinForms gốc
assignee: qa-engineer
status: todo
completed_at: —
deps: [3.2]
---

## Nhiệm vụ
Chạy song song WinForms gốc + Web trên cùng máy Windows với 2-3 device thật WiFi. Với 12 luồng ở 3.1, so sánh:
- Kết quả cuối (status device, version, thời gian cài).
- Log message (nội dung tiếng Việt, dấu chấm câu).
- Thời gian phản hồi (Web ≤ WinForms + 2s).

Test case → `docs/test-cases/TC-adb-publish-web-migrate.md`, evidence screenshot ≥ 5 luồng chính.

## Definition of Done
- [ ] 12/12 TC Pass, không P0/P1 open.
- [ ] Screenshot đối chiếu ≥ 5 luồng.
- [ ] QA Lead sign-off.

## Artifact
- `docs/test-cases/TC-adb-publish-web-migrate.md`, `docs/test-cases/screenshots/*.png`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
