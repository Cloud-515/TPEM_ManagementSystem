using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.Thermostat.Application;
using MeterAcquisition.Thermostat.Domain;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatMonitorControl : UserControl
    {
        private ThermostatWorkspaceService _workspace;
        private ThermostatWorkspaceConfigStore _configStore;
        private DataGridView _grid;
        private ComboBox _portComboBox;
        private NumericUpDown _startAddress;
        private NumericUpDown _endAddress;
        private ComboBox _modeComboBox;
        private ComboBox _fanSpeedComboBox;
        private Label _statusLabel;
        private readonly System.Windows.Forms.Timer _refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        private bool _refreshing;

        public ThermostatMonitorControl()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
            {
                return;
            }

            _workspace = new ThermostatWorkspaceService();
            _configStore = new ThermostatWorkspaceConfigStore();
            _portComboBox.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames().OrderBy(port => port).Cast<object>().ToArray());
            BindSelections();

            var connectButton = _connectionBar.ConnectButton;
            var disconnectButton = _connectionBar.DisconnectButton;
            var scanButton = _connectionBar.ScanButton;
            var refreshButton = _connectionBar.RefreshButton;
            var powerButton = _controlBar.PowerButton;
            var applyModeButton = _controlBar.ApplyModeButton;
            var applyFanSpeedButton = _controlBar.ApplyFanSpeedButton;
            var temperatureButton = _controlBar.TemperatureButton;
            var fanDiagnosticButton = _controlBar.FanDiagnosticButton;

            connectButton.Click += async (sender, args) => await ConnectAsync();
            disconnectButton.Click += async (sender, args) => await DisconnectAsync();
            scanButton.Click += async (sender, args) => await ScanAsync();
            refreshButton.Click += async (sender, args) => await RefreshAsync();
            powerButton.Click += async (sender, args) => await ChangePowerAsync();
            applyModeButton.Click += async (sender, args) => await ApplyModeAsync();
            applyFanSpeedButton.Click += async (sender, args) => await ApplyFanSpeedAsync();
            temperatureButton.Click += async (sender, args) => await ChangeTemperatureAsync();
            fanDiagnosticButton.Click += async (sender, args) => await ShowFanDiagnosticAsync();
            _refreshTimer.Tick += async (sender, args) => await RefreshAsync();
            _grid.CellDoubleClick += GridOnCellDoubleClick;
            Load += async (sender, args) => await LoadWorkspaceAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer.Dispose();
                _workspace?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void BindSelections()
        {
            _modeComboBox.DataSource = new[]
            {
                new Choice<ThermostatMode>("地暖制热", ThermostatMode.FloorHeating), new Choice<ThermostatMode>("风盘制热", ThermostatMode.FanCoilHeating),
                new Choice<ThermostatMode>("联合制热", ThermostatMode.CombinedHeating), new Choice<ThermostatMode>("制冷", ThermostatMode.Cooling),
                new Choice<ThermostatMode>("地板制冷", ThermostatMode.FloorCooling), new Choice<ThermostatMode>("联合制冷", ThermostatMode.CombinedCooling),
                new Choice<ThermostatMode>("通风", ThermostatMode.Ventilation)
            };
            _modeComboBox.DisplayMember = "Text"; _modeComboBox.ValueMember = "Value";
            _fanSpeedComboBox.DataSource = new[] { new Choice<ThermostatFanSpeedSetting>("低", ThermostatFanSpeedSetting.Low), new Choice<ThermostatFanSpeedSetting>("中", ThermostatFanSpeedSetting.Medium), new Choice<ThermostatFanSpeedSetting>("高", ThermostatFanSpeedSetting.High), new Choice<ThermostatFanSpeedSetting>("自动", ThermostatFanSpeedSetting.Auto) };
            _fanSpeedComboBox.DisplayMember = "Text"; _fanSpeedComboBox.ValueMember = "Value";
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 36, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, RowTemplate = { Height = 30 } };
            AddColumn(grid, "地址", 56, 55);
            AddColumn(grid, "名称", 110, 130);
            AddColumn(grid, "分组", 90, 110);
            AddColumn(grid, "状态", 60, 70);
            AddColumn(grid, "室温 (°C)", 78, 80);
            AddColumn(grid, "设温 (°C)", 78, 80);
            AddColumn(grid, "开关", 56, 55);
            AddColumn(grid, "模式", 96, 120);
            AddColumn(grid, "当前风速", 78, 80);
            AddColumn(grid, "风速设定", 78, 80);
            AddColumn(grid, "阀1状态", 66, 70);
            AddColumn(grid, "更新时间", 88, 100);
            return grid;
        }

        private static void AddColumn(DataGridView grid, string header, int minimumWidth, float fillWeight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = header, HeaderText = header, MinimumWidth = minimumWidth, FillWeight = fillWeight });
        }

        private async Task LoadWorkspaceAsync() { var configuration = _configStore.Load(out var warning); _workspace.ApplyConfiguration(configuration); Render(); _statusLabel.Text = string.IsNullOrWhiteSpace(warning) ? "未连接" : warning; await Task.CompletedTask; }
        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(_portComboBox.Text)) { MessageBox.Show(this, "请选择串口。", "温控器"); return; }
            _statusLabel.Text = "连接中...";
            var connected = await _workspace.ConnectAsync(new CommSettings { PortName = _portComboBox.Text.Trim(), BaudRate = 9600, DataBits = 8, Parity = "None", StopBits = 1, ReadTimeoutMs = 1000, WriteTimeoutMs = 1000 });
            _statusLabel.Text = connected ? "已连接 (9600/8N1)" : "连接失败";
            if (connected) { _refreshTimer.Start(); await RefreshAsync(); }
        }
        private async Task DisconnectAsync() { _refreshTimer.Stop(); await _workspace.DisconnectAsync(); _statusLabel.Text = "未连接"; Render(); }
        private async Task ScanAsync() { if (!EnsureConnected()) return; _statusLabel.Text = "扫描中..."; var found = await _workspace.ScanAsync((byte)_startAddress.Value, (byte)_endAddress.Value); _configStore.Save(_workspace.ExportConfiguration()); _statusLabel.Text = string.Format("扫描完成，发现 {0} 台温控器", found.Count); await RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (_refreshing || !_workspace.IsConnected) return;
            _refreshing = true;
            try { _statusLabel.Text = "刷新中..."; var results = await _workspace.RefreshTelemetryAsync(); var failures = results.Count(result => !result.IsSuccess); _statusLabel.Text = failures == 0 ? "已刷新" : string.Format("已刷新，{0} 台离线", failures); Render(); }
            catch (Exception ex) { _statusLabel.Text = "刷新失败: " + ex.Message; }
            finally { _refreshing = false; }
        }
        private async Task ChangePowerAsync() { var device = GetSelectedDevice(); if (device == null) return; var target = device.Telemetry != null && device.Telemetry.PowerState == ThermostatPowerState.On ? ThermostatPowerState.Off : ThermostatPowerState.On; await RunControlAsync(() => _workspace.SetPowerAsync(device.Device.SlaveId, target)); }
        private async Task ApplyModeAsync() { var device = GetSelectedDevice(); if (device == null) return; await RunControlAsync(() => _workspace.SetModeAsync(device.Device.SlaveId, ((Choice<ThermostatMode>)_modeComboBox.SelectedItem).Value)); }
        private async Task ApplyFanSpeedAsync() { var device = GetSelectedDevice(); if (device == null) return; await RunControlAsync(() => _workspace.SetFanSpeedAsync(device.Device.SlaveId, ((Choice<ThermostatFanSpeedSetting>)_fanSpeedComboBox.SelectedItem).Value)); }
        private async Task ShowFanDiagnosticAsync()
        {
            var device = GetSelectedDevice();
            if (device == null)
            {
                return;
            }

            try
            {
                _statusLabel.Text = "读取风机诊断中...";
                var diagnostic = await _workspace.DiagnoseFanControlAsync(device.Device.SlaveId);
                using (var dialog = new ThermostatFanDiagnosticForm(diagnostic))
                {
                    dialog.ShowDialog(this);
                }
                _statusLabel.Text = diagnostic.Summary;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "风机诊断失败: " + ex.Message;
            }
        }

        private async Task ChangeTemperatureAsync()
        {
            var device = GetSelectedDevice(); if (device == null) return;
            using (var dialog = new Form { Text = "设置温度", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(260, 92), MinimumSize = new Size(276, 130), MaximizeBox = false, MinimizeBox = false, AutoScaleMode = AutoScaleMode.Font, Font = UiStyle.BodyFont, Padding = new Padding(12) })
            {
                var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
                var value = new NumericUpDown { Width = 132, Minimum = 5, Maximum = 35, DecimalPlaces = 1, Increment = 0.1m, Value = device.Telemetry == null ? 26m : device.Telemetry.SetTemperatureCelsius, Font = UiStyle.BodyFont };
                var ok = UiStyle.CreateButton("确定");
                ok.DialogResult = DialogResult.OK;
                layout.Controls.Add(UiStyle.Cell(value)); layout.Controls.Add(ok); dialog.Controls.Add(layout); dialog.AcceptButton = ok;
                if (dialog.ShowDialog(this) == DialogResult.OK) await RunControlAsync(() => _workspace.SetTemperatureAsync(device.Device.SlaveId, value.Value));
            }
        }
        private async Task RunControlAsync(Func<Task<ThermostatTelemetry>> action) { try { _statusLabel.Text = "写入并回读中..."; await action(); _statusLabel.Text = "控制成功"; Render(); } catch (Exception ex) { _statusLabel.Text = "控制失败: " + ex.Message; } }
        private bool EnsureConnected() { if (_workspace.IsConnected) return true; MessageBox.Show(this, "请先连接温控器串口。", "温控器"); return false; }
        private ThermostatDeviceSnapshot GetSelectedDevice() { if (!EnsureConnected() || _grid.CurrentRow == null || !(_grid.CurrentRow.Tag is ThermostatDeviceSnapshot device)) { if (_workspace.IsConnected) MessageBox.Show(this, "请选择温控器。", "温控器"); return null; } return device; }
        private void Render()
        {
            byte? selectedAddress = _grid.CurrentRow != null && _grid.CurrentRow.Tag is ThermostatDeviceSnapshot selected ? selected.Device.SlaveId : (byte?)null;
            _grid.Rows.Clear();
            foreach (var device in _workspace.CreateSnapshot().Devices)
            {
                var t = device.Telemetry;
                var row = _grid.Rows[_grid.Rows.Add(device.Device.SlaveId, device.Device.Name, device.Device.GroupName, t == null ? "离线" : "在线", t == null ? "" : t.RoomTemperatureCelsius.ToString("0.0"), t == null ? "" : t.SetTemperatureCelsius.ToString("0.0"), t == null ? "" : FormatPower(t.PowerState), t == null ? "" : FormatMode(t.Mode), t == null ? "" : FormatCurrentFanSpeed(t.CurrentFanSpeed), t == null ? "" : FormatFanSpeedSetting(t.FanSpeedSetting), t == null ? "" : FormatValve1State(t.WaterValveOpen), t == null ? "" : t.CollectedAt.ToString("HH:mm:ss"))];
                row.Tag = device; if (selectedAddress == device.Device.SlaveId) row.Selected = true;
            }
        }
        private void GridOnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || !(_grid.Rows[e.RowIndex].Tag is ThermostatDeviceSnapshot device))
            {
                return;
            }

            using (var details = new ThermostatDetailsForm(device))
            {
                details.ShowDialog(this);
            }
        }

        internal static string FormatPower(ThermostatPowerState value) => value == ThermostatPowerState.On ? "开" : value == ThermostatPowerState.Off ? "关" : string.Empty;
        internal static string FormatMode(ThermostatMode value) { switch (value) { case ThermostatMode.FloorHeating: return "地暖制热"; case ThermostatMode.FanCoilHeating: return "风盘制热"; case ThermostatMode.CombinedHeating: return "联合制热"; case ThermostatMode.Cooling: return "制冷"; case ThermostatMode.FloorCooling: return "地板制冷"; case ThermostatMode.CombinedCooling: return "联合制冷"; case ThermostatMode.Ventilation: return "通风"; default: return string.Empty; } }
        internal static string FormatCurrentFanSpeed(ThermostatCurrentFanSpeed value) => value == ThermostatCurrentFanSpeed.Low ? "低" : value == ThermostatCurrentFanSpeed.Medium ? "中" : value == ThermostatCurrentFanSpeed.High ? "高" : string.Empty;
        internal static string FormatFanSpeedSetting(ThermostatFanSpeedSetting value) => value == ThermostatFanSpeedSetting.Low ? "低" : value == ThermostatFanSpeedSetting.Medium ? "中" : value == ThermostatFanSpeedSetting.High ? "高" : value == ThermostatFanSpeedSetting.Auto ? "自动" : string.Empty;
        internal static string FormatValve1State(bool? value) => !value.HasValue ? string.Empty : value.Value ? "开" : "关";
        private sealed class Choice<T> { public Choice(string text, T value) { Text = text; Value = value; } public string Text { get; } public T Value { get; } }
    }
}
