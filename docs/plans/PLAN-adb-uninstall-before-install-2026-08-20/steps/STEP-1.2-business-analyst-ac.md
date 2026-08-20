---
step: 1.2
plan: ../PLAN-MASTER.md
agent: business-analyst
status: todo
completed_at:
deps: [1.1]
---

# STEP 1.2 — Viết User Story và AC Given/When/Then

## Input nhận
- PRD từ bước 1.1: `docs/prd/PRD-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-1.1 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Chi tiết hóa các acceptance criteria theo format Given/When/Then và viết user story đầy đủ cho tính năng "Uninstall Before Install". Bao phủ các scenario: checkbox bật/tắt, uninstall thành công, uninstall thất bại (package chưa cài — graceful skip), install tiếp sau uninstall, hành vi mặc định (tắt).

## Definition of Done
- [ ] `docs/user-stories/US-adb-uninstall-before-install.md` đã tạo
- [ ] Có ≥ 5 scenario Given/When/Then bao phủ: (1) checkbox tắt — hành vi mặc định, (2) checkbox bật + package đã cài — uninstall thành công rồi install, (3) checkbox bật + package chưa cài — log warning, vẫn install, (4) checkbox bật + install thành công sau uninstall, (5) trạng thái lưu giữa các lần load trang (nếu TDD quyết định lưu DB)
- [ ] `docs/user-stories/US-adb-uninstall-before-install.docx` đã xuất thành công
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
