---
task: adb-launch-app-api
created: 2026-08-18
updated: 2026-08-18 16:19
status: active
workflow: WF-FEATURE
priority: P1
---

# PLAN MASTER: API Launch App trên thiết bị Android qua ADB

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả
Bổ sung 1 API mới cho server `KztekAdbPublishTool.Web` (ASP.NET Core Minimal API) để client HTTP gọi vào, server ra lệnh bật (launch) 1 ứng dụng Android trên một thiết bị cụ thể qua ADB. API tái sử dụng `AdbService.LaunchAppAsync` đã có sẵn. Cơ chế xác thực bằng API key tĩnh (mới hoàn toàn — chưa có auth mechanism nào trong project hiện tại), cấu hình qua appsettings/env. Không có thay đổi giao diện UI.

## Nguồn yêu cầu
- Yêu cầu gốc: Tạo API POST nhận `serial` thiết bị + `app` (package name), gọi `AdbService.LaunchAppAsync(serial, packageName, ct)`, auth bằng header `x-api-key`.
- Workflow: WF-FEATURE — Yêu cầu tính năng mới (rút gọn: không UX Designer, không UX/UI Reviewer, CTO optional)
- Agent chain: Product Manager → Business Analyst → Engineering Manager → Project Manager → Tech Lead → Senior Developer → Tech Lead (review + security-audit-stride) → QA Engineer → QA Lead → DevOps Engineer → DevOps Lead

## Bước bị SKIP và lý do

| Bước WF-FEATURE gốc | Trạng thái | Lý do |
|---|---|---|
| Bước 3 — UI/UX Designer | ⏭️ Skipped | Không có thay đổi UI/giao diện — API backend thuần túy |
| Bước 5 — CTO review kiến trúc | ⏭️ Skipped (có điều kiện) | Feature không thuộc dạng lớn/chiến lược/kiến trúc hệ thống; Tech Lead có thể escalate nếu thấy rủi ro auth cần CTO quyết |
| Bước 9 — Junior Developer | ⏭️ Skipped | Task đụng auth (cơ chế mới) — không phù hợp cấp Junior; toàn bộ code giao Senior Developer |
| Bước 10b — UX/UI Reviewer | ⏭️ Skipped | Không có thay đổi giao diện UI |

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Phân tích & Lên kế hoạch

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | Viết PRD phạm vi hẹp: mục tiêu, AC tổng quan, non-goals | Product Manager | ✅ | `steps/STEP-1.1-product-manager-prd.md` | 2026-08-18 15:53 |
| 1.2 | Chi tiết hóa AC theo Given/When/Then, user story | Business Analyst | ✅ | `steps/STEP-1.2-business-analyst-ac.md` | 2026-08-18 15:56 |
| 1.3 | Estimate resource, quyết định priority P1, phân bổ team | Engineering Manager | ✅ | `steps/STEP-1.3-engineering-manager-estimate.md` | 2026-08-18 16:07 |
| 1.4 | Lên task board, sprint plan cho feature này | Project Manager | ✅ | `steps/STEP-1.4-project-manager-sprint.md` | 2026-08-18 16:11 |

### Phase 2: Thiết kế kỹ thuật

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | Viết TDD: API contract, auth middleware thiết kế, error handling, cấu hình appsettings | Tech Lead | ✅ | `steps/STEP-2.1-tech-lead-tdd.md` | 2026-08-18 16:19 |

### Phase 3: Phát triển

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | Code endpoint, auth middleware, unit test | Senior Developer | ⬜ | `steps/STEP-3.1-senior-developer-code.md` | - |
| 3.2 | Code review, security-audit-stride (bắt buộc vì đụng auth), quyết định merge | Tech Lead | ⬜ | `steps/STEP-3.2-tech-lead-review.md` | - |

### Phase 4: Kiểm thử & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 4.1 | Viết test plan, thực thi test case, log kết quả | QA Engineer | ⬜ | `steps/STEP-4.1-qa-engineer-test.md` | - |
| 4.2 | Sign-off chất lượng (P1 — bắt buộc QA Lead) | QA Lead | ⬜ | `steps/STEP-4.2-qa-lead-signoff.md` | - |
| 4.3 | Deploy lên môi trường tương ứng | DevOps Engineer | ⬜ | `steps/STEP-4.3-devops-engineer-deploy.md` | - |
| 4.4 | Approve staging, verify smoke test, approve + deploy production | DevOps Lead | ⬜ | `steps/STEP-4.4-devops-lead-approve.md` | - |

## Artifacts dự kiến (tổng)
- [ ] `docs/prd/PRD-adb-launch-app-api.md`
- [ ] `docs/user-stories/US-adb-launch-app-api.md`
- [ ] `docs/planning/RESOURCE-adb-launch-app-api.md`
- [ ] `docs/planning/SPRINT-adb-launch-app-api.md`
- [ ] `docs/tech-design/TDD-adb-launch-app-api.md`
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoint.cs` (hoặc tương đương)
- [ ] `src/KztekAdbPublishTool.Web/Middleware/ApiKeyMiddleware.cs` (hoặc tương đương)
- [ ] `tests/` — unit test cho endpoint + middleware
- [ ] `docs/test-plans/TEST-PLAN-adb-launch-app-api.md`
- [ ] `docs/test-cases/TC-adb-launch-app-api.md`
- [ ] `docs/devops/DEPLOY-adb-launch-app-api.md`

## Blockers
Không có

## Quyết định / Ghi chú tổng
- Tái sử dụng `AdbService.LaunchAppAsync(serial, packageName, ct)` — KHÔNG dùng lệnh `monkey` thô (xem comment trong `AdbService.cs` ~dòng 167-188).
- Auth bằng API key tĩnh (`x-api-key` header) là cơ chế MỚI hoàn toàn cho project này — Tech Lead quyết định phạm vi áp dụng (chỉ route mới hay cả `/api/install` và các route cũ).
- Tech Lead có thể escalate lên CTO nếu đánh giá rủi ro bảo mật của API key mechanism cần quyết định kiến trúc cao hơn.
- Bước 3.2 BẮT BUỘC chạy `security-audit-stride` vì đụng auth (CLAUDE.md §4 WF-FEATURE Bước 10a).

## Lịch sử cập nhật
| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-18 | Plan tạo mới | task-planner |
| 2026-08-18 15:53 | STEP-1.1 Done — PRD tạo xong, DOCX xuất thành công, PDF thất bại do thiếu LaTeX | Product Manager |
| 2026-08-18 15:56 | STEP-1.2 Done — User story 8 scenario Given/When/Then, DOCX OK, PDF thất bại (xelatex) | Business Analyst |
| 2026-08-18 16:07 | STEP-1.3 Done — Priority P1, Senior Dev phụ trách toàn bộ code, estimate ~6–10h, RESOURCE tạo xong (DOCX OK, PDF fail xelatex) | Engineering Manager |
| 2026-08-18 16:11 | STEP-1.4 Done — Sprint plan 7 task (T-2.1→T-4.4) tạo xong, DOCX OK, PDF fail xelatex; Phase 1 hoàn thành | Project Manager |
| 2026-08-18 16:19 | STEP-2.1 Done — TDD chốt POST /api/launch-app + IEndpointFilter + config LaunchApp:ApiKey (env `LaunchApp__ApiKey`); Q1-Q5 quyết định hết; DOCX OK, PDF fail xelatex; Phase 2 hoàn thành | Tech Lead |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
