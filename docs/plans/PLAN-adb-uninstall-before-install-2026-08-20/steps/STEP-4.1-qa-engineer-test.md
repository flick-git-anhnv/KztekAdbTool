---
step: 4.1
plan: ../PLAN-MASTER.md
agent: qa-engineer
status: todo
completed_at:
deps: [3.3]
---

# STEP 4.1 — QA Engineer: Viết Test Plan và Thực thi Test Case

## Input nhận
- Code đã merge + UXR pass từ bước 3.3
- TDD: `docs/tech-design/TDD-adb-uninstall-before-install.md`
- AC Given/When/Then: `docs/user-stories/US-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-3.3 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Viết test plan + test case chi tiết và thực thi (chạy app thật nếu có thiết bị/emulator). Bao phủ các scenario: checkbox tắt (hành vi mặc định), checkbox bật + install thành công, checkbox bật + package chưa cài (graceful skip + install tiếp), trạng thái progress SignalR khi flag bật.

## Definition of Done
- [ ] `docs/test-plans/TEST-PLAN-adb-uninstall-before-install.md` đã tạo
- [ ] `docs/test-cases/TC-adb-uninstall-before-install.md` đã tạo với ≥ 5 TC bao phủ AC
- [ ] Mỗi TC có: ID, Precondition, Steps, Expected Result, Actual Result, Pass/Fail
- [ ] TC bao phủ: (1) checkbox off — install bình thường không uninstall, (2) checkbox on + package đã cài — uninstall rồi install, (3) checkbox on + package chưa cài — warning log + install tiếp không abort, (4) SignalR progress hiển thị bước "Đang gỡ cài đặt..." khi flag bật, (5) UI checkbox default state = off
- [ ] `docs/test-plans/TEST-PLAN-adb-uninstall-before-install.docx` + `docs/test-cases/TC-adb-uninstall-before-install.docx` xuất thành công
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
[Điền sau khi hoàn thành]

## Artifact
[Điền sau khi hoàn thành]

## Quyết định quan trọng
[Điền sau khi hoàn thành]

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không có
- watch_out: Không có
- next_inputs: Không có

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
