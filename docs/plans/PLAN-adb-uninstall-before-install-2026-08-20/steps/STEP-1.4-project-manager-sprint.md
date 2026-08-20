---
step: 1.4
plan: ../PLAN-MASTER.md
agent: project-manager
status: done
completed_at: 2026-08-20 13:58
deps: [1.3]
---

# STEP 1.4 — Lên Task Board và Sprint Plan

## Input nhận
- Resource plan từ bước 1.3: `docs/planning/RESOURCE-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-1.3 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Tạo sprint plan với task board cho feature này: ánh xạ từng bước trong PLAN-MASTER (2.1 → 4.4) thành task, điền estimate, priority, assignee, status ban đầu. Lưu tại `docs/planning/SPRINT-adb-uninstall-before-install.md`.

## Definition of Done
- [ ] `docs/planning/SPRINT-adb-uninstall-before-install.md` đã tạo với: bảng backlog (Task ID, Mô tả, Assignee, Estimate, Status), task board (TODO/IN PROGRESS/REVIEW/DONE), lịch sử cập nhật
- [ ] `docs/planning/SPRINT-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Tất cả task từ Phase 2, 3, 4 được liệt kê với status Todo
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
- Đọc `docs/planning/RESOURCE-adb-uninstall-before-install.md` để lấy estimate và phân bổ team từ bước 1.3.
- Tạo `docs/planning/SPRINT-adb-uninstall-before-install.md` với 8 task (T-2.1 → T-4.4), tất cả status Todo, kèm dependency chain, task board, và câu hỏi mở cần TL chốt tại TDD.
- Xuất `docs/planning/SPRINT-adb-uninstall-before-install.docx` bằng `scripts/md_to_docx_kztek.py` (DOCX thành công; PDF thất bại do thiếu LaTeX — không block).
- Commit local hash `4435cd0`, chưa push (theo tiền lệ).

## Artifact
- `docs/planning/SPRINT-adb-uninstall-before-install.md` — Sprint plan chính
- `docs/planning/SPRINT-adb-uninstall-before-install.docx` — Xuất DOCX thành công

## Quyết định quan trọng
- Dependency cứng T-2.1 → T-3.1: Senior Dev KHÔNG được bắt đầu code trước khi TDD hoàn thành và được user/TL confirm.
- 3 câu hỏi mở ghi nhận rõ trong sprint plan để TL ưu tiên chốt tại T-2.1: AC5/Q1 (DB Settings key), Q2 (parse lỗi ADB uninstall), EC1 (MDM lock behavior).
- Không phân bổ Junior Dev (theo quyết định EM tại 1.3) — chỉ Senior Dev toàn bộ T-3.1.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Sprint plan đã tạo xong tại `docs/planning/SPRINT-adb-uninstall-before-install.md`; Priority P2 và phân bổ team đã chốt từ bước 1.3 — không hỏi lại.
- watch_out: T-3.1 (Code) BẮT BUỘC chờ T-2.1 (TDD) hoàn thành. Ba câu hỏi mở AC5/Q1 (DB Settings), Q2 (parse lỗi ADB), EC1 (MDM lock) phải được chốt trong TDD trước khi Senior Dev bắt đầu. TDD cần tham khảo: `docs/user-stories/US-adb-uninstall-before-install.md` (13 scenario, AC chi tiết), `docs/planning/RESOURCE-adb-uninstall-before-install.md` (risk assessment R1-R5).
- next_inputs: Sprint plan tại `docs/planning/SPRINT-adb-uninstall-before-install.md` — đặc biệt mục "Câu hỏi mở cần Tech Lead chốt tại T-2.1". PRD tại `docs/prd/PRD-adb-uninstall-before-install.md`. User Stories + AC tại `docs/user-stories/US-adb-uninstall-before-install.md`. Code hiện tại cần đọc: `IAdbService.cs`, `AdbService.cs`, `InstallCoordinator.cs`, `InstallEndpoints.cs`, `Index.cshtml`, `dashboard.js`.

## Commit
- Hash: 4435cd0
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
