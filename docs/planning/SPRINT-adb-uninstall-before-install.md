---
sprint: adb-uninstall-before-install
feature: Tùy chọn Gỡ cài đặt App trước khi Cài đặt
priority: P2
created: 2026-08-20
updated: 2026-08-20
status: Planning
---

# SPRINT PLAN: Tùy chọn Gỡ cài đặt App trước khi Cài đặt (Uninstall Before Install)

## Thông tin Sprint

| Trường | Giá trị |
|---|---|
| Feature slug | adb-uninstall-before-install |
| Workflow | WF-FEATURE (rút gọn: skip UI/UX Designer, CTO, Junior Dev) |
| Priority | P2 |
| Sprint Goal | Thêm checkbox "Gỡ cài đặt trước khi cài" lên Dashboard, với logic uninstall-then-install trong `DoInstallAsync`, graceful skip khi package chưa cài, và persist trạng thái qua DB Settings |
| Velocity (estimate) | 10–13 giờ thực thi |
| Team | Tech Lead, Senior Developer, UX/UI Reviewer, QA Engineer, QA Lead, DevOps Engineer, DevOps Lead |
| Plan file | `docs/plans/PLAN-adb-uninstall-before-install-2026-08-20/PLAN-MASTER.md` |
| Resource file | `docs/planning/RESOURCE-adb-uninstall-before-install.md` |

---

## Sprint Backlog

> **Lưu ý quan trọng:** T-3.1 (Code) BẮT BUỘC chờ T-2.1 (TDD) hoàn thành và được user/Tech Lead confirm — AC5 (lưu DB Settings) và Q2 (phân biệt lỗi uninstall "package not found" vs lỗi ADB thực sự) chưa được chốt. Không bắt đầu code khi TDD chưa có.

| Task ID | Mô tả | Assignee | Estimate | SP | Priority | Status | Phụ thuộc |
|---|---|---|---|---|---|---|---|
| T-2.1 | **[TDD]** Viết Technical Design Doc: chốt field `UninstallBeforeInstall` trong DB Settings, vị trí checkbox UI, thứ tự gọi trong `DoInstallAsync`, SignalR progress bước "Đang gỡ cài đặt...", cơ chế phân biệt lỗi ADB uninstall | Tech Lead | 1.5–2h | 2 | P2 | Todo | Không có |
| T-3.1 | **[Code]** Implement full stack: `UninstallApkAsync` trong `IAdbService`/`AdbService`, field `UninstallBeforeInstall` trong `InstallRequest`, logic uninstall-before-install trong `DoInstallAsync`, checkbox `chk-uninstall-before-install` trong `Index.cshtml`, truyền flag qua `dashboard.js`, lưu DB Settings | Senior Developer | 4–6h | 5 | P2 | Todo | T-2.1 |
| T-3.2 | **[Code Review]** Review PR từ Senior Developer — kiểm tra: không regression code path flag=false, xử lý lỗi ADB đúng TDD, UI đúng pattern `form-check form-switch`, test coverage. Yêu cầu `/verify-pr` report trước khi mở review | Tech Lead | 1h | 1 | P2 | Todo | T-3.1 |
| T-3.3 | **[UXR]** Chạy app thật sau merge, chụp screenshot checkbox mới trên Dashboard, đánh giá 7 tiêu chí C1–C7 (consistency, alignment, feedback SignalR, clarity label, spacing, responsive, accessibility) | UX/UI Reviewer | 0.5–0.75h | 1 | P2 | Todo | T-3.2 |
| T-4.1 | **[QA]** Viết test plan, thực thi test case: 5 US × 13 scenario (checkbox on/off regression, uninstall graceful khi package chưa cài, nhiều thiết bị đồng thời, persist state sau F5, SignalR progress đúng thứ tự) | QA Engineer | 1.5–2h | 2 | P2 | Todo | T-3.3 |
| T-4.2 | **[Sign-off]** Review kết quả QA, veto nếu còn P0/P1 bug, sign-off chính thức | QA Lead | 0.5h | 1 | P2 | Todo | T-4.1 |
| T-4.3 | **[Deploy Staging]** `docker-compose up --build`, verify luồng install với checkbox bật/tắt không bị regression, kiểm tra DB Settings lưu đúng key | DevOps Engineer | 0.5–0.75h | 1 | P2 | Todo | T-4.2 |
| T-4.4 | **[Deploy Production]** Approve staging sau smoke test, approve + deploy production, monitor | DevOps Lead | 0.5h | 1 | P2 | Todo | T-4.3 |

**Tổng SP:** 14 SP | **Tổng estimate:** 10–13 giờ

---

## Dependencies (Thứ tự bắt buộc)

```
T-2.1 (TDD) ──┐
               ▼
            T-3.1 (Code)
               │
               ▼
            T-3.2 (Review)
               │
               ▼
            T-3.3 (UXR)
               │
               ▼
            T-4.1 (QA Test)
               │
               ▼
            T-4.2 (Sign-off)
               │
               ▼
            T-4.3 (Deploy Staging)
               │
               ▼
            T-4.4 (Deploy Production)
```

**Câu hỏi mở cần Tech Lead chốt tại T-2.1 (TDD) trước khi T-3.1 bắt đầu:**
- **AC5 / Q1:** Trạng thái checkbox có lưu DB Settings không? (Đề xuất: CÓ — nhất quán với PackageName/ApkPath. Tech Lead quyết định tên key.)
- **Q2:** Cơ chế phân biệt lỗi "package not found" vs lỗi ADB thực sự trong `UninstallApkAsync` — parse output ADB hay exit code? Pattern nào?
- **EC1:** Thiết bị MDM lock / không gỡ được — abort hay warning + tiếp tục install?

---

## Scope bị đẩy ra (Out of Scope)

| Item | Lý do |
|---|---|
| UI/UX Designer tạo mockup | Chỉ thêm 1 checkbox theo đúng pattern `chk-auto-detect` đã có — không cần thiết kế mới |
| CTO review kiến trúc | Không đụng auth/payment/kiến trúc hệ thống |
| Junior Developer | Không có task tách được phù hợp cấp Junior |
| security-audit-stride | Không đụng auth/payment/DB schema/dữ liệu nhạy cảm |

---

## Rủi ro Sprint

| ID | Rủi ro | Mức | Biện pháp |
|---|---|---|---|
| R1 | Regression hành vi mặc định (checkbox TẮT bị ảnh hưởng) | Cao / Thấp | Senior Dev giữ nguyên code path khi flag=false; QA verify US-001 |
| R2 | Parse lỗi ADB uninstall sai | Trung bình / Trung bình | TDD chốt cơ chế trước khi code; QA test US-003 với thiết bị thực |
| R3 | T-3.1 bắt đầu trước khi T-2.1 xong | Cao / Thấp | Project Manager enforce dependency; không assign T-3.1 khi T-2.1 chưa ✅ |

---

## Task Board

### TODO
- T-2.1 — TDD (Tech Lead, 1.5–2h)
- T-3.1 — Code full stack (Senior Developer, 4–6h) — chờ T-2.1
- T-3.2 — Code Review (Tech Lead, 1h) — chờ T-3.1
- T-3.3 — UXR Review (UX/UI Reviewer, 0.5–0.75h) — chờ T-3.2
- T-4.1 — QA Test (QA Engineer, 1.5–2h) — chờ T-3.3
- T-4.2 — Sign-off (QA Lead, 0.5h) — chờ T-4.1
- T-4.3 — Deploy Staging (DevOps Engineer, 0.5–0.75h) — chờ T-4.2
- T-4.4 — Deploy Production (DevOps Lead, 0.5h) — chờ T-4.3

### IN PROGRESS
_(Trống)_

### REVIEW
_(Trống)_

### DONE
_(Trống)_

---

## Definition of Done (Sprint)

- [ ] **P2 Done:** Tất cả task T-2.1 → T-4.4 trạng thái Done
- [ ] **QA sign-off:** T-4.2 QA Lead ký, không còn P0/P1 bug
- [ ] **Deploy staging:** T-4.3 hoàn thành, smoke test pass
- [ ] **Demo xong:** Checkbox hiển thị đúng, uninstall graceful khi package chưa cài, persist state sau F5
- [ ] **Deploy production:** T-4.4 DevOps Lead approve + monitor 30 phút

---

## Phê duyệt

| Vai trò | Tên | Trạng thái |
|---|---|---|
| Project Manager | dungnn@kztek.vn | Đã lập — 2026-08-20 |
| Tech Lead | — | Chờ xác nhận |
| QA Lead | — | Chờ xác nhận |
| Engineering Manager | — | Resource plan đã approve (STEP-1.3) |

---

## Lịch sử cập nhật

| Ngày | Phiên bản | Thay đổi | Agent |
|---|---|---|---|
| 2026-08-20 | v1.0 | Tạo sprint plan — liệt kê T-2.1 đến T-4.4, tất cả Todo | Project Manager |
