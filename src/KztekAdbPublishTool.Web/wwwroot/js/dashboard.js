/**
 * KZTEK ADB Publish Tool — Dashboard JS (STEP-2.8)
 * Responsibilities:
 *   - SignalR event handlers (DevicesUpdated, Log, InstallProgress, DeviceInstalled)
 *   - Fallback polling GET /api/devices every 5s when SignalR disconnects
 *   - Device grid: filter by IP/version, toggle select-all (visible rows only)
 *   - Row click → toggle checkbox (excluding direct checkbox clicks)
 *   - Buttons: Install selected/all, Remove selected, Connect, Refresh, Auto-detect
 *   - APK upload → POST /api/apk/upload → auto-fill package input
 *   - Package blur → POST /api/settings/package (non-empty only)
 *
 * Depends on: signalr-client.js (window.kzHubConnection)
 */
(function () {
    'use strict';

    const LOG_MAX_LINES = 1000;
    const POLL_INTERVAL_MS = 5000;

    let pollTimer = null;
    // Track checked state across DevicesUpdated re-renders
    const checkedSerials = new Set();

    // ── DOM helpers ──────────────────────────────────────────────────────────────
    function $id(id) { return document.getElementById(id); }

    // ── HTML escaping ────────────────────────────────────────────────────────────
    function esc(str) {
        return String(str == null ? '' : str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // ── Date formatting (dd/MM HH:mm:ss) ────────────────────────────────────────
    function fmtDate(isoStr) {
        if (!isoStr) return '-';
        try {
            const d = new Date(isoStr);
            if (isNaN(d.getTime())) return '-';
            const p = function (n) { return String(n).padStart(2, '0'); };
            return p(d.getDate()) + '/' + p(d.getMonth() + 1) + ' ' +
                p(d.getHours()) + ':' + p(d.getMinutes()) + ':' + p(d.getSeconds());
        } catch (_) { return '-'; }
    }

    // ── Toast notifications ──────────────────────────────────────────────────────
    function showToast(msg, type) {
        type = type || 'success';
        const container = $id('kz-toast-container');
        if (!container) return;
        const id = 'toast-' + Date.now();
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
        const el = $id(id);
        if (!el) return;
        const toast = new bootstrap.Toast(el, { delay: 3500 });
        toast.show();
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
    }

    // ── Log area ─────────────────────────────────────────────────────────────────
    function appendLog(msg) {
        const el = $id('log');
        if (!el) return;
        const now = new Date();
        const t = [now.getHours(), now.getMinutes(), now.getSeconds()]
            .map(function (n) { return String(n).padStart(2, '0'); }).join(':');
        el.textContent += '[' + t + '] ' + msg + '\n';
        // Giới hạn ~1000 dòng (cải tiến nhỏ so với WinForms không giới hạn)
        const lines = el.textContent.split('\n');
        if (lines.length > LOG_MAX_LINES + 1) {
            el.textContent = lines.slice(lines.length - LOG_MAX_LINES).join('\n');
        }
        el.scrollTop = el.scrollHeight;
    }

    // ── Device grid render (called by DevicesUpdated SignalR event) ───────────────
    function renderDevices(devices) {
        const tb = $id('device-tbody');
        if (!tb) return;

        // Sort: Online first, then ascending by serial — mirrors WinForms RenderGrid
        devices.sort(function (a, b) {
            const ao = (a.status === 'Online') ? 0 : 1;
            const bo = (b.status === 'Online') ? 0 : 1;
            if (ao !== bo) return ao - bo;
            return (a.serial || '').localeCompare(b.serial || '');
        });

        let html = '';
        devices.forEach(function (d) {
            const offline = d.status !== 'Online';
            const chk = checkedSerials.has(d.serial) ? ' checked' : '';
            html +=
                '<tr class="' + (offline ? 'offline-row' : '') + '"' +
                ' data-serial="' + esc(d.serial) + '"' +
                ' data-version="' + esc(d.installedVersion || '') + '">' +
                '<td class="text-center"><input type="checkbox" class="form-check-input device-checkbox"' + chk + '></td>' +
                '<td>' + esc(d.serial) + '</td>' +
                '<td>' + esc(d.model) + '</td>' +
                '<td>' + esc(d.status) + '</td>' +
                '<td>' + esc(d.connectionType) + '</td>' +
                '<td>' + esc(d.installedVersion || '-') + '</td>' +
                '<td>' + fmtDate(d.lastInstallTime) + '</td>' +
                '<td>' + fmtDate(d.lastSeen) + '</td>' +
                '<td>' + esc(d.lastInstallStatus || '-') + '</td>' +
                '</tr>';
        });
        tb.innerHTML = html;

        // Update count badge
        const badge = $id('device-count-badge');
        if (badge) badge.textContent = devices.length + ' thiết bị';

        applyFilter();
    }

    // ── Filter: hide/show rows by IP/serial and version ──────────────────────────
    function applyFilter() {
        const ipVal = (($id('filter-ip') || {}).value || '').toLowerCase().trim();
        const verVal = (($id('filter-version') || {}).value || '').toLowerCase().trim();
        const tb = $id('device-tbody');
        if (!tb) return;
        Array.from(tb.querySelectorAll('tr')).forEach(function (row) {
            const serial = (row.dataset.serial || '').toLowerCase();
            const ver = (row.dataset.version || '').toLowerCase();
            const show = (!ipVal || serial.includes(ipVal)) && (!verVal || ver.includes(verVal));
            row.style.display = show ? '' : 'none';
        });
    }

    // ── Toggle Select All — mirrors WinForms OnToggleSelectAll ───────────────────
    // Logic: nếu tất cả visible rows đều tick → bỏ tick hết; ngược lại → tick hết.
    function toggleSelectAll() {
        const tb = $id('device-tbody');
        if (!tb) return;
        const visibleRows = Array.from(tb.querySelectorAll('tr')).filter(function (r) {
            return r.style.display !== 'none';
        });
        const visibleSerials = visibleRows
            .map(function (r) { return r.dataset.serial; })
            .filter(Boolean);

        const allSelected = visibleSerials.length > 0 &&
            visibleSerials.every(function (s) { return checkedSerials.has(s); });

        visibleRows.forEach(function (row) {
            const serial = row.dataset.serial;
            const cb = row.querySelector('.device-checkbox');
            if (allSelected) {
                checkedSerials.delete(serial);
                if (cb) cb.checked = false;
            } else {
                checkedSerials.add(serial);
                if (cb) cb.checked = true;
            }
        });
    }

    // ── Get currently-checked serials from visible DOM ────────────────────────────
    function getCheckedSerials() {
        const tb = $id('device-tbody');
        if (!tb) return [];
        return Array.from(tb.querySelectorAll('tr')).filter(function (r) {
            const cb = r.querySelector('.device-checkbox');
            return cb && cb.checked;
        }).map(function (r) { return r.dataset.serial; }).filter(Boolean);
    }

    // ── API helpers ───────────────────────────────────────────────────────────────
    function apiPost(url, body) {
        const opts = {
            method: 'POST',
            headers: body !== undefined ? { 'Content-Type': 'application/json' } : {}
        };
        if (body !== undefined) opts.body = JSON.stringify(body);
        return fetch(url, opts);
    }

    // ── Fallback polling (GET /api/devices every 5s) ──────────────────────────────
    function startFallbackPoll() {
        if (pollTimer) return;
        appendLog('[Fallback] SignalR mất kết nối — bắt đầu polling /api/devices mỗi 5s');
        pollTimer = setInterval(function () {
            fetch('/api/devices')
                .then(function (r) { return r.ok ? r.json() : null; })
                // FIX-3.1c: backend trả {ok, data:[...]}, không phải array trực tiếp
                .then(function (resp) { if (resp && Array.isArray(resp.data)) renderDevices(resp.data); })
                .catch(function () { /* ignore transient errors */ });
        }, POLL_INTERVAL_MS);
    }

    function stopFallbackPoll() {
        if (pollTimer) {
            clearInterval(pollTimer);
            pollTimer = null;
            appendLog('[Fallback] SignalR khôi phục — dừng polling');
        }
    }

    // ── SignalR event registration ────────────────────────────────────────────────
    function setupSignalR() {
        const conn = window.kzHubConnection;
        if (!conn) {
            // signalr-client.js chưa chạy xong — thử lại sau 500ms
            setTimeout(setupSignalR, 500);
            return;
        }

        // Server → client: full device list refresh
        conn.on('DevicesUpdated', function (devices) {
            renderDevices(devices || []);
            stopFallbackPoll();
        });

        // Server → client: log message from backend
        conn.on('Log', function (msg) {
            appendLog(msg);
        });

        // Server → client: per-device install progress
        conn.on('InstallProgress', function (serial, percent, msg) {
            const pb = $id('install-progress');
            if (pb) {
                pb.style.width = percent + '%';
                pb.setAttribute('aria-valuenow', String(percent));
            }
            const lbl = $id('status-label');
            if (lbl) lbl.textContent = '[' + serial + '] ' + msg;
        });

        // Server → client: single device install finished
        conn.on('DeviceInstalled', function (serial, success, version) {
            // Update cells in-place without full re-render
            const tb = $id('device-tbody');
            if (!tb) return;
            const row = tb.querySelector('tr[data-serial="' + serial + '"]');
            if (row) {
                const cells = row.querySelectorAll('td');
                // cell[5] = installedVersion, cell[8] = lastInstallStatus
                if (cells[5]) cells[5].textContent = version || '-';
                if (cells[8]) cells[8].textContent = success ? 'Thành công' : 'Thất bại';
                row.dataset.version = version || '';
            }
            appendLog('[' + serial + '] ' + (success ? 'Cài thành công — version: ' + version : 'Cài thất bại'));
        });

        // Connection lifecycle: start fallback when permanently closed
        conn.onclose(function () {
            startFallbackPoll();
        });

        conn.onreconnected(function () {
            stopFallbackPoll();
        });
    }

    // ── Bind all interactive elements ─────────────────────────────────────────────
    function bindEvents() {

        // Filter inputs
        const fIp = $id('filter-ip');
        const fVer = $id('filter-version');
        if (fIp) fIp.addEventListener('input', applyFilter);
        if (fVer) fVer.addEventListener('input', applyFilter);

        // Device table: row click → toggle checkbox (delegate); checkbox change → update Set
        const tbl = $id('device-table');
        if (tbl) {
            tbl.addEventListener('click', function (e) {
                const td = e.target.closest('td');
                const tr = e.target.closest('tr[data-serial]');
                if (!tr || !td) return;
                // Ignore direct click on checkbox cell (let checkbox handle itself)
                if (e.target.matches('.device-checkbox') || td === tr.cells[0] && e.target.closest('input')) return;
                const cb = tr.querySelector('.device-checkbox');
                if (!cb) return;
                cb.checked = !cb.checked;
                const serial = tr.dataset.serial;
                if (serial) {
                    if (cb.checked) checkedSerials.add(serial);
                    else checkedSerials.delete(serial);
                }
            });

            tbl.addEventListener('change', function (e) {
                if (!e.target.classList.contains('device-checkbox')) return;
                const tr = e.target.closest('tr[data-serial]');
                if (!tr) return;
                const serial = tr.dataset.serial;
                if (serial) {
                    if (e.target.checked) checkedSerials.add(serial);
                    else checkedSerials.delete(serial);
                }
            });
        }

        // Select All toggle
        const btnSelAll = $id('btn-select-all');
        if (btnSelAll) btnSelAll.addEventListener('click', toggleSelectAll);

        // Connect single device
        const btnConnect = $id('btn-connect');
        if (btnConnect) {
            btnConnect.addEventListener('click', async function () {
                let ipPort = (($id('txt-connect') || {}).value || '').trim();
                if (!ipPort) {
                    alert('Nhập địa chỉ IP:port của thiết bị (VD: 192.168.1.50:5555).');
                    return;
                }
                if (!ipPort.includes(':')) ipPort += ':5555';
                appendLog('Đang kết nối tới ' + ipPort + '...');
                try {
                    const r = await apiPost('/api/devices/connect', { ipPort: ipPort });
                    const data = await r.json();
                    if (data.ok) {
                        appendLog('Đã kết nối ' + ipPort + (data.data ? ': ' + data.data : ''));
                        showToast('Đã kết nối ' + ipPort, 'success');
                    } else {
                        appendLog('Lỗi kết nối ' + ipPort + ': ' + (data.error || 'Không rõ lỗi'));
                        showToast('Lỗi kết nối: ' + (data.error || 'Không rõ lỗi'), 'danger');
                    }
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
            });
        }

        // Refresh (force poll)
        const btnRefresh = $id('btn-refresh');
        if (btnRefresh) {
            btnRefresh.addEventListener('click', async function () {
                appendLog('Đang làm mới trạng thái thiết bị đang kết nối...');
                try {
                    await apiPost('/api/devices/poll');
                    showToast('Đã làm mới trạng thái', 'info');
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
            });
        }

        // Auto-detect toggle switch
        const chkAuto = $id('chk-auto-detect');
        if (chkAuto) {
            chkAuto.addEventListener('change', async function () {
                try {
                    await apiPost('/api/polling/toggle', { enabled: chkAuto.checked });
                } catch (ex) {
                    appendLog('Lỗi toggle auto-detect: ' + ex.message);
                }
            });
        }

        // Browse APK button → trigger hidden file input
        const btnBrowse = $id('btn-browse-apk');
        const inputApk = $id('input-apk-file');
        if (btnBrowse && inputApk) {
            btnBrowse.addEventListener('click', function () { inputApk.click(); });

            inputApk.addEventListener('change', async function () {
                const file = inputApk.files[0];
                if (!file) return;
                const fd = new FormData();
                // FIX-3.2 (Code Migrator review): backend đọc form.Files["apk"], không phải "file" → upload luôn 400.
                fd.append('apk', file);
                appendLog('Đang tải lên APK: ' + file.name + '...');
                try {
                    const r = await fetch('/api/apk/upload', { method: 'POST', body: fd });
                    if (r.ok) {
                        const data = await r.json();
                        const pathEl = $id('txt-apk-path');
                        if (pathEl) pathEl.value = data.path || file.name;
                        if (data.packageName) {
                            const pkgEl = $id('txt-package');
                            if (pkgEl) pkgEl.value = data.packageName;
                            appendLog('Tự phát hiện package: ' + data.packageName);
                        } else {
                            appendLog('Không tự phát hiện được package — vui lòng nhập tay.');
                        }
                        appendLog('Đã tải lên APK: ' + file.name);
                        showToast('Đã tải lên APK thành công', 'success');
                    } else {
                        const txt = await r.text();
                        appendLog('Lỗi tải APK: ' + txt);
                        showToast('Lỗi tải APK: ' + txt, 'danger');
                    }
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
                // Reset so same file can be selected again
                inputApk.value = '';
            });
        }

        // Package input blur → save (only when non-empty — mirrors WinForms Leave handler)
        const txtPackage = $id('txt-package');
        if (txtPackage) {
            txtPackage.addEventListener('blur', async function () {
                const val = txtPackage.value.trim();
                if (!val) return;
                try {
                    await apiPost('/api/settings/package', { packageName: val });
                } catch (ex) {
                    appendLog('Lỗi lưu package: ' + ex.message);
                }
            });
        }

        // Install selected devices
        const btnInstSel = $id('btn-install-selected');
        if (btnInstSel) {
            btnInstSel.addEventListener('click', async function () {
                const serials = getCheckedSerials();
                if (serials.length === 0) {
                    alert('Vui lòng chọn ít nhất một thiết bị để cài đặt.');
                    return;
                }
                const pb = $id('install-progress');
                if (pb) { pb.style.width = '0%'; pb.setAttribute('aria-valuenow', '0'); }
                const lbl = $id('status-label');
                if (lbl) lbl.textContent = 'Đang cài đặt...';
                appendLog('Bắt đầu cài cho ' + serials.length + ' thiết bị đã chọn...');
                try {
                    const r = await apiPost('/api/install', { serials: serials, selectedOnly: true });
                    if (!r.ok) {
                        const txt = await r.text();
                        appendLog('Lỗi cài đặt: ' + txt);
                        showToast('Lỗi cài đặt: ' + txt, 'danger');
                    }
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
            });
        }

        // Install all online devices
        const btnInstAll = $id('btn-install-all');
        if (btnInstAll) {
            btnInstAll.addEventListener('click', async function () {
                const pb = $id('install-progress');
                if (pb) { pb.style.width = '0%'; pb.setAttribute('aria-valuenow', '0'); }
                const lbl = $id('status-label');
                if (lbl) lbl.textContent = 'Đang cài đặt tất cả...';
                appendLog('Bắt đầu cài cho tất cả thiết bị Online...');
                try {
                    // serials: [] + selectedOnly: false → backend cài tất cả Online
                    const r = await apiPost('/api/install', { serials: [], selectedOnly: false });
                    if (!r.ok) {
                        const txt = await r.text();
                        appendLog('Lỗi cài đặt: ' + txt);
                        showToast('Lỗi cài đặt: ' + txt, 'danger');
                    }
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
            });
        }

        // Remove selected devices — confirm dialog mirrors WinForms text exactly
        const btnRemove = $id('btn-remove-selected');
        if (btnRemove) {
            btnRemove.addEventListener('click', async function () {
                const serials = getCheckedSerials();
                if (serials.length === 0) {
                    alert('Chưa chọn thiết bị nào để xóa.');
                    return;
                }
                const ok = confirm(
                    'Xóa ' + serials.length + ' thiết bị đã chọn khỏi danh sách quản lý?\n' +
                    '(Sẽ ngắt kết nối adb luôn — muốn dùng lại phải Kết nối hoặc Quét dải mạng lại.)'
                );
                if (!ok) return;
                try {
                    const r = await apiPost('/api/devices/remove', { serials: serials });
                    if (r.ok) {
                        serials.forEach(function (s) { checkedSerials.delete(s); });
                        // FIX: xóa row khỏi DOM ngay, không chờ SignalR/poll kế tiếp —
                        // parity với WinForms OnRemoveSelected() gọi RenderGrid() ngay sau khi xóa
                        // (MainForm.cs:579). Trước fix: phải F5 hoặc chờ tick poll mới thấy mất.
                        const tb = $id('device-tbody');
                        if (tb) {
                            serials.forEach(function (s) {
                                const row = tb.querySelector('tr[data-serial="' + s.replace(/"/g, '\\"') + '"]');
                                if (row) row.remove();
                            });
                            const badge = $id('device-count-badge');
                            if (badge) badge.textContent = tb.querySelectorAll('tr').length + ' thiết bị';
                        }
                        appendLog('Đã xóa ' + serials.length + ' thiết bị khỏi danh sách quản lý.');
                        showToast('Đã xóa ' + serials.length + ' thiết bị', 'success');
                    } else {
                        const txt = await r.text();
                        showToast('Lỗi xóa thiết bị: ' + txt, 'danger');
                    }
                } catch (ex) {
                    appendLog('Lỗi: ' + ex.message);
                }
            });
        }
    }

    // ── Bootstrap init ────────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        setupSignalR();
        bindEvents();
    });

})();
