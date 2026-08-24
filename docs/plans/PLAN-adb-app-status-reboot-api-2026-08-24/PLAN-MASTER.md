---
task: adb-app-status-reboot-api
created: 2026-08-24
updated: 2026-08-24 23:09
status: in-progress
last_step: 3.2
workflow: WF-FEATURE
priority: P1
---

# PLAN MASTER: API Kiểm Tra Trạng Thái App + Reboot Thiết Bị Android

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả
Bổ sung 2 API endpoint mới vào server `KztekAdbPublishTool.Web` (ASP.NET Core Minimal API), đồng thời thêm 2 nút bấm tương ứng trên dashboard UI (`Pages/Index.cshtml` + `wwwroot/js/dashboard.js`):

1. **API "Kiểm tra trạng thái ứng dụng"** (`GET /api/devices/{serial}/app-status?package={packageName}`): kiểm tra app có đang chạy không, và nếu chạy thì đang ở foreground hay background. Dùng `adb shell pidof` (check running) + `adb shell dumpsys activity activities` (check foreground). Trả về `{ running: bool, state: "Foreground"|"Background"|"NotRunning" }`.

2. **API "Reboot thiết bị"** (`POST /api/devices/{serial}/reboot`): ra lệnh khởi động lại thiết bị Android qua `adb reboot`. Trả về `{ status: "RebootInitiated" }`.

Cả 2 API tái dùng `ApiKeyEndpointFilter` + `ApiRequestLoggingEndpointFilter` theo đúng pattern `LaunchAppEndpoints.cs` (OUTER logging + INNER auth). Cần thêm 2 method mới vào `AdbService` (`GetAppStatusAsync`, `RebootDeviceAsync`). **Feature có thay đổi UI** (thêm 2 nút bấm) → UX/UI Reviewer BẮT BUỘC.

## Nguồn yêu cầu
- Yêu cầu gốc: Thêm 2 API + nút bấm trên view để (1) check app đang bật/background/foreground, (2) ra lệnh reboot thiết bị.
- Workflow: WF-FEATURE — Yêu cầu tính năng mới (rút gọn: không UX Designer đầy đủ, không CTO optional — tương tự 2 plan trước)
- Agent chain: Product Manager → Business Analyst → Engineering Manager → Project Manager → Tech Lead → Senior Developer → Tech Lead (review) → UX/UI Reviewer (bắt buộc — có UI mới) → QA Engineer → QA Lead → DevOps Engineer → DevOps Lead

## Bước bị SKIP và lý do

| Bước WF-FEATURE gốc | Trạng thái | Lý do |
|---|---|---|
| Bước 3 — UI/UX Designer | ⏭️ Skipped | Không cần mockup/wireframe phức tạp — chỉ thêm 2 nút bấm vào action panel hiện có theo pattern các nút đã có |
| Bước 5 — CTO review kiến trúc | ⏭️ Skipped (có điều kiện) | Feature tái dùng cơ chế auth + logging đã có, không tạo cơ chế mới — không thuộc dạng lớn/chiến lược. Tech Lead có thể escalate nếu thấy cần |
| Bước 9 — Junior Developer | ⏭️ Skipped | Task đụng auth + ADB command mới cần hiểu pattern codebase tốt — giao toàn bộ Senior Developer |
| Bước 10b — UX/UI Reviewer | ✅ BẮT BUỘC | Feature có UI mới (2 nút bấm) → PHẢI có UX/UI Reviewer theo CLAUDE.md §3.5 — đặt ở Bước 3.3 |

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Phân tích & Lên kế hoạch

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | Viết PRD phạm vi hẹp: 2 API + 2 nút UI, mục tiêu, AC tổng quan, non-goals | Product Manager | ⏭️ | `steps/STEP-1.1-product-manager-prd.md` | 2026-08-24 (skip — user duyệt rút gọn) |
| 1.2 | Chi tiết hóa AC theo Given/When/Then, user story cho 2 API | Business Analyst | ⏭️ | `steps/STEP-1.2-business-analyst-ac.md` | 2026-08-24 (skip — user duyệt rút gọn) |
| 1.3 | Estimate resource, xác nhận priority P1, phân bổ team | Engineering Manager | ⏭️ | `steps/STEP-1.3-engineering-manager-estimate.md` | 2026-08-24 (skip — user duyệt rút gọn) |
| 1.4 | Lên task board, sprint plan cho feature này | Project Manager | ⏭️ | `steps/STEP-1.4-project-manager-sprint.md` | 2026-08-24 (skip — user duyệt rút gọn) |

> **Phase 1 SKIPPED toàn bộ (2026-08-24):** User duyệt plan với phương án rút gọn — feature nhỏ, pattern API đã có sẵn từ `launch-app`/`connect-by-ip`, AC tổng quan đã ghi rõ trong mục "API Contract dự kiến". Bắt đầu trực tiếp từ Bước 2.1 (Tech Lead TDD). Artifacts PRD/US/RESOURCE/SPRINT của Phase 1 không tạo.

### Phase 2: Thiết kế kỹ thuật

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | Viết TDD: API contract 2 endpoint, ADB command strategy (pidof + dumpsys vs alternatives), response schema, error handling, UI button spec | Tech Lead | ✅ | `steps/STEP-2.1-tech-lead-tdd.md` | 2026-08-24 22:28 |

### Phase 3: Phát triển

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | Code AdbService methods + 2 endpoint + UI buttons (Index.cshtml + dashboard.js) + unit tests + cập nhật CODE-GRAPH | Senior Developer | ✅ | `steps/STEP-3.1-senior-developer-code.md` | 2026-08-24 22:40 |
| 3.2 | Code review PR, security-audit-stride (kiểm tra reboot command), quyết định merge | Tech Lead | ✅ | `steps/STEP-3.2-tech-lead-review.md` | 2026-08-24 22:46 |
| 3.3 | Chạy app thật, chụp screenshot 2 nút mới, đánh giá C1–C7 UX | UX/UI Reviewer | ✅ | `steps/STEP-3.3-ux-ui-reviewer.md` | 2026-08-24 22:58 |

### Phase 4: Kiểm thử & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 4.1 | Viết test plan, thực thi test case (2 API + 2 nút UI), log kết quả | QA Engineer | ⬜ | `steps/STEP-4.1-qa-engineer-test.md` | — |
| 4.2 | Sign-off chất lượng (P1 — bắt buộc QA Lead) | QA Lead | ⬜ | `steps/STEP-4.2-qa-lead-signoff.md` | — |
| 4.3 | Deploy lên môi trường tương ứng, smoke test | DevOps Engineer | ⬜ | `steps/STEP-4.3-devops-engineer-deploy.md` | — |
| 4.4 | Approve staging, verify smoke test, approve + deploy production | DevOps Lead | ⬜ | `steps/STEP-4.4-devops-lead-approve.md` | — |

## Artifacts dự kiến (tổng)
- [ ] `docs/prd/PRD-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/user-stories/US-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/planning/RESOURCE-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/planning/SPRINT-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/tech-design/TDD-adb-app-status-reboot-api.md` + `.docx`
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/AppStatusEndpoints.cs` — GET /api/devices/{serial}/app-status
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/RebootEndpoints.cs` — POST /api/devices/{serial}/reboot
- [ ] `src/KztekAdbPublishTool.Web/Services/AdbService.cs` — thêm `GetAppStatusAsync` + `RebootDeviceAsync`
- [ ] `src/KztekAdbPublishTool.Web/Services/IAdbService.cs` — cập nhật interface
- [ ] `Pages/Index.cshtml` — 2 nút bấm mới trong action panel
- [ ] `wwwroot/js/dashboard.js` — handler 2 nút mới
- [ ] `tests/KztekAdbPublishTool.Web.Tests/` — unit tests cho 2 endpoint + 2 AdbService method mới
- [ ] `docs/test-plans/TEST-PLAN-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/test-cases/TC-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/ux-review/UX-REVIEW-adb-app-status-reboot-api.md` + `.docx`
- [ ] `docs/devops/DEPLOY-adb-app-status-reboot-api.md` + `.docx`
- [ ] `code-graph/CODE-GRAPH.md` — cập nhật + xuất lại `.pdf`

## API Contract dự kiến (chờ Tech Lead xác nhận ở TDD)

### API 1 — Kiểm tra trạng thái ứng dụng
```
GET /api/devices/{serial}/app-status?package={packageName}
Header: x-api-key: <key>
Response 200: { "serial": "...", "package": "...", "running": true, "state": "Foreground" }
  state values: "Foreground" | "Background" | "NotRunning"
Response 400: package missing / serial invalid format
Response 401: API key sai/thiếu
Response 404: serial không tìm thấy trong danh sách thiết bị
Response 500: lỗi ADB command
```

### API 2 — Reboot thiết bị
```
POST /api/devices/{serial}/reboot
Header: x-api-key: <key>
Body: {} (empty hoặc không có)
Response 200: { "serial": "...", "status": "RebootInitiated" }
Response 401: API key sai/thiếu
Response 404: serial không tìm thấy
Response 500: lỗi ADB command
```

## Blockers
Không có

## Quyết định / Ghi chú tổng
- Cả 2 API đều dùng BOTH filter: `ApiRequestLoggingEndpointFilter` (OUTER) + `ApiKeyEndpointFilter` (INNER) — theo đúng pattern `LaunchAppEndpoints.cs`. LƯU Ý: `GET /api/devices/{serial}/status` hiện chỉ có `ApiKeyEndpointFilter`, không có logging filter — plan này sẽ follow pattern đầy đủ hơn.
- ADB strategy kiểm tra foreground: `adb shell dumpsys activity activities` grep `mResumedActivity` (Android 8+) — Tech Lead cần xác nhận trong TDD và cân nhắc fallback cho Android cũ.
- Reboot command: `adb -s {serial} reboot` — không có confirm dialog từ phía ADB; cần response rõ là "initiated" không phải "completed" (device sẽ offline vài giây sau).
- UX/UI Reviewer BẮT BUỘC (Bước 3.3): feature có UI mới (2 nút bấm) — không được bỏ qua dù workflow rút gọn.
- File đã modified chưa commit từ plan trước (adb-reconnect STEP-3.3): `docker-compose.yml`, `AdbService.cs` cả 2 project, `appsettings.json` — Senior Developer PHẢI KHÔNG revert hoặc conflict với các thay đổi này.
- GOTCHA G008: KHÔNG đọc raw Request.Body trong endpoint filter — dùng `context.Arguments`.

## Lịch sử cập nhật
| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-24 | Plan tạo mới | task-planner |
| 2026-08-24 | User duyệt plan phương án rút gọn — Phase 1 (1.1–1.4) đánh dấu ⏭️ Skipped, bắt đầu từ Bước 2.1 | Dispatcher |
| 2026-08-24 22:28 | Bước 2.1 Done — TDD hoàn chỉnh (13 mục, 9 quyết định chốt, task breakdown 8 task ~7h) + xuất DOCX | Tech Lead |
| 2026-08-24 22:40 | Bước 3.1 Done — Code 2 API + UI + 15 test mới (119 total, 0 fail) + CODE-GRAPH cập nhật. Commit 5297bc2 pushed. | Senior Developer |
| 2026-08-24 22:46 | Bước 3.2 Done — Tech Lead code review 18/18 PASS + security-audit-stride (OWASP 7P/0F/3NA; STRIDE 4P/0F/1NA/1FYI-rate-limit). Quyết định: APPROVE MERGE. 4 comment Optional/FYI ghi nhận cho follow-up (regex serial, rate limit reboot, API key trong HTML source, DRY refactor). | Tech Lead |
| 2026-08-24 22:58 | Bước 3.3 Done — UX/UI Reviewer NEEDS-FIX: C3/C5/C6 PASS, 3 issue (UI-001 Medium: brand color, UI-002 Low: no loading state, UI-003 Low: aria-hidden). Không có blocker. Screenshot + report + DOCX created. | UX/UI Reviewer |
| 2026-08-24 23:10 | Fix UI-001/002/003 theo UX review — btn-kz-outline-navy/orange (CSS), disabled+spinner (JS), aria-hidden (cshtml). Build 0 error, 119/119 test pass. Chờ UXR re-check. | Senior Developer |
| 2026-08-24 23:09 | UXR re-check UI-001/002/003 — cả 3 RESOLVED (computed style Navy #251C53 / Cam #F05922 verified, disabled+spinner confirmed, aria-hidden="true" confirmed). Kết luận tổng quan: PASS. | UX/UI Reviewer |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
