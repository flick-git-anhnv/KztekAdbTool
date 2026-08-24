---
step: 3.3
plan: ../PLAN-MASTER.md
agent: UX/UI Reviewer
status: todo
completed_at: ~
deps: ["3.2"]
---

# STEP 3.3 — UX/UI Reviewer: chạy app thật, chụp screenshot, đánh giá C1–C7

## Input nhận
Từ Bước 3.2 Handoff Payload — code đã được Tech Lead APPROVE, commit hash, URL ứng dụng đang chạy (local hoặc staging).

**Lý do bước này BẮT BUỘC:** Feature thêm 2 nút bấm mới vào dashboard UI — theo CLAUDE.md §3.5 và §4 WF-FEATURE Bước 10b, UX/UI Reviewer phải chạy app thật và đánh giá trước khi QA test.

## Nhiệm vụ

### 1. Chạy ứng dụng thật:
- `dotnet run` hoặc `docker compose up` trong `src/KztekAdbPublishTool.Web`
- Mở browser → dashboard URL

### 2. Chụp screenshot:
- Màn hình dashboard hiển thị 2 nút bấm mới ("Check App Status" + "Reboot Device")
- Dialog/input nhập package name (khi click "Check App Status")
- Confirm dialog khi click "Reboot Device"
- Kết quả hiển thị sau khi API trả về (toast/panel)
- Lưu vào `docs/ux-review/screenshots/` với tên rõ ràng

### 3. Đánh giá 7 tiêu chí (C1–C7):
- **C1 — Nhất quán thương hiệu KZTEK:** màu Navy #251C53, Cam #F05922, font đúng brand?
- **C2 — Nhất quán UI hiện có:** nút mới có cùng style/kích thước với nút hiện có trong action panel?
- **C3 — Rõ ràng, dễ hiểu:** label nút ("Check App Status", "Reboot Device") rõ ý nghĩa với operator?
- **C4 — Phản hồi người dùng:** có loading state khi API đang gọi? Có hiển thị kết quả rõ ràng?
- **C5 — Xử lý lỗi trên UI:** khi API trả lỗi (400/404/500), UI có thông báo rõ không hay im lặng?
- **C6 — Phòng ngừa thao tác nguy hiểm:** Nút "Reboot Device" có confirm dialog không? Nút có disabled khi đang xử lý?
- **C7 — Accessibility cơ bản:** có aria-label, tab order hợp lý không?

### 4. Viết báo cáo:
- `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` — đánh giá từng tiêu chí + Pass/Fail/Needs-Fix
- Xuất DOCX

## Definition of Done
- [ ] Chạy app thật thành công (không crash)
- [ ] Screenshots chụp đủ các state (normal / dialog / result / error)
- [ ] Đánh giá đủ 7 tiêu chí C1–C7 với kết luận Pass/Fail/Needs-Fix
- [ ] `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` tạo xong + DOCX xuất
- [ ] Nếu có Needs-Fix → mô tả rõ vấn đề để Senior Developer sửa (có thể yêu cầu quay lại 3.1)
- [ ] Commit + push
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 3.3 → ✅

## Đã làm
(để trống)

## Artifact
(để trống)

## Quyết định quan trọng
(để trống)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
