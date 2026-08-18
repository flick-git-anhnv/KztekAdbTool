---
step: "1.4"
plan: ../PLAN-MASTER.md
agent: project-manager
status: done
completed_at: 2026-08-18 16:11
deps: ["1.3"]
---

# STEP 1.4 — Project Manager: Task Board & Sprint Plan

## Input nhận
Output từ Bước 1.3: RESOURCE plan, estimate effort đã duyệt.

## Nhiệm vụ
Lên task board và sprint plan mini cho feature này: liệt kê từng task theo agent chain còn lại (Tech Lead TDD → Senior Dev code → Tech Lead review → QA → DevOps), gán estimate, thiết lập Done criteria cho sprint.

## Definition of Done
- [ ] File `docs/planning/SPRINT-adb-launch-app-api.md` đã tạo
- [ ] Task board có đủ mọi bước còn lại (Phase 2, 3, 4) với status Todo
- [ ] Mỗi task có estimate, assignee, DoD ngắn gọn
- [ ] Đã xuất DOCX + PDF bằng `scripts/md_to_docx_kztek.py`

## Đã làm
- Tạo `docs/planning/SPRINT-adb-launch-app-api.md` với 7 task (T-2.1 đến T-4.4) cho Phase 2–4
- Gán estimate, assignee, Done criteria ngắn gọn cho từng task theo RESOURCE plan đã duyệt
- Định nghĩa DoD sprint (8 scenario pass, QA sign-off, staging + production deploy, security-audit-stride pass)
- Liệt kê rủi ro (Q1–Q5 chưa chốt là rủi ro chính) và dependency ngoài sprint
- Xuất DOCX thành công; PDF thất bại do thiếu xelatex (ghi nhận, không block)

## Artifact
- `docs/planning/SPRINT-adb-launch-app-api.md` — Sprint plan chính
- `docs/planning/SPRINT-adb-launch-app-api.docx` — Xuất bởi md_to_docx_kztek.py (OK)
- `docs/planning/SPRINT-adb-launch-app-api.pdf` — Thất bại (thiếu xelatex)

## Quyết định quan trọng
Không có — task board phản ánh đúng quyết định từ STEP-1.3 (P1, Senior Dev, skip Junior)

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Priority P1, phân bổ Senior Dev toàn bộ code, skip Junior — đã chốt, không xem lại; Sprint plan v1.0 đã có đủ 7 task Phase 2–4
- watch_out: Q1–Q5 (HTTP verb, response schema, config key, validation level, behavior am start khi app đang chạy) vẫn CHƯA chốt — Tech Lead PHẢI chốt toàn bộ trong TDD (T-2.1) TRƯỚC khi Senior Dev bắt đầu T-3.1; KHÔNG tự quyết các điểm này
- next_inputs: `docs/prd/PRD-adb-launch-app-api.md` + `docs/user-stories/US-adb-launch-app-api.md` + `docs/planning/SPRINT-adb-launch-app-api.md` — 3 file Tech Lead PHẢI đọc trước khi viết TDD (T-2.1)

## Commit
- Hash: [điền sau khi commit]
- Đã push: [điền sau khi push]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
