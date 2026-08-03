using System.IO.Ports;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.HeatPump.Forms;

public partial class MainForm : Form
{
    private IHeatPumpDataService _dataService;
    private readonly HeatPumpDashboardManager _dashboardManager;
    private bool _isConnected;
    private bool _isRefreshing;
    private bool _isUpdatingSelectionUi;
    private const string SingleControlScope = "独立控制";
    private const string MultiControlScope = "多选控制";

    public MainForm()
    {
        _dataService = new MockHeatPumpDataService();
        _dashboardManager = new HeatPumpDashboardManager(_dataService);
        InitializeComponent();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        PopulatePortList();
        PopulateCommOptions();
        PopulateControlOptions();
        cmbPort.SelectedIndexChanged += CmbPort_SelectedIndexChanged;
        UpdatePortMode();
        RebuildDashboardPanels();
        UpdateSelectionStatusText();
        UpdateConnectionState(false, "未连接");
    }

    private void PopulateCommOptions()
    {
        cmbBaudRate.Items.Clear();
        cmbBaudRate.Items.AddRange(["4800", "9600", "19200", "38400"]);
        cmbBaudRate.SelectedItem = "9600";

        cmbParity.Items.Clear();
        cmbParity.Items.AddRange(["None", "Odd", "Even"]);
        cmbParity.SelectedItem = "None";

        cmbStopBits.Items.Clear();
        cmbStopBits.Items.AddRange(["1", "2"]);
        cmbStopBits.SelectedItem = "1";
    }

    private void PopulateControlOptions()
    {
        cmbControlScope.Items.Clear();
        cmbControlScope.Items.AddRange([SingleControlScope, MultiControlScope]);
        cmbControlScope.SelectedItem = SingleControlScope;
        cmbControlScope.SelectedIndexChanged += (_, _) =>
        {
            if (cmbControlScope.SelectedItem?.ToString() == SingleControlScope)
            {
                var firstSelected = _dashboardManager.AllUnits.FirstOrDefault(unit => unit.IsSelected);
                var keepSlaveId = firstSelected?.SlaveId;
                foreach (var unit in _dashboardManager.AllUnits)
                {
                    unit.IsSelected = keepSlaveId is not null && unit.SlaveId == keepSlaveId;
                }
            }

            UpdateSelectionStatusText();
            UpdateAllCardLabels();
        };

        cmbControlMode.Items.Clear();
        cmbControlMode.Items.AddRange(["关机", "制冷", "制热", "水泵"]);
        cmbControlMode.SelectedIndexChanged += (_, _) => UpdateControlInputsByMode();
        cmbControlMode.SelectedItem = "制热";
        nudControlTemp.Value = 45;
        UpdateControlInputsByMode();
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        var selectedPort = cmbPort.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(selectedPort))
        {
            return;
        }

        var nextService = CreateDataService(selectedPort);
        var settings = BuildCommSettings(selectedPort);
        var connected = await nextService.ConnectAsync(settings);
        if (!connected)
        {
            nextService.Dispose();
            UpdateConnectionState(false, $"连接失败: {selectedPort}");
            return;
        }

        await _dataService.DisconnectAsync();
        _dataService.Dispose();
        _dataService = nextService;

        var loadDemoUnits = IsMockMode(selectedPort);
        _dashboardManager.SetDataService(_dataService, loadDemoUnits);
        if (!loadDemoUnits)
        {
            _dashboardManager.MarkAllOffline();
        }

        RebuildDashboardPanels();
        UpdateConnectionState(true, BuildConnectionStatusText(selectedPort));
        dashboardRefreshTimer.Start();
    }

    private async void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        dashboardRefreshTimer.Stop();
        await _dataService.DisconnectAsync();
        _dashboardManager.MarkAllOffline();
        UpdateAllCardLabels();
        UpdateConnectionState(false, "未连接");
    }

    private async void BtnAutoScan_Click(object? sender, EventArgs e)
    {
        btnAutoScan.Enabled = false;
        try
        {
            var startAddress = (byte)nudAddressStart.Value;
            var endAddress = (byte)nudAddressEnd.Value;
            var discovered = await _dashboardManager.AutoScanAsync(startAddress, endAddress);
            RebuildDashboardPanels();
            SetControlMessage(
                discovered.Count > 0 ? $"扫描完成，发现 {discovered.Count} 个模块" : "扫描完成，未发现模块",
                Color.FromArgb(0, 140, 72),
                Color.FromArgb(236, 251, 238));
        }
        catch (Exception ex)
        {
            SetControlMessage($"扫描失败: {ex.Message}", Color.Red, Color.FromArgb(255, 240, 240));
        }
        finally
        {
            btnAutoScan.Enabled = true;
        }
    }

    private void BtnAddUnit_Click(object? sender, EventArgs e)
    {
        btnAddUnit.Enabled = false;
        try
        {
            var initialSlaveId = (byte)nudSlaveAddress.Value;
            var existing = _dashboardManager.GetController(initialSlaveId);
            using var dialog = new ControllerEditorDialog(initialSlaveId, existing?.Name);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (_dashboardManager.HasDuplicateGroupName(dialog.SlaveId, dialog.GroupName))
            {
                var confirm = MessageBox.Show(
                    this,
                    $"已有其他线控器使用相同组别名 '{dialog.GroupName}'。\n继续保存可能会造成界面识别混淆。\n\n是否继续保存?",
                    "同名提醒",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.OK)
                {
                    return;
                }
            }

            var controller = _dashboardManager.UpsertController(dialog.SlaveId, dialog.GroupName);
            RebuildDashboardPanels();
            SetControlMessage($"线控器 {controller.SlaveId}# 已保存，组别名称: {controller.Name}", Color.FromArgb(0, 140, 72), Color.FromArgb(236, 251, 238));
        }
        catch (Exception ex)
        {
            SetControlMessage($"保存线控器失败: {ex.Message}", Color.Red, Color.FromArgb(255, 240, 240));
        }
        finally
        {
            btnAddUnit.Enabled = true;
        }
    }

    private async void DashboardRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_isRefreshing || !_isConnected)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            await _dashboardManager.RefreshTelemetryAsync();
            UpdateAllCardLabels();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async void BtnApplyControl_Click(object? sender, EventArgs e)
    {
        if (!_isConnected)
        {
            SetControlMessage("未连接，无法写入控制", Color.Red, Color.FromArgb(255, 240, 240));
            return;
        }

        var selectedUnits = GetSelectedUnits();
        if (selectedUnits.Count == 0)
        {
            SetControlMessage("请先勾选要控制的线控器", Color.Red, Color.FromArgb(255, 240, 240));
            return;
        }

        btnApplyControl.Enabled = false;
        try
        {
            var targetSlaveIds = selectedUnits.Select(unit => unit.SlaveId).Distinct().ToList();
            var affectedModules = BuildAffectedModulesText(selectedUnits);
            var mode = GetSelectedControlMode();
            var controllerConfigs = _dashboardManager.Controllers
                .Where(controller => targetSlaveIds.Contains(controller.SlaveId))
                .ToDictionary(controller => controller.SlaveId);

            var configText = string.Join("；", controllerConfigs.Values
                .OrderBy(controller => controller.SlaveId)
                .Select(controller => $"{controller.Name}: 回差={controller.Differential}°C, 锁定={(controller.ControllerLockEnabled ? "开启" : "关闭")}"));

            if (!ConfirmControlAction(
                    "写入控制",
                    targetSlaveIds,
                    $"模式={FormatMode(mode)}，设温={nudControlTemp.Value:F0}°C；{configText}"))
            {
                return;
            }

            foreach (var slaveId in targetSlaveIds)
            {
                await _dataService.SetRunModeAsync(slaveId, mode);
                await _dataService.SetTargetTemperatureAsync(slaveId, nudControlTemp.Value);
                await _dataService.SetDifferentialAsync(slaveId, controllerConfigs[slaveId].Differential);
                await _dataService.SetControllerLockAsync(slaveId, controllerConfigs[slaveId].ControllerLockEnabled);
            }

            await _dashboardManager.RefreshTelemetryAsync();
            UpdateAllCardLabels();

            var scopeText = cmbControlScope.SelectedItem?.ToString() == MultiControlScope ? "多选" : "独立";
            SetControlMessage(
                IsMockMode(cmbPort.SelectedItem?.ToString())
                    ? $"{scopeText}控制已下发，目标 {selectedUnits.Count} 个模块 / {targetSlaveIds.Count} 台线控器，当前选中模块: {affectedModules}"
                    : $"已向 {targetSlaveIds.Count} 台线控器串行下发控制，当前选中模块: {affectedModules}",
                Color.FromArgb(0, 140, 72),
                Color.FromArgb(236, 251, 238));
        }
        catch (Exception ex)
        {
            SetControlMessage($"写入失败: {ex.Message}", Color.Red, Color.FromArgb(255, 240, 240));
        }
        finally
        {
            btnApplyControl.Enabled = true;
        }
    }

    private async void BtnClearFault_Click(object? sender, EventArgs e)
    {
        if (!_isConnected)
        {
            SetControlMessage("未连接，无法清故障", Color.Red, Color.FromArgb(255, 240, 240));
            return;
        }

        var selectedUnits = GetSelectedUnits();
        if (selectedUnits.Count == 0)
        {
            SetControlMessage("请先勾选要清故障的线控器", Color.Red, Color.FromArgb(255, 240, 240));
            return;
        }

        btnClearFault.Enabled = false;
        try
        {
            var targetSlaveIds = selectedUnits.Select(unit => unit.SlaveId).Distinct().ToList();
            var affectedModules = BuildAffectedModulesText(selectedUnits);
            if (!ConfirmControlAction(
                    "清除故障",
                    targetSlaveIds,
                    "将向以下线控器发送清故障指令"))
            {
                return;
            }

            foreach (var slaveId in targetSlaveIds)
            {
                await _dataService.ClearFaultAsync(slaveId);
            }

            await _dashboardManager.RefreshTelemetryAsync();
            UpdateAllCardLabels();
            SetControlMessage(
                IsMockMode(cmbPort.SelectedItem?.ToString())
                    ? $"已向 {selectedUnits.Count} 个选中模块所属的 {targetSlaveIds.Count} 台线控器发送清故障指令，当前选中模块: {affectedModules}"
                    : $"已向 {targetSlaveIds.Count} 台线控器串行下发清故障指令，当前选中模块: {affectedModules}",
                Color.FromArgb(0, 140, 72),
                Color.FromArgb(236, 251, 238));
        }
        catch (Exception ex)
        {
            SetControlMessage($"清故障失败: {ex.Message}", Color.Red, Color.FromArgb(255, 240, 240));
        }
        finally
        {
            btnClearFault.Enabled = true;
        }
    }

    private void FlpDashboard_SizeChanged(object? sender, EventArgs e)
    {
        ResizeDashboardPanels();
    }

    private void CmbPort_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdatePortMode();
    }

    private void PopulatePortList()
    {
        cmbPort.Items.Clear();
        cmbPort.Items.Add("模拟模式");
        foreach (var portName in SerialPort.GetPortNames().OrderBy(static item => item, StringComparer.OrdinalIgnoreCase))
        {
            cmbPort.Items.Add(portName);
        }

        cmbPort.SelectedIndex = 0;
    }

    private void UpdatePortMode()
    {
        var isMockMode = IsMockMode(cmbPort.SelectedItem?.ToString());
        btnAddUnit.Enabled = isMockMode;
        btnAddUnit.Text = isMockMode ? "新增/修改线控器" : "查看/修改线控器";
        nudSlaveAddress.Enabled = !isMockMode;
        cmbBaudRate.Enabled = !isMockMode;
        cmbParity.Enabled = !isMockMode;
        cmbStopBits.Enabled = !isMockMode;
        cmbControlMode.Enabled = true;
        btnApplyControl.Enabled = true;
        btnClearFault.Enabled = true;
        UpdateControlInputsByMode();
    }

    private bool ConfirmControlAction(string actionName, IReadOnlyCollection<byte> slaveIds, string content)
    {
        var targetModules = GetSelectedUnits()
            .Where(unit => slaveIds.Contains(unit.SlaveId))
            .OrderBy(unit => unit.SlaveId)
            .ThenBy(unit => unit.ModuleIndex)
            .Select(unit => $"{unit.Name} / {GetControllerName(unit.SlaveId)} (SlaveId {unit.SlaveId}, 模块 {unit.ModuleIndex}#)")
            .ToList();

        var message = $"即将执行: {actionName}\n\n目标设备:\n- {string.Join("\n- ", targetModules)}\n\n操作内容:\n{content}\n\n是否继续?";
        return MessageBox.Show(this, message, "操作确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
    }

    private void UpdateControlInputsByMode()
    {
        var isPumpMode = string.Equals(cmbControlMode.SelectedItem?.ToString(), "水泵", StringComparison.Ordinal);
        nudControlTemp.Enabled = !isPumpMode;
    }

    private string BuildConnectionStatusText(string selectedPort)
    {
        if (IsMockMode(selectedPort))
        {
            return "模拟模式已连接";
        }

        return $"{selectedPort} / 地址 {nudSlaveAddress.Value} / {cmbBaudRate.SelectedItem},{cmbParity.SelectedItem},8,{cmbStopBits.SelectedItem}";
    }

    private string GetControllerName(byte slaveId)
    {
        return _dashboardManager.Controllers.FirstOrDefault(controller => controller.SlaveId == slaveId)?.Name ?? $"线控器 {slaveId}#";
    }

    private HeatPumpRunMode GetSelectedControlMode()
    {
        return cmbControlMode.SelectedItem?.ToString() switch
        {
            "关机" => HeatPumpRunMode.Off,
            "制冷" => HeatPumpRunMode.Cooling,
            "制热" => HeatPumpRunMode.Heating,
            "水泵" => HeatPumpRunMode.Pump,
            _ => HeatPumpRunMode.Heating,
        };
    }

    private static bool IsMockMode(string? selectedPort)
    {
        return string.Equals(selectedPort, "模拟模式", StringComparison.Ordinal);
    }

    private static IHeatPumpDataService CreateDataService(string selectedPort)
    {
        return IsMockMode(selectedPort)
            ? new MockHeatPumpDataService()
            : new ModbusHeatPumpDataService();
    }

    private List<HeatPumpModuleInfo> GetSelectedUnits()
    {
        var selected = _dashboardManager.AllUnits.Where(unit => unit.IsSelected).ToList();
        if (cmbControlScope.SelectedItem?.ToString() == SingleControlScope && selected.Count > 1)
        {
            return [selected[0]];
        }

        return selected;
    }

    private string BuildAffectedModulesText(IEnumerable<HeatPumpModuleInfo> units)
    {
        var affected = units
            .DistinctBy(unit => $"{unit.SlaveId}-{unit.ModuleIndex}")
            .OrderBy(unit => unit.SlaveId)
            .ThenBy(unit => unit.ModuleIndex)
            .Select(unit => $"{unit.SlaveId}#{unit.ModuleIndex}")
            .ToList();

        return affected.Count == 0
            ? "无"
            : string.Join(", ", affected);
    }

    private CommSettings BuildCommSettings(string selectedPort)
    {
        var baudRate = int.TryParse(cmbBaudRate.SelectedItem?.ToString(), out var parsedBaudRate)
            ? parsedBaudRate
            : 9600;
        var stopBits = int.TryParse(cmbStopBits.SelectedItem?.ToString(), out var parsedStopBits)
            ? parsedStopBits
            : 1;
        var parity = cmbParity.SelectedItem?.ToString() ?? "None";

        return new CommSettings
        {
            SlaveAddress = (byte)nudSlaveAddress.Value,
            PortName = IsMockMode(selectedPort) ? string.Empty : selectedPort,
            BaudRate = baudRate,
            DataBits = 8,
            StopBits = stopBits,
            Parity = parity,
            ReadTimeoutMs = 800,
            WriteTimeoutMs = 800,
        };
    }

    private void RebuildDashboardPanels()
    {
        flpDashboard.SuspendLayout();
        flpDashboard.Controls.Clear();

        var controllers = _dashboardManager.Controllers.OrderBy(controller => controller.SlaveId).ToList();
        for (var index = 0; index < controllers.Count; index++)
        {
            var controller = controllers[index];
            flpDashboard.Controls.Add(CreateControllerSection(controller));
            if (index < controllers.Count - 1)
            {
                flpDashboard.Controls.Add(CreateDashboardSeparator());
            }
        }

        flpDashboard.ResumeLayout(true);
        ResizeDashboardPanels();
        UpdateAllCardLabels();
    }

    private Control CreateControllerSection(WiredControllerInfo controller)
    {
        var section = CreateDashboardSection($"■ {controller.Name} / SlaveId {controller.SlaveId} / 模块 {controller.Modules.Count} 个", controller.Modules.OrderBy(module => module.DisplayOrder).ThenBy(module => module.ModuleIndex).ToList());
        if (section is TableLayoutPanel layout && layout.Controls.Count > 0 && layout.Controls[0] is Label title)
        {
            layout.Controls.RemoveAt(0);

            var titlePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.FromArgb(240, 248, 255),
                Height = 38,
            };
            titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            title.Dock = DockStyle.Fill;
            title.Margin = new Padding(0);
            titlePanel.Controls.Add(title, 0, 0);

            var btnEditGroup = new Button
            {
                Text = "修改配置",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(8, 5, 10, 0),
                Tag = controller,
            };
            btnEditGroup.Click += EditControllerGroup_Click;
            titlePanel.Controls.Add(btnEditGroup, 1, 0);

            layout.Controls.Add(titlePanel, 0, 0);
        }

        return section;
    }

    private Control CreateDashboardSection(string titleText, List<HeatPumpModuleInfo> units)
    {
        var section = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = titleText,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 38,
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 80, 160),
            Padding = new Padding(10, 8, 0, 0),
            BackColor = Color.FromArgb(240, 248, 255),
            Margin = new Padding(0),
        };
        section.Controls.Add(title, 0, 0);

        var cardsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10),
            Margin = new Padding(0),
            BackColor = Color.WhiteSmoke,
            Tag = "cardsPanel",
        };

        foreach (var unit in units)
        {
            cardsPanel.Controls.Add(BuildUnitCard(unit));
        }

        section.Controls.Add(cardsPanel, 0, 1);
        return section;
    }

    private static Control CreateDashboardSeparator()
    {
        return new Panel
        {
            Height = 2,
            BackColor = Color.FromArgb(200, 200, 200),
            Margin = new Padding(0, 4, 0, 8),
            Width = 1200,
        };
    }

    private Panel BuildUnitCard(HeatPumpModuleInfo unit)
    {
        var card = new Panel
        {
            Width = 320,
            Height = 220,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = unit.IsSelected ? Color.FromArgb(232, 244, 255) : Color.White,
            Margin = new Padding(6),
            Padding = new Padding(14, 12, 14, 12),
            Name = unit.Id,
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Controls.Add(layout);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var chkSelect = new CheckBox
        {
            Checked = unit.IsSelected,
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0),
            Tag = unit,
        };
        chkSelect.CheckedChanged += UnitSelect_CheckedChanged;
        header.Controls.Add(chkSelect, 0, 0);

        var txtName = new TextBox
        {
            Text = unit.Name,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 8, 0),
            BackColor = Color.White,
            MinimumSize = new Size(0, 24),
            Tag = unit,
        };
        txtName.TextChanged += (_, _) =>
        {
            var name = txtName.Text.Trim();
            unit.Name = string.IsNullOrWhiteSpace(name) ? "未命名机组" : name;
        };
        header.Controls.Add(txtName, 1, 0);

        var lblId = new Label
        {
            Text = unit.ModuleIndex == 0 ? "0#总机" : $"模块 {unit.ModuleIndex}#",
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 5, 6, 0),
        };
        header.Controls.Add(lblId, 2, 0);
        layout.Controls.Add(header, 0, 0);

        var line = new Panel
        {
            Dock = DockStyle.Top,
            Height = 2,
            BackColor = Color.FromArgb(230, 230, 230),
            Margin = new Padding(0, 10, 0, 10),
        };
        layout.Controls.Add(line, 0, 1);

        var metrics = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblOutletTitle = new Label
        {
            Text = "出水温度",
            Font = new Font("微软雅黑", 10),
            ForeColor = Color.Gray,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
        };
        metrics.Controls.Add(lblOutletTitle, 0, 0);

        var lblOutlet = new Label
        {
            Text = "-- °C",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 140, 255),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
        };
        metrics.Controls.Add(lblOutlet, 0, 1);
        metrics.SetColumnSpan(lblOutlet, 2);

        var lblReturnTitle = new Label
        {
            Text = "回水 / 设定",
            Font = new Font("微软雅黑", 10),
            ForeColor = Color.Gray,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 8, 0),
        };
        metrics.Controls.Add(lblReturnTitle, 0, 2);

        var lblReturnAndTarget = new Label
        {
            Text = "-- / -- °C",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleRight,
        };
        metrics.Controls.Add(lblReturnAndTarget, 1, 2);

        var lblMode = new Label
        {
            Text = "模式: --",
            Font = new Font("微软雅黑", 9.5F),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
        };
        metrics.Controls.Add(lblMode, 0, 3);
        metrics.SetColumnSpan(lblMode, 2);
        layout.Controls.Add(metrics, 0, 2);

        var bottomRow = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = unit.ModuleIndex == 0 ? 1 : 2,
            Margin = new Padding(0, 12, 0, 0),
            Padding = new Padding(0),
        };
        bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        if (unit.ModuleIndex != 0)
        {
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        var lblStatus = new Label
        {
            Text = "● 等待数据",
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            ForeColor = Color.Gray,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 5, 0, 0),
        };
        bottomRow.Controls.Add(lblStatus, 0, 0);

        if (unit.ModuleIndex != 0)
        {
            var btnSettings = new Button
            {
                Text = "设置按钮",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(72, 28),
                Font = new Font("微软雅黑", 8),
                Tag = unit,
                Margin = new Padding(8, 0, 0, 0),
                Anchor = AnchorStyles.Right,
            };
            btnSettings.Click += EditModuleConfig_Click;
            bottomRow.Controls.Add(btnSettings, 1, 0);
        }

        layout.Controls.Add(bottomRow, 0, 3);

        metrics.Click += UnitCard_Click;
        lblOutletTitle.Click += UnitCard_Click;
        lblOutlet.Click += UnitCard_Click;
        lblReturnTitle.Click += UnitCard_Click;
        lblReturnAndTarget.Click += UnitCard_Click;
        lblMode.Click += UnitCard_Click;
        bottomRow.Click += UnitCard_Click;
        lblStatus.Click += UnitCard_Click;

        card.Tag = new UnitCardTag(unit, card, chkSelect, lblOutlet, lblReturnAndTarget, lblMode, lblStatus);
        return card;
    }

    private void EditControllerGroup_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: WiredControllerInfo controller })
        {
            return;
        }

        using var dialog = new ControllerGroupEditorDialog(controller);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RebuildDashboardPanels();
            SetControlMessage($"已更新组别配置: {controller.Name}", Color.FromArgb(0, 140, 72), Color.FromArgb(236, 251, 238));
        }
    }

    private void EditModuleConfig_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: HeatPumpModuleInfo unit })
        {
            return;
        }

        using var dialog = new ModuleEditorDialog(unit);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RebuildDashboardPanels();
            SetControlMessage($"已更新设备配置: {unit.Name}", Color.FromArgb(0, 140, 72), Color.FromArgb(236, 251, 238));
        }
    }

    private void DeleteUnitCard_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: HeatPumpModuleInfo unit })
        {
            return;
        }

        _dashboardManager.RemoveUnit(unit.Id);
        RebuildDashboardPanels();
    }

    private void UnitCard_Click(object? sender, EventArgs e)
    {
        var card = FindCardFromSender(sender as Control);
        if (card?.Tag is not UnitCardTag tag)
        {
            return;
        }

        using var detailsForm = new HeatPumpDetailsForm();
        detailsForm.UpdateUnit(new HeatPumpUnitInfo
        {
            Id = tag.Unit.Id,
            Name = tag.Unit.Name,
            DeviceAddress = tag.Unit.ModuleIndex,
            IsPrimaryConnection = tag.Unit.ModuleIndex == 0,
            IsScannedUnit = tag.Unit.ModuleIndex != 0,
            IsSelected = tag.Unit.IsSelected,
            Telemetry = tag.Unit.Telemetry,
        });
        detailsForm.ShowDialog(this);
    }

    private void UnitSelect_CheckedChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingSelectionUi)
        {
            return;
        }

        if (sender is not CheckBox { Tag: HeatPumpModuleInfo unit } checkBox)
        {
            return;
        }

        ApplySelectionChange(unit, checkBox.Checked);
    }

    private void ApplySelectionChange(HeatPumpModuleInfo unit, bool isSelected)
    {
        _isUpdatingSelectionUi = true;
        try
        {
            if (cmbControlScope.SelectedItem?.ToString() == SingleControlScope && isSelected)
            {
                foreach (var item in _dashboardManager.AllUnits)
                {
                    item.IsSelected = false;
                }
            }

            unit.IsSelected = isSelected;

            UpdateSelectionStatusText();
            UpdateAllCardLabels();
        }
        finally
        {
            _isUpdatingSelectionUi = false;
        }
    }

    private void UpdateSelectionStatusText()
    {
        var selectedUnits = _dashboardManager.AllUnits.Where(item => item.IsSelected).ToList();
        var selectedSlaveIds = selectedUnits.Select(item => item.SlaveId).Distinct().ToList();
        SetControlMessage(selectedUnits.Count == 0
            ? "未选择模块"
            : $"已选择 {selectedUnits.Count} 个模块，涉及 {selectedSlaveIds.Count} 台线控器；受影响模块: {BuildAffectedModulesText(selectedUnits)}");
    }

    private void SetControlMessage(string message, Color? foreColor = null, Color? backColor = null)
    {
        lblControlMessage.Text = message;
        lblControlMessage.ForeColor = foreColor ?? Color.FromArgb(120, 90, 20);
        pnlControlMessage.BackColor = backColor ?? Color.FromArgb(255, 252, 235);
    }

    private Panel? FindCardFromSender(Control? control)
    {
        while (control is not null)
        {
            if (control is Panel panel && panel.Tag is UnitCardTag)
            {
                return panel;
            }

            control = control.Parent;
        }

        return null;
    }

    private void ResizeDashboardPanels()
    {
        var availableWidth = Math.Max(flpDashboard.ClientSize.Width - flpDashboard.Padding.Horizontal - 8, 320);
        foreach (Control control in flpDashboard.Controls)
        {
            control.Width = availableWidth;
            if (control is TableLayoutPanel section)
            {
                foreach (Control child in section.Controls)
                {
                    if (child is FlowLayoutPanel cardsPanel)
                    {
                        cardsPanel.MaximumSize = new Size(availableWidth, 0);
                        cardsPanel.MinimumSize = new Size(availableWidth, 0);
                        cardsPanel.Width = availableWidth;
                    }
                }
            }
        }
    }

    private void UpdateAllCardLabels()
    {
        foreach (Control card in EnumerateUnitCards(flpDashboard))
        {
            if (card.Tag is UnitCardTag tag)
            {
                UpdateUnitCardLabels(tag);
            }
        }
    }

    private static IEnumerable<Control> EnumerateUnitCards(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Panel panel && panel.Tag is UnitCardTag)
            {
                yield return panel;
            }

            foreach (var nested in EnumerateUnitCards(child))
            {
                yield return nested;
            }
        }
    }

    private void UpdateUnitCardLabels(UnitCardTag tag)
    {
        tag.Card.BackColor = tag.Unit.IsSelected
            ? Color.FromArgb(210, 235, 255)
            : Color.White;

        if (tag.CheckBox.Checked != tag.Unit.IsSelected)
        {
            tag.CheckBox.Checked = tag.Unit.IsSelected;
        }

        var telemetry = tag.Unit.Telemetry;
        if (telemetry.Status == HeatPumpUnitStatus.Offline)
        {
            tag.OutletLabel.Text = "-- °C";
            tag.ReturnAndTargetLabel.Text = "-- / -- °C";
        }
        else
        {
            tag.OutletLabel.Text = $"{telemetry.OutletWaterTemperature:F1} °C";
            tag.ReturnAndTargetLabel.Text = $"{telemetry.ReturnWaterTemperature:F1} / {telemetry.TargetTemperature:F1} °C";
        }

        tag.ModeLabel.Text = $"模式: {FormatMode(telemetry.RunMode)} / 环境 {telemetry.AmbientTemperature:F1} °C";
        tag.StatusLabel.Text = FormatStatusText(telemetry.Status);
        tag.StatusLabel.ForeColor = GetStatusColor(telemetry.Status);
    }

    private void UpdateConnectionState(bool isConnected, string statusText)
    {
        _isConnected = isConnected;
        btnConnect.Enabled = !isConnected;
        btnDisconnect.Enabled = isConnected;
        lblStatusDashboard.Text = statusText;
        lblStatusDashboard.ForeColor = isConnected ? Color.FromArgb(0, 140, 72) : Color.Red;
    }

    private static string FormatMode(HeatPumpRunMode mode)
    {
        return mode switch
        {
            HeatPumpRunMode.Off => "关机",
            HeatPumpRunMode.Heating => "制热",
            HeatPumpRunMode.Cooling => "制冷",
            HeatPumpRunMode.Pump => "水泵",
            HeatPumpRunMode.Defrost => "化霜",
            _ => "未知",
        };
    }

    private static string FormatStatusText(HeatPumpUnitStatus status)
    {
        return status switch
        {
            HeatPumpUnitStatus.Running => "● 运行中",
            HeatPumpUnitStatus.Standby => "● 待机",
            HeatPumpUnitStatus.Alarm => "● 告警",
            _ => "● 离线",
        };
    }

    private static Color GetStatusColor(HeatPumpUnitStatus status)
    {
        return status switch
        {
            HeatPumpUnitStatus.Running => Color.FromArgb(0, 140, 72),
            HeatPumpUnitStatus.Standby => Color.FromArgb(205, 136, 0),
            HeatPumpUnitStatus.Alarm => Color.FromArgb(214, 48, 49),
            _ => Color.Gray,
        };
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        dashboardRefreshTimer.Stop();
        _dataService.Dispose();
        base.OnFormClosed(e);
    }

    private sealed record UnitCardTag(
        HeatPumpModuleInfo Unit,
        Panel Card,
        CheckBox CheckBox,
        Label OutletLabel,
        Label ReturnAndTargetLabel,
        Label ModeLabel,
        Label StatusLabel);
}
