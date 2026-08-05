---
step: 2.8
title: Client-side JS (SignalR listeners + filter + select-all + toasts)
assignee: junior-developer
status: done
completed_at: 2026-08-05 16:13
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

## Đã làm
- wwwroot/js/dashboard.js: SignalR DevicesUpdated(renderDevices+stopFallbackPoll), Log(appendLog), InstallProgress(progress bar+status), DeviceInstalled(in-place cell update cells[5]+cells[8])
- Fallback: onclose→startFallbackPoll(setInterval GET /api/devices mỗi 5s), onreconnected→stopFallbackPoll
- Filter: applyFilter() hide/show <tr> theo data-serial/data-version — trigger on input event
- Select All: toggleSelectAll() chỉ chọn visible rows, logic mirrors WinForms OnToggleSelectAll
- Row click delegate: event bubbles từ td→tr, bỏ qua nếu e.target là .device-checkbox; change event update checkedSerials Set
- Install: POST /api/install {serials, selectedOnly}; Remove: confirm(text gốc) → POST /api/devices/remove {serials}
- Connect: POST /api/devices/connect {ipPort}; Refresh: POST /api/devices/poll; Toggle: POST /api/polling/toggle {enabled}
- APK upload: FormData POST /api/apk/upload → auto-fill txt-apk-path + txt-package
- Package blur: POST /api/settings/package {packageName} (non-empty only)
- Log giới hạn 1000 dòng; checkedSerials Set persist qua DevicesUpdated re-render
- Commit: 12edd5b

## Handoff Payload
- Đã làm: dashboard.js + scan-modal.js hoàn chỉnh, build pass 0 errors, 4 commits trên branch docker-deploy
- do_not_redo: signalr-client.js KHÔNG được sửa — dashboard.js hook vào window.kzHubConnection bằng .on() bổ sung, không replace
- watch_out: (1) DevicesUpdated gửi object với camelCase (serial, installedVersion...) do ASP.NET Core System.Text.Json default — đúng với code đã viết. (2) ScanCompleted tham số thứ tự (total, found) — cần xác nhận với backend agent. (3) POST /api/install với {serials:[], selectedOnly:false} là "install all online" — backend cần hiểu quy ước này
- next_inputs: Backend agents cần implement đúng: GET /api/devices trả List<DeviceRecord> JSON camelCase; SignalR hub push DevicesUpdated với kiểu object matching DeviceRecord
