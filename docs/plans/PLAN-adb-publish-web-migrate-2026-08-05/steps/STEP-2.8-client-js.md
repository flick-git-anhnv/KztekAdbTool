---
step: 2.8
title: Client-side JS (SignalR listeners + filter + select-all + toasts)
assignee: junior-developer
status: todo
completed_at: —
deps: [1.3]
---

## Nhiệm vụ
`wwwroot/js/dashboard.js`:
- SignalR `/hubs/device` với `.withAutomaticReconnect([0,2000,5000,10000])`.
- Listen: `DevicesUpdated` (replace `<tbody>`), `Log` (append `#log`), `InstallProgress` (update progress + status), `DeviceInstalled` (update cell).
- Fallback: nếu Disconnected sau reconnect fail → polling `GET /api/devices` mỗi 5s.
- **Filter IP/Version:** input event → hide/show `<tr>` theo `data-serial`/`data-version`.
- **Toggle Select All:** đúng logic `OnToggleSelectAll` gốc (nếu tất cả row visible tick → bỏ tick, ngược lại tick hết).
- **Click row:** delegate toggle checkbox trừ cell checkbox trực tiếp.
- **Cài selected / all / Xóa selected:** gom serials tick → gọi endpoint tương ứng.
- **Upload APK:** file change → FormData `POST /api/apk/upload` → auto-fill package input.
- **Package input blur:** `POST /api/settings/package` (chỉ khi non-empty như logic Leave gốc).
- **Switch Auto-detect:** `POST /api/polling/toggle`.
- **MessageBox tương đương:** `alert()` warning, `confirm()` xóa với text gốc "Xóa N thiết bị đã chọn khỏi danh sách quản lý?…".

## Definition of Done
- [ ] Cả 12 event ADR-001 §4.3 có handler tương đương.
- [ ] Ngắt mạng 30s → re-sync trong 5s khi mạng lại.
- [ ] Filter + Chọn tất cả đúng bản gốc (chỉ chọn visible).
- [ ] Log giới hạn ~1000 dòng (ghi chú trong PR — cải tiến nhỏ so với bản WinForms).

## Artifact
- `wwwroot/js/dashboard.js`, sửa `Pages/Index.cshtml` nạp script

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
