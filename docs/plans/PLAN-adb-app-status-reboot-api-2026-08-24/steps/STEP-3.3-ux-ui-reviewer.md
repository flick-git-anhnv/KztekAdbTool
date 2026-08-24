---
step: 3.3
plan: ../PLAN-MASTER.md
agent: UX/UI Reviewer
status: done
completed_at: 2026-08-24 23:09 (re-check)
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

### Re-check (2026-08-24 23:09) — verify fix commit 23838dc
- Đọc `git show 23838dc` — xác nhận đúng scope 3 fix (Index.cshtml, dashboard.css, dashboard.js).
- Chạy lại app: `dotnet run --urls http://localhost:5099` (HTTP 200 OK).
- Playwright Python: `getComputedStyle` đo màu trực tiếp từ DOM thật:
  - UI-001 RESOLVED: `btn-check-app-status` → `color: rgb(37,28,83)` = #251C53, `borderColor: rgb(37,28,83)`. `btn-reboot-device` → `color: rgb(240,89,34)` = #F05922, `borderColor: rgb(240,89,34)`.
  - UI-002 RESOLVED: `disabled = false` ở trạng thái nghỉ (đúng); code diff xác nhận `disabled=true` + spinner trước fetch + restore trong `finally`.
  - UI-003 RESOLVED: DOM evaluate → `checkIconAriaHidden = "true"`, `rebootIconAriaHidden = "true"`.
- Chụp 4 screenshots mới vào `docs/ux-review/screenshots/2026-08-24/app-status-reboot-fixed-*.png`.
- Cập nhật report: thêm section "Re-check sau fix", đổi kết quả tổng quan → PASS.
- Kill app sau khi xong.

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
- Đã làm: UX review C1–C7 done, app chạy OK, 5/5 kịch bản test pass, report + DOCX created. Re-check sau fix commit 23838dc: UI-001/002/003 RESOLVED, kết luận tổng quan PASS.
- do_not_redo: KHÔNG chạy lại UX review. KHÔNG test lại confirm dialog (đã verified). KHÔNG re-audit code review (STEP-3.2 đã APPROVE). KHÔNG verify lại brand color hay aria-hidden — đã đo bằng computed style thật.
- watch_out:
  1. UI-001/002/003 đã RESOLVED — QA không cần test lại các issue UI này.
  2. Không có thiết bị Android thật kết nối — QA Engineer cần device thật để verify kịch bản API trả Foreground/Background state và reboot thực sự.
  3. App port: dùng `dotnet run --urls http://localhost:5099` để tránh port random.
  4. `appsettings.json` có `LaunchApp:ApiKey = "123456a@"` — dùng header `x-api-key: 123456a@` khi test API manual.
- next_inputs:
  - Kết luận UXR: **PASS** — UX sign-off hoàn tất, QA có thể tiến hành STEP-4.1.
  - URL: `http://localhost:5099` (dùng `dotnet run --urls http://localhost:5099`).
  - Report UX: `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` — phần "Re-check sau fix" cho đầy đủ context.
  - API endpoints cần test: `GET /api/devices/{serial}/app-status?package={pkg}` và `POST /api/devices/{serial}/reboot`.
  - Kịch bản đã cover (UX, không cần repeat): (a)(b)(c)(d)(e) như trên.
  - Kịch bản cần QA test với device thật: trạng thái Foreground, Background, NotRunning; reboot thực và device tự online lại; 401 khi sai API key; 404 khi serial không tồn tại; 400 khi package rỗng.

## Commit
- Hash vòng đầu: fb279b9
- Re-check commit: cdb95ee

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
