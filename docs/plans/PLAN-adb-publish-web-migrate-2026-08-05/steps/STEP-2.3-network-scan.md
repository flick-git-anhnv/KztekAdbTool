---
step: 2.3
title: Network scan endpoints + cancel + SignalR progress
assignee: senior-developer
status: todo
completed_at: —
deps: [1.3]
---

## Nhiệm vụ
- `POST /api/scan/start` `{rangeText, port}`: parse "192.168.1.1-254" / "…1-…254", cùng /24, max 512 IP, port 1-65535 (giữ nguyên message lỗi gốc). Chạy `SemaphoreSlim(24)`, timeout 1200ms/connect.
- `POST /api/scan/cancel`: hủy `CancellationTokenSource` singleton `ScanCoordinator`.
- Chỉ 1 scan tại 1 thời điểm → 409 nếu đang chạy.
- SignalR: `ScanProgress {current,total,foundCount}`, `ScanFound {ipPort}`, `ScanCompleted {total,found}`.

## Definition of Done
- [ ] Validate đúng 3 định dạng range + message tiếng Việt gốc.
- [ ] Khác /24 hoặc >512 → 400.
- [ ] Cancel dừng trong 2s.

## Artifact
- `Endpoints/ScanEndpoints.cs`, `Services/ScanCoordinator.cs`

## Handoff Payload
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: —
