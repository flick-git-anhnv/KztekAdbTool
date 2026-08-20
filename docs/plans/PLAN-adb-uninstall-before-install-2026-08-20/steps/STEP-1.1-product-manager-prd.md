---
step: 1.1
plan: ../PLAN-MASTER.md
agent: product-manager
status: done
completed_at: 2026-08-20 13:47
deps: []
---

# STEP 1.1 — Viết PRD: Tùy chọn Gỡ cài đặt App trước khi Cài đặt

## Input nhận
- Yêu cầu gốc: "Thêm option lựa chọn có ra lệnh gỡ cài đặt app trước khi update lại hay không trên view"
- Bối cảnh kỹ thuật sơ bộ (đã khảo sát trước, không cần khảo sát lại):
  - Dashboard `Index.cshtml` hiện có toolbar với checkbox `chk-auto-detect` (`form-check form-switch`) — thêm checkbox mới theo cùng pattern.
  - Hành vi mặc định hiện tại: `adb install -r` (reinstall, không gỡ trước). Checkbox mặc định TẮT.
  - Khi bật: server chạy `adb uninstall <package>` TRƯỚC `adb install`; nếu package chưa cài → log warning, vẫn tiếp tục install (không abort).

## Nhiệm vụ
Viết PRD phạm vi hẹp cho tính năng "Uninstall Before Install": mục tiêu, acceptance criteria tổng quan, non-goals, metric đo lường. Lưu tại `docs/prd/PRD-adb-uninstall-before-install.md` và xuất DOCX bằng `scripts/md_to_docx_kztek.py`.

## Definition of Done
- [ ] `docs/prd/PRD-adb-uninstall-before-install.md` đã tạo với các mục: Goals, Problem Statement, Acceptance Criteria tổng quan (≥ 3 AC), Non-goals, Metric đo lường
- [ ] `docs/prd/PRD-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Nội dung PRD đủ để BA viết AC chi tiết Given/When/Then ở bước 1.2
- [ ] Cập nhật step file này (Đã làm, Artifact, Handoff Payload) + PLAN-MASTER.md status → ✅

## Đã làm
- Tạo `docs/prd/PRD-adb-uninstall-before-install.md` với đầy đủ: Tổng quan, 4 Goals, Non-goals (5 mục), User Story sơ lược, 5 AC tổng quan, 3 Metric, 2 Rủi ro + 1 Câu hỏi mở.
- Xuất `docs/prd/PRD-adb-uninstall-before-install.docx` thành công bằng `md_to_docx_kztek.py` (PDF fail do thiếu xelatex — bình thường, DOCX là artifact chính).
- Commit `ece966f` lên nhánh `docker-deploy`.

## Artifact
- `docs/prd/PRD-adb-uninstall-before-install.md` — nguồn Markdown
- `docs/prd/PRD-adb-uninstall-before-install.docx` — xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng
- AC5 thêm yêu cầu lưu trạng thái checkbox qua DB Settings (nhất quán với PackageName/ApkPath) — Tech Lead xác nhận chi tiết trong TDD bước 2.1.
- Mặc định checkbox TẮT để không thay đổi hành vi hiện tại (`adb install -r`).
- Ghi log warning (không phải lỗi nghiêm trọng) khi uninstall thất bại vì package chưa cài — luồng install vẫn tiếp tục.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã có PRD tại `docs/prd/PRD-adb-uninstall-before-install.md` — BA không cần viết lại, chỉ đọc PRD làm input cho bước 1.2.
- watch_out: AC5 (lưu DB Settings) và vị trí checkbox trên toolbar (Q1 trong PRD) cần Tech Lead xác nhận tại TDD bước 2.1 — BA ghi rõ điều này vào AC Given/When/Then như điều kiện chờ chốt.
- next_inputs: `docs/prd/PRD-adb-uninstall-before-install.md` — đọc toàn bộ file này để chi tiết hóa 5 AC thành Given/When/Then tại bước 1.2.

## Commit
- Hash: ece966f
- Đã push: không (remote có cấu hình nhưng không push để tránh xung đột với branch hiện tại)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
