---
agent: add-adb-api-endpoint
created: 2026-08-24
author: Dispatcher (sau WF-FEATURE adb-app-status-reboot-api)
status: active
---

# EVAL: add-adb-api-endpoint (skill)

> **Mục đích:** Pass/fail criteria cho skill trước khi implement (EDD §18.5 CLAUDE.md).
> **Nguồn gốc pattern:** 4 feature lặp cùng khuôn trong KztekAdbPublishTool.Web — `launch-app` (2026-08-18), `add-device` (2026-08-19), `uninstall-before-install` (2026-08-20), `app-status + reboot` (2026-08-24).

---

## 1. Mô tả năng lực (Capability Statement)

Skill `add-adb-api-endpoint` là checklist + scaffold guide cho chuỗi lặp "thêm 1 external API endpoint (+ nút UI tùy chọn) vào `KztekAdbPublishTool.Web`". Khi được invoke với mô tả API mới, skill dẫn agent đi qua đúng 7 lớp bắt buộc (TDD contract → AdbService method → Endpoint file + filter chain → constants/logging → UI button → unit tests → CODE-GRAPH) kèm các gotcha đã trả giá (G008, EndsWith route, encodeURIComponent, brand button class). Output: danh sách file phải tạo/sửa + các quyết định phải chốt, để bước TDD/code không phải khảo sát lại pattern từ đầu.

---

## 2. Test Scenarios (RED — trước khi có skill)

**Scenario 1:** User: "Thêm API lấy % pin thiết bị + hiển thị trên dashboard"
→ Hành vi hiện tại: Tech Lead ở bước TDD phải đọc lại `LaunchAppEndpoints.cs`, `ApiRequestLoggingEndpointFilter.cs`, `dashboard.js`… để tự suy ra lại filter chain, error matrix, chỗ đặt nút (≈15–25 tool calls lặp lại mỗi feature — đã xảy ra 4 lần).
→ Mong muốn: Invoke skill → nhận ngay bảng 7 lớp file + quyết định cần chốt + gotchas; TDD chỉ còn điền phần đặc thù (adb command, schema).
→ **Confirmed vi phạm:** 4 plan trước (xem TDD của từng plan) đều lặp cùng khảo sát; không có skill/command nào hiện có cover việc này.

**Scenario 2:** User: "Thêm API xóa cache app trên thiết bị" (destructive, có UI)
→ Hành vi hiện tại: quy tắc "destructive → confirm dialog + disabled state + response 'initiated'" chỉ nằm rải rác trong UX-REVIEW/TDD của plan `app-status-reboot` — agent mới không biết tra ở đâu.
→ Mong muốn: skill có mục riêng "API destructive" bắt buộc confirm dialog, disabled+spinner, wording response.
→ **Confirmed vi phạm:** UXR đã phải chấm NEEDS-FIX (UI-001/002/003) ở plan 2026-08-24 vì code lần đầu thiếu đúng các mục này.

**Scenario 3 (negative):** User: "Sửa response của API /api/launch-app hiện có"
→ Mong muốn: skill KHÔNG trigger (đây là sửa endpoint có sẵn — WF-BUGFIX/REFACTOR thường), description phải loại trừ rõ.

---

## 3. Capability Evals

### CE-01 — Happy path: API mới có UI button

**Input:** "Dùng skill add-adb-api-endpoint: thêm `GET /api/devices/{serial}/battery` trả % pin (adb shell dumpsys battery) + nút 'Kiểm tra pin' trên dashboard."

**Output mong đợi:**
- [ ] Liệt kê đủ 7 lớp: TDD doc, AdbService method (KHÔNG vào IAdbService trừ khi worker cần), `Endpoints/BatteryEndpoints.cs` + đăng ký Program.cs, `ApiRequestLogConstants` + `ResolveApiName` (EndsWith), UI button (Index.cshtml + dashboard.js), unit tests, CODE-GRAPH.
- [ ] Nêu filter chain đúng thứ tự: OUTER `ApiRequestLoggingEndpointFilter` → INNER `ApiKeyEndpointFilter`.
- [ ] Nêu ≥3 gotcha: G008 (`context.Arguments`), `EndsWith` cho route có `{serial}`, `encodeURIComponent(serial)`, brand class `btn-kz-outline-*`, aria-hidden, disabled+spinner.
- [ ] Trỏ đúng file mẫu tham chiếu (AppStatusEndpoints.cs / RebootEndpoints.cs / LaunchAppEndpoints.cs).

**Grader:** Code-based (output chứa các mục trên).

### CE-02 — Edge case: API destructive

**Input:** "Dùng skill add-adb-api-endpoint: thêm `POST /api/devices/{serial}/factory-reset` (adb shell recovery wipe) + nút UI."

**Output mong đợi:**
- [ ] Nhận diện destructive → bắt buộc: confirm dialog TRƯỚC fetch, disabled state, response wording "initiated" (không phải "completed").
- [ ] Yêu cầu security-audit-stride ở bước review (STRIDE cho destructive command).
- [ ] Cảnh báo QA: TC destructive chỉ chạy trên thiết bị staging.

**Grader:** Code-based.

### CE-03 — Negative case: ngoài scope

**Input:** "Dùng skill add-adb-api-endpoint để sửa lỗi 500 của API /api/install hiện có."

**Output mong đợi:**
- [ ] Skill từ chối áp dụng — chỉ ra đây là sửa endpoint CÓ SẴN → WF-BUGFIX, không phải thêm endpoint mới.
- [ ] KHÔNG scaffold file mới nào.

**Grader:** Code-based.

---

## 4. Trigger test queries (dùng cho skill-trigger-test)

**Should-trigger:**
1. "Thêm API mới trả danh sách app đã cài trên thiết bị + nút xem trên dashboard"
2. "/add-adb-api-endpoint GET battery level"
3. "Cần thêm endpoint POST clear-app-data cho tool ADB, có nút bấm"
4. "Thêm 1 API check nhiệt độ thiết bị vào KztekAdbPublishTool.Web"
5. "Viết API mới screenshot màn hình thiết bị qua adb, thêm nút trên view"
6. "Thêm endpoint external có x-api-key để tắt màn hình thiết bị"
7. "Bổ sung API + button khởi động lại app (force-stop rồi start)"
8. "Thêm API get-prop lấy Android version, không cần UI"

**Should-NOT-trigger (near-miss):**
1. "Sửa response 500 của /api/install" (endpoint có sẵn → WF-BUGFIX)
2. "Đổi màu nút Reboot trên dashboard" (UI thuần → WF-FASTTRACK)
3. "Thêm API vào project ParkingV8" (khác project — pattern này chỉ cho KztekAdbPublishTool.Web)
4. "Thêm cột mới vào bảng thiết bị trên dashboard" (UI/SignalR, không phải endpoint mới)
5. "API /api/launch-app đang trả 401, fix đi" (bug auth → WF-BUGFIX)
6. "Thêm SignalR event mới khi install xong" (không phải REST endpoint)
7. "Viết API cho app Android (phía device)" (không phải server Web này)
8. "Thêm middleware rate-limit cho toàn bộ API" (cross-cutting → WF-REFACTOR/ARCH)

---

## 5. Kết quả chạy thử

| Eval | Ngày chạy | Cách chạy | Kết quả | Ghi chú |
|------|-----------|-----------|---------|---------|
| CE-01 | 2026-08-24 | Cold-read subagent (chỉ đọc file skill) | ✅ PASS | Liệt kê đủ 7 lớp, filter chain đúng thứ tự, 6 gotcha/quy tắc UI, tham chiếu đúng AppStatusEndpoints.cs |
| CE-02 | 2026-08-24 | Cold-read subagent | ✅ PASS | Nhận diện destructive, đủ 4 yêu cầu (confirm dialog, "Initiated", security-audit-stride, QA staging-only) |
| CE-03 | 2026-08-24 | Cold-read subagent | ✅ PASS | Từ chối đúng, trỏ WF-BUGFIX, không scaffold |
| Reader Testing (cold-read) | 2026-08-24 | Bước 6 writing-agent-skill — subagent không có context session | ✅ PASS | Phát hiện 8 điểm mơ hồ → đã vá 5 điểm vào skill (tiêu chí destructive, naming hằng logging, filter gắn per-route, timeout gợi ý, chỗ ghi lý do bỏ lớp UI); 3 điểm còn lại (slug convention, nội bộ giữ logging, S6 edge) mức chấp nhận được |
| Trigger test | 2026-08-24 | 8 should-trigger + 8 should-NOT-trigger, chấm theo description | ✅ 16/16 | Không false positive/negative |

**Trạng thái:** **APPROVED** (3/3 CE pass + Reader Testing pass + trigger 16/16) — 2026-08-24. Skill active tại `.claude/commands/add-adb-api-endpoint.md`. Không thêm vào routing table CLAUDE.md §2 (skill hỗ trợ bước TDD/code bên trong WF-FEATURE, không phải workflow riêng — cùng loại với `pre-coding-check`).
