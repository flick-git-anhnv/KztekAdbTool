---
step: 2.3
title: Network scan endpoints + cancel + SignalR progress
assignee: senior-developer
status: done
completed_at: 2026-08-05 16:20
deps: [1.3]
---

## Nhiệm vụ
- `POST /api/scan/start` `{rangeText, port}`: parse "192.168.1.1-254" / full IP range / CIDR /24, max 512 IP, port 1-65535.
- `POST /api/scan/cancel`: hủy `CancellationTokenSource` singleton `ScanCoordinator`.
- Chỉ 1 scan tại 1 thời điểm → 409 nếu đang chạy.
- SignalR: `ScanProgress {current,total,foundCount}`, `ScanFound {ipPort}`, `ScanCompleted {total,found}`.

## Definition of Done
- [x] Validate đúng 3 định dạng range + message tiếng Việt gốc.
- [x] Khác /24 hoặc cross-subnet → 400.
- [x] Cancel dừng trong 2s (CancellationToken propagate qua TcpClient.ConnectAsync).
- [x] 409 khi scan đang chạy (SemaphoreSlim guard, non-blocking tryacquire).

## Đã làm
- `Services/ScanRangeParser.cs`: public static TryParse — hỗ trợ 3 format (octet range, full-IP range, CIDR /24), single IP, max 512 IP. Tách class riêng để unit test được.
- `Services/ScanCoordinator.cs`: TryStartScan trả enum ScanStartResult (Started/AlreadyRunning/ValidationError). CTS set null TRƯỚC khi release _scanGuard (thread-safe). SemaphoreSlim(24) per-scan, TcpProbeAsync timeout 1200ms.
- `Endpoints/ScanEndpoints.cs`: POST /api/scan/start + POST /api/scan/cancel. ScanStartRequest POCO (RangeText, Port=5555).
- `tests/ScanRangeParserTests.cs`: 14 test cases cho pure logic.
- ScanCompleted và ScanFound push không dùng ct (tránh mất event khi cancel).

## Artifact
- `Endpoints/ScanEndpoints.cs`
- `Services/ScanCoordinator.cs`
- `Services/ScanRangeParser.cs`
- `tests/KztekAdbPublishTool.Web.Tests/ScanRangeParserTests.cs`

## Handoff Payload — bước sau đọc phần này

- Đã làm: POST /api/scan/start → 202 Accepted (scan background) hoặc 409 (đang scan) hoặc 400 (validation). POST /api/scan/cancel → 200 (best-effort).
- do_not_redo: KHÔNG tạo thêm guard mechanism — _scanGuard (SemaphoreSlim(1)) đã đủ. KHÔNG cần check IsScanning trước TryStartScan — TryStartScan đã atomic.
- watch_out: ScanFound và ScanCompleted push KHÔNG dùng ct — nếu dùng ct, cancel sẽ làm mất event "ScanCompleted" cuối cùng. Frontend cần handle cả 2 cases: scan hoàn thành bình thường VÀ scan bị cancel (đều nhận ScanCompleted).
- next_inputs: Frontend (STEP-2.7) lắng nghe SignalR events: "ScanFound" `{ipPort: string}`, "ScanProgress" `{current, total, foundCount}`, "ScanCompleted" `{total, found}`. POST body: `{rangeText: string, port: number}`.
