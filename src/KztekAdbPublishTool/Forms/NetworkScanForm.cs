using System.Collections.Concurrent;
using System.Net;
using KztekAdbPublishTool.Services;
using KztekComponent.Controls;

namespace KztekAdbPublishTool.Forms;

/// <summary>
/// Quét 1 dải IP (cùng /24) bằng "adb connect ip:port" cho từng địa chỉ để tìm thiết bị
/// chưa có trong danh sách quản lý — dùng khi không thể tự động phát hiện qua polling
/// (thiết bị chưa từng adb connect lần nào nên "adb devices" chưa biết tới nó).
/// </summary>
public sealed class NetworkScanForm : Form
{
    private const int ScanConcurrency = 24;
    private const int ScanTimeoutMs = 1200;

    private readonly AdbService _adb;
    private CancellationTokenSource? _scanCts;
    private bool _scanning;

    private KzTextBox _txtRange = null!;
    private KzTextBox _txtPort = null!;
    private KzButton _btnScan = null!;
    private KzButton _btnStop = null!;
    private KzButton _btnAdd = null!;
    private KzButton _btnCancel = null!;
    private KzDataGrid _resultGrid = null!;
    private KzProgressBar _progress = null!;
    private Label _lblStatus = null!;

    public List<string> SelectedIpPorts { get; } = new();

    public NetworkScanForm(AdbService adb)
    {
        _adb = adb;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Quét dải mạng LAN";
        Size = new Size(620, 560);
        MinimumSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(root);

        // Hàng 1: dải IP (chiếm toàn bộ chiều rộng vì nhãn khá dài)
        var rangeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var lblRange = new Label { Text = "Dải IP (VD: 192.168.1.1-254):", AutoSize = true, Margin = new Padding(0, 10, 6, 0) };
        _txtRange = new KzTextBox { Width = 260, Height = 32, Margin = new Padding(0, 2, 0, 0), PlaceholderText = "192.168.1.1-254" };
        rangeRow.Controls.Add(lblRange);
        rangeRow.Controls.Add(_txtRange);

        // Hàng 2: cổng + nút Quét/Dừng
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var lblPort = new Label { Text = "Cổng:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) };
        _txtPort = new KzTextBox { Width = 70, Height = 32, Margin = new Padding(0, 2, 20, 0), Text = "5555" };

        _btnScan = new KzButton { Text = "Quét", Width = 110, Height = 34, Margin = new Padding(0, 2, 8, 0), Padding = Padding.Empty };
        _btnScan.Click += async (_, _) => await OnScanAsync();

        _btnStop = new KzButton { Text = "Dừng", Width = 110, Height = 34, Margin = new Padding(0, 2, 0, 0), Padding = Padding.Empty, Enabled = false };
        _btnStop.Click += (_, _) => _scanCts?.Cancel();

        actionRow.Controls.Add(lblPort);
        actionRow.Controls.Add(_txtPort);
        actionRow.Controls.Add(_btnScan);
        actionRow.Controls.Add(_btnStop);

        _resultGrid = new KzDataGrid
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        };
        _resultGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _resultGrid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _resultGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colAdd",
            HeaderText = "Thêm",
            MinimumWidth = 60,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
        });
        _resultGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAddress", HeaderText = "Địa chỉ thiết bị phản hồi", ReadOnly = true, MinimumWidth = 220 });
        _resultGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_resultGrid.IsCurrentCellDirty && _resultGrid.CurrentCell is DataGridViewCheckBoxCell)
                _resultGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        // Bấm vào bất kỳ ô nào trên dòng cũng tick/bỏ tick — không cần bấm chính xác vào checkbox.
        _resultGrid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex == 0) return;
            var cell = _resultGrid.Rows[e.RowIndex].Cells[0];
            cell.Value = !(cell.Value is bool b && b);
            _resultGrid.EndEdit();
        };

        var statusRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        _lblStatus = new Label { Dock = DockStyle.Fill, Text = "Nhập dải IP rồi bấm Quét.", TextAlign = ContentAlignment.MiddleLeft };
        _progress = new KzProgressBar { Dock = DockStyle.Fill, Height = 20, Margin = new Padding(8, 5, 0, 5) };
        statusRow.Controls.Add(_lblStatus, 0, 0);
        statusRow.Controls.Add(_progress, 1, 0);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        _btnCancel = new KzButton { Text = "Đóng", Width = 100, Height = 36, Margin = new Padding(8, 4, 0, 0), Padding = Padding.Empty };
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnAdd = new KzButton { Text = "Thêm vào danh sách", Width = 180, Height = 36, Margin = new Padding(8, 4, 0, 0), Padding = Padding.Empty };
        _btnAdd.Click += (_, _) => OnAddSelected();

        buttonRow.Controls.Add(_btnCancel);
        buttonRow.Controls.Add(_btnAdd);

        root.Controls.Add(rangeRow, 0, 0);
        root.Controls.Add(actionRow, 0, 1);
        // row 2 = spacer 10px, không gán control
        root.Controls.Add(_resultGrid, 0, 3);
        root.Controls.Add(statusRow, 0, 4);
        root.Controls.Add(buttonRow, 0, 5);
    }

    private async Task OnScanAsync()
    {
        if (_scanning) return;

        var rangeText = _txtRange.Text.Trim();
        var parts = rangeText.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var startIp))
        {
            MessageBox.Show(this, "Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254",
                "Dải IP không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var startBytes = startIp.GetAddressBytes();
        byte[] endBytes;

        // Hỗ trợ dạng rút gọn "192.168.1.1-254" — vế sau chỉ là octet cuối, dùng chung 3 octet đầu của start.
        // LƯU Ý: IPAddress.TryParse("254") KHÔNG báo lỗi (.NET hiểu theo cú pháp IPv4 rút gọn kiểu cũ:
        // 1 số đơn = giá trị 32-bit) nên phải tự kiểm tra có dấu '.' trước để phân biệt "IP đầy đủ" và
        // "chỉ octet cuối" — nếu dựa vào TryParse trước sẽ luôn khớp nhánh IP đầy đủ và tính sai subnet.
        if (parts[1].Contains('.'))
        {
            if (!IPAddress.TryParse(parts[1], out var endIpFull))
            {
                MessageBox.Show(this, "Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254",
                    "Dải IP không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            endBytes = endIpFull.GetAddressBytes();
        }
        else if (byte.TryParse(parts[1], out var lastOctet))
        {
            endBytes = (byte[])startBytes.Clone();
            endBytes[3] = lastOctet;
        }
        else
        {
            MessageBox.Show(this, "Nhập dải IP đúng định dạng, VD: 192.168.1.1-192.168.1.254 hoặc 192.168.1.1-254",
                "Dải IP không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (startBytes.Length != 4 || endBytes.Length != 4 ||
            startBytes[0] != endBytes[0] || startBytes[1] != endBytes[1] || startBytes[2] != endBytes[2])
        {
            MessageBox.Show(this, "Chỉ hỗ trợ quét trong cùng dải /24 (3 octet đầu phải giống nhau).", "Dải IP không hợp lệ",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!int.TryParse(_txtPort.Text.Trim(), out var port) || port <= 0 || port > 65535)
        {
            MessageBox.Show(this, "Cổng không hợp lệ.", "Cổng không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var prefix = $"{startBytes[0]}.{startBytes[1]}.{startBytes[2]}.";
        var from = Math.Min(startBytes[3], endBytes[3]);
        var to = Math.Max(startBytes[3], endBytes[3]);
        var candidates = Enumerable.Range(from, to - from + 1).Select(i => $"{prefix}{i}:{port}").ToList();

        if (candidates.Count > 512)
        {
            MessageBox.Show(this, "Dải IP quá lớn (tối đa 512 địa chỉ mỗi lần quét).", "Dải quá lớn",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _resultGrid.Rows.Clear();
        _progress.Minimum = 0;
        _progress.Maximum = candidates.Count;
        _progress.Value = 0;

        SetScanning(true);
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;
        var found = new ConcurrentBag<string>();
        var done = 0;

        try
        {
            using var semaphore = new SemaphoreSlim(ScanConcurrency);
            var tasks = candidates.Select(async ipPort =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var result = await _adb.ConnectAsync(ipPort, ScanTimeoutMs, ct);
                    var text = result.StdOut + result.StdErr;
                    var success = result.Success &&
                        (text.Contains("connected to", StringComparison.OrdinalIgnoreCase) ||
                         text.Contains("already connected", StringComparison.OrdinalIgnoreCase));
                    if (success) found.Add(ipPort);
                }
                catch (OperationCanceledException)
                {
                    // user bấm Dừng — bỏ qua phần còn lại
                }
                catch
                {
                    // không kết nối được (refused/timeout) — coi như địa chỉ đó không có thiết bị adb
                }
                finally
                {
                    semaphore.Release();
                    var current = Interlocked.Increment(ref done);
                    if (IsHandleCreated)
                    {
                        BeginInvoke(() =>
                        {
                            _progress.Value = Math.Min(current, _progress.Maximum);
                            _lblStatus.Text = $"Đang quét {current}/{candidates.Count} — tìm thấy {found.Count}";
                        });
                    }
                }
            });

            await Task.WhenAll(tasks);
        }
        finally
        {
            SetScanning(false);
            _scanCts?.Dispose();
            _scanCts = null;
        }

        foreach (var ipPort in found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            _resultGrid.Rows.Add(true, ipPort);

        _lblStatus.Text = $"Hoàn tất: {found.Count}/{candidates.Count} địa chỉ phản hồi adb.";
    }

    private void OnAddSelected()
    {
        SelectedIpPorts.Clear();
        foreach (DataGridViewRow row in _resultGrid.Rows)
        {
            var isChecked = row.Cells[0].Value is bool b && b;
            var ipPort = row.Cells[1].Value?.ToString();
            if (isChecked && ipPort != null) SelectedIpPorts.Add(ipPort);
        }

        if (SelectedIpPorts.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn thiết bị nào để thêm.", "Chưa chọn thiết bị",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetScanning(bool scanning)
    {
        _scanning = scanning;
        _btnScan.Enabled = !scanning;
        _btnStop.Enabled = scanning;
        _txtRange.Enabled = !scanning;
        _txtPort.Enabled = !scanning;
    }
}
