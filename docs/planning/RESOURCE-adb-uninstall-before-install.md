# RESOURCE PLAN: Tùy chọn Gỡ cài đặt App trước khi Cài đặt (Uninstall Before Install)

**Feature slug:** adb-uninstall-before-install
**Priority:** P2
**Ngày lập:** 2026-08-20
**Engineering Manager:** duongth@kztek.net

---

## 1. Tóm tắt tính năng

Thêm 1 checkbox trên Dashboard (`Index.cshtml`) cho phép người dùng chọn có muốn server chạy `adb uninstall <package>` TRƯỚC `adb install` hay không. Mặc định tắt — giữ nguyên hành vi hiện tại (`adb install -r`). Khi bật, nếu package chưa cài trên thiết bị, lỗi uninstall được log warning và bỏ qua, không abort luồng install.

Thay đổi code bao gồm: `IAdbService`/`AdbService` (thêm `UninstallApkAsync`), `InstallRequest` (thêm field `UninstallBeforeInstall`), `InstallCoordinator` (logic uninstall-then-install trong `DoInstallAsync`), `Index.cshtml` (checkbox mới), `dashboard.js` (truyền flag qua `apiPost`), và DB Settings (lưu trạng thái checkbox qua cơ chế hiện có).

---

## 2. Quyết định Priority

| Tiêu chí | Đánh giá |
|---|---|
| Mức độ | **P2** — cải thiện UX và giảm thao tác thủ công cho vận hành; không phải blocker production |
| Rủi ro | **Trung bình** — đụng `InstallCoordinator`/`AdbService` là core install flow; cần Senior Dev để đảm bảo không regression hành vi mặc định |
| Phụ thuộc | Phụ thuộc TDD (STEP-2.1) để chốt: vị trí checkbox, field name DB Settings, cơ chế phân biệt lỗi "package not found" vs lỗi ADB thực sự (BR3) |
| Deadline | Không có deadline cứng; không xung đột với task P1 đang chạy |
| Hành vi mặc định | Checkbox TẮT — hành vi giống hoàn toàn hiện tại; người dùng chủ động bật mới có hiệu lực |

**Kết luận: Xác nhận P2. Không nâng P1 — không block production; feature hoàn toàn opt-in (mặc định tắt), rủi ro regression được kiểm soát bằng phân bổ Senior Dev.**

---

## 3. Phân bổ Team

| Agent | Vai trò | Task cụ thể | Lý do chọn |
|---|---|---|---|
| **Tech Lead** | TDD + Code Review | STEP-2.1: Chốt vị trí checkbox, field `UninstallBeforeInstall` trong DB Settings, thứ tự gọi trong `DoInstallAsync`, SignalR progress bước mới, cơ chế phân biệt lỗi uninstall. STEP-3.2: Review PR cuối | Quyết định kiến trúc cho `InstallCoordinator` và cơ chế parse lỗi ADB cần Tech Lead — ảnh hưởng trực tiếp đến BR3 và EC1 |
| **Senior Developer** | Backend C# + Frontend UI + JS | STEP-3.1: `UninstallApkAsync` trong `IAdbService`/`AdbService`, field `UninstallBeforeInstall` trong `InstallRequest`, logic uninstall-before-install trong `DoInstallAsync`, checkbox `chk-uninstall-before-install` trong `Index.cshtml`, truyền flag qua `dashboard.js`, lưu DB Settings | Đụng trực tiếp `InstallCoordinator` (core install flow) — Junior Dev không đủ context về xử lý lỗi ADB và luồng `DoInstallAsync`. Senior Dev cũng handle phần checkbox UI (đơn giản) để giữ 1 PR gọn |
| **Junior Developer** | — | **Không phân bổ** | Không có task phù hợp cấp Junior: UI checkbox đơn giản nhưng kèm với backend core flow nên không tách được; Senior Dev handle toàn bộ để giảm overhead phối hợp |
| **UX/UI Reviewer** | Kiểm tra trực quan Dashboard | STEP-3.3: Chạy app thật, chụp screenshot checkbox mới, đánh giá C1–C7 | Dashboard có thay đổi UI (thêm checkbox) — bắt buộc UXR theo CLAUDE.md WF-FEATURE |
| **QA Engineer** | Test execution | STEP-4.1: Viết test plan, thực thi 5 US × 13 scenario (checkbox on/off, uninstall graceful khi package chưa cài, nhiều thiết bị, persist state) | 5 User Stories, 13 scenario đã có AC rõ từ STEP-1.2 làm test basis |
| **QA Lead** | Sign-off | STEP-4.2: Review kết quả QA, veto nếu còn P0/P1 bug | Bắt buộc sign-off P2 theo WF-FEATURE |
| **DevOps Engineer** | Deploy staging | STEP-4.3: `docker-compose up --build`, verify luồng install không bị regression | |
| **DevOps Lead** | Approve staging + production | STEP-4.4: Smoke test staging, approve deploy production, monitor | Two-Eyes cho deploy production |

**Quyết định không dùng Junior Dev:** Mặc dù phần checkbox UI (`Index.cshtml` + `dashboard.js`) đủ đơn giản cho Junior, phần backend `InstallCoordinator`/`AdbService` cần Senior Dev. Tách thành 2 PR (Junior UI + Senior backend) sẽ tạo overhead phối hợp và rủi ro merge conflict cho cùng 1 tính năng nhỏ — EM quyết định Senior Dev handle toàn bộ trong 1 PR.

---

## 4. Estimate Effort

> Feature quy mô nhỏ-vừa: thêm 1 method ADB + sửa 1 coordinator + thêm 1 field request + 1 checkbox UI + 1 JS call. Phần phức tạp nhất là cơ chế phân biệt lỗi "package not found" vs lỗi ADB thực sự trong `UninstallApkAsync`.

| Bước | Agent | Estimate | Ghi chú |
|---|---|---|---|
| STEP-2.1: Viết TDD | Tech Lead | **1.5–2 giờ** | Chốt: vị trí checkbox, field name DB Settings, thứ tự `DoInstallAsync`, cơ chế parse lỗi ADB uninstall, tên SignalR event mới. Feature nhỏ hơn api-request-log nên TDD ngắn hơn |
| STEP-3.1: Code (full stack) | Senior Developer | **4–6 giờ** | `UninstallApkAsync` + parse lỗi (~1.5h) · `InstallCoordinator` logic (~1.5h) · `InstallRequest` field + endpoint (~0.5h) · Checkbox UI + JS (~0.5h) · DB Settings persist (~0.5h) · Unit tests cơ bản (~0.5–1h) |
| STEP-3.2: Code review + merge | Tech Lead | **1 giờ** | Review 1 PR (full stack) — nhỏ hơn api-request-log (2 PR); yêu cầu `/verify-pr` report trước khi mở review |
| STEP-3.3: UXR review | UX/UI Reviewer | **30–45 phút** | Chạy app thật, xác nhận checkbox hiển thị đúng pattern, SignalR progress đúng thứ tự |
| STEP-4.1: QA test execution | QA Engineer | **1.5–2 giờ** | 5 US × 13 scenario; tập trung: checkbox on/off regression, graceful skip khi package chưa cài, persist state sau F5 |
| STEP-4.2: Sign-off | QA Lead | **30 phút** | Review kết quả QA |
| STEP-4.3: Deploy staging | DevOps Engineer | **30–45 phút** | Build + deploy; không có DB migration mới nếu field DB Settings dùng key-value hiện có |
| STEP-4.4: Approve + deploy production | DevOps Lead | **30 phút** | Smoke test, approve, monitor |

**Tổng estimate:** **10–13 giờ** thực thi (không kể thời gian chờ giữa bước).

**So sánh với feature cùng loại:** api-request-log (schema DB mới + service mới + 2 endpoint inject) ~14–19h. Feature này nhỏ hơn vì không có DB migration mới, không có service layer mới — chỉ thêm method + sửa flow hiện có.

---

## 5. Risk Assessment

| Rủi ro | Mức độ | Khả năng | Biện pháp giảm thiểu |
|---|---|---|---|
| R1: Regression hành vi mặc định (checkbox TẮT bị ảnh hưởng) | Cao | Thấp | Senior Dev bắt buộc giữ nguyên code path khi flag = false; QA có Scenario riêng verify US-001 |
| R2: Parse lỗi ADB uninstall sai (nhầm "package not found" thành lỗi khác, hoặc ngược lại) | Trung bình | Trung bình | Tech Lead chốt cơ chế parse chi tiết trong TDD (STEP-2.1); QA test US-003 với thiết bị thực |
| R3: Thời gian install tăng gây nhầm tưởng treo app | Thấp | Thấp | SignalR progress thêm bước "Đang gỡ cài đặt..." — người dùng thấy phản hồi ngay (US-004 Scenario 2) |
| R4: Lỗi MDM lock / thiết bị bị khoá không gỡ được | Thấp | Thấp | Tech Lead quyết định abort hay warning trong TDD (EC1); BR3 đã định nghĩa graceful skip cho "package not found" |
| R5: DB Settings key conflict với key hiện có | Thấp | Rất thấp | Dùng đúng pattern PackageName/ApkPath; Tech Lead chốt tên key trong TDD |

**Rủi ro tổng thể: Thấp-Trung bình.** Feature opt-in (mặc định tắt) — ngay cả khi có bug trong nhánh bật, hành vi mặc định không bị ảnh hưởng. Rủi ro cao nhất là parse lỗi ADB sai — được kiểm soát bằng TDD chi tiết và test case thực trên thiết bị.

---

## 6. Điều kiện bắt đầu

- [ ] STEP-1.4 (Project Manager lên sprint plan) hoàn thành — task board sẵn sàng
- [ ] STEP-2.1 (TDD) hoàn thành và được user confirm trước khi Senior Dev bắt đầu code
- [ ] Thiết bị Android thực (đã cài package test) sẵn sàng cho môi trường local để QA test US-003

---

## 7. Approve

**Engineering Manager:** Xác nhận phân bổ Senior Developer và Priority P2 cho feature này.

- Priority P2: Được xác nhận — improvement UX, không phải blocker, hành vi mặc định an toàn.
- Senior Developer: Được xác nhận — đụng `InstallCoordinator`/`AdbService` (core install flow), không giao Junior.
- Junior Developer: Không phân bổ — không có task tách được phù hợp cấp Junior cho feature này.
- UX/UI Reviewer: Bắt buộc — Dashboard có thay đổi UI (thêm checkbox).
