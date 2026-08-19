---
task: adb-reconnect-after-restart
created: 2026-08-19
updated: 2026-08-19 12:03
status: active
workflow: WF-BUGFIX
priority: P1
---

# PLAN MASTER: BUG — ADB không tự reconnect sau khi service restart

> File này CHỈ chứa tổng quan + trạng thái. Chi tiết từng bước (mô tả đầy đủ, Handoff Payload, artifact chi tiết) nằm ở `steps/STEP-[N.M]-[tên].md` tương ứng — xem cột "Step file" bên dưới.

## Mô tả

Sau khi restart service, các API `GET /api/devices/{serial}/status` và `POST /api/devices/connect-by-ip` trả sai/rỗng cho đến khi user bấm "quét lại" trên UI. Nguyên nhân nghi ngờ: `DevicePollWorker` không tự gọi lại `adb connect` cho các thiết bị đã có trong persisted list ngay sau khi khởi động — `DeviceState` trống cho đến khi UI trigger scan. `GET /api/devices` không bị ảnh hưởng vì đọc từ persisted list, không phụ thuộc `DeviceState`. Bug này đang block việc go-live production của feature `adb-add-device-api`.

## Nguồn yêu cầu

- Yêu cầu gốc: User báo cáo — restart service xong phải bấm quét thủ công trên UI thì 2 API mới mới hoạt động đúng. Điều này block smoke test nhóm B trước go-live production.
- Workflow: WF-BUGFIX — Bug fix thường (P1, đang block production go-live)
- Agent chain: Senior Developer (triage + BUG report) → Senior Developer (fix + PR) → Tech Lead (review, yêu cầu /verify-pr) → QA Engineer (verify staging + regression) → QA Lead (sign-off P1) → DevOps Engineer (deploy fix)

## Phases & Steps

> **Session isolation (CLAUDE.md §16.5):** Mỗi bước ⬜/🔄 PHẢI chạy tách session — LOCAL dùng `Agent` subagent, WEB dùng `RemoteTrigger`. Agent/trigger tự tạo/cập nhật step file riêng, commit+push, rồi cập nhật đúng 1 dòng status ở bảng dưới đây.

### Phase 1: Triage — Reproduce & Root Cause

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 1.1 | Reproduce bug, xác định root cause, viết BUG report | Senior Developer | ✅ | `steps/STEP-1.1-triage-bug-report.md` | 2026-08-19 11:27 |

### Phase 2: Fix & Code Review

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 2.1 | Viết fix auto-reconnect sau restart, tạo PR | Senior Developer | ✅ | `steps/STEP-2.1-fix-auto-reconnect.md` | 2026-08-19 11:39 |
| 2.2 | Code review PR (yêu cầu /verify-pr report trước) | Tech Lead | ✅ | `steps/STEP-2.2-tech-lead-review.md` | 2026-08-19 11:43 |
| 2.3 | **[Bổ sung — Dispatcher tự phát hiện qua verify code, nâng severity từ P2/P3 QA báo lên P1]** Fix bug thứ 2: `OperationCanceledException` do per-device timeout (linked CTS `CancelAfter`) bị `catch` nhầm thành lệnh dừng toàn bộ vòng warm-up (`break`) — khiến device đầu tiên timeout sẽ chặn luôn các device WiFi còn lại không được thử reconnect. Phải phân biệt: timeout riêng của device (tiếp tục device kế) vs `ct` gốc bị cancel (dừng hẳn) | Senior Developer | ✅ | `steps/STEP-2.3-fix-timeout-cancellation-bug.md` | 2026-08-19 12:03 |
| 2.4 | Code review lại fix 2.3 | Tech Lead | ⬜ | `steps/STEP-2.4-tech-lead-review-2.md` | - |

### Phase 3: Verify & Deploy

| # | Bước | Agent | Status | Step file | Hoàn thành lúc |
|---|------|-------|--------|-----------|-----------------|
| 3.1 | Verify fix trên staging, regression test | QA Engineer | ✅ | `steps/STEP-3.1-qa-verify-staging.md` | 2026-08-19 11:50 |
| 3.1b | Re-verify sau fix 2.3 — xác nhận nhiều WiFi device offline không còn chặn nhau | QA Engineer | ⬜ | `steps/STEP-3.1b-qa-reverify.md` | - |
| 3.2 | Sign-off chất lượng (P1 — bắt buộc) | QA Lead | ⬜ | `steps/STEP-3.2-qa-lead-signoff.md` | - |
| 3.3 | Deploy fix lên môi trường tương ứng | DevOps Engineer | ⬜ | `steps/STEP-3.3-deploy-fix.md` | - |

## Artifacts dự kiến (tổng)

- [ ] `docs/bugs/BUG-adb-reconnect-after-restart.md` — BUG report đầy đủ (root cause, steps to reproduce, fix approach)
- [ ] PR trong `src/KztekAdbPublishTool.Web/` — code fix auto-reconnect (có /verify-pr report đính kèm)
- [ ] Smoke test log trong PR — verify trên staging

## Blockers

Không có

## Quyết định / Ghi chú tổng

- Không cần tạo US/PRD mới — đây là bug fix trên feature `adb-add-device-api` đã có TDD.
- Không có bước UX/UI Reviewer — fix thuần backend, không đụng giao diện.
- QA Lead sign-off bắt buộc vì P1 (per WF-BUGFIX §4 CLAUDE.md).
- Fix phải xử lý trường hợp: khi service khởi động, `DevicePollWorker` (hoặc hosted service tương đương) tự đọc persisted device list và gọi `adb connect` cho từng thiết bị — trước khi nhận request API đầu tiên.

## Lịch sử cập nhật

| Ngày | Cập nhật | Agent |
|------|----------|-------|
| 2026-08-19 | Plan tạo mới | task-planner |
| 2026-08-19 | STEP-1.1 Done — root cause xác nhận tại DevicePollWorker.cs:56-77 + 111; BUG report viết xong | Senior Developer |
| 2026-08-19 11:39 | STEP-2.1 Done — IAdbService + WarmUpReconnectAsync + 6 unit tests; commit 3a86825; 71/71 PASS; chuyển Tech Lead review | Senior Developer |
| 2026-08-19 11:43 | STEP-2.2 Done — Tech Lead APPROVE commit 3a86825; verify build+test độc lập PASS; sẵn sàng QA staging | Tech Lead |
| 2026-08-19 11:50 | STEP-3.1 Done — QA verify local: build 0 error, 71/71 PASS, warm-up log confirmed, TC-3/TC-4 PASS, TC-1/TC-2 ENV_LIMIT (không có Android device thật); phát hiện P2/P3 log misleading; chuyển QA Lead sign-off | QA Engineer |
| 2026-08-19 | Dispatcher đọc trực tiếp `DevicePollWorker.WarmUpReconnectAsync` + `AdbService.RunAsync` để verify artifact trước khi chuyển QA Lead — phát hiện finding QA báo (P2/P3) thực chất nghiêm trọng hơn: `RunAsync` dùng `CancellationTokenSource.CreateLinkedTokenSource(ct).CancelAfter(timeoutMs)`, nên timeout riêng từng device cũng ném `OperationCanceledException` giống hệt việc `ct` gốc bị cancel. `WarmUpReconnectAsync` catch `OperationCanceledException` → `break` toàn vòng lặp → 1 device timeout sẽ chặn TẤT CẢ device WiFi còn lại không được thử reconnect, không đạt mục tiêu fix cho fleet nhiều thiết bị. Nâng severity P2/P3 → **P1**, chèn thêm Bước 2.3 (SD fix lại) → 2.4 (TL review lại) → 3.1b (QA re-verify) trước khi vào 3.2 sign-off. KHÔNG cho QA Lead sign-off với bug này còn tồn tại. | Dispatcher |
| 2026-08-19 12:03 | STEP-2.3 Done — fix RunAsync re-throw OCE khi ct cancel; WarmUpReconnectAsync phân biệt timeout vs shutdown; 2 test mới; 73/73 PASS; commit cff893f; chuyển Tech Lead review lại (2.4) | Senior Developer |

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
**Cách đọc nhanh:** đọc MASTER trước → nếu cần chi tiết bước cụ thể mới mở step file tương ứng.
