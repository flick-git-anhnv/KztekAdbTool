/**
 * KZTEK ADB Publish Tool — Network Scan Modal JS (STEP-2.7)
 * Mirrors NetworkScanForm.cs behaviour in the browser:
 *   - POST /api/scan/start  → SignalR ScanProgress / ScanFound / ScanCompleted
 *   - POST /api/scan/cancel
 *   - POST /api/devices/connect-batch → toast → close modal
 *
 * Depends on: signalr-client.js (window.kzHubConnection), dashboard.js (showToast-like)
 */
(function () {
    'use strict';

    // ── Simple toast (duplicated to keep this module self-contained) ─────────────
    function showToast(msg, type) {
        type = type || 'success';
        const container = document.getElementById('kz-toast-container');
        if (!container) return;
        const id = 'toast-scan-' + Date.now();
        const bgMap = {
            success: 'bg-success',
            warning: 'bg-warning text-dark',
            danger: 'bg-danger',
            info: 'bg-info text-dark'
        };
        const cls = bgMap[type] || 'bg-secondary';
        container.insertAdjacentHTML('beforeend',
            '<div id="' + id + '" class="toast align-items-center text-white ' + cls + ' border-0 mb-2" role="alert">' +
            '<div class="d-flex"><div class="toast-body">' + msg + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
            '</div></div>');
        const el = document.getElementById(id);
        if (!el) return;
        const toast = new bootstrap.Toast(el, { delay: 3500 });
        toast.show();
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
    }

    // ── Validate IP range — mirrors NetworkScanForm.OnScanAsync validation ────────
    // Accepts: "192.168.1.1-254" or "192.168.1.1-192.168.1.254"
    function validateRange(range) {
        if (!range) return 'Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254';
        const parts = range.split('-');
        if (parts.length !== 2) return 'Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254';

        const ipRegex = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/;
        const startMatch = ipRegex.exec(parts[0].trim());
        if (!startMatch) return 'Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254';

        const endPart = parts[1].trim();
        if (endPart.includes('.')) {
            // Full IP end
            const endMatch = ipRegex.exec(endPart);
            if (!endMatch) return 'Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254';
            // Must be same /24
            if (startMatch[1] !== endMatch[1] || startMatch[2] !== endMatch[2] || startMatch[3] !== endMatch[3]) {
                return 'Chỉ hỗ trợ quét trong cùng dải /24 (3 octet đầu phải giống nhau).';
            }
        } else {
            // Short form — only last octet
            const lastOctet = parseInt(endPart, 10);
            if (isNaN(lastOctet) || lastOctet < 0 || lastOctet > 255) {
                return 'Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254';
            }
        }
        return null; // valid
    }

    // ── Main setup ────────────────────────────────────────────────────────────────
    function setupScanModal() {
        const conn = window.kzHubConnection;
        if (!conn) {
            // Retry until signalr-client.js has populated window.kzHubConnection
            setTimeout(setupScanModal, 300);
            return;
        }

        // DOM refs
        const btnStart = document.getElementById('btn-scan-start');
        const btnStop = document.getElementById('btn-scan-stop');
        const btnAdd = document.getElementById('btn-scan-add');
        const scanRange = document.getElementById('scan-range');
        const scanPort = document.getElementById('scan-port');
        const progressBar = document.getElementById('scan-progress-bar');
        const statusEl = document.getElementById('scan-status');
        const tbody = document.getElementById('scan-result-tbody');
        const modalEl = document.getElementById('networkScanModal');

        if (!btnStart) return; // Modal not in DOM (should not happen)

        // ── Toggle scanning state (disable/enable inputs) ─────────────────────────
        function setScanning(scanning) {
            if (btnStart) btnStart.disabled = scanning;
            if (btnStop) btnStop.disabled = !scanning;
            if (scanRange) scanRange.disabled = scanning;
            if (scanPort) scanPort.disabled = scanning;
        }

        // ── Reset modal UI ────────────────────────────────────────────────────────
        function resetResults() {
            if (tbody) tbody.innerHTML = '';
            if (progressBar) progressBar.style.width = '0%';
            if (statusEl) statusEl.textContent = 'Nhập dải IP rồi bấm Quét.';
        }

        // ── Add a found device row to the result table ────────────────────────────
        function addFoundRow(ipPort) {
            if (!tbody) return;
            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td class="text-center"><input type="checkbox" class="form-check-input" checked></td>' +
                '<td>' + ipPort + '</td>';
            tbody.appendChild(tr);
        }

        // ── SignalR scan events ───────────────────────────────────────────────────
        conn.on('ScanProgress', function (found, scanned, total) {
            if (progressBar && total > 0) {
                const pct = Math.round((scanned / total) * 100);
                progressBar.style.width = pct + '%';
            }
            if (statusEl) {
                statusEl.textContent = 'Đang quét ' + scanned + '/' + total + ' — tìm thấy ' + found;
            }
        });

        conn.on('ScanFound', function (ipPort) {
            addFoundRow(ipPort);
        });

        conn.on('ScanCompleted', function (total, found) {
            setScanning(false);
            if (progressBar) progressBar.style.width = '100%';
            if (statusEl) {
                statusEl.textContent = 'Hoàn tất: ' + found + '/' + total + ' địa chỉ phản hồi adb.';
            }
        });

        // ── Row click in result table → toggle checkbox ───────────────────────────
        if (tbody) {
            tbody.addEventListener('click', function (e) {
                const td = e.target.closest('td');
                const tr = e.target.closest('tr');
                if (!tr || !td) return;
                // Direct click on checkbox → let it handle itself
                if (e.target.matches('input[type=checkbox]')) return;
                const cb = tr.querySelector('input[type=checkbox]');
                if (cb) cb.checked = !cb.checked;
            });
        }

        // ── Start scan ────────────────────────────────────────────────────────────
        if (btnStart) {
            btnStart.addEventListener('click', async function () {
                const range = ((scanRange || {}).value || '').trim();
                const portStr = ((scanPort || {}).value || '5555').trim();

                const rangeErr = validateRange(range);
                if (rangeErr) {
                    alert(rangeErr);
                    return;
                }

                const port = parseInt(portStr, 10);
                if (isNaN(port) || port <= 0 || port > 65535) {
                    alert('Cổng không hợp lệ.');
                    return;
                }

                resetResults();
                setScanning(true);

                try {
                    const r = await fetch('/api/scan/start', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        // FIX-3.2 (Code Migrator review): backend DTO là ScanStartRequest.RangeText,
                        // không phải "ipRange" → validation luôn fail vì rangeText null.
                        body: JSON.stringify({ rangeText: range, port: port })
                    });
                    if (!r.ok) {
                        const txt = await r.text();
                        alert('Lỗi khởi động quét: ' + txt);
                        setScanning(false);
                    }
                    // If ok: server will push ScanProgress / ScanFound / ScanCompleted via SignalR
                } catch (ex) {
                    alert('Lỗi kết nối: ' + ex.message);
                    setScanning(false);
                }
            });
        }

        // ── Stop scan ─────────────────────────────────────────────────────────────
        if (btnStop) {
            btnStop.addEventListener('click', async function () {
                try {
                    await fetch('/api/scan/cancel', { method: 'POST' });
                } catch (_) { /* ignore */ }
                setScanning(false);
                if (statusEl) statusEl.textContent = 'Đã dừng quét.';
            });
        }

        // ── Add selected to main device list ──────────────────────────────────────
        if (btnAdd) {
            btnAdd.addEventListener('click', async function () {
                if (!tbody) return;
                const selected = Array.from(tbody.querySelectorAll('tr')).filter(function (tr) {
                    const cb = tr.querySelector('input[type=checkbox]');
                    return cb && cb.checked;
                }).map(function (tr) {
                    return tr.cells[1] ? tr.cells[1].textContent.trim() : '';
                }).filter(Boolean);

                if (selected.length === 0) {
                    showToast('Chưa chọn thiết bị nào để thêm.', 'warning');
                    return;
                }

                try {
                    const r = await fetch('/api/devices/connect-batch', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ ipPorts: selected })
                    });
                    if (r.ok) {
                        showToast('Đã thêm ' + selected.length + ' thiết bị vào danh sách.', 'success');
                        // Close modal after successful add
                        if (modalEl) {
                            const modal = bootstrap.Modal.getInstance(modalEl);
                            if (modal) modal.hide();
                        }
                    } else {
                        const txt = await r.text();
                        showToast('Lỗi thêm thiết bị: ' + txt, 'danger');
                    }
                } catch (ex) {
                    showToast('Lỗi: ' + ex.message, 'danger');
                }
            });
        }

        // ── Reset modal when hidden (supports re-open) ────────────────────────────
        if (modalEl) {
            modalEl.addEventListener('hidden.bs.modal', function () {
                resetResults();
                setScanning(false);
            });
        }
    }

    // ── Init ──────────────────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', setupScanModal);

})();
