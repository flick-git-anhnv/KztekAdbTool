---
step: 3.2
plan: ../PLAN-MASTER.md
agent: tech-lead
status: todo
completed_at:
deps: [3.1]
---

# STEP 3.2 — Code Review và Quyết định Merge

## Input nhận
- Code đã commit từ bước 3.1
- TDD: `docs/tech-design/TDD-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-3.1 (đọc mục "Handoff Payload" trong step file đó — quan trọng: commit hash, các file đã sửa)

## Nhiệm vụ
Review toàn bộ code thay đổi từ bước 3.1: kiểm tra correctness của logic uninstall-before-install, xử lý lỗi graceful (package chưa cài), unit test coverage, UI checkbox đúng vị trí/style, JS truyền đúng flag. Quyết định APPROVE merge hoặc REQUEST CHANGES.

## Definition of Done
- [ ] Review checklist đầy đủ: correctness, error handling graceful, test coverage, UI/style, JS payload
- [ ] Build verify: build sạch, tất cả test pass sau review
- [ ] Quyết định APPROVE (hoặc REQUEST CHANGES với danh sách cụ thể) được ghi rõ trong step file
- [ ] Nếu APPROVE: merge code vào nhánh chính
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
