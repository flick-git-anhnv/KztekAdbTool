---
step: "1.1"
plan: ../PLAN-MASTER.md
agent: product-manager
status: done
completed_at: 2026-08-18 15:53
deps: []
---

# STEP 1.1 — Product Manager: Viết PRD

## Input nhận
Task mới. Xem PLAN-MASTER.md để biết bối cảnh tổng thể.

## Nhiệm vụ
Viết PRD phạm vi hẹp cho tính năng API Launch App: mục tiêu sản phẩm, acceptance criteria tổng quan, non-goals rõ ràng (không có UI, không có multi-key rotation, không có audit log trong phiên bản này).

## Definition of Done
- [ ] File `docs/prd/PRD-adb-launch-app-api.md` đã tạo
- [ ] PRD có đủ mục: Goals, Problem statement, Scope, AC tổng quan, Non-goals, Metric đo lường
- [ ] PRD nêu rõ: API nhận `x-api-key` header + `serial` + `app` (package name), gọi ADB launch, trả về kết quả
- [ ] Đã xuất DOCX + PDF bằng `scripts/md_to_docx_kztek.py`

## Đã làm
- Đọc PLAN-MASTER, STEP file, `AdbService.LaunchAppAsync` (~dòng 171-188 trong `AdbService.cs`) để nắm bối cảnh kỹ thuật.
- Tạo `docs/prd/PRD-adb-launch-app-api.md` đầy đủ 6 mục: Tổng quan, Goals, Non-goals, Scope, AC mức cao (AC1-AC6), Metric, Rủi ro/Câu hỏi mở.
- Chốt rõ: API key **chỉ áp cho endpoint mới**, không áp cho route cũ — ghi vào Non-goals.
- Xuất `PRD-adb-launch-app-api.docx` thành công; PDF thất bại do thiếu LaTeX (môi trường Linux không có xelatex/LibreOffice).

## Artifact
- `docs/prd/PRD-adb-launch-app-api.md` — PRD nguồn
- `docs/prd/PRD-adb-launch-app-api.docx` — DOCX (KZTEK brand) ✅
- PDF: thất bại do thiếu LaTeX converter (ghi nhận, không block workflow)

## Quyết định quan trọng
- Auth API key áp dụng **riêng** cho endpoint `/api/launch-app` mới — KHÔNG áp cho `/api/install` hay route cũ (chốt với user, ghi vào Non-goals).
- HTTP verb (POST hay GET) và cấu trúc response JSON giao Tech Lead quyết định ở TDD (STEP-2.1) — ghi vào Câu hỏi mở Q1, Q2.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Đã có PRD đầy đủ tại `docs/prd/PRD-adb-launch-app-api.md` — BA không cần viết lại Goals/Scope; chỉ chi tiết hóa AC thành Given/When/Then.
- watch_out: Auth scope đã chốt cứng — API key CHỈ áp cho endpoint mới, KHÔNG áp route cũ. BA phải phản ánh đúng điều này khi viết scenario. Câu hỏi HTTP verb (POST/GET) và response format chưa chốt — BA viết AC theo hành vi, không gắn cứng verb/format.
- next_inputs: `docs/prd/PRD-adb-launch-app-api.md` (AC1-AC6 mức cao cần chi tiết hóa), `AdbCommandResult` struct (xem `AdbService.cs` để biết `ExitCode`, `StdOut`, `StdErr`, `Success`).

## Commit
- Hash: 8771f1b
- Đã push: Thất bại — 403 Permission denied (duongth411 không có quyền push lên flick-git-anhnv/KztekAdbTool). Cần xác thực thủ công.

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
