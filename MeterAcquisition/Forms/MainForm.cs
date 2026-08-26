using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MeterAcquisition.HeatPump.Application;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Forms;
using MeterAcquisition.Thermostat.Forms;
using MeterAcquisition.HeatPump.Services;
using MeterAcquisition.Properties;
using Tpem.Diagnostics;
using Tpem.Thresholds;
using ThreadingCancellationToken = System.Threading.CancellationToken;

namespace MeterAcquisition
{
    public partial class MainForm : Form
    {
        private readonly ModbusService _modbusService;
        private readonly ModbusService _modbusService2;
        private IMeterDataReader _meterDataService;
        private IMeterDataReader _meterDataService2;
        private MeterDataService _legacyMeterDataService;
        private MeterManager _meterManager;
        private string _meterProtocol;
        private CommunicationConfig _meterCommunicationConfig;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly MqttPublisherService _mqttPublisherService;
        private readonly string _siteCode;
        private readonly string _toolbarBoxCode;
        private readonly string _dashboardBoxCode;
        private readonly Dictionary<string, MeterDetailsForm> _detailForms = new Dictionary<string, MeterDetailsForm>();
        private TabPage _tabMeterOverview;
        private MeterOverviewControl _meterOverview;
        private bool _isRefreshing = false;
        /// <summary>正在退出。P0-5：置位后采集周期不再触碰控件与串口。</summary>
        private bool _isShuttingDown = false;
        // P1-7：三类采样各自的上次执行时刻（UTC）。用时钟而不是 tick 计数判断是否到期。
        private DateTime _lastRealtimeSampleUtc = DateTime.MinValue;
        private DateTime _lastEnergySampleUtc = DateTime.MinValue;
        private DateTime _lastQualitySampleUtc = DateTime.MinValue;
        private int _refreshTickCount;
        private readonly int _realTimeIntervalSeconds;
        private readonly int _energyIntervalSeconds;
        private readonly int _qualityIntervalSeconds;
        private readonly int _offlineTimeoutSeconds;

        private int _boxCounter = 1;

        private readonly HistoryQueryService _historyQueryService = new HistoryQueryService();
        private readonly List<MeterLookupItem> _queryMeters = new List<MeterLookupItem>();
        private readonly int _quickPageSize = 100;
        private readonly Dictionary<Series, double[]> _compareHoverXValues = new Dictionary<Series, double[]>();
        private double[] _compareHoverTimeline = Array.Empty<double>();
        private bool _queryDataLoaded;
        private bool _suppressTreeAfterCheck;
        private bool _quickHoverActive;
        private bool _compareHoverActive;
        private int _quickCurrentPage = 1;
        private int _lastQuickHoverIndex = -1;
        private long _quickTotalRows;
        private double _lastCompareHoverAnchorX = double.NaN;
        private double[] _quickHoverXValues = Array.Empty<double>();
        private string _quickBaseStatusText = "等待查询";
        private string _compareBaseStatusText = "等待查询";
        private string _quickHoverMetricDisplayName = string.Empty;
        private string _quickHoverMetricUnit = string.Empty;
        private string _compareHoverMetricDisplayName = string.Empty;
        private string _compareHoverMetricUnit = string.Empty;
        private bool _quickBaseStatusIsError;
        private bool _compareBaseStatusIsError;
        private bool _suppressScanRangePersistence;
        private bool _mqttPublishErrorActive;
        private bool _mqttPublishErrorShown;
        private FlowLayoutPanel _flpBoxContainer;
        private readonly HeatPumpWorkspaceService _heatPumpWorkspaceService;
        private readonly HeatPumpWorkspaceConfigStore _heatPumpConfigStore = new HeatPumpWorkspaceConfigStore();
        private readonly HashSet<string> _selectedHeatPumpModuleKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly ToolTip _heatPumpToolTip = new ToolTip();
        private readonly System.Windows.Forms.Timer _heatPumpRefreshTimer;
        private bool _isHeatPumpScanning;
        private bool _isHeatPumpRefreshing;
        private string _pendingHeatPumpConfigurationWarning = string.Empty;
        private readonly int _heatPumpRefreshIntervalSeconds;
        private TabPage _tabLineController;
        private TabPage _tabHeatPumpAnalysis;
        private HeatPumpMonitorControl _heatPumpAnalysisControl;
        private TabPage _tabThermostat;
        private ThermostatMonitorControl _thermostatMonitorControl;
        private ComboBox _cmbHeatPumpPort;
        private ComboBox _cmbHeatPumpBaud;
        private ComboBox _cmbHeatPumpParity;
        private ComboBox _cmbHeatPumpStopBits;
        private NumericUpDown _nudHeatPumpScanStart;
        private NumericUpDown _nudHeatPumpScanEnd;
        private Button _btnHeatPumpConnect;
        private Button _btnHeatPumpDisconnect;
        private Button _btnHeatPumpScan;
        private Label _lblLineControllerConnectionValue;
        private Label _lblHeatPumpSelectionSummary;
        private Label _lblLineControllerControllerCountValue;
        private Label _lblLineControllerModuleCountValue;
        private Label _lblLineControllerRefreshValue;
        private FlowLayoutPanel _flpLineControllerCards;
        private Label _lblLineControllerPageStatus;

        public MainForm()
        {
            InitializeComponent();
            InitializeMeterOverviewTab();
            InitializeLineControllerTab();
            _modbusService = new ModbusService();
            _modbusService2 = new ModbusService();
            _meterProtocol = GetMeterProtocol();
            _meterCommunicationConfig = GetMeterCommunicationConfig(_meterProtocol);
            ApplyMeterCommunicationConfig(_modbusService, _meterCommunicationConfig);
            ApplyMeterCommunicationConfig(_modbusService2, _meterCommunicationConfig);
            _heatPumpRefreshIntervalSeconds = GetPositiveIntAppSetting("HeatPumpRefreshIntervalSeconds", 10);
            _heatPumpWorkspaceService = new HeatPumpWorkspaceService();
            _heatPumpRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = _heatPumpRefreshIntervalSeconds * 1000
            };
            _heatPumpRefreshTimer.Tick += HeatPumpRefreshTimer_Tick;
            LoadHeatPumpWorkspaceConfiguration();
            _legacyMeterDataService = new MeterDataService(_modbusService);
            _meterDataService = CreateMeterDataReader(_modbusService);
            _meterDataService2 = CreateMeterDataReader(_modbusService2);
            UpdateMeterProtocolButton();
            RefreshLineControllerPage();
            _modbusService.ConnectionStateChanged += OnConnectionStateChanged;
            _modbusService.CommunicationError += OnCommunicationError;
            _modbusService2.ConnectionStateChanged += OnSecondConnectionStateChanged;
            _modbusService2.CommunicationError += OnSecondCommunicationError;
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _mqttPublisherService = new MqttPublisherService();
            _siteCode = ConfigurationManager.AppSettings["SiteCode"] ?? "SITE-001";
            _toolbarBoxCode = ConfigurationManager.AppSettings["ToolbarBoxCode"] ?? "BOX-MAIN";
            _dashboardBoxCode = ConfigurationManager.AppSettings["DashboardBoxCode"] ?? "BOX-DASHBOARD";
            _realTimeIntervalSeconds = GetPositiveIntAppSetting("RealTimeIntervalSeconds", 1);
            _energyIntervalSeconds = GetPositiveIntAppSetting("EnergyIntervalSeconds", 60);
            _qualityIntervalSeconds = GetPositiveIntAppSetting("QualityIntervalSeconds", 60);
            _offlineTimeoutSeconds = GetPositiveIntAppSetting("OfflineTimeoutSeconds", 15);

            _meterManager = new MeterManager(_meterDataService, _meterDataService2);
            _flpBoxContainer = flpDashboard;
            flpDashboard.Resize += (sender, e) => ResizeDashboardPanels();

            SetupDefaultBoxes();
            RebuildDashboardPanels();

            cmbBaud.SelectedIndex = 3;
            cmbParamBaud.SelectedIndex = 3;
            cmbParamParity.SelectedIndex = 2;
            cmbWiring.SelectedIndex = 2;
            tslTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            chartQuickHistory.MouseMove += ChartQuickHistory_MouseMove;
            chartQuickHistory.MouseLeave += ChartQuickHistory_MouseLeave;
            chartCompareHistory.MouseMove += ChartCompareHistory_MouseMove;
            chartCompareHistory.MouseLeave += ChartCompareHistory_MouseLeave;
            cmbPort.DropDown += SerialPortCombo_DropDown;
            cmbSecondPort.DropDown += SerialPortCombo_DropDown;
            nudScanStart.ValueChanged += ScanRange_ValueChanged;
            nudScanEnd.ValueChanged += ScanRange_ValueChanged;
            LoadDashboardScanRangeSettings();
            clockTimer.Start();
            LogEffectiveThresholds();
            this.Shown += MainForm_Shown;
        }

        /// <summary>
        /// 启动时把生效的判定阈值写进日志（P1-1）。
        /// 现场排查"这台表为什么显示电压异常"时，先看这一行就能确认用的是哪套阈值。
        /// </summary>
        private void LogEffectiveThresholds()
        {
            try
            {
                var set = SharedThresholdProvider.Resolve(_siteCode, null, null);
                var voltage = set.Get(ThresholdSet.VoltagePhase);
                var pf = set.Get(ThresholdSet.PowerFactorTotal);
                AppLogger.Info(
                    "Threshold",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "判定阈值来源: {0}；相电压 {1}~{2} V，总功率因数下限 {3}，带电门槛 {4} V。",
                        SharedThresholdProvider.UsingFallback ? "内置兜底值（数据库不可用）" : "meter_threshold 表",
                        voltage != null && voltage.MinValue.HasValue ? voltage.MinValue.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-",
                        voltage != null && voltage.MaxValue.HasValue ? voltage.MaxValue.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-",
                        pf != null && pf.MinValue.HasValue ? pf.MinValue.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-",
                        set.EnergizedThreshold.ToString("0.##", CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Threshold", "输出生效阈值失败（不影响采集）。", ex);
            }
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                ResizeDashboardPanels();
                await EnsureQueryModuleLoadedAsync(false).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                HandleHandlerException("窗体初始化", ex);
            }
        }

        private void InitializeMeterOverviewTab()
        {
            _tabMeterOverview = new TabPage
            {
                Name = "tabMeterOverview",
                Text = "电表总览",
                Padding = new Padding(4),
                UseVisualStyleBackColor = true
            };

            _meterOverview = new MeterOverviewControl(GetMeterDisplayStatus);
            _meterOverview.MeterSelected += MeterOverview_MeterSelected;
            _tabMeterOverview.Controls.Add(_meterOverview);

            var dashboardIndex = tabControlMain.TabPages.IndexOf(tabDashboard);
            tabControlMain.TabPages.Insert(dashboardIndex >= 0 ? dashboardIndex + 1 : tabControlMain.TabPages.Count, _tabMeterOverview);
        }

        private void MeterOverview_MeterSelected(object sender, MeterSelectedEventArgs e)
        {
            ShowMeterDetails(e.MeterId);
        }

        private void InitializeLineControllerTab()
        {
            _tabLineController = new TabPage
            {
                Name = "tabLineController",
                Text = "线控器数据",
                Padding = new Padding(4),
                UseVisualStyleBackColor = true
            };

            var lineControllerControl = new LineControllerControl();
            var connectionBar = CreateHeatPumpConnectionBar();
            var groupControlBar = CreateHeatPumpGroupControlBar();
            lineControllerControl.ConnectionBarHost.Controls.Add(connectionBar);
            lineControllerControl.GroupControlBarHost.Controls.Add(groupControlBar);
            _lblLineControllerConnectionValue = lineControllerControl.ConnectionValueLabel;
            _lblLineControllerControllerCountValue = lineControllerControl.ControllerCountValueLabel;
            _lblLineControllerModuleCountValue = lineControllerControl.ModuleCountValueLabel;
            _lblLineControllerRefreshValue = lineControllerControl.RefreshValueLabel;
            _flpLineControllerCards = lineControllerControl.CardsPanel;
            _lblLineControllerPageStatus = lineControllerControl.PageStatusLabel;
            _lblLineControllerPageStatus.Text = string.IsNullOrWhiteSpace(_pendingHeatPumpConfigurationWarning)
                ? "请选择热泵独立串口并连接后扫描线控器。"
                : _pendingHeatPumpConfigurationWarning;

            _tabLineController.Controls.Add(lineControllerControl);
            tabControlMain.TabPages.Add(_tabLineController);

            _heatPumpAnalysisControl = new HeatPumpMonitorControl();
            _tabHeatPumpAnalysis = new TabPage
            {
                Name = "tabHeatPumpAnalysis",
                Text = "线控器历史分析",
                Padding = new Padding(4),
                UseVisualStyleBackColor = true
            };
            _tabHeatPumpAnalysis.Controls.Add(_heatPumpAnalysisControl);
            tabControlMain.TabPages.Add(_tabHeatPumpAnalysis);

            _thermostatMonitorControl = new ThermostatMonitorControl();
            _tabThermostat = new TabPage
            {
                Name = "tabThermostat",
                Text = "温控器",
                Padding = new Padding(4),
                UseVisualStyleBackColor = true
            };
            _tabThermostat.Controls.Add(_thermostatMonitorControl);
            tabControlMain.TabPages.Add(_tabThermostat);
        }

        private Control CreateHeatPumpConnectionBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0)
            };

            _cmbHeatPumpPort = CreateHeatPumpComboBox(120);
            _cmbHeatPumpBaud = CreateHeatPumpComboBox(90, new[] { "4800", "9600", "19200", "38400", "57600", "115200" });
            _cmbHeatPumpParity = CreateHeatPumpComboBox(80, new[] { "None", "Even", "Odd" });
            _cmbHeatPumpStopBits = CreateHeatPumpComboBox(60, new[] { "1", "2" });
            _nudHeatPumpScanStart = CreateHeatPumpAddressInput();
            _nudHeatPumpScanEnd = CreateHeatPumpAddressInput();
            _btnHeatPumpConnect = new Button { Text = "连接热泵", AutoSize = true, Margin = new Padding(12, 4, 0, 4) };
            _btnHeatPumpDisconnect = new Button { Text = "断开热泵", AutoSize = true, Enabled = false, Margin = new Padding(4) };
            _btnHeatPumpScan = new Button { Text = "扫描线控器", AutoSize = true, Enabled = false, Margin = new Padding(4) };

            RefreshHeatPumpPorts(false);
            LoadHeatPumpSettings();
            _cmbHeatPumpPort.DropDown += (sender, e) => RefreshHeatPumpPorts(true);
            // P0-1: 原来是 `async (sender, e) => await XxxAsync()`，异常无人接管会冒泡成未处理异常。
            _btnHeatPumpConnect.Click += async (sender, e) => await RunGuardedAsync("热泵连接", ConnectHeatPumpAsync);
            _btnHeatPumpDisconnect.Click += async (sender, e) => await RunGuardedAsync("热泵断开", DisconnectHeatPumpAsync);
            _btnHeatPumpScan.Click += async (sender, e) => await RunGuardedAsync("热泵扫描", ScanHeatPumpControllersAsync);

            AddHeatPumpField(bar, "端口", _cmbHeatPumpPort);
            AddHeatPumpField(bar, "波特率", _cmbHeatPumpBaud);
            AddHeatPumpField(bar, "校验", _cmbHeatPumpParity);
            AddHeatPumpField(bar, "停止位", _cmbHeatPumpStopBits);
            AddHeatPumpField(bar, "起始站号", _nudHeatPumpScanStart);
            AddHeatPumpField(bar, "结束站号", _nudHeatPumpScanEnd);
            bar.Controls.Add(_btnHeatPumpConnect);
            bar.Controls.Add(_btnHeatPumpDisconnect);
            bar.Controls.Add(_btnHeatPumpScan);
            return bar;
        }

        private static ComboBox CreateHeatPumpComboBox(int width, IEnumerable<string> values = null)
        {
            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = width,
                Margin = new Padding(4)
            };

            if (values != null)
            {
                comboBox.Items.AddRange(values.Cast<object>().ToArray());
            }

            return comboBox;
        }

        private static NumericUpDown CreateHeatPumpAddressInput()
        {
            return new NumericUpDown
            {
                Minimum = 1,
                Maximum = 247,
                Width = 64,
                Margin = new Padding(4)
            };
        }

        private static void AddHeatPumpField(FlowLayoutPanel bar, string title, Control control)
        {
            var field = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(0)
            };
            field.Controls.Add(new Label { Text = title, AutoSize = true, Margin = new Padding(0, 8, 2, 0) });
            field.Controls.Add(control);
            bar.Controls.Add(field);
        }

        private void RefreshHeatPumpPorts(bool preserveSelection)
        {
            var ports = SerialPort.GetPortNames().OrderBy(port => port, StringComparer.OrdinalIgnoreCase).ToArray();
            var previousSelection = preserveSelection ? _cmbHeatPumpPort.SelectedItem as string : null;
            _cmbHeatPumpPort.BeginUpdate();
            try
            {
                _cmbHeatPumpPort.Items.Clear();
                _cmbHeatPumpPort.Items.AddRange(ports);
                if (!string.IsNullOrWhiteSpace(previousSelection) && ports.Contains(previousSelection, StringComparer.OrdinalIgnoreCase))
                {
                    _cmbHeatPumpPort.SelectedItem = ports.First(port => string.Equals(port, previousSelection, StringComparison.OrdinalIgnoreCase));
                }
            }
            finally
            {
                _cmbHeatPumpPort.EndUpdate();
            }
        }

        private void LoadHeatPumpSettings()
        {
            var defaults = new CommSettings
            {
                PortName = ConfigurationManager.AppSettings["HeatPumpPortName"] ?? string.Empty,
                BaudRate = GetPositiveIntAppSetting("HeatPumpBaudRate", 9600),
                DataBits = GetPositiveIntAppSetting("HeatPumpDataBits", 8),
                Parity = ConfigurationManager.AppSettings["HeatPumpParity"] ?? "None",
                StopBits = GetPositiveIntAppSetting("HeatPumpStopBits", 1),
                ReadTimeoutMs = GetPositiveIntAppSetting("HeatPumpReadTimeoutMs", 1000),
                WriteTimeoutMs = GetPositiveIntAppSetting("HeatPumpWriteTimeoutMs", 1000)
            };
            var settings = Settings.Default;
            var portName = string.IsNullOrWhiteSpace(settings.HeatPumpPortName) ? defaults.PortName : settings.HeatPumpPortName;
            SelectHeatPumpComboValue(_cmbHeatPumpPort, portName);
            SelectHeatPumpComboValue(_cmbHeatPumpBaud, settings.HeatPumpBaudRate > 0 ? settings.HeatPumpBaudRate.ToString(CultureInfo.InvariantCulture) : defaults.BaudRate.ToString(CultureInfo.InvariantCulture));
            SelectHeatPumpComboValue(_cmbHeatPumpParity, string.IsNullOrWhiteSpace(settings.HeatPumpParity) ? defaults.Parity : settings.HeatPumpParity);
            SelectHeatPumpComboValue(_cmbHeatPumpStopBits, (settings.HeatPumpStopBits == 1 || settings.HeatPumpStopBits == 2 ? settings.HeatPumpStopBits : defaults.StopBits).ToString(CultureInfo.InvariantCulture));
            _nudHeatPumpScanStart.Value = ClampHeatPumpAddress(settings.HeatPumpScanStartAddress > 0 ? settings.HeatPumpScanStartAddress : GetHeatPumpScanAddress("HeatPumpScanStartAddress", 1));
            _nudHeatPumpScanEnd.Value = ClampHeatPumpAddress(settings.HeatPumpScanEndAddress > 0 ? settings.HeatPumpScanEndAddress : GetHeatPumpScanAddress("HeatPumpScanEndAddress", 16));
        }

        private static void SelectHeatPumpComboValue(ComboBox comboBox, string value)
        {
            var index = comboBox.FindStringExact(value ?? string.Empty);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }
        }

        private static decimal ClampHeatPumpAddress(int address)
        {
            return Math.Max(1, Math.Min(247, address));
        }

        private Control CreateHeatPumpGroupControlBar()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(8),
                BackColor = Color.FromArgb(246, 248, 250)
            };

            _lblHeatPumpSelectionSummary = new Label
            {
                AutoSize = true,
                Text = "已选择 0 个模块 / 0 个控制器",
                Margin = new Padding(0, 8, 12, 0)
            };
            var runMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 84,
                Enabled = false,
                Margin = new Padding(4)
            };
            runMode.Items.AddRange(new object[] { "制热", "制冷", "自动" });
            var targetTemperature = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 60,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Width = 70,
                Enabled = false,
                Margin = new Padding(4)
            };
            var applyButton = new Button { Text = "应用控制", AutoSize = true, Enabled = false, Margin = new Padding(8, 4, 4, 4) };
            var clearFaultButton = new Button { Text = "清故障", AutoSize = true, Enabled = false, Margin = new Padding(4) };
            const string writeDisabledReason = "控制写入尚未启用：待确认设备寄存器语义、运行联锁和现场操作流程。";
            foreach (Control control in new Control[] { runMode, targetTemperature, applyButton, clearFaultButton })
            {
                _heatPumpToolTip.SetToolTip(control, writeDisabledReason);
            }

            panel.Controls.Add(_lblHeatPumpSelectionSummary);
            AddHeatPumpField(panel, "运行模式", runMode);
            AddHeatPumpField(panel, "目标温度", targetTemperature);
            panel.Controls.Add(applyButton);
            panel.Controls.Add(clearFaultButton);
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = writeDisabledReason,
                ForeColor = Color.DimGray,
                Margin = new Padding(8, 8, 0, 0)
            });
            return panel;
        }

        private static Label AddSummaryCard(TableLayoutPanel summary, int columnIndex, string title, string initialValue)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(columnIndex == 0 ? 0 : 8, 0, 0, 0),
                Padding = new Padding(12),
                Height = 84
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 24,
                ForeColor = Color.DimGray
            };

            var valueLabel = new Label
            {
                Text = initialValue,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 16F, FontStyle.Bold, GraphicsUnit.Point, 134),
                ForeColor = Color.FromArgb(32, 64, 96),
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            summary.Controls.Add(card, columnIndex, 0);
            return valueLabel;
        }

        private void RefreshLineControllerPage()
        {
            var snapshot = _heatPumpWorkspaceService.CreateSnapshot();
            var availableModuleKeys = new HashSet<string>(
                snapshot.Controllers.SelectMany(controller => controller.Modules).Select(GetHeatPumpModuleKey),
                StringComparer.Ordinal);
            _selectedHeatPumpModuleKeys.RemoveWhere(key => !availableModuleKeys.Contains(key));
            _selectedHeatPumpModuleKeys.RemoveWhere(key => snapshot.Controllers
                .SelectMany(controller => controller.Modules)
                .Any(module => GetHeatPumpModuleKey(module) == key && !module.IsEnabled));
            UpdateHeatPumpSelectionSummary(snapshot);
            _lblLineControllerConnectionValue.Text = string.IsNullOrWhiteSpace(snapshot.ConnectionDescription)
                ? "未连接"
                : snapshot.ConnectionDescription;
            _lblLineControllerControllerCountValue.Text = snapshot.ControllerCount.ToString(CultureInfo.InvariantCulture);
            _lblLineControllerModuleCountValue.Text = snapshot.ModuleCount.ToString(CultureInfo.InvariantCulture);
            _lblLineControllerRefreshValue.Text = snapshot.LastRefreshAt?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "--";

            var groups = BuildLineControllerGroups(snapshot);
            RenderLineControllerGroups(groups);
            _lblLineControllerPageStatus.Text = groups.Count == 0
                ? "当前未扫描到线控器控制器或模块。"
                : "当前卡片数据来自 HeatPumpWorkspaceService 实时快照。";
        }

        private async Task ConnectHeatPumpAsync()
        {
            if (!TryBuildHeatPumpSettings(out var settings, out var error))
            {
                MessageBox.Show(error, "热泵通信参数", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetHeatPumpControlsEnabled(false);
            _lblLineControllerPageStatus.Text = "正在连接热泵独立串口...";
            try
            {
                var connected = await _heatPumpWorkspaceService.ConnectAsync(settings);
                if (!connected)
                {
                    _lblLineControllerPageStatus.Text = "热泵串口连接失败，请检查端口和通信参数。";
                    RefreshLineControllerPage();
                    return;
                }

                SaveHeatPumpSettings(settings);
                _heatPumpRefreshTimer.Start();
                RefreshLineControllerPage();
                _lblLineControllerPageStatus.Text = "热泵独立串口已连接，请扫描线控器。";
            }
            catch (Exception ex)
            {
                _lblLineControllerPageStatus.Text = "热泵串口连接失败: " + ex.Message;
            }
            finally
            {
                UpdateHeatPumpControlState();
            }
        }

        private async Task DisconnectHeatPumpAsync()
        {
            _heatPumpRefreshTimer.Stop();
            SetHeatPumpControlsEnabled(false);
            try
            {
                await _heatPumpWorkspaceService.DisconnectAsync();
                RefreshLineControllerPage();
                _lblLineControllerPageStatus.Text = "热泵独立串口已断开。";
            }
            catch (Exception ex)
            {
                _lblLineControllerPageStatus.Text = "热泵串口断开失败: " + ex.Message;
            }
            finally
            {
                UpdateHeatPumpControlState();
            }
        }

        private async Task ScanHeatPumpControllersAsync()
        {
            if (_isHeatPumpScanning || !_heatPumpWorkspaceService.IsConnected)
            {
                return;
            }

            _isHeatPumpScanning = true;
            _btnHeatPumpScan.Enabled = false;
            _lblLineControllerPageStatus.Text = "正在扫描热泵线控器和模块...";
            try
            {
                await _heatPumpWorkspaceService.ScanControllersAsync();
                var controllers = _heatPumpWorkspaceService.CreateSnapshot().Controllers;
                await _heatPumpWorkspaceService.ScanModulesAsync(controllers);
                SaveHeatPumpWorkspaceConfiguration();
                RefreshLineControllerPage();
                _lblLineControllerPageStatus.Text = controllers.Count == 0
                    ? "未扫描到热泵线控器，请核对站号范围和通信参数。"
                    : "热泵线控器扫描完成。";
            }
            catch (Exception ex)
            {
                _lblLineControllerPageStatus.Text = "热泵线控器扫描失败: " + ex.Message;
            }
            finally
            {
                _isHeatPumpScanning = false;
                UpdateHeatPumpControlState();
            }
        }

        private async void HeatPumpRefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await RefreshHeatPumpTelemetryAsync();
            }
            catch (Exception ex)
            {
                HandleHandlerException("热泵定时刷新", ex);
            }
        }

        private async Task RefreshHeatPumpTelemetryAsync()
        {
            if (_isHeatPumpRefreshing || _isHeatPumpScanning || !_heatPumpWorkspaceService.IsConnected)
            {
                return;
            }

            var controllers = _heatPumpWorkspaceService.CreateSnapshot().Controllers;
            if (controllers.Count == 0)
            {
                return;
            }

            _isHeatPumpRefreshing = true;
            try
            {
                var refreshResults = await _heatPumpWorkspaceService.RefreshTelemetryAsync(controllers);
                await PublishHeatPumpTelemetryAsync(refreshResults);
                RefreshLineControllerPage();

                var failedCount = refreshResults.Count(result => !result.IsSuccess);
                if (failedCount > 0)
                {
                    _lblLineControllerPageStatus.Text = string.Format(
                        CultureInfo.InvariantCulture,
                        "热泵遥测刷新完成，{0} 个模块读取失败。",
                        failedCount);
                }
            }
            catch (Exception ex)
            {
                _lblLineControllerPageStatus.Text = "热泵遥测刷新失败: " + ex.Message;
            }
            finally
            {
                _isHeatPumpRefreshing = false;
            }
        }

        private async Task PublishHeatPumpTelemetryAsync(IEnumerable<HeatPumpTelemetryRefreshResult> refreshResults)
        {
            if (_mqttPublisherService == null || refreshResults == null)
            {
                return;
            }

            var siteCode = ConfigurationManager.AppSettings["SiteCode"] ?? string.Empty;
            foreach (var result in refreshResults.Where(item => item.IsSuccess))
            {
                var module = result.Module;
                var telemetry = module.Telemetry;
                try
                {
                    await _mqttPublisherService.PublishHeatPumpTelemetryAsync(new HeatPumpTelemetryMessage
                    {
                        MessageId = Guid.NewGuid(),
                        SiteCode = siteCode,
                        ControllerSlaveId = module.SlaveId,
                        ModuleIndex = module.ModuleIndex,
                        ModuleName = module.Name,
                        GroupName = module.GroupName,
                        CollectedAt = new DateTimeOffset(DateTime.SpecifyKind(telemetry.LastUpdatedAt, DateTimeKind.Local)).ToUniversalTime(),
                        IsEnabled = module.IsEnabled,
                        RunMode = (int)telemetry.RunMode,
                        Status = (int)telemetry.Status,
                        OutletWaterTemperature = telemetry.OutletWaterTemperature,
                        ReturnWaterTemperature = telemetry.ReturnWaterTemperature,
                        TargetTemperature = telemetry.TargetTemperature,
                        AmbientTemperature = telemetry.AmbientTemperature,
                        CompressorOn = telemetry.CompressorOn,
                        PumpOn = telemetry.PumpOn,
                        ElectricHeaterOn = telemetry.ElectricHeaterOn,
                        FaultCode = string.Empty
                    });
                }
                catch (Exception ex)
                {
                    _lblLineControllerPageStatus.Text = "热泵 MQTT 发布失败: " + ex.Message;
                }
            }
        }

        private bool TryBuildHeatPumpSettings(out CommSettings settings, out string error)
        {
            settings = null;
            error = string.Empty;
            var portName = _cmbHeatPumpPort.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(portName))
            {
                error = "请选择热泵独立串口。";
                return false;
            }

            if (!SerialPort.GetPortNames().Any(port => string.Equals(port, portName, StringComparison.OrdinalIgnoreCase)))
            {
                error = "所选热泵串口当前不可用。";
                return false;
            }

            if (!int.TryParse(_cmbHeatPumpBaud.SelectedItem as string, out var baudRate) ||
                !int.TryParse(_cmbHeatPumpStopBits.SelectedItem as string, out var stopBits))
            {
                error = "热泵波特率或停止位无效。";
                return false;
            }

            var scanStart = decimal.ToInt32(_nudHeatPumpScanStart.Value);
            var scanEnd = decimal.ToInt32(_nudHeatPumpScanEnd.Value);
            if (scanStart > scanEnd)
            {
                error = "热泵扫描起始站号不能大于结束站号。";
                return false;
            }

            settings = new CommSettings
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = GetPositiveIntAppSetting("HeatPumpDataBits", 8),
                Parity = _cmbHeatPumpParity.SelectedItem as string ?? "None",
                StopBits = stopBits,
                ReadTimeoutMs = GetPositiveIntAppSetting("HeatPumpReadTimeoutMs", 1000),
                WriteTimeoutMs = GetPositiveIntAppSetting("HeatPumpWriteTimeoutMs", 1000),
                SlaveAddress = (byte)scanStart,
                ControllerScanStartAddress = (byte)scanStart,
                ControllerScanEndAddress = (byte)scanEnd
            };
            return true;
        }

        private void LoadHeatPumpWorkspaceConfiguration()
        {
            var configuration = _heatPumpConfigStore.Load(out var warning);
            _heatPumpWorkspaceService.ApplyConfiguration(configuration);
            foreach (var key in configuration.SelectedModuleKeys)
            {
                _selectedHeatPumpModuleKeys.Add(key);
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                _pendingHeatPumpConfigurationWarning = warning;
            }
        }

        private void SaveHeatPumpWorkspaceConfiguration()
        {
            _heatPumpConfigStore.Save(_heatPumpWorkspaceService.ExportConfiguration(_selectedHeatPumpModuleKeys));
        }

        private void SaveHeatPumpSettings(CommSettings settings)
        {
            var userSettings = Settings.Default;
            userSettings.HeatPumpPortName = settings.PortName;
            userSettings.HeatPumpBaudRate = settings.BaudRate;
            userSettings.HeatPumpDataBits = settings.DataBits;
            userSettings.HeatPumpParity = settings.Parity;
            userSettings.HeatPumpStopBits = settings.StopBits;
            userSettings.HeatPumpReadTimeoutMs = settings.ReadTimeoutMs;
            userSettings.HeatPumpWriteTimeoutMs = settings.WriteTimeoutMs;
            userSettings.HeatPumpScanStartAddress = decimal.ToInt32(_nudHeatPumpScanStart.Value);
            userSettings.HeatPumpScanEndAddress = decimal.ToInt32(_nudHeatPumpScanEnd.Value);
            userSettings.Save();
        }

        private void SetHeatPumpControlsEnabled(bool enabled)
        {
            _cmbHeatPumpPort.Enabled = enabled;
            _cmbHeatPumpBaud.Enabled = enabled;
            _cmbHeatPumpParity.Enabled = enabled;
            _cmbHeatPumpStopBits.Enabled = enabled;
            _nudHeatPumpScanStart.Enabled = enabled;
            _nudHeatPumpScanEnd.Enabled = enabled;
            _btnHeatPumpConnect.Enabled = enabled;
            _btnHeatPumpDisconnect.Enabled = enabled;
            _btnHeatPumpScan.Enabled = enabled;
        }

        private void UpdateHeatPumpControlState()
        {
            var connected = _heatPumpWorkspaceService.IsConnected;
            _cmbHeatPumpPort.Enabled = !connected;
            _cmbHeatPumpBaud.Enabled = !connected;
            _cmbHeatPumpParity.Enabled = !connected;
            _cmbHeatPumpStopBits.Enabled = !connected;
            _nudHeatPumpScanStart.Enabled = !connected;
            _nudHeatPumpScanEnd.Enabled = !connected;
            _btnHeatPumpConnect.Enabled = !connected;
            _btnHeatPumpDisconnect.Enabled = connected;
            _btnHeatPumpScan.Enabled = connected && !_isHeatPumpScanning;
        }

        private List<LineControllerGroupViewModel> BuildLineControllerGroups(HeatPumpWorkspaceSnapshot snapshot)
        {
            var groups = new List<LineControllerGroupViewModel>();
            foreach (var controller in snapshot.Controllers)
            {
                if (controller.Modules.Count == 0)
                {
                    groups.Add(new LineControllerGroupViewModel(BuildControllerTitle(controller), string.Empty, controller, BuildControllerCards(controller)));
                    continue;
                }

                foreach (var moduleGroup in controller.Modules
                    .OrderBy(module => module.DisplayOrder)
                    .ThenBy(module => module.ModuleIndex)
                    .GroupBy(module => string.IsNullOrWhiteSpace(module.GroupName) ? "未分组" : module.GroupName))
                {
                    groups.Add(new LineControllerGroupViewModel(
                        BuildControllerTitle(controller),
                        moduleGroup.Key,
                        controller,
                        moduleGroup.Select(module => BuildModuleCard(controller, module)).ToList()));
                }
            }

            return groups;
        }

        private List<LineControllerCardViewModel> BuildControllerCards(WiredControllerInfo controller)
        {
            if (controller.Modules.Count == 0)
            {
                return new List<LineControllerCardViewModel>
                {
                    new LineControllerCardViewModel(
                        "未发现模块",
                        $"控制器 {controller.Name}",
                        null,
                        new List<string>
                        {
                            $"控制器从站 {controller.SlaveId}",
                            $"在线状态 {(controller.IsOnline ? "在线" : "离线")}",
                            $"锁定状态 {(controller.ControllerLockEnabled ? "已锁定" : "未锁定")}",
                            $"回差 {controller.Differential} °C"
                        })
                };
            }

            return controller.Modules
                .OrderBy(module => module.DisplayOrder)
                .ThenBy(module => module.ModuleIndex)
                .Select(module => BuildModuleCard(controller, module))
                .ToList();
        }

        private LineControllerCardViewModel BuildModuleCard(WiredControllerInfo controller, HeatPumpModuleInfo module)
        {
            var telemetry = module.Telemetry;
            return new LineControllerCardViewModel(
                module.Name,
                $"模块 {module.ModuleIndex + 1} / 从站 {module.SlaveId} / {(module.IsEnabled ? "启用" : "禁用")}",
                module,
                new List<string>
                {
                    $"控制器 {controller.Name} / {(controller.IsOnline ? "在线" : "离线")}",
                    $"出水温度 {telemetry.OutletWaterTemperature:F1} °C",
                    $"回水温度 {telemetry.ReturnWaterTemperature:F1} °C",
                    $"目标温度 {telemetry.TargetTemperature:F1} °C",
                    $"环境温度 {telemetry.AmbientTemperature:F1} °C",
                    $"模式 {telemetry.RunMode}",
                    $"状态 {telemetry.Status}",
                    $"压缩机 {(telemetry.CompressorOn ? "开" : "关")} / 水泵 {(telemetry.PumpOn ? "开" : "关")} / 电辅热 {(telemetry.ElectricHeaterOn ? "开" : "关")}",
                    $"最近更新 {FormatTelemetryTimestamp(telemetry.LastUpdatedAt)}"
                });
        }

        private static string BuildControllerTitle(WiredControllerInfo controller)
        {
            return $"{controller.Name} / 从站 {controller.SlaveId} / {(controller.IsOnline ? "在线" : "离线")} / 锁定 {(controller.ControllerLockEnabled ? "开" : "关")} / 回差 {controller.Differential} °C";
        }

        private static string FormatTelemetryTimestamp(DateTime timestamp)
        {
            return timestamp == default
                ? "--"
                : timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void RenderLineControllerGroups(IEnumerable<LineControllerGroupViewModel> groups)
        {
            if (_flpLineControllerCards == null)
            {
                return;
            }

            var groupList = groups.ToList();

            _lblLineControllerControllerCountValue.Text = groupList.Count.ToString(CultureInfo.InvariantCulture);
            _lblLineControllerModuleCountValue.Text = groupList.Sum(group => group.Cards.Count).ToString(CultureInfo.InvariantCulture);

            _flpLineControllerCards.SuspendLayout();
            _flpLineControllerCards.Controls.Clear();

            foreach (var group in groupList)
            {
                AddLineControllerGroup(group);
            }

            _flpLineControllerCards.ResumeLayout();
        }

        private void AddLineControllerGroup(LineControllerGroupViewModel group)
        {
            var section = new Panel
            {
                Width = 1120,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(0)
            };

            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                Margin = new Padding(0, 0, 0, 8)
            };
            var titleLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(group.ModuleGroupName) ? group.Title : group.Title + " / " + group.ModuleGroupName,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134),
                ForeColor = Color.FromArgb(48, 48, 48)
            };
            var editControllerButton = new Button
            {
                Text = "配置控制器",
                Dock = DockStyle.Right,
                Width = 92,
                Enabled = group.Controller != null
            };
            editControllerButton.Click += (sender, e) => EditHeatPumpController(group.Controller);
            var editGroupButton = new Button
            {
                Text = "编辑分组",
                Dock = DockStyle.Right,
                Width = 82,
                Enabled = group.Controller != null && group.Controller.Modules.Count > 0
            };
            editGroupButton.Click += (sender, e) => EditHeatPumpModuleGroups(group.Controller);
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(editControllerButton);
            titleBar.Controls.Add(editGroupButton);

            var cardFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            foreach (var card in group.Cards)
            {
                cardFlow.Controls.Add(CreateLineControllerCard(card));
            }

            section.Controls.Add(cardFlow);
            section.Controls.Add(titleBar);
            _flpLineControllerCards.Controls.Add(section);
        }

        private Control CreateLineControllerCard(LineControllerCardViewModel cardModel)
        {
            var isSelectable = cardModel.Module != null;
            var card = new Panel
            {
                Width = 340,
                Height = 208,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(12),
                Cursor = isSelectable ? Cursors.Hand : Cursors.Default
            };

            var selection = new CheckBox
            {
                Text = "选择模块",
                AutoSize = true,
                Dock = DockStyle.Right,
                Enabled = isSelectable && cardModel.Module.IsEnabled,
                Checked = isSelectable && cardModel.Module.IsEnabled && _selectedHeatPumpModuleKeys.Contains(GetHeatPumpModuleKey(cardModel.Module)),
                AccessibleName = "选择模块"
            };
            selection.CheckedChanged += (sender, e) => SetHeatPumpModuleSelected(cardModel.Module, selection.Checked);
            var editModuleButton = new Button
            {
                Text = "配置",
                AutoSize = true,
                Dock = DockStyle.Right,
                Enabled = isSelectable
            };
            editModuleButton.Click += (sender, e) => EditHeatPumpModule(cardModel.Module);

            var moduleLabel = new Label
            {
                Text = cardModel.ModuleName,
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("微软雅黑", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134),
                ForeColor = Color.FromArgb(32, 64, 96)
            };

            var subtitleLabel = new Label
            {
                Text = cardModel.Subtitle,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134),
                ForeColor = Color.DimGray
            };

            var lineContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            foreach (var line in cardModel.Lines)
            {
                lineContainer.Controls.Add(new Label
                {
                    Text = line,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 6),
                    Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134),
                    ForeColor = Color.FromArgb(60, 60, 60)
                });
            }

            if (isSelectable)
            {
                EventHandler openDetails = (sender, e) => ShowHeatPumpModuleDetails(cardModel.Module);
                card.Click += openDetails;
                moduleLabel.Click += openDetails;
                subtitleLabel.Click += openDetails;
                lineContainer.Click += openDetails;
                foreach (Control line in lineContainer.Controls)
                {
                    line.Click += openDetails;
                }
            }

            card.Controls.Add(lineContainer);
            card.Controls.Add(subtitleLabel);
            card.Controls.Add(selection);
            card.Controls.Add(editModuleButton);
            card.Controls.Add(moduleLabel);
            return card;
        }

        private static string GetHeatPumpModuleKey(HeatPumpModuleInfo module)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", module.SlaveId, module.ModuleIndex);
        }

        private void SetHeatPumpModuleSelected(HeatPumpModuleInfo module, bool isSelected)
        {
            if (module == null)
            {
                return;
            }

            var key = GetHeatPumpModuleKey(module);
            module.IsSelected = isSelected;
            if (isSelected)
            {
                _selectedHeatPumpModuleKeys.Add(key);
            }
            else
            {
                _selectedHeatPumpModuleKeys.Remove(key);
            }

            UpdateHeatPumpSelectionSummary(_heatPumpWorkspaceService.CreateSnapshot());
        }

        private void UpdateHeatPumpSelectionSummary(HeatPumpWorkspaceSnapshot snapshot)
        {
            if (_lblHeatPumpSelectionSummary == null)
            {
                return;
            }

            var selectedModules = snapshot.Controllers
                .SelectMany(controller => controller.Modules)
                .Where(module => _selectedHeatPumpModuleKeys.Contains(GetHeatPumpModuleKey(module)))
                .ToList();
            var controllerCount = selectedModules.Select(module => module.SlaveId).Distinct().Count();
            _lblHeatPumpSelectionSummary.Text = string.Format(
                CultureInfo.InvariantCulture,
                "已选择 {0} 个模块 / {1} 个控制器",
                selectedModules.Count,
                controllerCount);
        }

        private void ShowHeatPumpModuleDetails(HeatPumpModuleInfo module)
        {
            if (module == null)
            {
                return;
            }

            using (var detailsForm = new HeatPumpDetailsForm())
            {
                detailsForm.UpdateModule(module);
                detailsForm.ShowDialog(this);
            }
        }

        private void EditHeatPumpController(WiredControllerInfo controller)
        {
            if (controller == null)
            {
                return;
            }

            using (var dialog = new HeatPumpDeviceEditorDialog(controller))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    PersistHeatPumpDeviceEdits("控制器配置已保存。");
                }
            }
        }

        private void EditHeatPumpModule(HeatPumpModuleInfo module)
        {
            if (module == null)
            {
                return;
            }

            var controller = _heatPumpWorkspaceService.CreateSnapshot().Controllers
                .FirstOrDefault(item => item.SlaveId == module.SlaveId);
            if (controller == null)
            {
                return;
            }

            using (var dialog = new HeatPumpDeviceEditorDialog(controller, module))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (!module.IsEnabled)
                    {
                        _selectedHeatPumpModuleKeys.Remove(GetHeatPumpModuleKey(module));
                    }

                    PersistHeatPumpDeviceEdits("模块配置已保存。");
                }
            }
        }

        private void PersistHeatPumpDeviceEdits(string successMessage)
        {
            try
            {
                SaveHeatPumpWorkspaceConfiguration();
                RefreshLineControllerPage();
                _lblLineControllerPageStatus.Text = successMessage;
            }
            catch (Exception ex)
            {
                _lblLineControllerPageStatus.Text = "热泵配置保存失败: " + ex.Message;
            }
        }

        private void EditHeatPumpModuleGroups(WiredControllerInfo controller)
        {
            if (controller == null || controller.Modules.Count == 0)
            {
                return;
            }

            using (var dialog = new HeatPumpModuleGroupEditorDialog(controller))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    SaveHeatPumpWorkspaceConfiguration();
                    RefreshLineControllerPage();
                    _lblLineControllerPageStatus.Text = "模块分组已保存。";
                }
                catch (Exception ex)
                {
                    _lblLineControllerPageStatus.Text = "模块分组保存失败: " + ex.Message;
                }
            }
        }

        private sealed class LineControllerGroupViewModel
        {
            public LineControllerGroupViewModel(string title, string moduleGroupName, WiredControllerInfo controller, List<LineControllerCardViewModel> cards)
            {
                Title = title;
                ModuleGroupName = moduleGroupName;
                Controller = controller;
                Cards = cards;
            }

            public string Title { get; }
            public string ModuleGroupName { get; }
            public WiredControllerInfo Controller { get; }
            public List<LineControllerCardViewModel> Cards { get; }
        }

        private sealed class LineControllerCardViewModel
        {
            public LineControllerCardViewModel(string moduleName, string subtitle, HeatPumpModuleInfo module, List<string> lines)
            {
                ModuleName = moduleName;
                Subtitle = subtitle;
                Module = module;
                Lines = lines;
            }

            public string ModuleName { get; }
            public string Subtitle { get; }
            public HeatPumpModuleInfo Module { get; }
            public List<string> Lines { get; }
        }

        /// <summary>创建默认分组，主串口与仪表盘串口各自按实际连接设备生成卡片</summary>
        private void SetupDefaultBoxes()
        {
            _meterManager.AddBox("外部串口设备");
            _meterManager.AddBox("仪表盘设备");
        }

        #region Query Module

        private async Task EnsureQueryModuleLoadedAsync(bool force)
        {
            if (_queryDataLoaded && !force)
            {
                return;
            }

            try
            {
                SetQuickStatus("正在加载电表目录...");
                SetCompareStatus("正在加载电表目录...");
                var metrics = _historyQueryService.GetMetricDefinitions().ToList();
                var metersTask = _historyQueryService.GetMetersAsync();
                var treeTask = _historyQueryService.GetMeterTreeAsync();
                await Task.WhenAll(metersTask, treeTask).ConfigureAwait(true);

                _queryMeters.Clear();
                _queryMeters.AddRange(metersTask.Result);
                BindMetricCombos(metrics);
                BindQuickMeterCombo(_queryMeters);
                BindCompareTree(treeTask.Result);
                _queryDataLoaded = true;
                SetQuickStatus(_queryMeters.Count > 0 ? "电表目录已加载" : "数据库中没有可查询电表");
                SetCompareStatus(_queryMeters.Count > 0 ? "请选择 1-4 台电表进行对比" : "数据库中没有可查询电表");
            }
            catch (Exception ex)
            {
                SetQuickStatus("查询模块初始化失败: " + ex.Message, true);
                SetCompareStatus("查询模块初始化失败: " + ex.Message, true);
            }
        }

        private void BindMetricCombos(List<HistoryMetricDefinition> metrics)
        {
            cmbQuickMetric.DataSource = metrics.ToList();
            cmbCompareMetric.DataSource = metrics.ToList();
        }

        private void BindQuickMeterCombo(List<MeterLookupItem> meters)
        {
            cmbQuickMeter.DataSource = null;
            cmbQuickMeter.DataSource = meters.ToList();
            if (cmbQuickMeter.Items.Count > 0)
            {
                cmbQuickMeter.SelectedIndex = 0;
            }
        }

        private void BindCompareTree(List<SiteTreeItem> sites)
        {
            _suppressTreeAfterCheck = true;
            tvCompareMeters.BeginUpdate();
            tvCompareMeters.Nodes.Clear();
            foreach (var site in sites)
            {
                var siteNode = new TreeNode(string.Format(CultureInfo.InvariantCulture, "{0} ({1})", site.SiteName, site.SiteCode));
                foreach (var box in site.Boxes)
                {
                    var boxNode = new TreeNode(string.Format(CultureInfo.InvariantCulture, "{0} ({1})", box.BoxName, box.BoxCode));
                    foreach (var meter in box.Meters)
                    {
                        var meterNode = new TreeNode(string.Format(CultureInfo.InvariantCulture, "{0} ({1})", meter.MeterName, meter.MeterCode))
                        {
                            Tag = meter
                        };
                        boxNode.Nodes.Add(meterNode);
                    }
                    siteNode.Nodes.Add(boxNode);
                }
                tvCompareMeters.Nodes.Add(siteNode);
            }
            tvCompareMeters.ExpandAll();
            tvCompareMeters.EndUpdate();
            _suppressTreeAfterCheck = false;
        }

        private async void QuickSearchButton_Click(object sender, EventArgs e)
        {
            _quickCurrentPage = 1;
            await RunQuickHistoryQueryAsync().ConfigureAwait(true);
        }

        private async void QuickPrevButton_Click(object sender, EventArgs e)
        {
            if (_quickCurrentPage <= 1)
            {
                return;
            }

            _quickCurrentPage--;
            await RunQuickHistoryQueryAsync().ConfigureAwait(true);
        }

        private async void QuickNextButton_Click(object sender, EventArgs e)
        {
            var totalPages = GetQuickTotalPages();
            if (_quickCurrentPage >= totalPages)
            {
                return;
            }

            _quickCurrentPage++;
            await RunQuickHistoryQueryAsync().ConfigureAwait(true);
        }

        private async void QuickRefreshButton_Click(object sender, EventArgs e)
        {
            await EnsureQueryModuleLoadedAsync(true).ConfigureAwait(true);
        }

        private async Task RunQuickHistoryQueryAsync()
        {
            var meter = cmbQuickMeter.SelectedItem as MeterLookupItem;
            var metric = cmbQuickMetric.SelectedItem as HistoryMetricDefinition;
            if (meter == null || metric == null)
            {
                SetQuickStatus("请先选择电表和参数", true);
                return;
            }

            if (dtQuickStart.Value > dtQuickEnd.Value)
            {
                SetQuickStatus("开始时间不能晚于结束时间", true);
                return;
            }

            try
            {
                ToggleQuickControls(false);
                SetQuickStatus("正在查询历史数据...");
                var result = await _historyQueryService.QueryQuickHistoryAsync(meter.MeterId, metric.Key, dtQuickStart.Value, dtQuickEnd.Value, _quickCurrentPage, _quickPageSize).ConfigureAwait(true);
                _quickTotalRows = result.TotalRows;
                RenderQuickHistoryChart(meter, result, dtQuickStart.Value, dtQuickEnd.Value);
                PopulateQuickHistoryGrid(result.Rows);
                UpdateQuickPagingState();
                SetQuickStatus(string.Format(CultureInfo.InvariantCulture, "共 {0} 条，图表点数 {1}", result.TotalRows, result.ChartPoints.Count));
            }
            catch (Exception ex)
            {
                SetQuickStatus("查询失败: " + ex.Message, true);
            }
            finally
            {
                ToggleQuickControls(true);
            }
        }

        private async void CompareSearchButton_Click(object sender, EventArgs e)
        {
            if (dtCompareStart.Value > dtCompareEnd.Value)
            {
                SetCompareStatus("开始时间不能晚于结束时间", true);
                return;
            }

            var selectedMeters = GetCheckedMeters();
            if (selectedMeters.Count == 0)
            {
                SetCompareStatus("请至少勾选 1 台电表", true);
                return;
            }

            if (selectedMeters.Count > 4)
            {
                SetCompareStatus("最多勾选 4 台电表", true);
                return;
            }

            var metric = cmbCompareMetric.SelectedItem as HistoryMetricDefinition;
            if (metric == null)
            {
                SetCompareStatus("请选择对比参数", true);
                return;
            }

            try
            {
                ToggleCompareControls(false);
                SetCompareStatus("正在并发查询多表历史数据...");
                var series = await _historyQueryService.QueryComparisonAsync(selectedMeters, metric.Key, dtCompareStart.Value, dtCompareEnd.Value).ConfigureAwait(true);
                RenderCompareChart(metric, series);
                var summary = string.Join("，", series.Select(s => s.MeterName + ":" + s.Points.Count.ToString(CultureInfo.InvariantCulture) + "点"));
                SetCompareStatus(string.IsNullOrWhiteSpace(summary) ? "没有查到符合条件的数据" : summary);
            }
            catch (Exception ex)
            {
                SetCompareStatus("对比查询失败: " + ex.Message, true);
            }
            finally
            {
                ToggleCompareControls(true);
            }
        }

        private async void CompareRefreshButton_Click(object sender, EventArgs e)
        {
            await EnsureQueryModuleLoadedAsync(true).ConfigureAwait(true);
        }

        private void CompareTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_suppressTreeAfterCheck)
            {
                return;
            }

            if (e.Node.Nodes.Count > 0)
            {
                _suppressTreeAfterCheck = true;
                e.Node.Checked = false;
                _suppressTreeAfterCheck = false;
                return;
            }

            if (GetCheckedMeters().Count > 4)
            {
                _suppressTreeAfterCheck = true;
                e.Node.Checked = false;
                _suppressTreeAfterCheck = false;
                MessageBox.Show("最多勾选 4 台电表进行对比。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private List<MeterLookupItem> GetCheckedMeters()
        {
            var meters = new List<MeterLookupItem>();
            foreach (TreeNode siteNode in tvCompareMeters.Nodes)
            {
                CollectCheckedMeters(siteNode, meters);
            }
            return meters;
        }

        private void CollectCheckedMeters(TreeNode node, List<MeterLookupItem> meters)
        {
            if (node == null)
            {
                return;
            }

            if (node.Checked && node.Tag is MeterLookupItem meter)
            {
                meters.Add(meter);
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectCheckedMeters(child, meters);
            }
        }

        private void RenderQuickHistoryChart(MeterLookupItem meter, QuickHistoryResult result, DateTime startTime, DateTime endTime)
        {
            chartQuickHistory.Series.Clear();
            chartQuickHistory.Titles.Clear();
            _quickHoverActive = false;
            _lastQuickHoverIndex = -1;
            _quickHoverMetricDisplayName = result.Metric.DisplayName;
            _quickHoverMetricUnit = result.Metric.Unit ?? string.Empty;
            _quickHoverXValues = Array.Empty<double>();
            var area = chartQuickHistory.ChartAreas[0];
            area.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            area.AxisX.Title = "采集时间";
            area.AxisY.Title = result.Metric.DisplayName + (string.IsNullOrWhiteSpace(result.Metric.Unit) ? string.Empty : " (" + result.Metric.Unit + ")");
            chartQuickHistory.Titles.Add(meter.MeterName + " - " + result.Metric.DisplayName);

            var series = new Series(meter.MeterName)
            {
                ChartType = SeriesChartType.FastLine,
                BorderWidth = 2,
                XValueType = ChartValueType.DateTime
            };

            foreach (var point in result.ChartPoints)
            {
                series.Points.AddXY(point.CollectTime.ToOADate(), point.Value);
            }

            chartQuickHistory.Series.Add(series);
            _quickHoverXValues = series.Points.Select(p => p.XValue).ToArray();
            area.AxisX.Minimum = startTime.ToOADate();
            area.AxisX.Maximum = endTime.ToOADate();
            ApplyAdaptiveYAxis(area, result.ChartPoints.Select(p => p.Value));
            area.RecalculateAxesScale();
        }

        private void PopulateQuickHistoryGrid(List<HistoryGridRow> rows)
        {
            dgvQuickHistory.Rows.Clear();
            foreach (var row in rows)
            {
                dgvQuickHistory.Rows.Add(
                    row.CollectTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    row.Value.HasValue ? row.Value.Value.ToString("F4", CultureInfo.InvariantCulture) : string.Empty,
                    row.Unit ?? string.Empty,
                    row.Source ?? string.Empty);
            }
        }

        private void RenderCompareChart(HistoryMetricDefinition metric, List<ComparisonSeriesResult> seriesList)
        {
            chartCompareHistory.Series.Clear();
            chartCompareHistory.Titles.Clear();
            _compareHoverActive = false;
            _lastCompareHoverAnchorX = double.NaN;
            _compareHoverMetricDisplayName = metric.DisplayName;
            _compareHoverMetricUnit = metric.Unit ?? string.Empty;
            _compareHoverXValues.Clear();
            var compareHoverTimeline = new HashSet<double>();
            chartCompareHistory.Titles.Add("多表对比 - " + metric.DisplayName);
            var area = chartCompareHistory.ChartAreas[0];
            area.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            area.AxisX.Title = "采集时间";
            area.AxisY.Title = metric.DisplayName + (string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : " (" + metric.Unit + ")");

            foreach (var seriesResult in seriesList)
            {
                var series = new Series(seriesResult.MeterName)
                {
                    ChartType = SeriesChartType.FastLine,
                    BorderWidth = 2,
                    XValueType = ChartValueType.DateTime
                };
                foreach (var point in seriesResult.Points)
                {
                    var xValue = point.CollectTime.ToOADate();
                    series.Points.AddXY(xValue, point.Value);
                    compareHoverTimeline.Add(xValue);
                }
                chartCompareHistory.Series.Add(series);
                _compareHoverXValues[series] = series.Points.Select(p => p.XValue).ToArray();
            }

            _compareHoverTimeline = compareHoverTimeline.OrderBy(v => v).ToArray();
            ApplyAdaptiveYAxis(area, seriesList.SelectMany(s => s.Points).Select(p => p.Value));
            area.RecalculateAxesScale();
        }

        private void UpdateQuickPagingState()
        {
            var totalPages = GetQuickTotalPages();
            lblQuickPage.Text = string.Format(CultureInfo.InvariantCulture, "第 {0} 页 / 共 {1} 页（{2} 条）", _quickCurrentPage, totalPages, _quickTotalRows);
            btnQuickPrev.Enabled = _quickCurrentPage > 1;
            btnQuickNext.Enabled = _quickCurrentPage < totalPages;
        }

        private int GetQuickTotalPages()
        {
            if (_quickTotalRows <= 0)
            {
                return 1;
            }

            return Math.Max(1, (int)Math.Ceiling(_quickTotalRows / (double)_quickPageSize));
        }

        private void ToggleQuickControls(bool enabled)
        {
            cmbQuickMeter.Enabled = enabled;
            cmbQuickMetric.Enabled = enabled;
            dtQuickStart.Enabled = enabled;
            dtQuickEnd.Enabled = enabled;
            btnQuickSearch.Enabled = enabled;
            btnQuickRefresh.Enabled = enabled;
            btnQuickPrev.Enabled = enabled;
            btnQuickNext.Enabled = enabled;
        }

        private void ToggleCompareControls(bool enabled)
        {
            cmbCompareMetric.Enabled = enabled;
            dtCompareStart.Enabled = enabled;
            dtCompareEnd.Enabled = enabled;
            btnCompareSearch.Enabled = enabled;
            btnCompareRefresh.Enabled = enabled;
            tvCompareMeters.Enabled = enabled;
        }

        private void SetQuickStatus(string text, bool isError)
        {
            if (!_quickHoverActive)
            {
                _quickBaseStatusText = text;
                _quickBaseStatusIsError = isError;
            }

            lblQuickStatus.Text = text;
            lblQuickStatus.ForeColor = isError ? Color.OrangeRed : Color.DimGray;
        }

        private void SetQuickStatus(string text)
        {
            SetQuickStatus(text, false);
        }

        private void SetCompareStatus(string text, bool isError)
        {
            if (!_compareHoverActive)
            {
                _compareBaseStatusText = text;
                _compareBaseStatusIsError = isError;
            }

            lblCompareStatus.Text = text;
            lblCompareStatus.ForeColor = isError ? Color.OrangeRed : Color.DimGray;
        }

        private void SetCompareStatus(string text)
        {
            SetCompareStatus(text, false);
        }

        private void ChartQuickHistory_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryGetHoverXValue(chartQuickHistory, e.Location, out var xValue) || chartQuickHistory.Series.Count == 0)
            {
                RestoreQuickHoverStatus();
                return;
            }

            var series = chartQuickHistory.Series[0];
            var nearestIndex = FindNearestIndex(_quickHoverXValues, xValue);
            if (nearestIndex < 0 || nearestIndex >= series.Points.Count)
            {
                RestoreQuickHoverStatus();
                return;
            }

            if (_quickHoverActive && nearestIndex == _lastQuickHoverIndex)
            {
                return;
            }

            _quickHoverActive = true;
            _lastQuickHoverIndex = nearestIndex;
            var point = series.Points[nearestIndex];
            var hoverTime = DateTime.FromOADate(point.XValue);
            var unitSuffix = string.IsNullOrWhiteSpace(_quickHoverMetricUnit) ? string.Empty : " " + _quickHoverMetricUnit;
            SetQuickStatus(string.Format(CultureInfo.InvariantCulture, "悬停时间: {0}，{1}: {2:F4}{3}", hoverTime, _quickHoverMetricDisplayName, point.YValues.FirstOrDefault(), unitSuffix));
        }

        private void ChartQuickHistory_MouseLeave(object sender, EventArgs e)
        {
            RestoreQuickHoverStatus();
        }

        private void ChartCompareHistory_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryGetHoverXValue(chartCompareHistory, e.Location, out var xValue) || chartCompareHistory.Series.Count == 0 || _compareHoverTimeline.Length == 0)
            {
                RestoreCompareHoverStatus();
                return;
            }

            var anchorX = FindNearestExactHoverTime(xValue);
            if (double.IsNaN(anchorX))
            {
                RestoreCompareHoverStatus();
                return;
            }

            if (_compareHoverActive && Math.Abs(anchorX - _lastCompareHoverAnchorX) < 1d / 86400d)
            {
                return;
            }

            _compareHoverActive = true;
            _lastCompareHoverAnchorX = anchorX;
            var hoverTime = DateTime.FromOADate(anchorX);
            var unitSuffix = string.IsNullOrWhiteSpace(_compareHoverMetricUnit) ? string.Empty : " " + _compareHoverMetricUnit;
            var parts = new List<string>
            {
                "时间: " + hoverTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            };

            foreach (var series in chartCompareHistory.Series.Cast<Series>())
            {
                var value = GetSeriesValueAtExactTime(series, anchorX);
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1:F4}{2}", series.Name, value, unitSuffix));
            }

            SetCompareStatus(string.Join(" | ", parts));
        }

        private void ChartCompareHistory_MouseLeave(object sender, EventArgs e)
        {
            RestoreCompareHoverStatus();
        }

        private void RestoreQuickHoverStatus()
        {
            if (!_quickHoverActive)
            {
                return;
            }

            _quickHoverActive = false;
            _lastQuickHoverIndex = -1;
            SetQuickStatus(_quickBaseStatusText, _quickBaseStatusIsError);
        }

        private void RestoreCompareHoverStatus()
        {
            if (!_compareHoverActive)
            {
                return;
            }

            _compareHoverActive = false;
            _lastCompareHoverAnchorX = double.NaN;
            SetCompareStatus(_compareBaseStatusText, _compareBaseStatusIsError);
        }

        private static bool TryGetHoverXValue(Chart chart, Point location, out double xValue)
        {
            xValue = double.NaN;
            if (chart == null || chart.ChartAreas.Count == 0)
            {
                return false;
            }

            var hit = chart.HitTest(location.X, location.Y);
            if (hit == null || hit.ChartArea == null)
            {
                return false;
            }

            if (hit.ChartElementType != ChartElementType.PlottingArea &&
                hit.ChartElementType != ChartElementType.DataPoint &&
                hit.ChartElementType != ChartElementType.Gridlines)
            {
                return false;
            }

            var area = chart.ChartAreas[0];
            try
            {
                xValue = area.AxisX.PixelPositionToValue(location.X);
                return !double.IsNaN(xValue) && !double.IsInfinity(xValue);
            }
            catch
            {
                return false;
            }
        }

        private int FindNearestIndex(double[] values, double target)
        {
            if (values == null || values.Length == 0)
            {
                return -1;
            }

            var index = Array.BinarySearch(values, target);
            if (index >= 0)
            {
                return index;
            }

            var next = ~index;
            if (next <= 0)
            {
                return 0;
            }

            if (next >= values.Length)
            {
                return values.Length - 1;
            }

            var previous = next - 1;
            return Math.Abs(values[previous] - target) <= Math.Abs(values[next] - target) ? previous : next;
        }

        private double FindNearestExactHoverTime(double xValue)
        {
            if (_compareHoverTimeline.Length == 0)
            {
                return double.NaN;
            }

            var nearestIndex = FindNearestIndex(_compareHoverTimeline, xValue);
            return nearestIndex >= 0 && nearestIndex < _compareHoverTimeline.Length ? _compareHoverTimeline[nearestIndex] : double.NaN;
        }

        private double GetSeriesValueAtExactTime(Series series, double exactXValue)
        {
            if (series == null || !_compareHoverXValues.TryGetValue(series, out var seriesXValues))
            {
                return 0d;
            }

            var exactIndex = Array.BinarySearch(seriesXValues, exactXValue);
            if (exactIndex < 0 || exactIndex >= series.Points.Count)
            {
                return 0d;
            }

            return series.Points[exactIndex].YValues.FirstOrDefault();
        }

        #endregion

        #region Dashboard

        private void BtnSecondConnect_Click(object sender, EventArgs e)
        {
            if (cmbSecondPort.SelectedItem == null)
            {
                MessageBox.Show("请选择串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ApplyMeterCommunicationConfig(_modbusService2, _meterCommunicationConfig);
            _modbusService2.Connect(cmbSecondPort.SelectedItem.ToString());
        }

        private void BtnSecondDisconnect_Click(object sender, EventArgs e)
        {
            _modbusService2.Disconnect();
        }

        private void OnSecondConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke((MethodInvoker)(() => UpdateSecondConnectionUI(e.IsConnected, e.Message)));
                return;
            }
            UpdateSecondConnectionUI(e.IsConnected, e.Message);
        }

        private void UpdateSecondConnectionUI(bool isConnected, string message)
        {
            btnSecondConnect.Enabled = !isConnected;
            btnSecondDisconnect.Enabled = isConnected;
            cmbSecondPort.Enabled = !isConnected;
            if (nudScanStart != null) nudScanStart.Enabled = !isConnected;
            if (nudScanEnd != null) nudScanEnd.Enabled = !isConnected;

            if (isConnected)
            {
                _refreshTimer.Start();
                lblSecondStatusDashboard.Text = "● 已连接";
                lblSecondStatusDashboard.ForeColor = Color.Green;
            }
            else
            {
                if (!_modbusService.IsConnected) _refreshTimer.Stop();
                ClearDashboardMeters();
                _meterManager.ClearData();
                RebuildDashboardPanels();
                ResizeDashboardPanels();
                UpdateAllCardLabels();
                try
                {
                    PublishBoxRegistryAsync(GetOrCreateDashboardBox(), "scan", ThreadingCancellationToken.None).GetAwaiter().GetResult();
                }
                catch
                {
                }
                lblSecondStatusDashboard.Text = message;
                lblSecondStatusDashboard.ForeColor = Color.Red;
            }
        }

        private void OnSecondCommunicationError(object sender, Exception ex)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        lblSecondStatusDashboard.Text = "错误: " + ex.Message;
                        lblSecondStatusDashboard.ForeColor = Color.Orange;
                    }));
                return;
            }
            lblSecondStatusDashboard.Text = "错误: " + ex.Message;
            lblSecondStatusDashboard.ForeColor = Color.Orange;
        }

        private void BtnAddBox_Click(object sender, EventArgs e)
        {
            _boxCounter++;
            var dashBox = GetOrCreateDashboardBox();
            var meter = _meterManager.AddMeterToBox(dashBox, "E" + _boxCounter.ToString("D3"), "仪表 " + _boxCounter, dashBox.Name, 1);
            meter.IsToolbar = false;
            meter.DeviceModel = _meterDataService2.DeviceModel;
            RebuildDashboardPanels();
            ResizeDashboardPanels();
        }

        private async void BtnAutoScan_Click(object sender, EventArgs e)
        {
            if (!_modbusService2.IsConnected)
            {
                MessageBox.Show("请先连接仪表盘串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte start = (byte)nudScanStart.Value;
            byte end = (byte)nudScanEnd.Value;
            if (start > end)
            {
                MessageBox.Show("扫描起始地址不能大于结束地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAddMeter.Enabled = false;
            btnSecondConnect.Enabled = false;
            btnAutoScanDashboard.Enabled = false;
            lblSecondStatusDashboard.Text = "正在扫描 " + start + "~" + end + " ...";
            lblSecondStatusDashboard.ForeColor = Color.Blue;
            AppLogger.Info("Scan", "开始扫描仪表盘串口地址 " + start + "~" + end + "。");

            // P0-1: 原实现中 await PublishBoxRegistryAsync(...) 没有任何 try/catch，
            // Broker 不可达时会在 async void 里抛出未处理异常；同时按钮的重新启用也在正常路径上，
            // 一旦中途抛出，三个按钮会永久卡在禁用状态。现在整体 try/catch/finally。
            try
            {
                var discovered = new List<byte>();
                await Task.Run(() =>
                {
                    for (int addr = start; addr <= end; addr++)
                    {
                        try
                        {
                            if (_meterDataService2.Probe((byte)addr))
                                lock (discovered) discovered.Add((byte)addr);
                        }
                        catch (Exception probeEx)
                        {
                            AppLogger.Debug("Scan", "探测从站 " + addr + " 异常: " + probeEx.Message);
                        }
                    }
                });

                lblSecondStatusDashboard.Text = "● 已连接 (发现 " + discovered.Count + " 台)";
                lblSecondStatusDashboard.ForeColor = Color.Green;
                AppLogger.Info(
                    "Scan",
                    "扫描完成，发现 " + discovered.Count + " 台: " +
                    (discovered.Count == 0 ? "(无)" : string.Join(",", discovered.OrderBy(a => a))));

                if (discovered.Count == 0)
                {
                    ClearDashboardMeters();
                    RebuildDashboardPanels();
                    ResizeDashboardPanels();
                    await PublishBoxRegistryAsync(GetOrCreateDashboardBox(), "scan", ThreadingCancellationToken.None);
                    MessageBox.Show("未在 " + start + "~" + end + " 范围内发现设备", "扫描完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ReplaceDashboardMeters(discovered.OrderBy(a => a).ToList());
                RebuildDashboardPanels();
                ResizeDashboardPanels();
                UpdateAllCardLabels();
                await PublishBoxRegistryAsync(GetOrCreateDashboardBox(), "scan", ThreadingCancellationToken.None);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Scan", "自动扫描过程中发生异常。", ex);
                lblSecondStatusDashboard.Text = "扫描/档案同步失败: " + ex.Message;
                lblSecondStatusDashboard.ForeColor = Color.OrangeRed;
                MessageBox.Show(
                    "扫描已中断：" + ex.Message + "\n\n设备列表可能未同步到平台，请检查 MQTT 连接后重试。",
                    "扫描失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                btnAddMeter.Enabled = true;
                btnSecondConnect.Enabled = true;
                btnAutoScanDashboard.Enabled = true;
            }
        }

        private async void DeleteMeterCard_Click(object sender, EventArgs e)
        {
            try
            {
                var btn = (Button)sender;
                var meter = (MeterInfo)btn.Tag;
                if (MessageBox.Show("确定删除电表 " + meter.Name + "？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                AppLogger.Info("Dashboard", "删除电表卡片: " + meter.Id + " (" + meter.Name + ", 从站 " + meter.SlaveAddress + ")。");
                _meterManager.RemoveMeter(meter.Id);
                RebuildDashboardPanels();
                ResizeDashboardPanels();

                try
                {
                    await PublishDisableRegistryAsync(meter, ThreadingCancellationToken.None);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Dashboard", "删除后档案同步失败: " + meter.Id, ex);
                    lblSecondStatusDashboard.Text = "档案同步失败: " + ex.Message;
                    lblSecondStatusDashboard.ForeColor = Color.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                HandleHandlerException("删除电表卡片", ex);
            }
        }

        private void ShowMeterDetails(string meterId)
        {
            if (string.IsNullOrWhiteSpace(meterId))
                return;

            if (_detailForms.TryGetValue(meterId, out MeterDetailsForm existingForm))
            {
                if (!existingForm.IsDisposed)
                {
                    existingForm.Show();
                    existingForm.BringToFront();
                    existingForm.Activate();
                    existingForm.RefreshFromMeter();
                    return;
                }

                _detailForms.Remove(meterId);
            }

            var form = new MeterDetailsForm(_meterManager, meterId);
            form.FormClosed += (s, e) => _detailForms.Remove(meterId);
            _detailForms[meterId] = form;
            form.Show(this);
        }

        private void MeterCard_Click(object sender, EventArgs e)
        {
            Control current = sender as Control;
            while (current != null)
            {
                if (current is Panel panel && panel.Tag != null)
                {
                    ShowMeterDetails(panel.Name);
                    return;
                }

                current = current.Parent;
            }
        }

        private DistributionBox GetOrCreateToolbarBox()
        {
            return _meterManager.Boxes.FirstOrDefault(b => b.Name == "外部串口设备") ?? _meterManager.AddBox("外部串口设备");
        }

        private DistributionBox GetOrCreateDashboardBox()
        {
            return _meterManager.Boxes.FirstOrDefault(b => b.Name == "仪表盘设备") ?? _meterManager.AddBox("仪表盘设备");
        }

        private void ReplaceToolbarMeter(byte slaveAddress)
        {
            var toolbarBox = GetOrCreateToolbarBox();
            toolbarBox.Meters.Clear();

            var meter = _meterManager.AddMeterToBox(toolbarBox, "MAIN-" + slaveAddress, "外串 " + slaveAddress, toolbarBox.Name, slaveAddress);
            meter.IsToolbar = true;

            try
            {
                PublishBoxRegistryAsync(toolbarBox, "replace", ThreadingCancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "档案同步失败: " + ex.Message;
                lblStatus.ForeColor = Color.OrangeRed;
            }
        }

        private void ClearDashboardMeters()
        {
            var dashBox = GetOrCreateDashboardBox();
            dashBox.Meters.Clear();
        }

        private void ReplaceDashboardMeters(List<byte> addresses)
        {
            var dashBox = GetOrCreateDashboardBox();
            dashBox.Meters.Clear();

            foreach (var addr in addresses)
            {
                _boxCounter++;
                var meter = _meterManager.AddMeterToBox(dashBox, "DASH-" + addr, "仪表 " + addr, dashBox.Name, addr);
                meter.IsToolbar = false;
            }
        }

        private async Task PublishBoxRegistryAsync(DistributionBox box, string action, ThreadingCancellationToken cancellationToken)
        {
            if (box == null)
            {
                return;
            }

            var message = new MeterRegistryMessage
            {
                SiteCode = _siteCode,
                BoxCode = ResolveBoxCode(box),
                BoxName = box.Name,
                Action = action,
                ScanTime = GetBeijingNow(),
                Meters = box.Meters
                    .Select(BuildRegistryItem)
                    .OrderBy(item => item.SlaveAddress)
                    .ThenBy(item => item.MeterCode)
                    .ToList()
            };

            await _mqttPublisherService.PublishRegistryAsync(message, cancellationToken).ConfigureAwait(false);
        }

        private async Task PublishDisableRegistryAsync(MeterInfo meter, ThreadingCancellationToken cancellationToken)
        {
            if (meter == null)
            {
                return;
            }

            var message = new MeterRegistryMessage
            {
                SiteCode = _siteCode,
                BoxCode = meter.IsToolbar ? _toolbarBoxCode : _dashboardBoxCode,
                BoxName = meter.Location,
                Action = "disable",
                ScanTime = GetBeijingNow(),
                Meters = new List<MeterRegistryItem> { BuildRegistryItem(meter) }
            };

            await _mqttPublisherService.PublishRegistryAsync(message, cancellationToken).ConfigureAwait(false);
        }

        private MeterRegistryItem BuildRegistryItem(MeterInfo meter)
        {
            return new MeterRegistryItem
            {
                MeterCode = meter.Id,
                MeterName = meter.Name,
                Location = meter.Location,
                SlaveAddress = meter.SlaveAddress,
                IsToolbar = meter.IsToolbar,
                DeviceModel = meter.DeviceModel
            };
        }

        private string ResolveBoxCode(DistributionBox box)
        {
            return box != null && box.Name == "外部串口设备" ? _toolbarBoxCode : _dashboardBoxCode;
        }

        private void ResizeDashboardPanels()
        {
            if (_flpBoxContainer == null) return;

            int availableWidth = _flpBoxContainer.ClientSize.Width - _flpBoxContainer.Padding.Horizontal;
            if (availableWidth < 320)
                availableWidth = 320;

            _flpBoxContainer.SuspendLayout();
            foreach (Control ctrl in _flpBoxContainer.Controls)
            {
                ctrl.Margin = new Padding(0, 0, 0, 12);
                ctrl.MaximumSize = new Size(availableWidth, 0);
                ctrl.MinimumSize = new Size(availableWidth, 0);
                ctrl.Width = availableWidth;

                if (ctrl is TableLayoutPanel section && section.Controls.Count > 1 && section.Controls[1] is FlowLayoutPanel cardsPanel)
                {
                    cardsPanel.MaximumSize = new Size(availableWidth, 0);
                    cardsPanel.MinimumSize = new Size(availableWidth, 0);
                    cardsPanel.Width = availableWidth;
                }
            }
            _flpBoxContainer.ResumeLayout(true);
        }

        private void RebuildDashboardPanels()
        {
            _flpBoxContainer.SuspendLayout();
            _flpBoxContainer.Controls.Clear();

            var allMeters = _meterManager.AllMeters;
            _meterOverview?.RebuildMeters(allMeters);
            var realMeters = allMeters.Where(m => m.IsToolbar).ToList();
            var dashMeters = allMeters.Where(m => !m.IsToolbar).ToList();

            if (realMeters.Count > 0)
                _flpBoxContainer.Controls.Add(CreateDashboardSection("■ 外部串口（真实串口）— " + realMeters.Count + " 台设备", realMeters));

            if (realMeters.Count > 0 && dashMeters.Count > 0)
                _flpBoxContainer.Controls.Add(CreateDashboardSeparator());

            if (dashMeters.Count > 0)
                _flpBoxContainer.Controls.Add(CreateDashboardSection("■ 仪表盘扫描串口 — " + dashMeters.Count + " 台设备", dashMeters));

            _flpBoxContainer.ResumeLayout(true);
            ResizeDashboardPanels();
        }

        private Control CreateDashboardSection(string titleText, List<MeterInfo> meters)
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
            };
            foreach (var meter in meters)
                cardsPanel.Controls.Add(BuildMeterCard(meter));

            section.Controls.Add(cardsPanel, 0, 1);
            return section;
        }

        private Control CreateDashboardSeparator()
        {
            return new Panel
            {
                Height = 2,
                BackColor = Color.FromArgb(200, 200, 200),
                Margin = new Padding(0, 4, 0, 8)
            };
        }

        internal Panel BuildMeterCard(MeterInfo meter)
        {
            var card = new Panel
            {
                Width = 320,
                Height = 220,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(14, 12, 14, 12),
                Name = meter.Id,
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
                ColumnCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var txtName = new TextBox
            {
                Text = meter.Name,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 0),
                BackColor = Color.White,
                MinimumSize = new Size(0, 24),
                Tag = meter,
            };
            txtName.TextChanged += (s, ev) =>
            {
                var tb = (TextBox)s;
                var m = (MeterInfo)tb.Tag;
                var name = tb.Text.Trim();
                m.Name = string.IsNullOrWhiteSpace(name) ? "未命名" : name;
            };
            header.Controls.Add(txtName, 0, 0);

            var lblId = new Label
            {
                Text = "ID:",
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 6, 0),
            };
            header.Controls.Add(lblId, 1, 0);

            var cmbId = new ComboBox
            {
                Width = 62,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9),
                Tag = meter,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };
            for (int i = 1; i <= 247; i++)
            {
                cmbId.Items.Add(i);
            }

            cmbId.SelectedItem = (int)meter.SlaveAddress;
            cmbId.SelectedIndexChanged += (s, ev) =>
            {
                var cb = (ComboBox)s;
                var m = (MeterInfo)cb.Tag;
                if (cb.SelectedItem != null)
                {
                    m.SlaveAddress = (byte)(int)cb.SelectedItem;
                }
            };
            header.Controls.Add(cmbId, 2, 0);
            layout.Controls.Add(header, 0, 0);

            header.Click += MeterCard_Click;
            lblId.Click += MeterCard_Click;

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

            var lblPowerTitle = new Label
            {
                Text = "三相功率",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
            };
            metrics.Controls.Add(lblPowerTitle, 0, 0);

            var lblPower = new Label
            {
                Text = "-- kW",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 140, 255),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14),
            };
            metrics.Controls.Add(lblPower, 0, 1);
            metrics.SetColumnSpan(lblPower, 2);

            var lblCurrentTitle = new Label
            {
                Text = "最大电流",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 8, 0),
            };
            metrics.Controls.Add(lblCurrentTitle, 0, 2);

            var lblCurrent = new Label
            {
                Text = "-- A",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleRight,
            };
            metrics.Controls.Add(lblCurrent, 1, 2);
            layout.Controls.Add(metrics, 0, 2);

            var bottomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                ColumnCount = meter.IsToolbar ? 1 : 2,
                Margin = new Padding(0, 12, 0, 0),
                Padding = new Padding(0),
            };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (!meter.IsToolbar)
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

            if (!meter.IsToolbar)
            {
                var btnDelete = new Button
                {
                    Text = "删除",
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    MinimumSize = new Size(58, 28),
                    Font = new Font("微软雅黑", 8),
                    ForeColor = Color.Red,
                    Tag = meter,
                    Margin = new Padding(8, 0, 0, 0),
                    Anchor = AnchorStyles.Right,
                };
                btnDelete.Click += DeleteMeterCard_Click;
                bottomRow.Controls.Add(btnDelete, 1, 0);
            }

            layout.Controls.Add(bottomRow, 0, 3);

            card.Click += MeterCard_Click;
            layout.Click += MeterCard_Click;
            metrics.Click += MeterCard_Click;
            lblPowerTitle.Click += MeterCard_Click;
            lblPower.Click += MeterCard_Click;
            lblCurrentTitle.Click += MeterCard_Click;
            lblCurrent.Click += MeterCard_Click;
            lblStatus.Click += MeterCard_Click;
            line.Click += MeterCard_Click;

            card.Tag = new { PowerLabel = lblPower, CurrentLabel = lblCurrent, StatusLabel = lblStatus };
            return card;
        }

        #endregion

        #region Data Update

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_isRefreshing) return;
            if (_isShuttingDown) return;
            if (!_modbusService.IsConnected && !_modbusService2.IsConnected) return;

            _isRefreshing = true;
            try
            {
                _refreshTickCount++;
                // P1-7：采样是否到期改为按时钟判断，不再用 tick 计数取模。
                // 原实现的问题：_isRefreshing 跳过的 tick 不计数，一轮轮询超过 1 秒
                // （多台表 × 最长 500ms 超时，很容易）就会整体漂移 ——
                // EnergyIntervalSeconds=60 实际可能变成 120、180 秒，配置值与真实行为对不上。
                var nowUtc = DateTime.UtcNow;
                var readRealTime = IsSampleDue(ref _lastRealtimeSampleUtc, nowUtc, _realTimeIntervalSeconds);
                var readEnergy = IsSampleDue(ref _lastEnergySampleUtc, nowUtc, _energyIntervalSeconds);
                var readQuality = IsSampleDue(ref _lastQualitySampleUtc, nowUtc, _qualityIntervalSeconds);
                RealTimeData rtData = null;
                EnergyData enData = null;
                PowerQualityData qlData = null;
                List<MeterPollResult> pollResults = null;

                await Task.Run(() =>
                {
                    if (_modbusService.IsConnected)
                    {
                        if (readRealTime)
                        {
                            rtData = _meterDataService.ReadRealTimeData();
                        }

                        if (readEnergy)
                        {
                            enData = _meterDataService.ReadEnergyData();
                        }

                        if (readQuality)
                        {
                            qlData = _meterDataService.ReadPowerQualityData();
                        }
                    }

                    pollResults = _meterManager.PollAll(readRealTime, readEnergy, readQuality);
                });

                if (_isShuttingDown) return;

                if (readRealTime)
                {
                    MeterGridRenderer.UpdateRealTimeGrid(dgvRealTime, rtData);
                }

                if (readEnergy)
                {
                    MeterGridRenderer.UpdateEnergyGrid(dgvEnergy, enData);
                }

                if (readQuality)
                {
                    MeterGridRenderer.UpdateQualityGrid(dgvQuality, qlData);
                }

                await PublishAllMetersAsync(pollResults, rtData, enData, qlData, readRealTime, readEnergy, readQuality);
                UpdateAllCardLabels();
            }
            catch (ObjectDisposedException ex)
            {
                // 退出过程中串口/客户端已释放，属于预期竞态，不打扰用户（P0-5 已尽量避免）。
                AppLogger.Debug("Poll", "采集周期在资源释放后被打断: " + ex.Message);
            }
            catch (Exception ex)
            {
                // P0-1: 这里原来没有 catch —— 报文解析越界、界面渲染、MQTT 发布的任何异常
                // 都会从 async void 冒泡成未处理异常。采集主循环绝不允许因单次失败而终止。
                HandleHandlerException("定时采集", ex);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// P1-7：按时钟判断某类采样是否到期。到期即记录本次时刻并返回 true。
        /// 首次调用（字段为 MinValue）一定到期，保证连接后立刻采一轮。
        /// </summary>
        private static bool IsSampleDue(ref DateTime lastUtc, DateTime nowUtc, int intervalSeconds)
        {
            var interval = TimeSpan.FromSeconds(intervalSeconds < 1 ? 1 : intervalSeconds);
            if (lastUtc != DateTime.MinValue && nowUtc - lastUtc < interval)
            {
                return false;
            }

            lastUtc = nowUtc;
            return true;
        }

        private void UpdateAllCardLabels()
        {
            if (_flpBoxContainer == null) return;

            var allMeters = _meterManager.AllMeters;
            _meterOverview?.RefreshMeterValues(allMeters);
            foreach (Control card in EnumerateMeterCards(_flpBoxContainer))
            {
                var meter = allMeters.Find(m => m.Id == card.Name);
                if (meter == null) continue;

                dynamic tags = card.Tag;
                UpdateMeterCardLabels(tags, meter);
            }
        }

        private IEnumerable<Control> EnumerateMeterCards(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Panel panel && panel.Tag != null)
                    yield return panel;

                foreach (var nested in EnumerateMeterCards(child))
                    yield return nested;
            }
        }

        private void UpdateMeterCardLabels(dynamic tags, MeterInfo meter)
        {
            var (statusText, statusColor, isOffline) = GetMeterDisplayStatus(meter);
            if (!isOffline && meter.RealTime != null)
            {
                var powerKw = meter.RealTime.ActivePowerTotal.HasValue
                    ? meter.RealTime.ActivePowerTotal.Value / 1000f
                    : (float?)null;
                var powerText = FormatMeasure(powerKw, "F1") + " kW";
                var currentText = FormatMeasure(GetMaxCurrent(meter.RealTime), "F1") + " A";

                if (tags.PowerLabel.Text != powerText)
                    tags.PowerLabel.Text = powerText;
                if (tags.CurrentLabel.Text != currentText)
                    tags.CurrentLabel.Text = currentText;
            }
            else
            {
                tags.PowerLabel.Text = "-- kW";
                tags.CurrentLabel.Text = "-- A";
            }

            if (tags.StatusLabel.Text != statusText)
                tags.StatusLabel.Text = statusText;
            if (tags.StatusLabel.ForeColor != statusColor)
                tags.StatusLabel.ForeColor = statusColor;
        }

        private async Task PublishAllMetersAsync(List<MeterPollResult> pollResults, RealTimeData toolbarRealTime, EnergyData toolbarEnergy, PowerQualityData toolbarQuality, bool includeRealTime, bool includeEnergy, bool includeQuality)
        {
            if (pollResults == null || pollResults.Count == 0)
            {
                return;
            }

            var collectTime = GetBeijingNow();
            foreach (var result in pollResults)
            {
                var meter = result.Meter;
                if (meter == null)
                {
                    continue;
                }

                if (meter.IsToolbar)
                {
                    result.RealTime = includeRealTime ? toolbarRealTime : null;
                    result.Energy = includeEnergy ? toolbarEnergy : null;
                    result.Quality = includeQuality ? toolbarQuality : null;
                }

                var message = BuildTelemetryMessage(meter, result, collectTime, includeRealTime, includeEnergy, includeQuality);
                if (message.RealTime == null && message.Energy == null && message.Quality == null)
                {
                    continue;
                }

                try
                {
                    await _mqttPublisherService.PublishAsync(message);
                    ClearMqttPublishError();
                }
                catch (Exception ex)
                {
                    ShowMqttPublishError(ex);
                    return;
                }
            }
        }

        private MeterTelemetryMessage BuildTelemetryMessage(MeterInfo meter, MeterPollResult result, DateTimeOffset collectTime, bool includeRealTime, bool includeEnergy, bool includeQuality)
        {
            return new MeterTelemetryMessage
            {
                MessageType = "meter.telemetry.v1",
                MessageId = Guid.NewGuid(),
                SiteCode = _siteCode,
                BoxCode = meter.IsToolbar ? _toolbarBoxCode : _dashboardBoxCode,
                MeterCode = meter.Id,
                MeterName = meter.Name,
                Location = meter.Location,
                SlaveAddress = meter.SlaveAddress,
                IsToolbar = meter.IsToolbar,
                DeviceModel = meter.DeviceModel,
                Source = "mqtt",
                CollectTime = collectTime,
                SampleType = BuildSampleType(includeRealTime, includeEnergy, includeQuality),
                RealTime = includeRealTime ? result.RealTime : null,
                Energy = includeEnergy ? result.Energy : null,
                Quality = includeQuality ? result.Quality : null
            };
        }

        private void ShowMqttPublishError(Exception ex)
        {
            _mqttPublishErrorActive = true;
            lblStatus.Text = "MQTT 发布失败: " + ex.Message;
            lblStatus.ForeColor = Color.OrangeRed;

            if (_mqttPublishErrorShown)
            {
                return;
            }

            _mqttPublishErrorShown = true;
            MessageBox.Show(
                "MQTT 发布失败，程序将继续运行并在下次发布时自动重连。\n\n" + ex.Message,
                "MQTT 连接异常",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ClearMqttPublishError()
        {
            if (!_mqttPublishErrorActive)
            {
                return;
            }

            _mqttPublishErrorActive = false;
            _mqttPublishErrorShown = false;

            if (_modbusService.IsConnected)
            {
                var portName = cmbPort.SelectedItem?.ToString() ?? cmbPort.Text;
                lblStatus.Text = "已连接 " + portName + " (地址: " + txtAddr.Text + ")";
                lblStatus.ForeColor = Color.Green;
                return;
            }

            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Red;
        }

        private static int GetPositiveIntAppSetting(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
        }

        private static readonly Lazy<ThresholdProvider> LazyThresholdProvider =
            new Lazy<ThresholdProvider>(CreateThresholdProvider, true);

        /// <summary>
        /// 判定阈值提供者（P1-1）。全进程一份，带缓存与断库兜底。
        /// 界面各处、详情窗口都从这里取，保证同一时刻用的是同一套阈值。
        /// </summary>
        internal static ThresholdProvider SharedThresholdProvider
        {
            get { return LazyThresholdProvider.Value; }
        }

        private static ThresholdProvider CreateThresholdProvider()
        {
            string connectionString = null;
            foreach (var name in new[] { "MeterDb", "MeterAcquisition" })
            {
                var settings = ConfigurationManager.ConnectionStrings[name];
                if (settings != null && !string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    connectionString = settings.ConnectionString;
                    break;
                }
            }

            var refreshSeconds = GetPositiveIntAppSetting("ThresholdRefreshSeconds", 300);
            return new ThresholdProvider(connectionString, TimeSpan.FromSeconds(refreshSeconds));
        }

        /// <summary>
        /// `async void` 事件处理器的统一兜底（P0-1）。
        ///
        /// WinForms 里 async void 处理器抛出的异常会被抛回同步上下文，
        /// 在没有全局兜底时表现为一个没有任何上下文的"未处理异常"对话框，用户点错就退出、且不留痕迹。
        /// 这里保证：一定落日志、状态栏有提示、程序继续运行。
        /// </summary>
        private void HandleHandlerException(string context, Exception ex)
        {
            AppLogger.Error("UI", "事件处理器 " + context + " 发生异常。", ex);

            try
            {
                lblStatus.Text = context + " 失败: " + ex.Message;
                lblStatus.ForeColor = Color.OrangeRed;
            }
            catch
            {
                // 控件已释放（例如窗体正在关闭）时忽略。
            }
        }

        /// <summary>
        /// 供 `async` lambda 事件处理器使用的兜底包装（P0-1）。
        /// </summary>
        private async Task RunGuardedAsync(string context, Func<Task> body)
        {
            try
            {
                await body().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                HandleHandlerException(context, ex);
            }
        }

        private static string GetMeterProtocol()
        {
            var protocol = ConfigurationManager.AppSettings["MeterProtocol"];
            return string.Equals(protocol, "Amc96lE4", StringComparison.OrdinalIgnoreCase) ? "Amc96lE4" : "Legacy";
        }

        private static CommunicationConfig GetMeterCommunicationConfig(string protocol)
        {
            var prefix = protocol == "Amc96lE4" ? "Amc96lE4Meter" : "LegacyMeter";
            return new CommunicationConfig
            {
                BaudRate = GetPositiveIntAppSetting(prefix + "BaudRate", 9600),
                DataBits = GetPositiveIntAppSetting(prefix + "DataBits", 8),
                Parity = ParseParity(ConfigurationManager.AppSettings[prefix + "Parity"], protocol == "Amc96lE4" ? Parity.None : Parity.Even),
                StopBits = ParseStopBits(ConfigurationManager.AppSettings[prefix + "StopBits"], StopBits.One)
            };
        }

        private static Parity ParseParity(string value, Parity defaultValue)
        {
            return Enum.TryParse(value, true, out Parity parity) ? parity : defaultValue;
        }

        private static StopBits ParseStopBits(string value, StopBits defaultValue)
        {
            return Enum.TryParse(value, true, out StopBits stopBits) ? stopBits : defaultValue;
        }

        private static void ApplyMeterCommunicationConfig(ModbusService service, CommunicationConfig config)
        {
            service.Config.BaudRate = config.BaudRate;
            service.Config.DataBits = config.DataBits;
            service.Config.Parity = config.Parity;
            service.Config.StopBits = config.StopBits;
            service.Config.SlaveAddress = config.SlaveAddress;
        }

        private IMeterDataReader CreateMeterDataReader(ModbusService service)
        {
            return _meterProtocol == "Amc96lE4"
                ? (IMeterDataReader)new Amc96lE4MeterDataReader(service)
                : new MeterDataService(service);
        }

        private static byte? DiscoverMeterAddress(ModbusService service, IMeterDataReader reader)
        {
            for (byte address = 1; address <= 247; address++)
            {
                if (reader.Probe(address))
                {
                    service.Config.SlaveAddress = address;
                    return address;
                }
            }

            return null;
        }

        private void btnMeterProtocol_Click(object sender, EventArgs e)
        {
            if (_modbusService.IsConnected || _modbusService2.IsConnected)
            {
                MessageBox.Show("请先断开两个串口，再切换电表型号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _meterProtocol = _meterProtocol == "Amc96lE4" ? "Legacy" : "Amc96lE4";
            _meterCommunicationConfig = GetMeterCommunicationConfig(_meterProtocol);
            ApplyMeterCommunicationConfig(_modbusService, _meterCommunicationConfig);
            ApplyMeterCommunicationConfig(_modbusService2, _meterCommunicationConfig);
            _legacyMeterDataService = new MeterDataService(_modbusService);
            _meterDataService = CreateMeterDataReader(_modbusService);
            _meterDataService2 = CreateMeterDataReader(_modbusService2);
            _meterManager = new MeterManager(_meterDataService, _meterDataService2);
            UpdateMeterProtocolButton();
            lblStatus.Text = "已切换为" + _meterDataService.DeviceModel + "，请连接串口后扫描设备";
            lblStatus.ForeColor = Color.Green;
        }

        private void UpdateMeterProtocolButton()
        {
            btnMeterProtocol.Text = _meterProtocol == "Amc96lE4" ? "电表：AMC96L-E4" : "电表：原有电表";
        }

        private static byte GetHeatPumpScanAddress(string key, byte defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return byte.TryParse(value, out var parsed) && parsed >= 1 && parsed <= 247 ? parsed : defaultValue;
        }

        private static string BuildSampleType(bool includeRealTime, bool includeEnergy, bool includeQuality)
        {
            var parts = new List<string>();
            if (includeRealTime)
            {
                parts.Add("realtime");
            }

            if (includeEnergy)
            {
                parts.Add("energy");
            }

            if (includeQuality)
            {
                parts.Add("quality");
            }

            return string.Join(",", parts);
        }

        private static DateTimeOffset GetBeijingNow()
        {
            return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        }

        internal (string Text, Color Color, bool IsOffline) GetMeterDisplayStatus(MeterInfo meter)
        {
            if (meter == null)
            {
                return ("● 等待数据", Color.Gray, false);
            }

            if (meter.LastSuccessfulReadTime.HasValue)
            {
                var elapsed = DateTimeOffset.UtcNow - meter.LastSuccessfulReadTime.Value;
                if (elapsed.TotalSeconds > _offlineTimeoutSeconds)
                {
                    return ("● 设备掉线", Color.OrangeRed, true);
                }
            }

            if (meter.RealTime != null)
            {
                // P1-1：把电能质量数据一并带上，阈值按该表的站点/配电箱/表号解析，
                // 使界面结论与入库服务、网页端一致。
                var thresholds = SharedThresholdProvider.Resolve(
                    _siteCode,
                    meter.IsToolbar ? _toolbarBoxCode : _dashboardBoxCode,
                    meter.Id);
                var (text, color) = DetermineMeterStatus(meter.RealTime, meter.Quality, thresholds);
                return (text, color, false);
            }

            return ("● 等待数据", Color.Gray, false);
        }

        private static void ApplyAdaptiveYAxis(ChartArea area, IEnumerable<double> values)
        {
            var samples = values
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .OrderBy(v => v)
                .ToList();

            if (samples.Count == 0)
            {
                area.AxisY.Minimum = double.NaN;
                area.AxisY.Maximum = double.NaN;
                return;
            }

            var min = samples.First();
            var max = samples.Last();
            if (samples.Count == 1)
            {
                var singlePadding = Math.Max(Math.Abs(samples[0]) * 0.1d, 1d);
                area.AxisY.Minimum = samples[0] - singlePadding;
                area.AxisY.Maximum = samples[0] + singlePadding;
                return;
            }

            var p05 = GetPercentile(samples, 0.05d);
            var p95 = GetPercentile(samples, 0.95d);
            var coreMin = Math.Min(p05, p95);
            var coreMax = Math.Max(p05, p95);
            var coreRange = Math.Max(coreMax - coreMin, Math.Max(Math.Abs(coreMax), Math.Abs(coreMin)) * 0.05d);
            var padding = Math.Max(coreRange * 0.1d, 0.5d);
            var displayMin = coreMin - padding;
            var displayMax = coreMax + padding;

            if (min < displayMin)
            {
                displayMin = min - Math.Max((displayMax - min) * 0.02d, 0.2d);
            }

            if (max > displayMax)
            {
                displayMax = max + Math.Max((max - displayMin) * 0.02d, 0.2d);
            }

            if (displayMin == displayMax)
            {
                var fallbackPadding = Math.Max(Math.Abs(displayMin) * 0.1d, 1d);
                displayMin -= fallbackPadding;
                displayMax += fallbackPadding;
            }

            area.AxisY.Minimum = displayMin;
            area.AxisY.Maximum = displayMax;
        }

        private static double GetPercentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0)
            {
                return 0d;
            }

            if (sortedValues.Count == 1)
            {
                return sortedValues[0];
            }

            var position = percentile * (sortedValues.Count - 1);
            var lowerIndex = (int)Math.Floor(position);
            var upperIndex = (int)Math.Ceiling(position);
            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }

            var weight = position - lowerIndex;
            return sortedValues[lowerIndex] + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight;
        }

        /// <summary>
        /// 三相电流里最大的那一相。P1-5：全都没采到时返回 null，而不是 0 ——
        /// 0 A 是一个有意义的测量结果，不能用来表示"没测到"。
        /// </summary>
        internal static float? GetMaxCurrent(RealTimeData data)
        {
            if (data == null)
                return null;

            var values = new[] { data.CurrentA, data.CurrentB, data.CurrentC }
                .Where(v => v.HasValue && !float.IsNaN(v.Value) && !float.IsInfinity(v.Value))
                .Select(v => Math.Abs(v.Value))
                .ToList();

            return values.Count == 0 ? (float?)null : values.Max();
        }

        /// <summary>P1-5：可空测量值的统一显示。缺数据、NaN、无穷都显示 "--"。</summary>
        internal static string FormatMeasure(float? value, string format)
        {
            if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            {
                return "--";
            }

            return value.Value.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// P1-1：界面状态判定改为走共用判定引擎 + meter_threshold 表，
        /// 不再在这里硬编码 180~260V / PF 0.5 / 40~70Hz。
        /// 这样上位机、入库服务、网页端对同一台表给出的结论才会一致。
        ///
        /// 保留静态重载是为了兼容 MeterDetailsForm 的既有调用；阈值来源仍是同一个提供者。
        /// </summary>
        internal static (string text, Color color) DetermineMeterStatus(RealTimeData data)
        {
            return DetermineMeterStatus(data, null, SharedThresholdProvider.Resolve(null, null, null));
        }

        internal static (string text, Color color) DetermineMeterStatus(
            RealTimeData realtime,
            PowerQualityData quality,
            ThresholdSet thresholds)
        {
            if (realtime == null)
                return ("● 无数据", Color.Gray);

            var sample = BuildSampleView(realtime, quality);

            // 未带电时不做质量判定：停电/空载的 0V、0 PF 不是"异常"，是"没在运行"。
            if (!MeterQualityEvaluator.IsEnergized(thresholds, sample))
                return ("● 未带电", Color.Gray);

            var hits = MeterQualityEvaluator.Evaluate(thresholds, sample);
            if (hits.Count == 0)
                return ("● 运行正常", Color.Green);

            // critical 优先显示，其余取第一条。
            var worst = hits.FirstOrDefault(h => h.AlarmLevelIsCritical) ?? hits[0];
            var color = worst.AlarmLevelIsCritical ? Color.Red : Color.Orange;
            var suffix = hits.Count > 1 ? " 等" + hits.Count + "项" : string.Empty;
            return ("● " + worst.AlarmName + suffix, color);
        }

        /// <summary>把上位机的两个 DTO 映射成判定引擎的输入视图（P1-1）。</summary>
        internal static MeterSampleView BuildSampleView(RealTimeData realtime, PowerQualityData quality)
        {
            var view = new MeterSampleView();
            if (realtime != null)
            {
                view.VoltageA = realtime.VoltageA;
                view.VoltageB = realtime.VoltageB;
                view.VoltageC = realtime.VoltageC;
                view.CurrentA = realtime.CurrentA;
                view.CurrentB = realtime.CurrentB;
                view.CurrentC = realtime.CurrentC;
                view.PowerFactorTotal = realtime.PowerFactorTotal;
                view.Frequency = realtime.Frequency;
            }

            if (quality != null)
            {
                view.VoltageThdA = quality.VoltageTHDA;
                view.VoltageThdB = quality.VoltageTHDB;
                view.VoltageThdC = quality.VoltageTHDC;
                view.CurrentThdA = quality.CurrentTHDA;
                view.CurrentThdB = quality.CurrentTHDB;
                view.CurrentThdC = quality.CurrentTHDC;
                view.VoltageUnbalance = quality.VoltageUnbalance;
                view.CurrentUnbalance = quality.CurrentUnbalance;
            }

            return view;
        }

        #endregion

        #region Toolbar (真实串口) Events

        private void InitializeGridRows()
        {
            MeterGridRenderer.InitializeRealTimeGrid(dgvRealTime);
            MeterGridRenderer.InitializeEnergyGrid(dgvEnergy);
            MeterGridRenderer.InitializeQualityGrid(dgvQuality);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeGridRows();
            RefreshSerialPortCombos(false);
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        { tslTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }

        private void SerialPortCombo_DropDown(object sender, EventArgs e)
        {
            RefreshSerialPortCombos(true);
        }

        private void ScanRange_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressScanRangePersistence)
            {
                return;
            }

            SaveDashboardScanRangeSettings();
        }

        private void LoadDashboardScanRangeSettings()
        {
            ApplyDashboardScanRange(Settings.Default.DashboardScanStart, Settings.Default.DashboardScanEnd);
        }

        private void SaveDashboardScanRangeSettings()
        {
            var start = nudScanStart.Value;
            var end = nudScanEnd.Value;
            if (start > end)
            {
                if (ReferenceEquals(ActiveControl, nudScanStart))
                {
                    nudScanEnd.Value = start;
                    end = nudScanEnd.Value;
                }
                else
                {
                    nudScanStart.Value = end;
                    start = nudScanStart.Value;
                }
            }

            if (Settings.Default.DashboardScanStart == start && Settings.Default.DashboardScanEnd == end)
            {
                return;
            }

            Settings.Default.DashboardScanStart = start;
            Settings.Default.DashboardScanEnd = end;
            Settings.Default.Save();
        }

        private void ApplyDashboardScanRange(decimal start, decimal end)
        {
            var boundedStart = Math.Max(nudScanStart.Minimum, Math.Min(nudScanStart.Maximum, start));
            var boundedEnd = Math.Max(nudScanEnd.Minimum, Math.Min(nudScanEnd.Maximum, end));
            if (boundedStart > boundedEnd)
            {
                boundedEnd = boundedStart;
            }

            _suppressScanRangePersistence = true;
            try
            {
                nudScanStart.Value = boundedStart;
                nudScanEnd.Value = boundedEnd;
            }
            finally
            {
                _suppressScanRangePersistence = false;
            }
        }

        private void RefreshSerialPortCombos(bool preserveSelection)
        {
            var ports = SerialPort.GetPortNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            RefreshSerialPortCombo(cmbPort, ports, preserveSelection, 0);
            RefreshSerialPortCombo(cmbSecondPort, ports, preserveSelection, 1);
        }

        private static void RefreshSerialPortCombo(ComboBox comboBox, string[] ports, bool preserveSelection, int preferredIndex)
        {
            if (comboBox == null)
            {
                return;
            }

            var previousSelection = preserveSelection ? comboBox.SelectedItem as string : null;
            comboBox.BeginUpdate();
            try
            {
                comboBox.Items.Clear();
                comboBox.Items.AddRange(ports);

                if (!string.IsNullOrWhiteSpace(previousSelection) && ports.Contains(previousSelection, StringComparer.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = ports.First(port => string.Equals(port, previousSelection, StringComparison.OrdinalIgnoreCase));
                }
                else if (ports.Length > 0)
                {
                    comboBox.SelectedIndex = Math.Min(preferredIndex, ports.Length - 1);
                }
            }
            finally
            {
                comboBox.EndUpdate();
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke((MethodInvoker)(() => UpdateConnectionUI(e.IsConnected, e.PortName, e.Message)));
                return;
            }
            UpdateConnectionUI(e.IsConnected, e.PortName, e.Message);
        }

        private void OnCommunicationError(object sender, Exception ex)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke(
                        (MethodInvoker)(() =>
                        {
                            lblStatus.Text = "通信错误: " + ex.Message;
                            lblStatus.ForeColor = Color.Orange;
                        }));
                return;
            }
            lblStatus.Text = "通信错误: " + ex.Message;
            lblStatus.ForeColor = Color.Orange;
        }

        private void UpdateConnectionUI(bool isConnected, string portName, string message)
        {
            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;
            btnDiscover.Enabled = !isConnected;
            cmbPort.Enabled = !isConnected;
            cmbBaud.Enabled = !isConnected;
            txtAddr.Enabled = !isConnected;
            if (isConnected)
            {
                if (byte.TryParse(txtAddr.Text, out byte slaveAddress))
                {
                    ReplaceToolbarMeter(slaveAddress);
                    RebuildDashboardPanels();
                    ResizeDashboardPanels();
                }
                _refreshTimer.Start();
                if (!_mqttPublishErrorActive)
                {
                    lblStatus.Text = "已连接 " + portName + " (地址: " + txtAddr.Text + ")";
                    lblStatus.ForeColor = Color.Green;
                }
            }
            else
            {
                if (!_modbusService2.IsConnected) _refreshTimer.Stop();
                var toolbarBox = GetOrCreateToolbarBox();
                toolbarBox.Meters.Clear();
                _meterManager.ClearData();
                RebuildDashboardPanels();
                UpdateAllCardLabels();
                try
                {
                    PublishBoxRegistryAsync(toolbarBox, "scan", ThreadingCancellationToken.None).GetAwaiter().GetResult();
                }
                catch
                {
                }
                _mqttPublishErrorActive = false;
                _mqttPublishErrorShown = false;
                lblStatus.Text = message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateRealTimeGrid(RealTimeData data)
        {
            MeterGridRenderer.UpdateRealTimeGrid(dgvRealTime, data);
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (cmbPort.SelectedItem == null)
            {
                MessageBox.Show("请选择串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!byte.TryParse(txtAddr.Text, out byte slaveAddress) || (slaveAddress < 1) || (slaveAddress > 247))
            {
                MessageBox.Show("从站地址必须在 1-247 之间", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _meterCommunicationConfig.SlaveAddress = slaveAddress;
            _meterCommunicationConfig.BaudRate = int.Parse(cmbBaud.SelectedItem.ToString());
            ApplyMeterCommunicationConfig(_modbusService, _meterCommunicationConfig);
            _modbusService.Connect(cmbPort.SelectedItem.ToString());
        }

        private void BtnDisconnect_Click(object sender, EventArgs e) { _modbusService.Disconnect(); }

        private void BtnDiscover_Click(object sender, EventArgs e)
        {
            if (cmbPort.SelectedItem == null)
            {
                MessageBox.Show("请先选择串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            lblStatus.Text = "正在探测设备地址...";
            lblStatus.ForeColor = Color.Blue;
            var config = new CommunicationConfig
            {
                BaudRate = int.Parse(cmbBaud.SelectedItem.ToString()),
                Parity = _meterCommunicationConfig.Parity,
                DataBits = _meterCommunicationConfig.DataBits,
                StopBits = _meterCommunicationConfig.StopBits
            };
            _modbusService.UpdateConfig(config);
            if (!_modbusService.IsConnected)
                _modbusService.Connect(cmbPort.SelectedItem.ToString());
            if (_modbusService.IsConnected)
            {
                byte? address = DiscoverMeterAddress(_modbusService, _meterDataService);
                if (address.HasValue)
                {
                    txtAddr.Text = address.Value.ToString();
                    MessageBox.Show("探测成功！设备地址为: " + address.Value, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("未找到设备，请检查接线和通讯参数", "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _modbusService.Disconnect();
            }
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Red;
        }

        private void BtnReadParams_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            CommunicationConfig config = _legacyMeterDataService.ReadCommunicationConfig();
            if (config == null)
            {
                MessageBox.Show("参数读取失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            txtParamAddr.Text = config.SlaveAddress.ToString();
            cmbParamBaud.SelectedIndex = GetBaudRateComboIndex(config.BaudRate);
            cmbParamParity.SelectedIndex = GetParityComboIndex(config.Parity);
            MessageBox.Show("参数读取成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// P0-3：写通讯参数是唯一会造成"设备从总线上消失、必须到现场恢复"的操作。
        /// 因此这里补齐了输入校验、二次确认、写后回读比对，并全程留审计。
        /// </summary>
        private void BtnWriteParams_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte newAddress;
            if (!byte.TryParse(txtParamAddr.Text, out newAddress) || newAddress < 1 || newAddress > 247)
            {
                MessageBox.Show("从站地址必须是 1~247 之间的整数", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int newBaudRate;
            if (cmbParamBaud.SelectedItem == null || !int.TryParse(cmbParamBaud.SelectedItem.ToString(), out newBaudRate))
            {
                MessageBox.Show("请选择有效的波特率", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbParamParity.SelectedItem == null)
            {
                MessageBox.Show("请选择校验位", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var config = new CommunicationConfig
            {
                SlaveAddress = newAddress,
                BaudRate = newBaudRate,
                Parity = ParseParityFromString(cmbParamParity.SelectedItem.ToString()),
                StopBits = _meterCommunicationConfig.StopBits,
                DataBits = _meterCommunicationConfig.DataBits
            };

            var target = "从站 " + _legacyMeterDataService.SlaveAddress;
            var summary = string.Format(
                CultureInfo.InvariantCulture,
                "即将修改电表的通讯参数：\n\n" +
                "  目标设备：{0}\n" +
                "  从站地址：{1}  →  {2}\n" +
                "  波特率：  {3}  →  {4}\n" +
                "  校验位：  {5}  →  {6}（停止位 {7}）\n\n" +
                "写入后电表会按新参数通讯，上位机需要用相同参数重新连接。\n" +
                "如果参数填错，电表将无法再被访问，只能到现场恢复。\n\n" +
                "确认写入吗？",
                target,
                _legacyMeterDataService.SlaveAddress, config.SlaveAddress,
                _meterCommunicationConfig.BaudRate, config.BaudRate,
                _meterCommunicationConfig.Parity, config.Parity, config.StopBits);

            if (MessageBox.Show(summary, "确认写入通讯参数", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                AppLogger.Info("MeterConfig", "用户取消写入通讯参数。");
                return;
            }

            string error;
            var ok = _legacyMeterDataService.WriteCommunicationConfig(config, out error);
            var detail = string.Format(
                CultureInfo.InvariantCulture,
                "addr={0} baud={1} parity={2} stopBits={3}{4}",
                config.SlaveAddress, config.BaudRate, config.Parity, config.StopBits,
                ok ? string.Empty : " error=" + error);
            AppLogger.Audit("写通讯参数", target, ok, detail);

            if (!ok)
            {
                MessageBox.Show("参数写入失败：\n\n" + (error ?? "未知原因"), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 写后回读比对：设备通常在应答后才切换参数，回读失败并不代表写失败，
            // 因此这里只做提示，不当作错误。
            var verify = _legacyMeterDataService.ReadCommunicationConfig();
            if (verify == null)
            {
                MessageBox.Show(
                    "参数已下发，但用原参数回读已无响应。\n\n" +
                    "这通常说明设备已切换到新参数。请断开后用新的波特率/校验位重新连接并点击\"读取参数\"确认。",
                    "写入完成（需重新连接确认）",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var matched = verify.SlaveAddress == config.SlaveAddress
                && verify.BaudRate == config.BaudRate
                && verify.Parity == config.Parity;
            AppLogger.Info(
                "MeterConfig",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "写后回读: addr={0} baud={1} parity={2}，与目标{3}。",
                    verify.SlaveAddress, verify.BaudRate, verify.Parity, matched ? "一致" : "不一致"));

            MessageBox.Show(
                matched
                    ? "参数写入成功，回读校验一致。"
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "参数已写入，但回读结果与目标不一致：\n\n回读值：地址 {0}，波特率 {1}，校验 {2}\n\n请断开后按新参数重连确认。",
                        verify.SlaveAddress, verify.BaudRate, verify.Parity),
                matched ? "成功" : "写入完成（回读不一致）",
                MessageBoxButtons.OK,
                matched ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnReadDeviceInfo_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DeviceInfo info = _legacyMeterDataService.ReadDeviceInfo();
            if (info == null)
            {
                MessageBox.Show("设备信息读取失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            lblDeviceType.Text = info.DeviceType;
            dgvDeviceInfo.Rows.Clear();
            dgvDeviceInfo.Rows.Add("程序版本", info.ProgramVersion);
            dgvDeviceInfo.Rows.Add("协议版本", info.ProtocolVersion);
            dgvDeviceInfo.Rows.Add("版本日期", info.VersionDate);
            dgvDeviceInfo.Rows.Add("序列号", info.SerialNumber);
            dgvDeviceInfo.Rows.Add("IO配置", (info.IoConfig == 1) ? "2DI+2DO" : "无");
            MessageBox.Show("设备信息读取成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDO1On_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO1 合闸", "该动作会闭合 DO1 输出回路。", () => _legacyMeterDataService.DO1_On()); }

        private void BtnDO1Off_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO1 分闸", "该动作会断开 DO1 输出回路，可能切断下游设备供电。", () => _legacyMeterDataService.DO1_Off()); }

        private void BtnDO2On_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO2 合闸", "该动作会闭合 DO2 输出回路。", () => _legacyMeterDataService.DO2_On()); }

        private void BtnDO2Off_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO2 分闸", "该动作会断开 DO2 输出回路，可能切断下游设备供电。", () => _legacyMeterDataService.DO2_Off()); }

        private void BtnClearEnergy_Click(object sender, EventArgs e)
        {
            ExecuteControlAction(
                "清除总电能",
                "电能累计值将被清零，该操作不可撤销，历史累计读数会永久丢失。",
                () => _legacyMeterDataService.ClearEnergy());
        }

        /// <summary>
        /// P0-4：遥控动作统一入口。原实现点一下就直接下发分/合闸，而"清除电能"反而有确认框。
        /// 现在所有会改变现场设备状态的动作都必须二次确认，并写入审计（审计是同步落盘的）。
        /// </summary>
        private void ExecuteControlAction(string actionName, string riskNote, Func<bool> action)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var target = "从站 " + _legacyMeterDataService.SlaveAddress + " @ " + GetCurrentPortName();
            var confirm = string.Format(
                CultureInfo.InvariantCulture,
                "确认执行遥控操作？\n\n  操作：{0}\n  目标：{1}\n\n{2}",
                actionName,
                target,
                riskNote);

            if (MessageBox.Show(confirm, "确认遥控操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                AppLogger.Info("Control", "用户取消遥控操作: " + actionName + " → " + target);
                return;
            }

            bool ok;
            try
            {
                ok = action();
            }
            catch (Exception ex)
            {
                AppLogger.Audit(actionName, target, false, "exception=" + ex.Message);
                AppLogger.Error("Control", "执行遥控操作 " + actionName + " 异常。", ex);
                MessageBox.Show(actionName + " 执行异常：\n\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppLogger.Audit(actionName, target, ok, ok ? "device acknowledged" : "no/invalid response");

            if (ok)
            {
                MessageBox.Show(actionName + " 命令已发送并被设备确认", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    actionName + " 失败：设备未确认。\n\n请注意：命令可能已到达设备但应答丢失，" +
                    "请通过实时数据核对实际状态后再重试。",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string GetCurrentPortName()
        {
            try
            {
                return cmbPort.SelectedItem?.ToString() ?? cmbPort.Text ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private int GetBaudRateComboIndex(int baudRate)
        {
            string[] items = { "1200", "2400", "4800", "9600", "19200", "38400" };
            return Array.IndexOf(items, baudRate.ToString());
        }

        private int GetParityComboIndex(Parity parity)
        {
            switch (parity)
            {
                case Parity.None: return 0;
                case Parity.Odd: return 1;
                case Parity.Even: return 2;
                default: return 2;
            }
        }

        private Parity ParseParityFromString(string parityStr)
        {
            switch (parityStr)
            {
                case "8N1":
                case "8N2": return Parity.None;
                case "8O1":
                case "8O2": return Parity.Odd;
                case "8E1":
                case "8E2": return Parity.Even;
                default: return Parity.Even;
            }
        }

        #endregion

        /// <summary>
        /// P0-5：只负责"停止产生新工作"并让关闭流程有机会被取消。
        /// 资源释放全部搬到 OnFormClosed —— 原实现在 base.OnFormClosing(e) 之前就 Dispose 了串口和
        /// MQTT 客户端，一旦关闭被取消，程序会带着已释放的资源继续运行。
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _heatPumpRefreshTimer?.Stop();

            base.OnFormClosing(e);

            if (e.Cancel)
            {
                // 关闭被取消：恢复采集，不能留下"定时器停了但界面还在"的半死状态。
                AppLogger.Info("Shutdown", "关闭被取消，恢复定时采集。");
                if (_modbusService.IsConnected || _modbusService2.IsConnected)
                {
                    _refreshTimer?.Start();
                }

                if (_heatPumpWorkspaceService != null && _heatPumpWorkspaceService.IsConnected)
                {
                    _heatPumpRefreshTimer?.Start();
                }

                return;
            }

            _isShuttingDown = true;
        }

        /// <summary>
        /// P0-5：等在途采集落地后再释放资源，避免线程池里正在读串口的 Task
        /// 撞上已 Dispose 的 SerialPort。
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            AppLogger.Info("Shutdown", "开始释放资源。");
            WaitForRefreshToSettle(TimeSpan.FromSeconds(3));

            try
            {
                SaveHeatPumpWorkspaceConfiguration();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Shutdown", "保存热泵工作区配置失败。", ex);
            }

            SafeDispose("热泵工作区", () => _heatPumpWorkspaceService?.Dispose());
            SafeDispose("主串口", () =>
            {
                _modbusService?.Disconnect();
                _modbusService?.Dispose();
            });
            SafeDispose("仪表盘串口", () =>
            {
                _modbusService2?.Disconnect();
                _modbusService2?.Dispose();
            });
            SafeDispose("MQTT 发布器", () => _mqttPublisherService?.Dispose());

            AppLogger.Info("Shutdown", "资源释放完成。");
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 等待当前采集周期结束。定时器已停，所以最多等一轮。
        /// 超时也不阻断退出 —— 卡住不能关窗比竞态更糟。
        /// </summary>
        private void WaitForRefreshToSettle(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (_isRefreshing && DateTime.UtcNow < deadline)
            {
                // 采集完成后要回到 UI 线程收尾，因此这里必须继续抽消息泵，否则会死锁。
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            if (_isRefreshing)
            {
                AppLogger.Warn("Shutdown", "等待采集周期结束超时，仍继续释放资源。");
            }
        }

        private static void SafeDispose(string what, Action dispose)
        {
            try
            {
                dispose();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Shutdown", "释放" + what + "时发生异常。", ex);
            }
        }
    }
}
