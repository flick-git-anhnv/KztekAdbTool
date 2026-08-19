---
task: api-request-log
created: 2026-08-19
updated: 2026-08-19 17:25
status: active
workflow: WF-FEATURE
priority: P2
---

# PLAN MASTER: API Request Log — Ghi lịch sử & hiển thị real-time cho AddDevice/LaunchApp API

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả
Thêm tính năng ghi lịch sử mỗi request gọi vào 2 API mới có ApiKey protection: AddDevice API (`DeviceEndpoints.cs`) và LaunchApp API (`LaunchAppEndpoints.cs`). Lịch sử được lưu bền vững vào SQLite (bảng `ApiRequestLog` mới) và đẩy real-time qua SignalR vào panel "Nhật ký hoạt động" (`#log`) trên dashboard — nhất quán với cơ chế `appendLog()` hiện tại.

## Nguồn yêu cầu
- Yêu cầu gốc: Ghi lại lịch sử request gọi vào AddDevice API và LaunchApp API; lưu bền vững vào SQLite (bảng `ApiRequestLog`: thời điểm, tên API, tham số chính, kết quả, caller info); đẩy real-time qua SignalR vào panel `#log` của mọi dashboard đang mở.
- Workflow: WF-FEATURE — Yêu cầu tính năng mới
- Agent chain: PM → BA → [UX Designer - SKIP] → EM → [CTO - SKIP] → PJM → TL → (Senior Dev ∥ Junior Dev) → TL (review) → TL (security audit - conditional) → UXR → QAE → QAL → DOE → DOL (staging) → DOL (production)
- Priority: P2

## Bước WF-FEATURE bị bỏ qua (có lý do)

| Bước WF-FEATURE | Lý do bỏ qua |
|---|---|
| Bước 3 — UI/UX Designer | Không có UI mới cần thiết kế. Panel `#log` hiện có giữ nguyên cấu trúc; log entries mới dùng cùng cơ chế `appendLog()` |
| Bước 5 — CTO review | P2, quy mô nhỏ-vừa, không phải kiến trúc lớn/bảo mật/chiến lược (CLAUDE.md §4 WF-FEATURE điều kiện "CHỈ khi feature lớn/bảo mật/chiến lược") |

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Phân tích & Thiết kế

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | PRD: mục tiêu, AC, non-goals, metric đo lường | Product Manager | ✅ | `steps/STEP-1.1-pm-prd.md` | 2026-08-19 13:19 |
| 1.2 | User stories + AC dạng Given/When/Then | Business Analyst | ✅ | `steps/STEP-1.2-ba-user-stories.md` | 2026-08-19 13:24 |
| 1.3 | Estimate resource, confirm priority P2, phân bổ dev | Engineering Manager | ✅ | `steps/STEP-1.3-em-resource-estimate.md` | 2026-08-19 13:26 |
| 1.4 | Sprint plan (lightweight) + task board | Project Manager | ✅ | `steps/STEP-1.4-pjm-sprint-plan.md` | 2026-08-19 13:31 |
| 1.5 | TDD: schema ApiRequestLog, service interface, endpoint changes, SignalR event contract, JS handler contract | Tech Lead | ✅ | `steps/STEP-1.5-tl-tdd.md` | 2026-08-19 13:38 |

### Phase 2: Triển khai

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | [∥ 2.2] Backend: entity, EF migration, service, inject vào 2 endpoints, SignalR broadcast | Senior Developer | ✅ | `steps/STEP-2.1-senior-dev-backend.md` | 2026-08-19 13:49 |
| 2.2 | [∥ 2.1] Frontend JS: SignalR event handler mới, render log entry trong panel `#log` | Junior Developer | ✅ | `steps/STEP-2.2-junior-dev-frontend.md` | 2026-08-19 13:42 |
| 2.3 | Code review cuối + merge decision (sau khi 2.1 + 2.2 đều xong; Senior Dev phải đính kèm /verify-pr report) | Tech Lead | ✅ | `steps/STEP-2.3-tl-code-review.md` | 2026-08-19 13:55 |
| 2.4 | Security audit — conditional: có DB schema mới; dữ liệu không nhạy cảm; Tech Lead tự quyết có chạy stride không | Tech Lead | ⏭️ | `steps/STEP-2.4-tl-security-audit.md` | 2026-08-19 13:55 |

### Phase 3: Kiểm thử & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | UXR: chạy app thật, gọi API, verify log entries xuất hiện đúng trong panel `#log` (JS layer có thay đổi) | UX/UI Reviewer | ✅ | `steps/STEP-3.1-uxr-review.md` | 2026-08-19 14:12 |
| 3.2 | Test execution: gọi 2 API, kiểm tra logging/SignalR real-time/DB persistence/panel display | QA Engineer | ✅ | `steps/STEP-3.2-qae-test.md` | 2026-08-19 17:13 |
| 3.3 | Sign-off chất lượng, veto nếu còn P0/P1 bug | QA Lead | ✅ | `steps/STEP-3.3-qal-signoff.md` | 2026-08-19 17:18 |
| 3.4 | Deploy staging (docker-compose), verify migration apply thành công | DevOps Engineer | ✅ | `steps/STEP-3.4-doe-deploy-staging.md` | 2026-08-19 17:25 |
| 3.5 | Approve staging + smoke test, cấp phép deploy production | DevOps Lead | ⬜ | `steps/STEP-3.5-dol-approve-staging.md` | - |
| 3.6 | Approve + deploy production + monitor | DevOps Lead | ⬜ | `steps/STEP-3.6-dol-deploy-production.md` | - |

## Artifacts dự kiến (tổng)

- [ ] `docs/prd/PRD-api-request-log.md`
- [ ] `docs/user-stories/US-api-request-log.md`
- [ ] `docs/planning/RESOURCE-api-request-log.md`
- [ ] `docs/planning/SPRINT-api-request-log.md`
- [ ] `docs/tech-design/TDD-api-request-log.md`
- [ ] `src/KztekAdbPublishTool.Web/Data/ApiRequestLog.cs` (entity mới)
- [ ] EF Migration mới (`Migrations/[timestamp]_AddApiRequestLog.cs`)
- [ ] `src/KztekAdbPublishTool.Web/Services/IApiRequestLogService.cs`
- [ ] `src/KztekAdbPublishTool.Web/Services/ApiRequestLogService.cs`
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs` (modified)
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs` (modified)
- [ ] `src/KztekAdbPublishTool.Web/wwwroot/js/signalr-client.js` (modified — new event handler)
- [ ] `docs/ux-review/UX-REVIEW-api-request-log.md`
- [ ] `docs/test-cases/TC-api-request-log.md`
- [ ] `docs/devops/DEPLOY-api-request-log.md`

## Blockers
Không có

## Quyết định / Ghi chú tổng

- **2.1 ∥ 2.2 song song:** TDD (1.5) PHẢI định nghĩa rõ tên SignalR event + payload schema trước khi giao 2.1/2.2. Junior Dev code JS handler độc lập với backend dựa trên contract trong TDD.
- **Bước 2.4 (security audit):** Tech Lead quyết định khi tới bước đó. Nếu kết luận không cần chạy STRIDE (dữ liệu không nhạy cảm, không có auth/payment), ghi rõ lý do và đánh dấu ⏭️ Skipped.
- **UXR (3.1) lightweight:** Không có UI component mới — chỉ verify log entries hiển thị đúng format/nội dung/timing trong panel `#log` hiện có.
- **Không đụng ApiKeyEndpointFilter:** Logging chỉ được inject vào phần xử lý sau khi đã pass auth filter — KHÔNG sửa cơ chế auth.

## Lịch sử cập nhật

| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-19 | Plan tạo mới | task-planner |
| 2026-08-19 13:19 | STEP-1.1 Done — PRD tạo xong, commit e0fa758 | Product Manager |
| 2026-08-19 13:24 | STEP-1.2 Done — 6 User Stories + AC + DOCX, commit 6c77fa4 | Business Analyst |
| 2026-08-19 13:26 | STEP-1.3 Done — RESOURCE plan tạo xong, P2 xác nhận, không conflict P1, commit 2ac134b | Engineering Manager |
| 2026-08-19 13:31 | STEP-1.4 Done — Sprint plan 11 task, timeline 3 ngày, dependencies rõ, commit 641e49d | Project Manager |
| 2026-08-19 13:38 | STEP-1.5 Done — TDD 12 sections, 12 decisions chốt (D1-D12), sẵn cho 2.1 ∥ 2.2 chạy song song, commit 1b73bca | Tech Lead |
| 2026-08-19 13:49 | STEP-2.1 Done — 6 file mới (entity/constants/repository/service/interface/filter), 3 file sửa (2 endpoint + Program.cs), 11 test mới, 84/84 pass, CODE-GRAPH cập nhật, commit 79ae3cf | Senior Developer |
| 2026-08-19 13:42 | STEP-2.2 Done — JS handler ApiRequestLogged trong dashboard.js (+43 dòng), deviation đúng theo TDD (dashboard.js không phải signalr-client.js), syntax OK, commit d8abe37 | Junior Developer |
| 2026-08-19 13:55 | STEP-2.3 Done — Code review PASS 12 hạng mục vs TDD, build 0/0, test 84/84 pass, APPROVED cả 2 PR (79ae3cf backend + d8abe37 frontend), không có code change từ TL | Tech Lead |
| 2026-08-19 13:55 | STEP-2.4 Skipped — KHÔNG chạy STRIDE (không đụng auth/payment, DB schema chỉ INSERT parameterized, dữ liệu log không nhạy cảm, XSS-safe qua textContent); ghi backlog: cap body size 8KB ở filter khi go public | Tech Lead |
| 2026-08-19 14:12 | STEP-3.1 Done — UXR pass 6/7 criteria (C3 Fail: UI-001 High — Parameters null); 5 TC chạy xong; rebuild image cần thiết trước test; commit 345792c | UX/UI Reviewer |
| 2026-08-19 14:35 | Fix UI-001 — root cause: model binding trong Minimal API consume body stream TRƯỚC filter chain; fix: serialize context.Arguments[0] thay vì đọc raw stream; 85/85 test pass; verify curl OK; commit ae2a211 | Senior Developer |
| 2026-08-19 14:50 | Tech Lead review fix UI-001 — APPROVED (build 0/0, test 85/85, root cause đúng, null-safety đủ, TDD đã cập nhật pseudocode + R1/R5 + task T2.1.5); không có code change; sẵn sàng chuyển QA Engineer (STEP-3.2) | Tech Lead |
| 2026-08-19 17:13 | STEP-3.2 Done — 13/13 TC PASS (AddDevice+LaunchApp success/400/401/422, concurrent, DB persistence, UI-001 retest); rebuild image cần thiết (ae2a211 không có trong UXR image); 0 bug open; commit ac817b0 | QA Engineer |
| 2026-08-19 17:18 | STEP-3.3 Done — QA Lead sign-off APPROVED; 7/7 AC phủ đủ; 0 P0/P1 bug; OBS-02 truyền DevOps: rebuild image trước deploy staging | QA Lead |
| 2026-08-19 17:25 | STEP-3.4 Done — Rebuild --no-cache (SHA 1900e158, fix UI-001 verified), bảng ApiRequestLog OK, smoke test 6/6 pass (Parameters NOT null), commit 264a9a5 | DevOps Engineer |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
