---
step: 1.2
plan: ../PLAN-MASTER.md
agent: business-analyst
status: done
completed_at: 2026-08-20 13:51
deps: [1.1]
---

# STEP 1.2 — Viết User Story và AC Given/When/Then

## Input nhận
- PRD từ bước 1.1: `docs/prd/PRD-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-1.1 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Chi tiết hóa các acceptance criteria theo format Given/When/Then và viết user story đầy đủ cho tính năng "Uninstall Before Install". Bao phủ các scenario: checkbox bật/tắt, uninstall thành công, uninstall thất bại (package chưa cài — graceful skip), install tiếp sau uninstall, hành vi mặc định (tắt).

## Definition of Done
- [ ] `docs/user-stories/US-adb-uninstall-before-install.md` đã tạo
- [ ] Có ≥ 5 scenario Given/When/Then bao phủ: (1) checkbox tắt — hành vi mặc định, (2) checkbox bật + package đã cài — uninstall thành công rồi install, (3) checkbox bật + package chưa cài — log warning, vẫn install, (4) checkbox bật + install thành công sau uninstall, (5) trạng thái lưu giữa các lần load trang (nếu TDD quyết định lưu DB)
- [ ] `docs/user-stories/US-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
- Đọc PRD `docs/prd/PRD-adb-uninstall-before-install.md` để hiểu 5 AC mức cao (AC1-AC5).
- Viết `docs/user-stories/US-adb-uninstall-before-install.md` gồm 5 User Story (US-001 đến US-005) với 13 scenario Given/When/Then bao phủ toàn bộ AC từ PRD.
- Vẽ Business Flow mermaid (flowchart TD) thể hiện toàn bộ luồng từ load Dashboard → kiểm tra DB Settings → install loop → nhánh uninstall (bật/tắt) → kết quả.
- Định nghĩa 6 Business Rules (BR1-BR6) và 5 Edge Cases (EC1-EC5).
- Ghi rõ 3 câu hỏi mở (Q1-Q3) cho Tech Lead và PM, đặc biệt Q1 (vị trí checkbox) và Q2 (phân biệt lỗi "package not found" vs lỗi ADB thực sự) chờ Tech Lead chốt tại TDD (Bước 2.1).
- Xuất DOCX thành công. PDF thất bại do thiếu LaTeX (bình thường).

## Artifact
- `docs/user-stories/US-adb-uninstall-before-install.md` — User Story chính (5 US, 13 scenario)
- `docs/user-stories/US-adb-uninstall-before-install.docx` — Xuất DOCX (thành công)

## Quyết định quan trọng
- US-005 (Scenario lưu DB Settings) được viết theo hướng CÓ lưu (đề xuất từ PRD AC5), nhưng Tech Lead sẽ chốt cơ chế thực tế tại TDD-2.1 — nếu Tech Lead quyết định KHÔNG lưu, scenario 1 và 2 của US-005 cần cập nhật lại.
- BR3: Lỗi "package not found" = WARNING + bỏ qua, KHÔNG abort — ghi rõ để Tech Lead chốt cơ chế phân biệt (parse stdout vs exit code) tại TDD.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã có US tại `docs/user-stories/US-adb-uninstall-before-install.md` với 5 US và 13 scenario — không viết lại.
- watch_out: AC5 (lưu DB Settings) và Q1 (vị trí checkbox) và Q2 (phân biệt lỗi uninstall) cần Tech Lead chốt tại TDD (Bước 2.1) — EM chỉ cần tham chiếu US này để estimate effort, không cần quyết định các câu hỏi kỹ thuật đó.
- next_inputs: Đọc `docs/user-stories/US-adb-uninstall-before-install.md` và `docs/prd/PRD-adb-uninstall-before-install.md` để estimate resource và confirm Senior Developer là người phù hợp (vì US-002/US-003/US-004 đụng `InstallCoordinator`/`AdbService`).

## Commit
- Hash: 2f5b1e0
- Đã push: không (theo tiền lệ Bước 1.1 — KHÔNG push để tránh xung đột)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
