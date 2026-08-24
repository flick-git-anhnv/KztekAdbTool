---
step: 3.2
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: todo
completed_at: ~
deps: ["3.1"]
---

# STEP 3.2 — Code review PR + security-audit-stride + quyết định merge

## Input nhận
Từ Bước 3.1 Handoff Payload — commit hash, danh sách file thay đổi, kết quả `dotnet test`, CODE-GRAPH đã cập nhật.

## Nhiệm vụ

### Code Review checklist:
- [ ] `GetAppStatusAsync`: ADB command an toàn, xử lý đúng edge cases (empty pidof output, dumpsys parse fail, device offline)
- [ ] `RebootDeviceAsync`: lệnh reboot không block thread, error handling khi device offline/đã offline sau reboot
- [ ] 2 endpoints: filter chain đúng thứ tự (OUTER logging TRƯỚC, INNER auth SAU) — theo đúng LaunchAppEndpoints.cs pattern
- [ ] `context.Arguments` được dùng thay vì `Request.Body` trong filter (GOTCHA G008)
- [ ] UI: confirm dialog cho Reboot có hoạt động, không có JS error
- [ ] Tests đủ coverage (happy path + edge cases), không có test giả (assertion trống)
- [ ] `Program.cs`: 2 endpoint mới đã đăng ký đúng
- [ ] CODE-GRAPH: 2 module mới được thêm, callers/relationships đúng
- [ ] Không có conflict với file từ plan adb-reconnect (docker-compose.yml, AdbService.cs, appsettings.json)
- [ ] Checklist tài liệu đồng bộ (CLAUDE.md §15.3): PRD/US/TDD/DESIGN/CODE-GRAPH đã khớp

### Security audit (security-audit-stride — BƯỚC NÀY BẮT BUỘC theo CLAUDE.md §4 WF-FEATURE Bước 10a vì đụng auth endpoint + reboot command):
- OWASP Top 10 cho 2 endpoint mới
- STRIDE: reboot là destructive action — kiểm tra Tampering (giả mạo serial), Spoofing (bypass API key), Denial of Service (reboot liên tục)
- BLOCK merge nếu Fail nhóm rủi ro cao

### Quyết định:
- APPROVE merge → chuyển Bước 3.3 UX/UI Reviewer
- REQUEST CHANGES → Senior Developer sửa, quay lại review

## Definition of Done
- [ ] Code review checklist đầy đủ, tất cả mục PASS hoặc có ghi chú chấp nhận được
- [ ] Security-audit-stride chạy xong, không có Fail nhóm rủi ro cao
- [ ] Quyết định APPROVE hoặc REQUEST CHANGES ghi rõ lý do
- [ ] Commit + push (ghi kết quả review)
- [ ] Cập nhật STEP file này + PLAN-MASTER.md Bước 3.2 → ✅

## Đã làm
(để trống)

## Artifact
(để trống)

## Quyết định quan trọng
(để trống)

## Handoff Payload — bước sau đọc phần này
- do_not_redo: (để trống)
- watch_out: (để trống)
- next_inputs: (để trống)

## Commit
- Hash: (chưa có)
- Đã push: No

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
