---
task: adb-add-device-api
created: 2026-08-19
updated: 2026-08-19 17:20
status: active
workflow: WF-FEATURE
priority: P1
---

# PLAN MASTER: API Thêm Thiết Bị (Connect-by-IP) và Kiểm Tra Kết Nối ADB

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả
Bổ sung 2 API endpoint mới vào server `KztekAdbPublishTool.Web` (ASP.NET Core Minimal API):

1. **API "Kết nối thiết bị theo IP"** (`POST /api/devices/connect-by-ip`): nhận `ip` (bắt buộc) + `port` (tùy chọn, mặc định `5555`), thực hiện `adb connect ip:port` bằng cách tái sử dụng `AdbService.ConnectAsync` đã có. Vừa kết nối vừa đưa thiết bị vào danh sách theo dõi. API public, có auth `x-api-key` — tách biệt khỏi endpoint nội bộ `/api/devices/connect` dành cho UI.

2. **API "Kiểm tra trạng thái kết nối ADB"** (`GET /api/devices/{serial}/status`): nhận `serial` của 1 thiết bị, trả về trạng thái kết nối hiện tại (`Online` / `Offline` / `NotFound`) bằng cách tái sử dụng `DeviceState` (đã dùng trong `LaunchAppEndpoints.cs` qua `CheckDeviceState`) — không gọi lệnh ADB trực tiếp nếu `DeviceState` đã đủ thông tin (đồng bộ qua `DevicePollWorker`).

Cả 2 API dùng lại đúng `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` đã có — không tạo thêm section config hay API key mới.

## Nguồn yêu cầu
- Yêu cầu gốc: Bổ sung 2 API: (1) connect-by-ip (gộp add-device + kết nối, port mặc định 5555), (2) check-connection theo serial (dùng DeviceState, không gọi adb trực tiếp). Auth x-api-key tái dùng LaunchApp:ApiKey đã có.
- Workflow: WF-FEATURE — Yêu cầu tính năng mới (rút gọn: không UI/UX Designer, không UX/UI Reviewer, CTO optional)
- Agent chain: Product Manager → Business Analyst → Engineering Manager → Project Manager → Tech Lead → Senior Developer → Tech Lead (review + security-audit-stride) → QA Engineer → QA Lead → DevOps Engineer → DevOps Lead

## Bước bị SKIP và lý do

| Bước WF-FEATURE gốc | Trạng thái | Lý do |
|---|---|---|
| Bước 3 — UI/UX Designer | ⏭️ Skipped | Không có thay đổi UI/giao diện — API backend thuần túy |
| Bước 5 — CTO review kiến trúc | ⏭️ Skipped (có điều kiện) | Feature tái dùng cơ chế auth đã có, không tạo cơ chế mới — không thuộc dạng lớn/chiến lược. Tech Lead có thể escalate nếu đánh giá rủi ro bảo mật cần CTO quyết |
| Bước 9 — Junior Developer | ⏭️ Skipped | Task đụng auth + kết nối thiết bị — không phù hợp cấp Junior; toàn bộ code giao Senior Developer |
| Bước 10b — UX/UI Reviewer | ⏭️ Skipped | Không có thay đổi giao diện UI |

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Phân tích & Lên kế hoạch

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | Viết PRD phạm vi hẹp: 2 API mới, mục tiêu, AC tổng quan, non-goals | Product Manager | ✅ | `steps/STEP-1.1-product-manager-prd.md` | 2026-08-19 10:09 |
| 1.2 | Chi tiết hóa AC theo Given/When/Then, user story cho 2 API | Business Analyst | ✅ | `steps/STEP-1.2-business-analyst-ac.md` | 2026-08-19 10:13 |
| 1.3 | Estimate resource, xác nhận priority P1, phân bổ team | Engineering Manager | ✅ | `steps/STEP-1.3-engineering-manager-estimate.md` | 2026-08-19 10:16 |
| 1.4 | Lên task board, sprint plan cho feature này | Project Manager | ✅ | `steps/STEP-1.4-project-manager-sprint.md` | 2026-08-19 10:18 |

### Phase 2: Thiết kế kỹ thuật

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | Viết TDD: API contract 2 endpoint, routing design, reuse strategy (AdbService.ConnectAsync + DeviceState), error handling | Tech Lead | ✅ | `steps/STEP-2.1-tech-lead-tdd.md` | 2026-08-19 10:35 |

### Phase 3: Phát triển

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | Code 2 endpoint mới, unit test, cập nhật CODE-GRAPH | Senior Developer | ✅ | `steps/STEP-3.1-senior-developer-code.md` | 2026-08-19 16:30 |
| 3.2 | Code review, security-audit-stride (bắt buộc vì đụng auth + kết nối thiết bị), quyết định merge | Tech Lead | ✅ | `steps/STEP-3.2-tech-lead-review.md` | 2026-08-19 17:05 |

### Phase 4: Kiểm thử & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 4.1 | Viết test plan, thực thi test case, log kết quả | QA Engineer | ✅ | `steps/STEP-4.1-qa-engineer-test.md` | 2026-08-19 10:46 |
| 4.2 | Sign-off chất lượng (P1 — bắt buộc QA Lead) | QA Lead | ✅ | `steps/STEP-4.2-qa-lead-signoff.md` | 2026-08-19 17:20 |
| 4.3 | Deploy lên môi trường tương ứng | DevOps Engineer | ⬜ | `steps/STEP-4.3-devops-engineer-deploy.md` | - |
| 4.4 | Approve staging, verify smoke test, approve + deploy production | DevOps Lead | ⬜ | `steps/STEP-4.4-devops-lead-approve.md` | - |

## Artifacts dự kiến (tổng)
- [ ] `docs/prd/PRD-adb-add-device-api.md`
- [ ] `docs/user-stories/US-adb-add-device-api.md`
- [ ] `docs/planning/RESOURCE-adb-add-device-api.md`
- [ ] `docs/planning/SPRINT-adb-add-device-api.md`
- [ ] `docs/tech-design/TDD-adb-add-device-api.md`
- [ ] `src/KztekAdbPublishTool.Web/Endpoints/` — file endpoint mới cho 2 API (tên cụ thể do Tech Lead quyết ở TDD)
- [ ] `tests/` — unit test cho 2 endpoint mới
- [ ] `docs/test-plans/TEST-PLAN-adb-add-device-api.md`
- [ ] `docs/test-cases/TC-adb-add-device-api.md`
- [ ] `docs/devops/DEPLOY-adb-add-device-api.md`

## Blockers
Không có

## Quyết định / Ghi chú tổng
- **Gộp "Add device" + "Connect by IP" thành 1 API duy nhất** — gọi `AdbService.ConnectAsync` là đủ để vừa kết nối vừa đưa thiết bị vào danh sách theo dõi.
- **Auth tái dùng nguyên trạng**: `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` (env `LaunchApp__ApiKey`) — KHÔNG tạo section config mới hay API key mới. Tech Lead có thể đề xuất đổi tên section cho rõ nghĩa hơn nhưng ưu tiên tái dùng nguyên trạng.
- **Check-connection dùng `DeviceState`** (không gọi ADB trực tiếp) — đây là thiết kế đã chốt. DevicePollWorker đồng bộ trạng thái định kỳ; API chỉ đọc cache đó. Tech Lead cần xác nhận `DeviceState` đủ thông tin cho use case này ở TDD.
- **API mới tách biệt khỏi endpoint nội bộ** — `/api/devices/connect` (UI nội bộ, không auth) và `/api/devices/connect-by-ip` (public, có auth x-api-key) là 2 route khác nhau, không conflict.
- **Bước 3.2 BẮT BUỘC chạy `security-audit-stride`** — dù tái dùng auth cũ nhưng route mới, surface attack mới; Tech Lead phải verify trước khi merge (CLAUDE.md §4 WF-FEATURE Bước 10a).
- **Port mặc định 5555**: nếu client không truyền `port`, API tự điền `5555` trước khi gọi `ConnectAsync` — hành vi đã chốt.
- **`appsettings.json` bị hook bảo vệ**: Tương tự plan mẫu, nếu cần thêm config mới thì user tự tay sửa ngoài quy trình agent.

## Lịch sử cập nhật
| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-19 | Plan tạo mới | task-planner |
| 2026-08-19 10:09 | STEP-1.1 Done — PRD viết xong, DOCX xuất, commit 359f537 | Product Manager |
| 2026-08-19 10:13 | STEP-1.2 Done — US viết xong 14 scenario, DOCX xuất, commit b7e9709 | Business Analyst |
| 2026-08-19 10:16 | STEP-1.3 Done — RESOURCE estimate ~5-8h, Senior Developer phân bổ, P1 xác nhận, commit 788a0f9 | Engineering Manager |
| 2026-08-19 10:18 | STEP-1.4 Done — Sprint plan viết xong, DOCX xuất, commit 5992455. **Phase 1 hoàn thành toàn bộ (1.1–1.4 Done).** Status plan: planning → active | Project Manager |
| 2026-08-19 10:35 | STEP-2.1 Done — TDD viết xong, chốt Q1-Q7, DOCX xuất (PDF skip do thiếu xelatex). Quyết định: serial trong response API 1 = "ip:port", 404 cho serial NotFound API 2, trigger poll sau connect, không escalate CTO. **Phase 2 hoàn thành.** | Tech Lead |
| 2026-08-19 16:30 | STEP-3.1 Done — DeviceConnectionEndpoints.cs tạo xong 2 endpoint, ValidateConnectInput public static, Program.cs đăng ký, 22 unit test mới (64/64 PASS), CODE-GRAPH cập nhật, commit 3c5541b đã push. | Senior Developer |
| 2026-08-19 17:05 | STEP-3.2 Done — Code review APPROVE (13/13 checklist TDD Pass). verify-pr: 4 PASS + 1 SKIP (lint). security-audit-stride: OWASP 7 Pass + 3 N-A + 0 Fail; STRIDE 4 Pass + 1 N-A + 1 FYI (DoS optional, không blocker). Không escalate CTO. **Phase 3 hoàn thành.** Chuyển tiếp Phase 4 QA. | Tech Lead |
| 2026-08-19 10:46 | STEP-4.1 Done — TEST-PLAN + TC viết xong. Thực thi 19/19 PASS (NHOM A, HTTP thật, app local port 5299). 6 case NHOM B cần smoke test thủ công tại staging. DOCX xuất OK (PDF skip). Commit b77dff4 đã push. | QA Engineer |
| 2026-08-19 17:20 | STEP-4.2 Done — QA Lead sign-off PASS CÓ ĐIỀU KIỆN. P0=0, P1=0. Điều kiện: DevOps PHẢI smoke test TC-C01–TC-C06 (NHOM B) trên thiết bị Android thật tại staging trước go-live production. Commit e90d9d0 đã push. | QA Lead |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
