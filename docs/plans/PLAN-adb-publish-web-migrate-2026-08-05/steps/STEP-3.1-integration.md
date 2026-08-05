---
step: 3.1
title: Integration + chạy local end-to-end
assignee: senior-developer
status: todo
completed_at: —
deps: [2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8]
---

## Nhiệm vụ
Ghép, chạy `dotnet run` trên máy dev Windows (dev override `AdbPath` về `../platform-tools/adb.exe`). Test 12 luồng chính:
1. Load trang → grid render từ DB.
2. Poll tự động 3s → Online/Offline realtime.
3. Toggle Auto-detect off → ngừng poll.
4. Nút Quét lại → poll thủ công 1 lần.
5. Kết nối nhanh IP:port.
6. Quét dải mạng: mở modal, quét, cancel, add batch.
7. Upload APK → auto-detect package.
8. Cài đặt cho đã chọn — progress + log realtime + app tự mở.
9. Cài đặt cho tất cả.
10. Filter IP + Version — hide/show đúng.
11. Chọn tất cả sau filter — chỉ chọn visible.
12. Xóa thiết bị đã chọn — confirm → xóa grid + DB.

Bug log `_workspace/03_integration_bugs.md`; fix nhỏ (<10 dòng) tại chỗ; lớn → giao Senior.

## Definition of Done
- [ ] 12/12 luồng pass.
- [ ] Không còn console error/warning.
- [ ] `dotnet build -c Release` sạch.

## Artifact
- `_workspace/03_integration_bugs.md` + code fix

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
