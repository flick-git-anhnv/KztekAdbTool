---
step: 2.7
title: Network scan modal (Bootstrap)
assignee: junior-developer
status: todo
completed_at: —
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

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
