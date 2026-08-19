---
step: 1.3
plan: ../PLAN-MASTER.md
agent: Engineering Manager
status: done
completed_at: 2026-08-19 10:16
deps: [1.2]
---

# STEP 1.3 — Estimate resource, xác nhận priority, phân bổ team

## Input nhận
Nhận Handoff Payload từ STEP-1.2. PRD + US đã có tại `docs/prd/PRD-adb-add-device-api.md` và `docs/user-stories/US-adb-add-device-api.md`.

## Nhiệm vụ
Estimate effort (2 API tái sử dụng nhiều service có sẵn — `AdbService.ConnectAsync`, `DeviceState` — nên effort thấp hơn plan mẫu launch-app một chút). Xác nhận priority P1. Phân bổ toàn bộ code cho Senior Developer (đụng auth + kết nối thiết bị, không phù hợp Junior — giống quyết định ở plan mẫu).

## Definition of Done
- [ ] `docs/planning/RESOURCE-adb-add-device-api.md` có estimate giờ, phân bổ Senior Developer
- [ ] Xuất DOCX/PDF theo R1

## Đã làm
- Đọc PRD + US để nắm scope 2 API (connect-by-ip, check-status).
- So sánh với plan mẫu (adb-launch-app-api): auth tái dùng ApiKeyEndpointFilter — effort thấp hơn.
- Viết `docs/planning/RESOURCE-adb-add-device-api.md`: estimate ~5–8h, phân bổ Senior Developer toàn bộ code, Junior skip, xác nhận P1.
- Xuất `docs/planning/RESOURCE-adb-add-device-api.docx` (PDF thất bại do thiếu xelatex — lỗi môi trường đã biết, không block).
- Commit 788a0f9 + push thành công.

## Artifact
- `docs/planning/RESOURCE-adb-add-device-api.md`
- `docs/planning/RESOURCE-adb-add-device-api.docx`

## Quyết định quan trọng
- Priority P1 xác nhận — không escalate.
- Toàn bộ code giao Senior Developer; Junior skip (đụng auth + kết nối thiết bị).
- Estimate ~5–8h (thấp hơn plan mẫu ~6–10h) vì auth tái dùng ApiKeyEndpointFilter nguyên trạng, không tạo middleware mới.
- Bước 3.2 bắt buộc security-audit-stride dù auth cũ — route mới tạo surface attack mới.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: RESOURCE file đã viết và commit 788a0f9, không cần viết lại estimate hay phân bổ team.
- watch_out: 2 assumption mở (mã HTTP NotFound cho serial, serial có trong response connect-by-ip không) vẫn chưa chốt — Project Manager lên sprint plan bình thường, Tech Lead chốt ở TDD (STEP-2.1).
- next_inputs: `docs/planning/RESOURCE-adb-add-device-api.md` + `docs/prd/PRD-adb-add-device-api.md` + `docs/user-stories/US-adb-add-device-api.md` — Project Manager dùng để lên task board và sprint plan.

## Commit
- Hash: 788a0f9
- Đã push: Yes

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
