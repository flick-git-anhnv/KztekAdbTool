---
step: "1.4"
plan: ../PLAN-MASTER.md
agent: project-manager
status: done
completed_at: "2026-08-19 13:31"
deps: ["1.1", "1.2", "1.3"]
---

# STEP 1.4 — Project Manager: Sprint Plan

## Input nhận
- `docs/prd/PRD-api-request-log.md` từ STEP-1.1
- `docs/user-stories/US-api-request-log.md` từ STEP-1.2
- `docs/planning/RESOURCE-api-request-log.md` từ STEP-1.3 (estimate + phân bổ)

## Nhiệm vụ
Lên sprint plan lightweight cho feature API Request Log: chia task, set timeline, tạo task board theo velocity thực tế của team. Sprint plan chỉ cần ở mức vừa đủ (P2 feature, không cần sprint dài chính thức).

## Definition of Done
- [ ] `docs/planning/SPRINT-api-request-log.md` đã được tạo
- [ ] Bảng backlog task có đủ cột: Task ID, Mô tả, Assignee, Story Points/Estimate, Status
- [ ] Task board chia rõ 4 cột: TODO / IN PROGRESS / REVIEW / DONE
- [ ] Timeline ước tính (ngày bắt đầu → ngày hoàn thành dự kiến) được ghi rõ
- [ ] Dependencies giữa các task (backend phải xong trước code review) được ghi rõ
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
- Tạo `docs/planning/SPRINT-api-request-log.md`: backlog 11 task (S1-T01–T11), task board 4 cột, timeline 3 ngày (19–21/08/2026)
- Ghi rõ dependency: S1-T09 (deploy staging) phải chờ P1 (adb-reconnect) deploy production xong; S1-T02 ∥ S1-T03 song song sau S1-T01 (TDD done); S1-T04 (code review) cần cả hai xong + verify-pr report
- Xuất DOCX thành công; PDF skip do thiếu xelatex trong môi trường Linux (không block workflow)
- Commit + không push (theo yêu cầu)

## Artifact
- `docs/planning/SPRINT-api-request-log.md` — Sprint plan nguồn gốc Markdown
- `docs/planning/SPRINT-api-request-log.docx` — xuất bởi `md_to_docx_kztek.py` (DOCX OK, PDF fail — xelatex missing)

## Quyết định quan trọng
- **Task order:** S1-T01 (TDD) → [S1-T02 ∥ S1-T03] → S1-T04 (review) → S1-T05 (security audit) → S1-T06 (UXR) → S1-T07 (QA) → S1-T08 (sign-off) → S1-T09 (staging) → S1-T10 (approve) → S1-T11 (prod)
- **WATCH OUT:** S1-T09 deploy staging BẮT BUỘC chờ P1 (adb-reconnect-after-restart) deploy production xong — DevOps Lead xác nhận thời điểm. Không deploy staging P2 trước P1 production để tránh staging bị contaminate bởi chưa có code P1 merged.
- **S1-T05 (security audit):** Đánh mark conditional — Tech Lead tự quyết khi tới bước đó. Nếu skip phải ghi rõ lý do trong step file 2.4.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Sprint plan đã chốt tại 11 task S1-T01–T11, không cần tạo lại hay đổi task ID
- watch_out: S1-T09 (staging) bị BLOCK cho đến khi P1 adb-reconnect deploy production xong — Tech Lead (1.5) không cần lo, nhưng nên biết để tham chiếu khi đặt điều kiện build
- next_inputs: `docs/planning/SPRINT-api-request-log.md` — TL tham chiếu task breakdown (S1-T01 là task của TL) khi lập TDD (1.5); đặc biệt xem cột Estimate và Phụ thuộc để hiểu scope

## Commit
- Hash: 641e49d
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
