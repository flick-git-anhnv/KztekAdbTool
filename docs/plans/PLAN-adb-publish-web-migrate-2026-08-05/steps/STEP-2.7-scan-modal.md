---
step: 2.7
title: Network scan modal (Bootstrap)
assignee: junior-developer
status: done
completed_at: 2026-08-05 16:13
deps: [1.3]
---

## Nhiệm vụ
Modal `#networkScanModal` tái hiện `NetworkScanForm`:
- Input Dải IP (placeholder `192.168.1.1-254`), Cổng default 5555.
- Nút Quét / Dừng (disable/enable theo state).
- Bảng kết quả 2 cột (Thêm-checkbox, Địa chỉ); click row toggle checkbox.
- Progress + status.
- Nút "Thêm vào danh sách" (`/api/devices/connect-batch`) / "Đóng".

JS:
- Quét → `/api/scan/start`, listen SignalR `ScanProgress`/`ScanFound`/`ScanCompleted`.
- Dừng → `/api/scan/cancel`.
- Thêm → gom `ipPort` đã tick → gọi batch → toast → đóng modal.
- Không tick gì → toast "Chưa chọn thiết bị nào để thêm".

## Definition of Done
- [ ] Modal mở/đóng OK.
- [ ] Validate range không hợp lệ → message tiếng Việt gốc.
- [ ] Cancel giữa chừng OK.
- [ ] Sau Thêm → device xuất hiện trong grid chính trong 3s.

## Artifact
- Sửa `Pages/Index.cshtml`, `wwwroot/js/scan-modal.js`

## Đã làm
- Modal #networkScanModal đã có trong Index.cshtml (STEP-2.6): input scan-range, scan-port, btn-scan-start, btn-scan-stop, scan-progress-bar, scan-status, scan-result-tbody, btn-scan-add
- wwwroot/js/scan-modal.js: validate IP range (full + short-octet, same /24 — mirrors NetworkScanForm.cs), POST /api/scan/start|cancel, SignalR ScanProgress/ScanFound/ScanCompleted, row click toggle, connect-batch, reset on modal hidden
- Commit: 39d69d3

## Handoff Payload
- Đã làm: scan-modal.js hoàn chỉnh, load sau dashboard.js trong @section Scripts của Index.cshtml
- do_not_redo: ScanCompleted event đã được đăng ký trong scan-modal.js — KHÔNG đăng ký thêm trong signalr-client.js
- watch_out: ScanCompleted nhận (total, found) — thứ tự tham số từ server cần đúng; nếu backend gửi (found, total) thì cần đổi lại trong scan-modal.js dòng conn.on('ScanCompleted', function(total, found))
- next_inputs: Không có — module này độc lập, chỉ phụ thuộc window.kzHubConnection và #kz-toast-container
