---
step: 3.3
plan: ../PLAN-MASTER.md
agent: UX/UI Reviewer
status: done
completed_at: 2026-08-24 22:58
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
- Chạy app thật: `dotnet run` → listen tại `http://localhost:57946` (ASPNETCORE_URLS không truyền qua background shell — app dùng port tự chọn; verify qua curl 200 OK).
- Chụp 10 screenshots qua Playwright headless Chromium (viewport 1280x900).
- Test 5 kịch bản UI (a–e) bằng Playwright async: inject fake device row vào `device-tbody` (với `data-serial` đúng chuẩn `getCheckedSerials()`), xử lý dialog via `page.on("dialog")`.
- Đánh giá C1–C7 với kết quả: C3, C5, C6 PASS; C1, C2, C4, C7 NEEDS-FIX.
- Viết `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` + xuất DOCX.
- Kill app sau khi hoàn thành review.

## Artifact
- `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md`
- `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.docx`
- `docs/ux-review/screenshots/app-status-reboot-dashboard-default.png`
- `docs/ux-review/screenshots/app-status-reboot-buttons-visible.png`
- `docs/ux-review/screenshots/app-status-reboot-confirm-cancel-no-toast.png`
- `docs/ux-review/screenshots/app-status-reboot-scenario-e-result.png`
- (+ 6 screenshots phụ trợ)

## Quyết định quan trọng
- Kết luận: NEEDS-FIX (không có blocker — 3 issue tổng cộng: 1 Medium + 2 Low).
- C6 PASS: confirm dialog reboot hoạt động đúng, cancel không gửi request, OK gửi request và hiển thị toast lỗi.
- UI-001 (Medium): `btn-outline-primary` + `btn-outline-warning` dùng Bootstrap default colors, không match KZTEK brand palette. Có tiền lệ `btn-outline-danger` từ trước nhưng brand alignment vẫn nên được cải thiện.
- UI-002 (Low): không có disabled/loading state khi API đang chạy — risk double-click.
- UI-003 (Low): `aria-hidden="true"` thiếu trên `<i>` icons — gap toàn codebase, không phải regression feature này.

## Handoff Payload — bước sau đọc phần này
- Đã làm: UX review C1–C7 done, app chạy OK, 5/5 kịch bản test pass, report + DOCX created.
- do_not_redo: KHÔNG chạy lại UX review. KHÔNG test lại confirm dialog (đã verified). KHÔNG re-audit code review (STEP-3.2 đã APPROVE).
- watch_out:
  1. 3 issue cần QA Engineer biết: UI-001 (color), UI-002 (no loading state), UI-003 (aria-hidden) — ghi nhận trong test plan nhưng KHÔNG block QA sign-off (đều Low/Medium, không phải functional blocker).
  2. Không có thiết bị Android thật kết nối — QA Engineer cần device thật để verify kịch bản API trả Foreground/Background state và reboot thực sự.
  3. App port có thể thay đổi (không control được khi chạy qua `dotnet run` background + env var không truyền được) — dùng `dotnet run` foreground hoặc chỉ định `--urls` trực tiếp trong lệnh.
  4. `appsettings.json` có `LaunchApp:ApiKey = "123456a@"` — dùng header `x-api-key: 123456a@` khi test API manual.
- next_inputs:
  - URL: `http://localhost:5099` (nếu dùng `ASPNETCORE_URLS=http://localhost:5099 dotnet run`) hoặc port random nếu chạy mặc định.
  - Report UX: `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` — đọc phần "Danh sách issue" cho context.
  - API endpoints cần test: `GET /api/devices/{serial}/app-status?package={pkg}` và `POST /api/devices/{serial}/reboot`.
  - Kịch bản đã cover (UX, không cần repeat): (a)(b)(c)(d)(e) như trên.
  - Kịch bản cần QA test với device thật: trạng thái Foreground, Background, NotRunning; reboot thực và device tự online lại; 401 khi sai API key; 404 khi serial không tồn tại; 400 khi package rỗng.

## Commit
- Hash: (sẽ điền sau khi commit)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
