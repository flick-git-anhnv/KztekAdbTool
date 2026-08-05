/**
 * KZTEK ADB Publish Tool — SignalR client (Phase 1 khung)
 * Phase 2 sẽ bổ sung: DevicesUpdated, InstallProgress, DeviceInstalled, ScanProgress, ScanFound.
 */
(function () {
    'use strict';

    const HUB_URL = '/hubs/device';
    const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 15000, 30000]; // ms

    const statusEl = document.getElementById('signalr-status');

    function setStatus(connected) {
        if (!statusEl) return;
        if (connected) {
            statusEl.className = 'badge ms-3 bg-success';
            statusEl.innerHTML = '<i class="bi bi-wifi me-1"></i>Đã kết nối';
        } else {
            statusEl.className = 'badge ms-3 bg-secondary';
            statusEl.innerHTML = '<i class="bi bi-wifi-off me-1"></i>Mất kết nối';
        }
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect(RECONNECT_DELAYS)
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    // ── Server → Client events (Phase 2 sẽ implement handler thực tế) ──────────

    connection.on('DevicesUpdated', function (devices) {
        // Phase 2: cập nhật device grid
        console.debug('[SignalR] DevicesUpdated', devices?.length ?? 0, 'devices');
    });

    connection.on('InstallProgress', function (serial, percent, message) {
        console.debug('[SignalR] InstallProgress', serial, percent + '%', message);
    });

    connection.on('DeviceInstalled', function (serial, success, version) {
        console.debug('[SignalR] DeviceInstalled', serial, success, version);
    });

    connection.on('ScanProgress', function (found, scanned, total) {
        console.debug('[SignalR] ScanProgress', found, '/', scanned, '/', total);
    });

    connection.on('ScanFound', function (ipPort) {
        console.debug('[SignalR] ScanFound', ipPort);
    });

    // ── Connection lifecycle ───────────────────────────────────────────────────

    connection.onreconnecting(function (error) {
        setStatus(false);
        console.warn('[SignalR] Reconnecting...', error?.message);
    });

    connection.onreconnected(function (connectionId) {
        setStatus(true);
        console.info('[SignalR] Reconnected. connectionId=' + connectionId);
    });

    connection.onclose(function (error) {
        setStatus(false);
        if (error) console.error('[SignalR] Connection closed with error:', error.message);
    });

    async function startConnection() {
        try {
            await connection.start();
            setStatus(true);
            console.info('[SignalR] Connected to ' + HUB_URL);
        } catch (err) {
            setStatus(false);
            console.error('[SignalR] Start failed:', err.message);
            // withAutomaticReconnect xử lý retry tự động sau khi start thành công ít nhất 1 lần.
            // Nếu start() thất bại ngay từ đầu, thử lại thủ công sau 5s.
            setTimeout(startConnection, 5000);
        }
    }

    // Expose connection cho Phase 2 (các module JS khác có thể gọi connection.invoke(...))
    window.kzHubConnection = connection;

    // Khởi động khi DOM sẵn sàng
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startConnection);
    } else {
        startConnection();
    }
})();
