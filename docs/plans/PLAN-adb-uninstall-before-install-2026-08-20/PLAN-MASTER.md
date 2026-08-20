---
task: adb-uninstall-before-install
created: 2026-08-20
updated: 2026-08-20 13:58
status: active
workflow: WF-FEATURE
priority: P2
---

# PLAN MASTER: Tùy chọn Gỡ cài đặt App trước khi Cài đặt (Uninstall Before Install)

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả
Thêm 1 checkbox trên Dashboard (`Index.cshtml`) cho phép user chọn có muốn server chạy `adb uninstall <package>` trên thiết bị TRƯỚC KHI chạy `adb install` hay không. Mặc định tắt (giữ hành vi hiện tại: chỉ `adb install -r`). Khi bật, nếu package chưa cài trên thiết bị, lỗi uninstall được log warning và bỏ qua — không abort luồng install.

## Nguồn yêu cầu
- Yêu cầu gốc: "Thêm option lựa chọn có ra lệnh gỡ cài đặt app trước khi update lại hay không trên view"
- Workflow: WF-FEATURE — Yêu cầu tính năng mới (rút gọn: không UI/UX Designer; CÓ UX/UI Reviewer vì đổi UI Dashboard)
- Agent chain: Product Manager → Business Analyst → Engineering Manager → Project Manager → Tech Lead → Senior Developer → Tech Lead (review) → UX/UI Reviewer → QA Engineer → QA Lead → DevOps Engineer → DevOps Lead

## Bước bị SKIP và lý do

| Bước WF-FEATURE gốc | Trạng thái | Lý do |
|---|---|---|
| Bước 3 — UI/UX Designer | ⏭️ Skipped | Chỉ thêm 1 checkbox theo đúng pattern `chk-auto-detect` đã có (`form-check form-switch`), không phải màn hình/luồng mới — không cần thiết kế mockup/wireframe riêng |
| Bước 5 — CTO review kiến trúc | ⏭️ Skipped (có điều kiện) | Feature không thuộc dạng lớn/chiến lược; không đụng auth/payment/kiến trúc hệ thống. Tech Lead có thể escalate nếu đánh giá cần |
| Bước 9 — Junior Developer | ⏭️ Skipped | Mặc dù UI đơn giản, phần `InstallCoordinator`/`AdbService` đụng core install flow — EM quyết định tại bước 1.3 giao Senior để an toàn |
| Bước 10a — security-audit-stride | ⏭️ Skipped (có điều kiện) | Không đụng auth/payment/DB schema/dữ liệu nhạy cảm theo định nghĩa cứng — Tech Lead có thể tự chạy nếu thấy cần |

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Phân tích & Lên kế hoạch

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | Viết PRD phạm vi hẹp: mục tiêu, AC tổng quan, non-goals | Product Manager | ✅ | `steps/STEP-1.1-product-manager-prd.md` | 2026-08-20 13:47 |
| 1.2 | Chi tiết hóa AC theo Given/When/Then, user story | Business Analyst | ✅ | `steps/STEP-1.2-business-analyst-ac.md` | 2026-08-20 13:51 |
| 1.3 | Estimate resource, quyết định priority P2, phân bổ team (confirm Senior Dev) | Engineering Manager | ✅ | `steps/STEP-1.3-engineering-manager-estimate.md` | 2026-08-20 13:55 |
| 1.4 | Lên task board, sprint plan cho feature này | Project Manager | ✅ | `steps/STEP-1.4-project-manager-sprint.md` | 2026-08-20 13:58 |

### Phase 2: Thiết kế kỹ thuật

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | Viết TDD: chốt field `UninstallBeforeInstall`, vị trí checkbox UI, thứ tự gọi trong `DoInstallAsync`, SignalR progress bước mới, quyết định lưu DB Settings | Tech Lead | ⬜ | `steps/STEP-2.1-tech-lead-tdd.md` | - |

### Phase 3: Phát triển

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | Code: `UninstallApkAsync` trong AdbService/IAdbService, field `UninstallBeforeInstall` trong InstallRequest, logic uninstall-then-install trong `DoInstallAsync`, checkbox UI + JS | Senior Developer | ⬜ | `steps/STEP-3.1-senior-developer-code.md` | - |
| 3.2 | Code review, quyết định merge | Tech Lead | ⬜ | `steps/STEP-3.2-tech-lead-review.md` | - |
| 3.3 | Chạy app thật, chụp screenshot checkbox mới trên Dashboard, đánh giá C1–C7 | UX/UI Reviewer | ⬜ | `steps/STEP-3.3-uxr-review.md` | - |

### Phase 4: Kiểm thử & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 4.1 | Viết test plan, thực thi test case (checkbox on/off, uninstall graceful khi package chưa cài) | QA Engineer | ⬜ | `steps/STEP-4.1-qa-engineer-test.md` | - |
| 4.2 | Sign-off chất lượng (P2 — bắt buộc QA Lead) | QA Lead | ⬜ | `steps/STEP-4.2-qa-lead-signoff.md` | - |
| 4.3 | Deploy lên môi trường tương ứng | DevOps Engineer | ⬜ | `steps/STEP-4.3-devops-engineer-deploy.md` | - |
| 4.4 | Approve staging, verify smoke test, approve + deploy production | DevOps Lead | ⬜ | `steps/STEP-4.4-devops-lead-approve.md` | - |

## Artifacts dự kiến (tổng)
- [ ] `docs/prd/PRD-adb-uninstall-before-install.md`
- [ ] `docs/user-stories/US-adb-uninstall-before-install.md`
- [ ] `docs/planning/RESOURCE-adb-uninstall-before-install.md`
- [ ] `docs/planning/SPRINT-adb-uninstall-before-install.md`
- [ ] `docs/tech-design/TDD-adb-uninstall-before-install.md`
- [ ] `src/KztekAdbPublishTool.Web/Services/IAdbService.cs` (thêm `UninstallApkAsync`)
- [ ] `src/KztekAdbPublishTool.Web/Services/AdbService.cs` (thêm `UninstallApkAsync`)
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/InstallEndpoints.cs` (thêm field `UninstallBeforeInstall` trong `InstallRequest`)
- [ ] `src/KztekAdbPublishTool.Web/Services/InstallCoordinator.cs` (logic uninstall-before-install trong `DoInstallAsync`)
- [ ] `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` (thêm checkbox `chk-uninstall-before-install`)
- [ ] `src/KztekAdbPublishTool.Web/wwwroot/js/dashboard.js` (truyền flag `uninstallBeforeInstall` qua `apiPost`)
- [ ] `docs/ux-review/UX-REVIEW-adb-uninstall-before-install.md`
- [ ] `docs/test-plans/TEST-PLAN-adb-uninstall-before-install.md`
- [ ] `docs/test-cases/TC-adb-uninstall-before-install.md`
- [ ] `docs/devops/DEPLOY-adb-uninstall-before-install.md`

## Blockers
Không có

## Quyết định / Ghi chú tổng
- `UninstallApkAsync`: nếu package chưa cài trên thiết bị, adb trả exit code khác 0 (vd: `Failure [DELETE_FAILED_INTERNAL_ERROR]`) — KHÔNG abort luồng install, chỉ log warning và tiếp tục. Tech Lead chốt xử lý lỗi chi tiết trong TDD (2.1).
- Checkbox UI dùng đúng pattern `form-check form-switch` như `chk-auto-detect` đã có — Tech Lead chốt vị trí cụ thể trong TDD (2.1).
- Trạng thái checkbox lưu DB Settings: Tech Lead quyết định cuối trong TDD (2.1) — đề xuất CÓ để nhất quán với PackageName/ApkPath.
- SignalR progress khi flag bật: thêm bước "Đang gỡ cài đặt..." (trước các bước tiến độ hiện tại) để UX nhất quán.
- Senior Developer được chọn thay vì Junior vì đụng core install flow `InstallCoordinator`/`AdbService`.

## Lịch sử cập nhật
| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-20 | Plan tạo mới | task-planner |
| 2026-08-20 13:47 | Bước 1.1 Done — PRD tạo tại docs/prd/PRD-adb-uninstall-before-install.md, commit ece966f | Product Manager |
| 2026-08-20 13:51 | Bước 1.2 Done — US tạo tại docs/user-stories/US-adb-uninstall-before-install.md (5 US, 13 scenario), commit 2f5b1e0 | Business Analyst |
| 2026-08-20 13:55 | Bước 1.3 Done — RESOURCE tạo tại docs/planning/RESOURCE-adb-uninstall-before-install.md (Priority P2, Senior Dev, estimate 10–13h), commit 92dd32d | Engineering Manager |
| 2026-08-20 13:58 | Bước 1.4 Done — SPRINT tạo tại docs/planning/SPRINT-adb-uninstall-before-install.md (8 task T-2.1→T-4.4, tất cả Todo), commit 4435cd0. **Phase 1 hoàn thành.** | Project Manager |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
