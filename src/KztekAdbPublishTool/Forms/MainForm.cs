using System.Data;
using KztekAdbPublishTool.Models;
using KztekAdbPublishTool.Services;
using KztekComponent.Controls;

namespace KztekAdbPublishTool.Forms;

public sealed class MainForm : Form
{
    private const int ColSelect = 0;
    private const int ColSerial = 1;
    private const int ColModel = 2;
    private const int ColStatus = 3;
    private const int ColConnType = 4;
    private const int ColVersion = 5;
    private const int ColInstallTime = 6;
    private const int ColLastSeen = 7;
    private const int ColInstallResult = 8;

    private readonly AdbService _adb;
    private readonly DeviceRepository _repo;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly Dictionary<string, DeviceRecord> _devices = new();
    private readonly HashSet<string> _selectedSerials = new();

    private string _apkPath = string.Empty;
    private bool _isBusy;
    private bool _suppressGridEvents;

    private KzDataGrid _grid = null!;
    private KzTextBox _txtPackage = null!;
    private KzTextBox _txtApkPath = null!;
    private KzTextBox _txtConnect = null!;
    private KzTextBox _txtLog = null!;
    private KzButton _btnBrowseApk = null!;
    private KzButton _btnInstallSelected = null!;
    private KzButton _btnInstallAll = null!;
    private KzButton _btnConnect = null!;
    private KzButton _btnNetworkScan = null!;
    private KzButton _btnRefresh = null!;
    private KzButton _btnRemoveSelected = null!;
    private KzButton _btnSelectAll = null!;
    private KzTextBox _txtFilterIp = null!;
    private KzTextBox _txtFilterVersion = null!;
    private KzCheckBox _chkAutoDetect = null!;
    private KzProgressBar _progress = null!;
    private Label _lblStatus = null!;

    public MainForm()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var adbPath = Path.Combine(baseDir, "platform-tools", "adb.exe");
        var dbPath = Path.Combine(baseDir, "adbpublishtool.db");

        _adb = new AdbService(adbPath);
        _repo = new DeviceRepository(dbPath);

        BuildUi();
        LoadPersistedState();

        _pollTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _pollTimer.Tick += async (_, _) => await PollDevicesAsync();

        Load += async (_, _) =>
        {
            await PollDevicesAsync();
            _pollTimer.Start();
        };
    }

    private void LoadPersistedState()
    {
        foreach (var device in _repo.GetAll())
        {
            device.Status = "Offline";
            _devices[device.Serial] = device;
        }
        RenderGrid();

        _txtPackage.Text = _repo.GetSetting("PackageName") ?? string.Empty;
        _apkPath = _repo.GetSetting("ApkPath") ?? string.Empty;
        _txtApkPath.Text = _apkPath;
    }

    // ------------------------------------------------------------------
    // UI construction
    // ------------------------------------------------------------------

    private void BuildUi()
    {
        Text = "KZTEK ADB Publish Tool";
        Size = new Size(1280, 780);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        Controls.Add(root);

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGridArea(), 0, 1);
        root.Controls.Add(BuildLogArea(), 0, 2);
    }

    private Control BuildToolbar()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 33));

        // Hàng 1: package cần theo dõi + chọn file APK
        var row1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
        };

        var lblPackage = new Label { Text = "Gói cần theo dõi (package):", AutoSize = true, Margin = new Padding(0, 12, 6, 0) };
        _txtPackage = new KzTextBox { Width = 220, Height = 34, Margin = new Padding(0, 2, 20, 0), PlaceholderText = "com.company.app" };
        _txtPackage.Leave += (_, _) =>
        {
            // Chỉ lưu khi có giá trị — tránh mất package đã lưu nếu ô bị rỗng thoáng qua
            // (VD: focus đi qua ô này trước khi user gõ xong) vô tình ghi đè giá trị tốt trước đó.
            var text = _txtPackage.Text.Trim();
            if (!string.IsNullOrEmpty(text)) _repo.SetSetting("PackageName", text);
        };

        _btnBrowseApk = new KzButton { Text = "Chọn APK...", Width = 140, Height = 34, Margin = new Padding(0, 4, 8, 0), Padding = Padding.Empty };
        _btnBrowseApk.Click += (_, _) => OnBrowseApk();

        _txtApkPath = new KzTextBox { Width = 380, Height = 34, Margin = new Padding(0, 2, 0, 0), ReadOnly = true, PlaceholderText = "Chưa chọn file APK" };

        row1.Controls.Add(lblPackage);
        row1.Controls.Add(_txtPackage);
        row1.Controls.Add(_btnBrowseApk);
        row1.Controls.Add(_txtApkPath);

        // Hàng 2: thêm nhanh thiết bị theo IP:port
        var row2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
        };

        var lblConnect = new Label { Text = "Thêm nhanh (IP:port):", AutoSize = true, Margin = new Padding(0, 12, 6, 0) };
        _txtConnect = new KzTextBox { Width = 170, Height = 34, Margin = new Padding(0, 2, 8, 0), PlaceholderText = "192.168.1.50:5555" };
        _btnConnect = new KzButton { Text = "Kết nối", Width = 130, Height = 34, Margin = new Padding(0, 4, 0, 0), Padding = Padding.Empty };
        _btnConnect.Click += async (_, _) => await OnConnectAsync();

        row2.Controls.Add(lblConnect);
        row2.Controls.Add(_txtConnect);
        row2.Controls.Add(_btnConnect);

        // Hàng 3: quét dải mạng + quét lại + auto-detect
        var row3 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
        };

        _btnNetworkScan = new KzButton { Text = "Quét dải mạng...", Width = 150, Height = 34, Margin = new Padding(0, 4, 20, 0), Padding = Padding.Empty };
        _btnNetworkScan.Click += async (_, _) => await OnNetworkScanAsync();

        _btnRefresh = new KzButton { Text = "Quét lại", Width = 130, Height = 34, Margin = new Padding(0, 4, 20, 0), Padding = Padding.Empty };
        _btnRefresh.Click += async (_, _) => await PollDevicesAsync(force: true);

        _chkAutoDetect = new KzCheckBox { Text = "Tự động phát hiện thiết bị", Checked = true, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        _chkAutoDetect.CheckedChanged += (_, _) =>
        {
            if (_chkAutoDetect.Checked) _pollTimer.Start(); else _pollTimer.Stop();
        };

        row3.Controls.Add(_btnNetworkScan);
        row3.Controls.Add(_btnRefresh);
        row3.Controls.Add(_chkAutoDetect);

        outer.Controls.Add(row1, 0, 0);
        outer.Controls.Add(row2, 0, 1);
        outer.Controls.Add(row3, 0, 2);

        return outer;
    }

    private Control BuildGridArea()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));

        var gridStack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        gridStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        gridStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        gridStack.Controls.Add(BuildFilterBar(), 0, 0);

        _grid = new KzDataGrid
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        };
        _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colSelect",
            HeaderText = "Chọn",
            MinimumWidth = 55,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial", HeaderText = "Serial", ReadOnly = true, MinimumWidth = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModel", HeaderText = "Model", ReadOnly = true, MinimumWidth = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Trạng thái", ReadOnly = true, MinimumWidth = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConnType", HeaderText = "Kết nối", ReadOnly = true, MinimumWidth = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVersion", HeaderText = "Version đã cài", ReadOnly = true, MinimumWidth = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInstallTime", HeaderText = "Thời gian cài", ReadOnly = true, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastSeen", HeaderText = "Lần thấy cuối", ReadOnly = true, MinimumWidth = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInstallResult", HeaderText = "Kết quả cài lần cuối", ReadOnly = true, MinimumWidth = 170 });

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (_suppressGridEvents || e.RowIndex < 0 || e.ColumnIndex != ColSelect) return;
            var serial = _grid.Rows[e.RowIndex].Cells[ColSerial].Value?.ToString();
            if (serial is null) return;
            var isChecked = _grid.Rows[e.RowIndex].Cells[ColSelect].Value is bool b && b;
            if (isChecked) _selectedSerials.Add(serial); else _selectedSerials.Remove(serial);
        };
        // Bấm vào bất kỳ ô nào trên dòng cũng tick/bỏ tick — không cần bấm chính xác vào checkbox.
        _grid.CellClick += (_, e) =>
        {
            if (_suppressGridEvents || e.RowIndex < 0 || e.ColumnIndex == ColSelect) return;
            var cell = _grid.Rows[e.RowIndex].Cells[ColSelect];
            cell.Value = !(cell.Value is bool b && b);
            _grid.EndEdit();
        };

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12, 0, 0, 0),
        };

        _btnSelectAll = new KzButton { Text = "Chọn tất cả", Width = 200, Height = 34, Margin = new Padding(0, 0, 0, 14), Padding = Padding.Empty };
        _btnSelectAll.Click += (_, _) => OnToggleSelectAll();

        _btnInstallSelected = new KzButton { Text = "Cài đặt cho thiết bị đã chọn", Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 10), Padding = Padding.Empty };
        _btnInstallSelected.Click += async (_, _) => await OnInstallAsync(selectedOnly: true);

        _btnInstallAll = new KzButton { Text = "Cài đặt cho tất cả (Online)", Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 10), Padding = Padding.Empty };
        _btnInstallAll.Click += async (_, _) => await OnInstallAsync(selectedOnly: false);

        _btnRemoveSelected = new KzButton { Text = "Xóa thiết bị đã chọn", Width = 200, Height = 40, Margin = new Padding(0, 0, 0, 10), Padding = Padding.Empty };
        _btnRemoveSelected.Click += (_, _) => OnRemoveSelected();

        _progress = new KzProgressBar { Width = 200, Height = 22, Margin = new Padding(0, 10, 0, 6) };
        _lblStatus = new Label { Width = 200, AutoSize = false, Height = 40, Text = "Sẵn sàng." };

        actionsPanel.Controls.Add(_btnSelectAll);
        actionsPanel.Controls.Add(_btnInstallSelected);
        actionsPanel.Controls.Add(_btnInstallAll);
        actionsPanel.Controls.Add(_btnRemoveSelected);
        actionsPanel.Controls.Add(_progress);
        actionsPanel.Controls.Add(_lblStatus);

        gridStack.Controls.Add(_grid, 0, 1);

        layout.Controls.Add(gridStack, 0, 0);
        layout.Controls.Add(actionsPanel, 1, 0);
        return layout;
    }

    private Control BuildFilterBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        var lblFilterIp = new Label { Text = "Lọc theo IP/Serial:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) };
        _txtFilterIp = new KzTextBox { Width = 160, Height = 32, Margin = new Padding(0, 2, 20, 0), PlaceholderText = "192.168..." };
        _txtFilterIp.TextChanged += (_, _) => RenderGrid();

        var lblFilterVersion = new Label { Text = "Lọc theo version:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) };
        _txtFilterVersion = new KzTextBox { Width = 140, Height = 32, Margin = new Padding(0, 2, 0, 0), PlaceholderText = "1.0.0" };
        _txtFilterVersion.TextChanged += (_, _) => RenderGrid();

        panel.Controls.Add(lblFilterIp);
        panel.Controls.Add(_txtFilterIp);
        panel.Controls.Add(lblFilterVersion);
        panel.Controls.Add(_txtFilterVersion);

        return panel;
    }

    private Control BuildLogArea()
    {
        _txtLog = new KzTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
        };
        return _txtLog;
    }

    // ------------------------------------------------------------------
    // Device discovery (polling adb devices -l)
    // ------------------------------------------------------------------

    private async Task PollDevicesAsync(bool force = false)
    {
        if (_isBusy && !force) return;

        try
        {
            var liveDevices = await _adb.GetDevicesAsync();
            var now = DateTime.Now;
            var liveSerials = new HashSet<string>(liveDevices.Select(d => d.Serial));
            var packageName = SafeInvoke(() => _txtPackage.Text.Trim());

            foreach (var live in liveDevices)
            {
                var isNew = !_devices.TryGetValue(live.Serial, out var record);
                if (isNew)
                {
                    record = new DeviceRecord
                    {
                        Serial = live.Serial,
                        Model = live.Model,
                        ConnectionType = live.Serial.Contains(':') ? "WIFI" : "USB",
                        FirstSeen = now,
                    };
                    _devices[live.Serial] = record;
                    AppendLog($"Phát hiện thiết bị mới: {live.Serial}");
                }

                record!.Status = live.State == "device" ? "Online" : live.State;
                record.LastSeen = now;
                if (!string.IsNullOrEmpty(live.Model)) record.Model = live.Model;
                _repo.Upsert(record);

                if (record.Status == "Online" && !string.IsNullOrWhiteSpace(packageName))
                    _ = RefreshVersionAsync(record, packageName);
            }

            foreach (var record in _devices.Values.Where(d => !liveSerials.Contains(d.Serial) && d.Status != "Offline"))
            {
                record.Status = "Offline";
                _repo.Upsert(record);
            }

            RunOnUi(RenderGrid);
        }
        catch (Exception ex)
        {
            RunOnUi(() => AppendLog($"Lỗi quét thiết bị: {ex.Message}"));
        }
    }

    private async Task RefreshVersionAsync(DeviceRecord record, string packageName)
    {
        try
        {
            var version = await _adb.GetPackageVersionAsync(record.Serial, packageName);
            record.InstalledVersion = version ?? "Chưa cài";
            _repo.Upsert(record);
            RunOnUi(RenderGrid);
        }
        catch
        {
            // Bỏ qua lỗi tạm thời khi đọc version — vòng poll kế tiếp sẽ thử lại.
        }
    }

    // ------------------------------------------------------------------
    // Install
    // ------------------------------------------------------------------

    private async Task OnInstallAsync(bool selectedOnly)
    {
        if (string.IsNullOrWhiteSpace(_apkPath) || !File.Exists(_apkPath))
        {
            MessageBox.Show(this, "Vui lòng chọn file APK hợp lệ trước khi cài đặt.", "Thiếu file APK",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var targets = selectedOnly
            ? _devices.Values.Where(d => _selectedSerials.Contains(d.Serial) && d.Status == "Online").ToList()
            : _devices.Values.Where(d => d.Status == "Online").ToList();

        if (targets.Count == 0)
        {
            MessageBox.Show(this, "Không có thiết bị Online phù hợp để cài đặt.", "Chưa có thiết bị",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        _progress.Minimum = 0;
        _progress.Maximum = targets.Count;
        _progress.Value = 0;

        var done = 0;
        using var semaphore = new SemaphoreSlim(4);
        var packageName = _txtPackage.Text.Trim();
        var apkPath = _apkPath;

        var tasks = targets.Select(async device =>
        {
            await semaphore.WaitAsync();
            try
            {
                RunOnUi(() => AppendLog($"[{device.Serial}] Bắt đầu cài đặt..."));

                // Chụp danh sách package trước khi cài để xác định chính xác package vừa cài
                // (không phụ thuộc user có nhập đúng package theo dõi hay không) — dùng để mở app tự động.
                var beforePackages = await _adb.ListThirdPartyPackagesAsync(device.Serial);

                var result = await _adb.InstallApkAsync(device.Serial, apkPath);
                var success = result.Success && result.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);
                var status = success ? "Thành công" : $"Thất bại: {(result.StdErr + result.StdOut).Trim()}";

                string? version = null;
                if (success)
                {
                    var installedPackage = packageName;
                    var afterPackages = await _adb.ListThirdPartyPackagesAsync(device.Serial);
                    var newPackage = afterPackages.Except(beforePackages).FirstOrDefault();
                    if (newPackage != null) installedPackage = newPackage;

                    if (!string.IsNullOrWhiteSpace(installedPackage))
                    {
                        version = await _adb.GetPackageVersionAsync(device.Serial, installedPackage);

                        RunOnUi(() => AppendLog($"[{device.Serial}] Đang mở lại app ({installedPackage})..."));
                        // monkey trả exit code không nhất quán giữa các bản ROM (nhiều máy vẫn mở app thành
                        // công dù exit code khác 0, kèm log verbose dài) — chỉ coi là đáng báo khi output
                        // thật sự nói không tìm thấy activity, tránh làm nhiễu log với dump argument của monkey.
                        var launch = await _adb.LaunchAppAsync(device.Serial, installedPackage);
                        var launchText = launch.StdOut + launch.StdErr;
                        if (!launch.Success && launchText.Contains("No activities found", StringComparison.OrdinalIgnoreCase))
                            RunOnUi(() => AppendLog($"[{device.Serial}] Không tìm thấy activity để mở app tự động."));
                    }
                }

                var now = DateTime.Now;
                device.LastInstallStatus = status;
                device.LastInstallTime = now;
                if (version != null) device.InstalledVersion = version;
                _repo.UpdateInstallResult(device.Serial, status, now, version);

                RunOnUi(() => AppendLog($"[{device.Serial}] {status}"));
            }
            catch (Exception ex)
            {
                var now = DateTime.Now;
                var status = $"Lỗi: {ex.Message}";
                device.LastInstallStatus = status;
                device.LastInstallTime = now;
                _repo.UpdateInstallResult(device.Serial, status, now, null);
                RunOnUi(() => AppendLog($"[{device.Serial}] {status}"));
            }
            finally
            {
                semaphore.Release();
                var current = Interlocked.Increment(ref done);
                RunOnUi(() =>
                {
                    _progress.Value = Math.Min(current, _progress.Maximum);
                    _lblStatus.Text = $"Đang cài đặt: {current}/{targets.Count}";
                    RenderGrid();
                });
            }
        });

        await Task.WhenAll(tasks);

        SetBusy(false);
        _lblStatus.Text = $"Hoàn tất: {targets.Count} thiết bị.";
        AppendLog($"Hoàn tất cài đặt cho {targets.Count} thiết bị.");
        RenderGrid();
    }

    // ------------------------------------------------------------------
    // Manual add / remove
    // ------------------------------------------------------------------

    private async Task OnConnectAsync()
    {
        var input = _txtConnect.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            MessageBox.Show(this, "Nhập địa chỉ IP:port của thiết bị (VD: 192.168.1.50:5555).", "Thiếu thông tin",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ipPort = input.Contains(':') ? input : $"{input}:5555";
        AppendLog($"Đang kết nối tới {ipPort}...");
        var result = await _adb.ConnectAsync(ipPort);
        AppendLog(result.Success ? $"{ipPort}: {result.StdOut.Trim()}" : $"{ipPort}: lỗi - {result.StdErr.Trim()}");

        await PollDevicesAsync(force: true);
    }

    /// <summary>
    /// Quét cả 1 dải IP bằng "adb connect" cho từng địa chỉ — dùng khi không biết trước
    /// IP cụ thể của thiết bị (thay cho việc thêm nhanh từng thiết bị một qua ô IP:port).
    /// </summary>
    private async Task OnNetworkScanAsync()
    {
        using var dialog = new NetworkScanForm(_adb);
        var result = dialog.ShowDialog(this);
        if (result != DialogResult.OK || dialog.SelectedIpPorts.Count == 0) return;

        AppendLog($"Đã thêm {dialog.SelectedIpPorts.Count} thiết bị từ quét dải mạng: {string.Join(", ", dialog.SelectedIpPorts)}");
        await PollDevicesAsync(force: true);
    }

    private void OnRemoveSelected()
    {
        if (_selectedSerials.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn thiết bị nào để xóa.", "Chưa chọn thiết bị",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(this, $"Xóa {_selectedSerials.Count} thiết bị đã chọn khỏi danh sách quản lý?" +
            "\n(Thiết bị Online nếu còn kết nối adb sẽ tự xuất hiện lại ở lần quét kế tiếp.)",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        var removed = _selectedSerials.ToList();
        foreach (var serial in removed)
        {
            _devices.Remove(serial);
            _selectedSerials.Remove(serial);
            _repo.Remove(serial);
        }
        RenderGrid();
        AppendLog($"Đã xóa {removed.Count} thiết bị khỏi danh sách quản lý.");
    }

    private void OnBrowseApk()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Android Package (*.apk)|*.apk",
            Title = "Chọn file APK cần cài đặt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _apkPath = dialog.FileName;
        _txtApkPath.Text = _apkPath;
        _repo.SetSetting("ApkPath", _apkPath);
        AppendLog($"Đã chọn APK: {_apkPath}");

        // Tự động đọc package name từ AndroidManifest.xml bên trong APK — đỡ phải gõ tay,
        // và luôn khớp đúng với file APK đang chọn (không lệ thuộc lần trước nhập package gì).
        var detectedPackage = ApkManifestReader.TryGetPackageName(_apkPath);
        if (!string.IsNullOrWhiteSpace(detectedPackage))
        {
            _txtPackage.Text = detectedPackage;
            _repo.SetSetting("PackageName", detectedPackage);
            AppendLog($"Tự phát hiện package: {detectedPackage}");
        }
        else
        {
            AppendLog("Không tự phát hiện được package từ APK này — vui lòng nhập tay ô 'Gói cần theo dõi'.");
        }
    }

    // ------------------------------------------------------------------
    // Rendering / helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// 1 nút toggle thay cho 2 nút riêng: nếu đang có dòng nào chưa chọn (trong các dòng đang hiển thị
    /// sau filter) → chọn hết; nếu tất cả đã chọn rồi → bỏ chọn hết.
    /// </summary>
    private void OnToggleSelectAll()
    {
        var visibleSerials = _grid.Rows.Cast<DataGridViewRow>()
            .Select(row => row.Cells[ColSerial].Value?.ToString())
            .Where(serial => serial != null)
            .Cast<string>()
            .ToList();

        var allSelected = visibleSerials.Count > 0 && visibleSerials.All(_selectedSerials.Contains);

        if (allSelected)
        {
            foreach (var serial in visibleSerials) _selectedSerials.Remove(serial);
        }
        else
        {
            foreach (var serial in visibleSerials) _selectedSerials.Add(serial);
        }

        RenderGrid();
    }

    private void RenderGrid()
    {
        _suppressGridEvents = true;
        try
        {
            _grid.Rows.Clear();

            var ipFilter = _txtFilterIp.Text.Trim();
            var versionFilter = _txtFilterVersion.Text.Trim();

            var filtered = _devices.Values.Where(d =>
                (string.IsNullOrEmpty(ipFilter) || d.Serial.Contains(ipFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(versionFilter) || (d.InstalledVersion ?? string.Empty).Contains(versionFilter, StringComparison.OrdinalIgnoreCase)));

            foreach (var device in filtered.OrderByDescending(d => d.Status == "Online").ThenBy(d => d.Serial))
            {
                var rowIndex = _grid.Rows.Add(
                    _selectedSerials.Contains(device.Serial),
                    device.Serial,
                    device.Model,
                    device.Status,
                    device.ConnectionType,
                    device.InstalledVersion ?? "-",
                    device.LastInstallTime?.ToString("dd/MM HH:mm:ss") ?? "-",
                    device.LastSeen.ToString("dd/MM HH:mm:ss"),
                    device.LastInstallStatus ?? "-");

                if (device.Status != "Online")
                    _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }
        finally
        {
            _suppressGridEvents = false;
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        _btnInstallSelected.Enabled = !busy;
        _btnInstallAll.Enabled = !busy;
        _btnBrowseApk.Enabled = !busy;
        _btnConnect.Enabled = !busy;
        _btnNetworkScan.Enabled = !busy;
        _btnRefresh.Enabled = !busy;
        _btnRemoveSelected.Enabled = !busy;
    }

    private void AppendLog(string message)
    {
        _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void RunOnUi(Action action)
    {
        if (!IsHandleCreated || IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private T SafeInvoke<T>(Func<T> func)
    {
        if (InvokeRequired)
            return (T)Invoke(func);
        return func();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();
        base.OnFormClosed(e);
    }
}
