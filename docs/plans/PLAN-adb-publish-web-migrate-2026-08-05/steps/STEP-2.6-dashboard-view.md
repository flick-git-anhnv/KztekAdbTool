---
step: 2.6
title: Dashboard Razor Page (toolbar + grid + action + log)
assignee: junior-developer
status: todo
completed_at: —
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

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
