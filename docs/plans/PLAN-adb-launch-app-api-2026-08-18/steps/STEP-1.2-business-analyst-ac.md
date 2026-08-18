---
step: "1.2"
plan: ../PLAN-MASTER.md
agent: business-analyst
status: done
completed_at: 2026-08-18 15:56
deps: ["1.1"]
---

# STEP 1.2 — Business Analyst: Chi tiết hóa AC

## Input nhận
Output từ Bước 1.1 (Product Manager): `docs/prd/PRD-adb-launch-app-api.md`

## Nhiệm vụ
Chi tiết hóa acceptance criteria theo format Given/When/Then, viết user story đầy đủ bao gồm các luồng happy path và error path (thiết bị không tồn tại, package không hợp lệ, sai API key, thiết bị offline).

## Definition of Done
- [ ] File `docs/user-stories/US-adb-launch-app-api.md` đã tạo
- [ ] User story có AC cho: happy path launch thành công, API key sai → 401, serial không tồn tại → 404, package name trống/invalid → 400, thiết bị offline → lỗi phù hợp
- [ ] Mọi AC theo format Given/When/Then
- [ ] Đã xuất DOCX + PDF bằng `scripts/md_to_docx_kztek.py`

## Đã làm
- Đọc PRD AC1-AC6, struct `AdbCommandResult` và hành vi `LaunchAppAsync` (resolve-activity + am start).
- Đọc `DeviceState`: trạng thái Online (`adb.State == "device"`) và Offline (mọi trạng thái khác).
- Tạo `docs/user-stories/US-adb-launch-app-api.md` với 8 scenario Given/When/Then (SC-01 đến SC-08), business flow Mermaid, 6 business rules, 7 edge cases, và 5 câu hỏi mở cho Tech Lead.
- Xuất DOCX thành công; PDF thất bại do thiếu xelatex/LibreOffice (ghi nhận, không block).

## Artifact
- `docs/user-stories/US-adb-launch-app-api.md` — User story đầy đủ 8 scenario
- `docs/user-stories/US-adb-launch-app-api.docx` — Xuất DOCX OK
- `docs/user-stories/US-adb-launch-app-api.pdf` — Thất bại (thiếu xelatex)

## Quyết định quan trọng
- SC-03 (404) vs SC-05 (422): phân biệt rõ "serial chưa bao giờ tồn tại" với "thiết bị offline" — Business Rule BR2.
- SC-06: Package không cài → ADB trả `StdErr = "No activities found to run"`, ExitCode = -1 → HTTP 422 kèm thông báo ADB.
- Auth scope: BR3 xác nhận cứng — `x-api-key` CHỈ áp endpoint mới.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: AC đã đủ 8 scenario Given/When/Then trong `docs/user-stories/US-adb-launch-app-api.md` — KHÔNG viết lại AC; chỉ tham chiếu file này khi thiết kế TDD.
- watch_out: HTTP verb (POST/GET), response schema (có trả stdOut/stdErr không), tên config key, mức validation package name, và hành vi am start khi app đã chạy (Q1-Q5) CHƯA chốt — Tech Lead cần quyết định tất cả ở STEP-2.1 TDD. Không gắn cứng verb hay schema khi code.
- next_inputs: `docs/user-stories/US-adb-launch-app-api.md` (SC-01 đến SC-08 + BR1-BR6 + EC1-EC7 + Q1-Q5 để Tech Lead thiết kế API contract), `docs/prd/PRD-adb-launch-app-api.md` (Non-goals và Scope).

## Commit
- Hash: dcb475c
- Đã push: Không — remote trả 403 (không có quyền push), cần xác thực thủ công

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
