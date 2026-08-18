---
step: "4.2"
plan: ../PLAN-MASTER.md
agent: qa-lead
status: todo
completed_at:
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
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành — đặc biệt: có VETO không, lý do]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
