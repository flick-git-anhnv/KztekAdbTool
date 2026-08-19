---
step: "1.1"
plan: ../PLAN-MASTER.md
agent: product-manager
status: done
completed_at: "2026-08-19 13:19"
deps: []
---

# STEP 1.1 — Product Manager: Viết PRD

## Input nhận
- Yêu cầu từ user: ghi lịch sử request gọi vào AddDevice API và LaunchApp API; lưu bền vững SQLite; hiển thị real-time SignalR vào panel `#log`.
- Tài liệu liên quan: `docs/prd/PRD-adb-add-device-api.md`, `docs/prd/PRD-adb-launch-app-api.md` (2 API cần log).
- Priority: P2.

## Nhiệm vụ
Viết PRD cho feature API Request Log: xác định rõ mục tiêu, acceptance criteria đo được, non-goals (tránh scope creep), và metric để đánh giá tính năng đã hoạt động đúng.

## Definition of Done
- [ ] `docs/prd/PRD-api-request-log.md` đã được tạo
- [ ] Mục "Goals" nêu rõ: log 2 API nào, lưu trữ ở đâu (SQLite), hiển thị ở đâu (panel #log, real-time SignalR)
- [ ] Mục "Acceptance Criteria" có ít nhất 4 tiêu chí đo được (VD: request được log trong vòng X giây, DB persist sau restart, hiển thị đúng trên mọi tab đang mở...)
- [ ] Mục "Non-goals" ghi rõ: không log API khác ngoài 2 API này, không thay đổi ApiKeyEndpointFilter, không UI mới
- [ ] Mục "Metric đo lường" có ít nhất 1 metric khả thi để verify sau deploy
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx` (và `.pdf` nếu converter có sẵn)

## Đã làm
- Đọc PRD của 2 API gốc (`PRD-adb-add-device-api.md`, `PRD-adb-launch-app-api.md`) để nắm rõ scope và Non-goals đã có.
- Viết `docs/prd/PRD-api-request-log.md` đầy đủ: Tổng quan, Goals (4 mục), Non-goals (7 mục), User Story, AC (7 tiêu chí đo được), Metric đo lường (4 metric), Rủi ro/Câu hỏi mở (3 rủi ro, 2 câu hỏi mở).
- Chạy `scripts/md_to_docx_kztek.py` — DOCX thành công, PDF thất bại do thiếu LaTeX (chấp nhận theo §19.4).
- Commit hash `e0fa758`.

## Artifact
- `docs/prd/PRD-api-request-log.md` — PRD hoàn chỉnh
- `docs/prd/PRD-api-request-log.docx` — xuất bởi md_to_docx_kztek.py (DOCX OK, PDF skip)

## Quyết định quan trọng
1. **AC6 — Log cả 401 auth fail:** Quyết định ghi log ngay cả khi request bị từ chối do sai API key — cần thiết để phát hiện brute-force. Tuy nhiên, ghi chú Q1 vào Rủi ro: nếu `ApiKeyEndpointFilter` throw 401 trước khi vào handler thì cần interceptor/middleware riêng (Tech Lead quyết định ở TDD).
2. **Non-goal rõ ràng về retention:** Không có retention policy trong phiên bản này — tránh scope creep, ghi vào Non-goals để track ở backlog tương lai.
3. **Metric khả thi:** 4 metric đều có thể đo thủ công bằng Postman/curl + query SQLite trực tiếp — không phụ thuộc tool monitoring ngoài.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không viết lại PRD, không hỏi lại scope (đã chốt qua scope-check). Không mở rộng sang API khác ngoài 2 API đã nêu.
- watch_out: AC6 (log 401) đòi hỏi Tech Lead quyết định inject point (middleware vs. handler-level) — BA cần phản ánh điều này trong Given/When/Then của user story auth-fail. Q1 trong PRD cần được trả lời ở TDD (STEP-1.5).
- next_inputs: `docs/prd/PRD-api-request-log.md` — BA dùng làm input chính cho STEP-1.2 để viết user stories + AC dạng Given/When/Then.

## Commit
- Hash: e0fa758
- Đã push: không (branch docker-deploy đã có remote, nhưng không push trong bước này)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
