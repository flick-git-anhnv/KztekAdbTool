---
step: 3.3
plan: ../PLAN-MASTER.md
agent: ux-ui-reviewer
status: todo
completed_at:
deps: [3.2]
---

# STEP 3.3 — UX/UI Reviewer: Kiểm tra Trực quan Checkbox Mới trên Dashboard

## Input nhận
- Code đã merge từ bước 3.2
- TDD (vị trí checkbox, label, style): `docs/tech-design/TDD-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-3.2 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Chạy app thật (server KztekAdbPublishTool.Web), mở Dashboard, chụp screenshot màn hình có checkbox "Uninstall Before Install" mới, đánh giá 7 tiêu chí C1–C7. Ghi kết quả vào `docs/ux-review/UX-REVIEW-adb-uninstall-before-install.md`.

## Definition of Done
- [ ] App chạy thật, Dashboard mở thành công
- [ ] Chụp ≥ 1 screenshot thực tế có checkbox mới (lưu vào `docs/ux-review/screenshots/`)
- [ ] Đánh giá C1–C7 đầy đủ: C1 (nhận diện label), C2 (vị trí nhất quán với toolbar), C3 (visual style khớp `chk-auto-detect`), C4 (default state đúng — tắt), C5 (không che khuất element khác), C6 (responsive/mobile-friendly nếu áp dụng), C7 (accessible — label liên kết đúng với input)
- [ ] `docs/ux-review/UX-REVIEW-adb-uninstall-before-install.md` đã tạo
- [ ] `docs/ux-review/UX-REVIEW-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Kết luận PASS (hoặc FAIL kèm danh sách vấn đề cụ thể)
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
