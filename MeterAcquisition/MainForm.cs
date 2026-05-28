using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using ThreadingCancellationToken = System.Threading.CancellationToken;
using System.Windows.Forms;

namespace MeterAcquisition
{
    public partial class MainForm : Form
    {
        private readonly ModbusService _modbusService;
        private readonly ModbusService _modbusService2;
        private readonly MeterDataService _meterDataService;
        private readonly MeterDataService _meterDataService2;
        private readonly MeterManager _meterManager;
        private readonly Timer _refreshTimer;
        private readonly MqttPublisherService _mqttPublisherService;
        private readonly string _siteCode;
        private readonly string _toolbarBoxCode;
        private readonly string _dashboardBoxCode;
        private readonly Dictionary<string, MeterDetailsForm> _detailForms = new Dictionary<string, MeterDetailsForm>();
        private bool _isRefreshing = false;

        private int _boxCounter = 1;

        public MainForm()
        {
            InitializeComponent();
            _modbusService = new ModbusService();
            _modbusService2 = new ModbusService();
            _meterDataService = new MeterDataService(_modbusService);
            _meterDataService2 = new MeterDataService(_modbusService2);
            _modbusService.ConnectionStateChanged += OnConnectionStateChanged;
            _modbusService.CommunicationError += OnCommunicationError;
            _modbusService2.ConnectionStateChanged += OnSecondConnectionStateChanged;
            _modbusService2.CommunicationError += OnSecondCommunicationError;
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _mqttPublisherService = new MqttPublisherService();
            _siteCode = ConfigurationManager.AppSettings["SiteCode"] ?? "SITE-001";
            _toolbarBoxCode = ConfigurationManager.AppSettings["ToolbarBoxCode"] ?? "BOX-MAIN";
            _dashboardBoxCode = ConfigurationManager.AppSettings["DashboardBoxCode"] ?? "BOX-DASHBOARD";

            _meterManager = new MeterManager(_meterDataService, _meterDataService2);

            SetupDefaultBoxes();
            InitDashboardControls();
            RebuildDashboardPanels();

            cmbBaud.SelectedIndex = 3;
            cmbParamBaud.SelectedIndex = 3;
            cmbParamParity.SelectedIndex = 2;
            cmbWiring.SelectedIndex = 2;
            tslTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clockTimer.Start();
            this.Shown += MainForm_Shown;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            ResizeDashboardPanels();
        }

        /// <summary>创建默认分组，主串口与仪表盘串口各自按实际连接设备生成卡片</summary>
        private void SetupDefaultBoxes()
        {
            _meterManager.AddBox("外部串口设备");
            _meterManager.AddBox("仪表盘设备");
        }

        #region Dashboard

        private void BtnSecondConnect_Click(object sender, EventArgs e)
        {
            if (_cmbSecondPort.SelectedItem == null)
            {
                MessageBox.Show("请选择串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _modbusService2.Config.BaudRate = 9600;
            _modbusService2.Config.Parity = Parity.Even;
            _modbusService2.Connect(_cmbSecondPort.SelectedItem.ToString());
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
            _btnSecondConnect.Enabled = !isConnected;
            _btnSecondDisconnect.Enabled = isConnected;
            _cmbSecondPort.Enabled = !isConnected;
            if (_nudScanStart != null) _nudScanStart.Enabled = !isConnected;
            if (_nudScanEnd != null) _nudScanEnd.Enabled = !isConnected;

            if (isConnected)
            {
                _refreshTimer.Start();
                _lblSecondStatus.Text = "● 已连接";
                _lblSecondStatus.ForeColor = Color.Green;
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
                _lblSecondStatus.Text = message;
                _lblSecondStatus.ForeColor = Color.Red;
            }
        }

        private void OnSecondCommunicationError(object sender, Exception ex)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        _lblSecondStatus.Text = "错误: " + ex.Message;
                        _lblSecondStatus.ForeColor = Color.Orange;
                    }));
                return;
            }
            _lblSecondStatus.Text = "错误: " + ex.Message;
            _lblSecondStatus.ForeColor = Color.Orange;
        }

        private void BtnAddBox_Click(object sender, EventArgs e)
        {
            _boxCounter++;
            var dashBox = GetOrCreateDashboardBox();
            var meter = _meterManager.AddMeterToBox(dashBox, "E" + _boxCounter.ToString("D3"), "仪表 " + _boxCounter, dashBox.Name, 1);
            meter.IsToolbar = false;
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

            byte start = (byte)_nudScanStart.Value;
            byte end = (byte)_nudScanEnd.Value;
            if (start > end)
            {
                MessageBox.Show("扫描起始地址不能大于结束地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnAddBox.Enabled = false;
            _btnSecondConnect.Enabled = false;
            _btnAutoScan.Enabled = false;
            _lblSecondStatus.Text = "正在扫描 " + start + "~" + end + " ...";
            _lblSecondStatus.ForeColor = Color.Blue;

            var discovered = new List<byte>();
            await Task.Run(() =>
            {
                for (int addr = start; addr <= end; addr++)
                {
                    try
                    {
                        var rt = _meterDataService2.ReadRealTimeData((byte)addr);
                        if (rt != null)
                            lock (discovered) discovered.Add((byte)addr);
                    }
                    catch
                    {
                    }
                }
            });

            _btnAddBox.Enabled = true;
            _btnSecondConnect.Enabled = true;
            _btnAutoScan.Enabled = true;
            _lblSecondStatus.Text = "● 已连接 (发现 " + discovered.Count + " 台)";
            _lblSecondStatus.ForeColor = Color.Green;

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

        private async void DeleteMeterCard_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var meter = (MeterInfo)btn.Tag;
            if (MessageBox.Show("确定删除电表 " + meter.Name + "？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _meterManager.RemoveMeter(meter.Id);
                RebuildDashboardPanels();
                ResizeDashboardPanels();

                try
                {
                    await PublishDisableRegistryAsync(meter, ThreadingCancellationToken.None);
                }
                catch (Exception ex)
                {
                    _lblSecondStatus.Text = "档案同步失败: " + ex.Message;
                    _lblSecondStatus.ForeColor = Color.OrangeRed;
                }
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
                ScanTime = DateTime.Now,
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
                ScanTime = DateTime.Now,
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
                IsToolbar = meter.IsToolbar
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

        #endregion

        #region Data Update

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_isRefreshing) return;
            if (!_modbusService.IsConnected && !_modbusService2.IsConnected) return;

            _isRefreshing = true;
            try
            {
                RealTimeData rtData = null;
                EnergyData enData = null;
                PowerQualityData qlData = null;

                await Task.Run(() =>
                {
                    if (_modbusService.IsConnected)
                    {
                        rtData = _meterDataService.ReadRealTimeData();
                        enData = _meterDataService.ReadEnergyData();
                        qlData = _meterDataService.ReadPowerQualityData();
                    }

                    _meterManager.PollAll();
                });

                MeterGridRenderer.UpdateRealTimeGrid(dgvRealTime, rtData);
                MeterGridRenderer.UpdateEnergyGrid(dgvEnergy, enData);
                MeterGridRenderer.UpdateQualityGrid(dgvQuality, qlData);
                await PublishAllMetersAsync(rtData, enData, qlData);
                UpdateAllCardLabels();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void UpdateAllCardLabels()
        {
            if (_flpBoxContainer == null) return;

            var allMeters = _meterManager.AllMeters;
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
            if (meter.RealTime != null)
            {
                float powerKw = meter.RealTime.ActivePowerTotal / 1000f;
                float maxCurrent = GetMaxCurrent(meter.RealTime);

                if (tags.PowerLabel.Text != powerKw.ToString("F1") + " kW")
                    tags.PowerLabel.Text = powerKw.ToString("F1") + " kW";
                if (tags.CurrentLabel.Text != maxCurrent.ToString("F1") + " A")
                    tags.CurrentLabel.Text = maxCurrent.ToString("F1") + " A";

                var (text, color) = DetermineMeterStatus(meter.RealTime);
                if (tags.StatusLabel.Text != text)
                    tags.StatusLabel.Text = text;
                if (tags.StatusLabel.ForeColor != color)
                    tags.StatusLabel.ForeColor = color;
            }
            else
            {
                tags.PowerLabel.Text = "-- kW";
                tags.CurrentLabel.Text = "-- A";
                tags.StatusLabel.Text = "● 等待数据";
                tags.StatusLabel.ForeColor = Color.Gray;
            }
        }

        private async Task PublishAllMetersAsync(RealTimeData toolbarRealTime, EnergyData toolbarEnergy, PowerQualityData toolbarQuality)
        {
            var collectTime = DateTime.Now;
            var meters = _meterManager.AllMeters.Where(m => m.RealTime != null || m.Energy != null || m.Quality != null).ToList();

            foreach (var meter in meters)
            {
                if (meter.IsToolbar)
                {
                    meter.RealTime = toolbarRealTime ?? meter.RealTime;
                    meter.Energy = toolbarEnergy ?? meter.Energy;
                    meter.Quality = toolbarQuality ?? meter.Quality;
                }

                var message = BuildTelemetryMessage(meter, collectTime);
                try
                {
                    await _mqttPublisherService.PublishAsync(message);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "MQTT 发布失败: " + ex.Message;
                    lblStatus.ForeColor = Color.OrangeRed;
                    throw;
                }
            }
        }

        private MeterTelemetryMessage BuildTelemetryMessage(MeterInfo meter, DateTime collectTime)
        {
            return new MeterTelemetryMessage
            {
                SiteCode = _siteCode,
                BoxCode = meter.IsToolbar ? _toolbarBoxCode : _dashboardBoxCode,
                MeterCode = meter.Id,
                MeterName = meter.Name,
                Location = meter.Location,
                SlaveAddress = meter.SlaveAddress,
                IsToolbar = meter.IsToolbar,
                Source = "mqtt",
                CollectTime = collectTime,
                RealTime = meter.RealTime,
                Energy = meter.Energy,
                Quality = meter.Quality
            };
        }

        internal static float GetMaxCurrent(RealTimeData data)
        {
            if (data == null)
                return 0f;

            return new[] { data.CurrentA, data.CurrentB, data.CurrentC }
                .Where(v => !float.IsNaN(v) && !float.IsInfinity(v))
                .Select(Math.Abs)
                .DefaultIfEmpty(0f)
                .Max();
        }

        internal static (string text, Color color) DetermineMeterStatus(RealTimeData data)
        {
            if (data == null)
                return ("● 无数据", Color.Gray);

            if ((data.VoltageA < 50 && data.VoltageB < 50 && data.VoltageC < 50) ||
                data.Frequency < 40 || data.Frequency > 70)
                return ("● 异常", Color.Red);

            if (data.VoltageA < 180 || data.VoltageA > 260 ||
                data.VoltageB < 180 || data.VoltageB > 260 ||
                data.VoltageC < 180 || data.VoltageC > 260)
                return ("● 电压异常", Color.Orange);

            if (data.PowerFactorTotal < 0.5f)
                return ("● 功率因数低", Color.Orange);

            return ("● 运行正常", Color.Green);
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
            string[] ports = SerialPort.GetPortNames();
            cmbPort.Items.Clear();
            cmbPort.Items.AddRange(ports);
            if (cmbPort.Items.Count > 0)
                cmbPort.SelectedIndex = 0;

            _cmbSecondPort.Items.Clear();
            _cmbSecondPort.Items.AddRange(ports);
            if (_cmbSecondPort.Items.Count > 1)
                _cmbSecondPort.SelectedIndex = 1;
            else if (_cmbSecondPort.Items.Count > 0)
                _cmbSecondPort.SelectedIndex = 0;
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        { tslTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }

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
                lblStatus.Text = "已连接 " + portName + " (地址: " + txtAddr.Text + ")";
                lblStatus.ForeColor = Color.Green;
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
            _modbusService.Config.SlaveAddress = slaveAddress;
            _modbusService.Config.BaudRate = int.Parse(cmbBaud.SelectedItem.ToString());
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
            CommunicationConfig config = new CommunicationConfig
            {
                BaudRate = int.Parse(cmbBaud.SelectedItem.ToString()),
                Parity = Parity.Even
            };
            _modbusService.UpdateConfig(config);
            if (!_modbusService.IsConnected)
                _modbusService.Connect(cmbPort.SelectedItem.ToString());
            if (_modbusService.IsConnected)
            {
                byte? address = _modbusService.DiscoverSlaveAddress();
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
            CommunicationConfig config = _meterDataService.ReadCommunicationConfig();
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

        private void BtnWriteParams_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            CommunicationConfig config = new CommunicationConfig
            {
                SlaveAddress = byte.Parse(txtParamAddr.Text),
                BaudRate = int.Parse(cmbParamBaud.SelectedItem.ToString()),
                Parity = ParseParityFromString(cmbParamParity.SelectedItem.ToString())
            };
            if (_meterDataService.WriteCommunicationConfig(config))
                MessageBox.Show("参数写入成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("参数写入失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnReadDeviceInfo_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DeviceInfo info = _meterDataService.ReadDeviceInfo();
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
        { ExecuteControlAction("DO1合闸", () => _meterDataService.DO1_On()); }

        private void BtnDO1Off_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO1分闸", () => _meterDataService.DO1_Off()); }

        private void BtnDO2On_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO2合闸", () => _meterDataService.DO2_On()); }

        private void BtnDO2Off_Click(object sender, EventArgs e)
        { ExecuteControlAction("DO2分闸", () => _meterDataService.DO2_Off()); }

        private void BtnClearEnergy_Click(object sender, EventArgs e)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("确定要清除总电能记录吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_meterDataService.ClearEnergy())
                    MessageBox.Show("清除成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("清除失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteControlAction(string actionName, Func<bool> action)
        {
            if (!_modbusService.IsConnected)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (action())
                MessageBox.Show(actionName + "命令已发送", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(actionName + "失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _modbusService?.Disconnect();
            _modbusService?.Dispose();
            _modbusService2?.Disconnect();
            _modbusService2?.Dispose();
            _mqttPublisherService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
