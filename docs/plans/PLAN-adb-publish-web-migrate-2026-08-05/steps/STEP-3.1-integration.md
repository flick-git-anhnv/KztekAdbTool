---
step: 3.1
title: Integration + chạy local end-to-end
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:31
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

## Đã làm
- Đọc code 2 phía (frontend JS + backend C#) để tìm contract mismatch — không tin báo cáo Phase 2.
- Fix ScanCoordinator: 3 sự kiện (ScanFound, ScanProgress, ScanCompleted) gửi anonymous object thay vì primitive args → JS nhận sai kiểu. Fix: gửi primitive args tách biệt theo đúng thứ tự `(found, scanned, total)` và `(total, found)`.
- Fix InstallCoordinator: InstallProgress gửi object với field `current` (0-5) thay vì `percent` (0-100) → progress bar không di chuyển. Fix: hardcode percent 0/20/40/60/80/100. DeviceInstalled gửi string `status` thay vì bool `success` → frontend hiển thị sai. Fix: compute `success = status == "Thành công"`.
- Fix dashboard.js fallback poll: backend trả `{ok, data:[]}` nhưng frontend gọi `renderDevices(resp)` → TypeError `sort` trên plain object. Fix: extract `resp.data`.
- Xóa `PollingState` khỏi DI (không ai inject, dead code).
- `dotnet build`: 0 lỗi. `dotnet test`: 19/19 pass.
- Chạy server `dotnet run` → test 8 endpoint qua curl, tất cả trả response hợp lệ (không crash 500).
- Commit: 058aea5.

## Handoff Payload
- do_not_redo: Không build lại từ đầu — Phase 1+2 đã có 0 lỗi, chỉ bước 3.1 sửa 4 file (ScanCoordinator, InstallCoordinator, dashboard.js, Program.cs). Không sửa lại contract install: `{serials:[], selectedOnly:false}` = "cài tất cả" là ĐÚNG theo backend logic hiện tại.
- watch_out: 12 luồng trong task description — 4 luồng cần ADB thật (Luồng 2,5,8,9) chỉ verify được qua code review + unit test, không có thiết bị Android thật trong môi trường này. Luồng 7 (upload APK) cần file .apk thật để test ApkManifestReader. Luồng 1 (load grid từ DB) verify được qua GET /api/devices (đã test, trả `[]` vì chưa có thiết bị). Luồng 3,4,6 (toggle auto-detect, quét manual, scan modal) verify được qua curl endpoint — endpoint trả đúng. Luồng 10,11,12 là pure JS client-side, chỉ verify bằng code review vì không có browser automation.
- next_inputs: Commit hash 058aea5 trên branch docker-deploy. 4 file đã sửa: ScanCoordinator.cs, InstallCoordinator.cs, dashboard.js, Program.cs. Rủi ro còn lại cho Code Migrator review (3.2): (a) InstallCoordinator — field `current` ở DoInstallAsync biến mất khỏi SignalR payload, nếu bên nào khác parse field này sẽ lỗi — nhưng hiện tại chỉ dashboard.js xử lý; (b) PollingState.cs còn tồn tại trong source tree (chỉ bỏ DI registration), Code Migrator có thể đề xuất xóa file hoàn toàn.
