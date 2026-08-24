# UX/UI Review Report — 2026-08-24

**App / Module:** KztekAdbPublishTool.Web — Feature: API Kiểm Tra Trạng Thái App + Reboot Thiết Bị
**Reviewer:** UX/UI Reviewer Agent
**Môi trường:** Local (Linux/WSL2) | Branch: `docker-deploy` | Commit: sau 3.2 APPROVE
**Tổng số màn hình review:** 1 (Dashboard `Pages/Index.cshtml` — section "Thao tác thiết bị")
**Kết quả tổng quan:** ~~NEEDS-FIX~~ **PASS** (sau re-check 2026-08-24)

---

## Tóm tắt phát hiện

| Mức độ | Số lượng |
|--------|---------|
| Critical (chặn release) | 0 |
| High (ảnh hưởng UX đáng kể) | 0 |
| Medium (khó chịu nhưng dùng được) | 1 |
| Low (polish / nice-to-have) | 2 |

---

## Phương pháp

App chạy thật tại `http://localhost:57946` (dotnet run). Screenshot qua Playwright headless Chromium (viewport 1280x900). Test kịch bản UI bằng Playwright async API.

Screenshots lưu tại: `docs/ux-review/screenshots/`

---

## Chi tiết từng màn hình

### Dashboard — Section "Thao tác thiết bị (1 thiết bị đã chọn)"

**Screenshots:**
- `screenshots/app-status-reboot-dashboard-default.png` — Dashboard tổng quan (0 thiết bị, nhìn thấy 2 nút mới)
- `screenshots/app-status-reboot-buttons-visible.png` — Zoom vào action panel 2 nút mới
- `screenshots/app-status-reboot-confirm-cancel-no-toast.png` — Sau khi Click Reboot + Cancel confirm (không toast)
- `screenshots/app-status-reboot-scenario-e-result.png` — Sau khi Reboot + OK confirm → API 404 → toast lỗi

| Tiêu chí | Kết quả | Ghi chú |
|---|---|---|
| C1 Màu sắc & Brand | NEEDS-FIX | Xem UI-001 |
| C2 Nhất quán UI | NEEDS-FIX | Xem UI-001 |
| C3 Rõ ràng, dễ hiểu | PASS | |
| C4 Phản hồi người dùng | NEEDS-FIX | Xem UI-002 |
| C5 Xử lý lỗi trên UI | PASS | |
| C6 Phòng ngừa thao tác nguy hiểm | PASS | |
| C7 Accessibility cơ bản | NEEDS-FIX | Xem UI-003 |

---

### Chi tiết đánh giá từng tiêu chí

#### C1 — Màu sắc & Brand KZTEK

**Kết quả: NEEDS-FIX (Medium)**

Palette KZTEK: Navy #251C53 (heading/accent), Cam #F05922 (CTA/highlight).

Qua CSS computed: `btn-outline-primary` → `rgb(13, 110, 253)` (Bootstrap blue), `btn-outline-warning` → `rgb(255, 193, 7)` (Bootstrap amber). Cả hai màu không nằm trong palette KZTEK.

Hiện trạng existing buttons:
- `btn-kz-primary` (Cam #F05922): Cài đặt, Kết nối — PASS
- `btn-kz-secondary` (Navy): Chọn tất cả, Quét mạng — PASS
- `btn-outline-danger` (Bootstrap red): Xóa thiết bị — đây là **precedent đã có** cho việc dùng Bootstrap semantic class cho destructive action

Hai nút mới dùng Bootstrap mặc định thay vì KZTEK brand. Tuy nhiên `btn-outline-danger` đã có tiền lệ từ trước, nên không phải vi phạm mới hoàn toàn — chỉ là brand alignment chưa hoàn chỉnh.

#### C2 — Nhất quán UI

**Kết quả: NEEDS-FIX (kết hợp UI-001 với C1)**

Size, padding, icon style (`btn-sm w-100 text-start`, Bootstrap Icons): nhất quán với tất cả nút khác — PASS.
Vị trí (dưới `<hr>` section "Thao tác thiết bị"): hợp lý, tạo phân cấp rõ giữa thao tác multi-device và single-device — PASS.
Màu sắc: như C1 trên.

#### C3 — Rõ ràng, dễ hiểu

**Kết quả: PASS**

- Label "Kiểm tra trạng thái app": rõ nghĩa, mô tả đúng hành động.
- Label "Khởi động lại thiết bị": rõ nghĩa, mô tả đúng hành động.
- Icon `bi-info-circle` (kiểm tra) và `bi-arrow-repeat` (reboot): phù hợp ngữ nghĩa.
- Section label "Thao tác thiết bị (1 thiết bị đã chọn)": đặt đúng kỳ vọng cho operator.
- Alert messages rõ ràng và có hướng dẫn cụ thể:
  - "Chọn 1 thiết bị (tick 1 dòng) trước khi kiểm tra." — PASS
  - "Chỉ chọn 1 thiết bị. Đang có N thiết bị được chọn." — PASS (code-verified)
  - "Nhập package name ở ô 'Gói cần theo dõi' trước khi kiểm tra." — PASS (code-verified)

#### C4 — Phản hồi người dùng

**Kết quả: NEEDS-FIX (Low)**

- Toast notification hoạt động đúng: `bg-danger` cho lỗi API, màu text rõ — PASS
- Log area ghi nhận đầy đủ: `[API] RebootDevice /api/devices/FAKE-001/reboot → Failure (404, 3ms)` — PASS
- Exception catch trong `fetch()` block: PASS (code-verified)
- **Vấn đề:** Không có loading/disabled state trên nút trong khi API đang gọi. User có thể click liên tiếp và gửi nhiều request reboot, hoặc nhiều request check-status cùng lúc.
  - Button không bị disable sau khi click
  - Không có spinner/indicator "đang xử lý"

#### C5 — Xử lý lỗi trên UI

**Kết quả: PASS**

- API 404 (device not found) → Toast đỏ: "Lỗi: Device 'FAKE-001' not found." — đã verify bằng screenshot `app-status-reboot-scenario-e-result.png`
- API error log: timestamp + endpoint + status code + latency — PASS
- Error message từ API được hiển thị đầy đủ qua `(data && data.message) || ('HTTP ' + r.status)` — PASS
- Toast hiển thị tự đóng (Bootstrap auto-dismiss) — PASS

#### C6 — Phòng ngừa thao tác nguy hiểm

**Kết quả: PASS**

Confirm dialog reboot (đã verify bằng Playwright):
- Loại: `confirm` (browser native) — user phải chủ động chọn OK/Cancel
- Nội dung: `"Khởi động lại thiết bị "FAKE-001"?\n(Thiết bị sẽ offline vài giây và tự online lại. Không thể hoàn tác lệnh này.)"`
  - Hiển thị serial để user xác nhận đúng thiết bị — PASS
  - Cảnh báo "offline vài giây" — PASS
  - Nhấn mạnh "Không thể hoàn tác" — PASS
- Cancel → không gửi request (requests_captured = [] sau dismiss) — PASS
- API "Check App Status" KHÔNG có confirm (đọc-only, không nguy hiểm) — design đúng

#### C7 — Accessibility cơ bản

**Kết quả: NEEDS-FIX (Low)**

- `type="button"` được đặt đúng trên cả 2 nút — PASS
- Văn bản nút mô tả rõ hành động (không cần aria-label riêng) — PASS
- Tab order tự nhiên theo DOM — PASS
- **Vấn đề:** `<i class="bi bi-info-circle me-1">` và `<i class="bi bi-arrow-repeat me-1">` không có `aria-hidden="true"`. Screen reader có thể đọc class name của icon thay vì văn bản.
- **Lưu ý:** Tất cả button hiện có trong action panel cũng không có `aria-hidden="true"` trên icon — đây là gap nhất quán trong toàn bộ codebase, không phải regression riêng của feature này.

---

## Kiểm tra kịch bản UI (5 kịch bản)

| Kịch bản | Test method | Kết quả |
|---|---|---|
| (a) Không chọn thiết bị → click Check App Status → alert | Playwright click | PASS — alert "Chọn 1 thiết bị (tick 1 dòng) trước khi kiểm tra." |
| (b) Chọn >1 thiết bị → alert | Code-verified | PASS — "Chỉ chọn 1 thiết bị. Đang có N thiết bị được chọn." |
| (c) Có thiết bị nhưng không nhập package → alert | Code-verified (`if (!pkg)`) | PASS — "Nhập package name ở ô 'Gói cần theo dõi' trước khi kiểm tra." |
| (d) Chọn 1 thiết bị → Reboot → Confirm Cancel → không gửi request | Playwright inject device + click + dismiss | PASS — requests_captured = [] |
| (e) Chọn 1 thiết bị → Reboot → Confirm OK → gửi request → toast lỗi | Playwright inject device + click + accept | PASS — toast "Lỗi: Device 'FAKE-001' not found." (bg-danger) |

*Ghi chú kịch bản (a)(b)(c): không có thiết bị Android thật kết nối — kịch bản cần API response thật (trạng thái app Foreground/Background) là N/A.*

---

## Danh sách issue cần fix

| ID | Màn hình | Mô tả | Mức độ | Tiêu chí | Đề xuất fix |
|---|---|---|---|---|---|
| UI-001 | Dashboard — Action Panel | Nút "Kiểm tra trạng thái app" dùng `btn-outline-primary` (Bootstrap blue `rgb(13,110,253)`) không khớp KZTEK Navy `#251C53`. Nút "Khởi động lại thiết bị" dùng `btn-outline-warning` (Bootstrap amber `rgb(255,193,7)`) không khớp KZTEK Cam `#F05922`. | Medium | C1, C2 | Thêm CSS class `btn-kz-outline-info` (border/text dùng shade của Navy) và `btn-kz-outline-caution` (border/text dùng shade của Cam) trong KZTEK custom CSS. Hoặc nếu chấp nhận Bootstrap semantic classes (theo precedent `btn-outline-danger` đã có), thì override màu trong `site.css` cho phù hợp brand. |
| UI-002 | Dashboard — Nút Check App Status + Reboot Device | Không có disabled state / loading spinner trong khi API call đang chạy. User có thể click nhiều lần → gửi nhiều request reboot liên tiếp. | Low | C4 | Thêm `button.disabled = true` trước `fetch()`, restore sau khi `fetch()` complete (trong `finally` block). Tùy chọn: thêm spinner `<span class="spinner-border spinner-border-sm">`. |
| UI-003 | Dashboard — Action Panel (toàn bộ) | `<i class="bi bi-...">` icons trong tất cả button không có `aria-hidden="true"`. Không phải regression riêng của feature này — là gap nhất quán toàn codebase. | Low | C7 | Thêm `aria-hidden="true"` vào tất cả `<i>` tag trong button. Ưu tiên fix ở 2 nút mới này trước, sau đó backlog cho toàn bộ. |

---

## Kết luận & Đề xuất (vòng đầu — 2026-08-24)

Feature "API Kiểm Tra Trạng Thái App + Reboot" đã triển khai đúng logic UI, luồng người dùng rõ ràng, phòng ngừa thao tác nguy hiểm (confirm dialog reboot) hoạt động chính xác, xử lý lỗi API hiển thị đầy đủ qua toast và log.

Không có issue Critical hoặc High. 3 issue được phát hiện:
- **UI-001 (Medium):** màu button mới dùng Bootstrap defaults không khớp KZTEK brand — nên fix trước release P1 nhưng không block hoạt động.
- **UI-002 (Low):** thiếu disabled state trong API call — risk nhỏ (double-click) nhưng không phải blocker.
- **UI-003 (Low):** thiếu `aria-hidden` trên icon — gap nhất quán toàn codebase, không regression.

**Khuyến nghị:** Fix UI-001 trước khi release (Senior Developer cân nhắc UI-001 cùng lúc với sprint hiện tại). UI-002 và UI-003 có thể đưa vào backlog sprint tiếp theo.

---

## Re-check sau fix (2026-08-24)

**Fix commit:** `23838dc` — Senior Developer đã fix 3 issue UI-001/002/003 tại commit này.
**Re-check method:** Playwright headless Chromium, `getComputedStyle()` trên DOM thật, đọc DOM attributes, phân tích code diff.
**Screenshots re-check:** `docs/ux-review/screenshots/2026-08-24/app-status-reboot-fixed-*.png`

### Kết quả verify từng issue

| Issue | Mô tả | Trạng thái | Bằng chứng |
|---|---|---|---|
| UI-001 | Màu brand KZTEK không đúng (Bootstrap blue/amber) | **Resolved** | `getComputedStyle` đo được: nút Check App Status `color: rgb(37, 28, 83)` = #251C53 (Navy), `borderColor: rgb(37, 28, 83)`. Nút Reboot `color: rgb(240, 89, 34)` = #F05922 (Cam), `borderColor: rgb(240, 89, 34)`. Class đổi thành `btn-kz-outline-navy` / `btn-kz-outline-orange`. |
| UI-002 | Không có disabled state / loading spinner khi API call | **Resolved** | Code diff: `btnAppStatus.disabled = true` + spinner `spinner-border-sm` trước `fetch()`, restore trong `finally`. DOM verify: `disabled = false` khi ở trạng thái nghỉ (đúng). JS pattern bảo đảm double-click prevented trong suốt API call. |
| UI-003 | Thiếu `aria-hidden="true"` trên `<i>` icon | **Resolved** | DOM evaluate: `checkIconAriaHidden = "true"`, `rebootIconAriaHidden = "true"`. Playwright xác nhận cả 2 icon đã có attribute. |

### Re-check tiêu chí C1, C2, C4, C7 (các tiêu chí bị ảnh hưởng)

| Tiêu chí | Kết quả vòng đầu | Kết quả sau re-check |
|---|---|---|
| C1 Màu sắc & Brand | NEEDS-FIX | **PASS** — computed style Navy #251C53 / Cam #F05922 đo được |
| C2 Nhất quán UI | NEEDS-FIX | **PASS** — class mới `.btn-kz-outline-navy` / `.btn-kz-outline-orange` nhất quán brand |
| C4 Phản hồi người dùng | NEEDS-FIX | **PASS** — disabled + spinner trước fetch, restore trong finally |
| C7 Accessibility cơ bản | NEEDS-FIX | **PASS** — `aria-hidden="true"` có mặt trong DOM |

### Kết luận re-check

**Tất cả 3 issue đã được Resolved. Kết quả tổng quan: PASS.**

Không phát hiện thay đổi nào ngoài scope 3 issue (diff giới hạn đúng 3 file: `Index.cshtml`, `dashboard.css`, `dashboard.js`). Các tiêu chí C3, C5, C6 (đã PASS từ vòng đầu) không bị ảnh hưởng — không cần kiểm tra lại.

Feature sẵn sàng chuyển sang QA Engineer (STEP-4.1). QA không cần test lại UI brand color hoặc aria-hidden — đã verified bằng computed style thật.
