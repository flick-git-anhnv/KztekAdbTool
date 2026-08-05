---
step: 2.6
title: Dashboard Razor Page (toolbar + grid + action + log)
assignee: junior-developer
status: done
completed_at: 2026-08-05 16:13
deps: [1.3]
---

## Nhiệm vụ
`Pages/Index.cshtml` + `.cs` — layout 3 tầng như MainForm gốc:
- **Toolbar 3 hàng:** (1) Package input + file APK; (2) IP:port + nút Kết nối; (3) Quét dải mạng + Quét lại + switch Tự động phát hiện.
- **Filter bar:** 2 input (IP/Serial, Version).
- **Grid device:** `<table>` 9 cột (Chọn/Serial/Model/Trạng thái/Kết nối/Version/Thời gian cài/Lần thấy cuối/Kết quả cài lần cuối); server-side render lần đầu từ `DeviceState.Devices`; row Offline `.text-muted`.
- **Action panel** cột phải: 4 nút (Chọn tất cả / Cài selected / Cài all / Xóa selected) + progress + status label.
- **Log:** `<pre id="log">` height 150px scroll.

`OnGetAsync` load `DeviceState.Devices` + settings PackageName/ApkPath vào ViewData.

## Definition of Done
- [ ] Layout tương đương WinForms (screenshot đối chiếu ở 3.3).
- [ ] Brand màu Navy/Cam đúng, không lỗi CSS/JS console.

## Artifact
- `Pages/Index.cshtml`, `Pages/Index.cshtml.cs`, `wwwroot/css/dashboard.css`

## Đã làm
- Pages/Index.cshtml.cs: inject DeviceRepository (singleton), OnGet() load Devices/PackageName/ApkPath
- Pages/Index.cshtml: toolbar 3 hàng, filter bar, table#device-table/tbody#device-tbody (9 cột, data-serial, data-version attrs), action panel (4 nút + progress kz-progress-fill + status-label), pre#log, modal markup #networkScanModal
- wwwroot/css/dashboard.css: brand Navy/Cam, sticky thead, offline-row text-muted, kz-action-panel fixed 215px, log dark-theme, #kz-toast-container fixed top-right
- Commit: 31bf693 (cshtml), 00fec86 (css)

## Handoff Payload
- Đã làm: Index.cshtml + cshtml.cs + dashboard.css hoàn chỉnh, build pass 0 errors
- do_not_redo: Không thêm @using System.Linq vào _ViewImports (đã có global using .NET8); đã thêm trực tiếp trong Index.cshtml
- watch_out: Cột table có thứ tự cố định [0=checkbox,1=serial,2=model,3=status,4=conn,5=version,6=installTime,7=lastSeen,8=result] — dashboard.js và DeviceInstalled handler phụ thuộc vào index này
- next_inputs: tbody#device-tbody, data-serial, data-version, progress#install-progress, #status-label, #log, #kz-toast-container đều sẵn sàng cho dashboard.js (STEP-2.8)
