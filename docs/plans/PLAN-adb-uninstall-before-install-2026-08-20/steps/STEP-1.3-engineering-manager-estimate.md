---
step: 1.3
plan: ../PLAN-MASTER.md
agent: engineering-manager
status: done
completed_at: 2026-08-20 13:55
deps: [1.2]
---

# STEP 1.3 — Estimate Resource, Quyết định Priority, Phân bổ Team

## Input nhận
- PRD từ bước 1.1: `docs/prd/PRD-adb-uninstall-before-install.md`
- User story/AC từ bước 1.2: `docs/user-stories/US-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-1.2 (đọc mục "Handoff Payload" trong step file đó)

## Nhiệm vụ
Estimate effort (giờ), quyết định priority (đề xuất P2 vì là improvement UX, không phải blocker), xác nhận phân bổ Senior Developer thay vì Junior (vì đụng core install flow `InstallCoordinator`/`AdbService`), và ghi lại resource plan.

## Definition of Done
- [ ] `docs/planning/RESOURCE-adb-uninstall-before-install.md` đã tạo với: estimate giờ per agent, priority chính thức, lý do chọn Senior Dev, risk assessment ngắn
- [ ] `docs/planning/RESOURCE-adb-uninstall-before-install.docx` đã xuất thành công
- [ ] Priority P2 (hoặc quyết định khác có lý do ghi rõ) được confirm
- [ ] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm
- Đọc PRD (`docs/prd/PRD-adb-uninstall-before-install.md`) và US (`docs/user-stories/US-adb-uninstall-before-install.md`) — 5 US, 13 scenario.
- Xác nhận Priority P2: improvement UX, không phải blocker, mặc định tắt an toàn.
- Quyết định Senior Developer (không Junior): `InstallCoordinator`/`AdbService` là core install flow, không tách task UI riêng được vì phần backend cần context toàn bộ flow.
- Estimate tổng: 10–13 giờ thực thi (TDD 1.5–2h, Code 4–6h, Review 1h, UXR 0.5h, QA 2–2.5h, DevOps 1h).
- Risk assessment: Thấp-Trung bình — rủi ro cao nhất là parse lỗi ADB sai (R2), được kiểm soát bằng TDD chi tiết.
- Tạo `docs/planning/RESOURCE-adb-uninstall-before-install.md` + `.docx`.

## Artifact
- `docs/planning/RESOURCE-adb-uninstall-before-install.md` — Resource plan đầy đủ
- `docs/planning/RESOURCE-adb-uninstall-before-install.docx` — Xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng
1. **Priority P2 confirmed** — UX improvement, không phải blocker; hành vi mặc định (checkbox TẮT) không bị ảnh hưởng dù có bug trong nhánh bật.
2. **Senior Developer, không Junior** — Cả phần backend (`InstallCoordinator`/`AdbService`) và UI checkbox đều do Senior handle trong 1 PR để giảm overhead phối hợp và tránh merge conflict.
3. **Junior Developer: không phân bổ** — Không có task tách được phù hợp cấp Junior cho feature quy mô nhỏ này.
4. **UX/UI Reviewer: bắt buộc** — Dashboard có thay đổi UI (thêm checkbox), theo CLAUDE.md WF-FEATURE.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Priority P2 đã confirmed; phân bổ Senior Dev (không Junior) đã quyết — Project Manager không cần hỏi lại.
- watch_out: AC5 (lưu DB Settings) và Q2 (phân biệt lỗi uninstall) vẫn chờ Tech Lead chốt tại TDD (Bước 2.1) — sprint plan cần để Bước 2.1 (TDD) hoàn thành trước khi Bước 3.1 (Code) bắt đầu.
- next_inputs: `docs/planning/RESOURCE-adb-uninstall-before-install.md` — estimate effort và team đã có đủ để lập task board và sprint plan.

## Commit
- Hash: 92dd32d
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
