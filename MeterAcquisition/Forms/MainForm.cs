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
using MeterAcquisition.Properties;
using ThreadingCancellationToken = System.Threading.CancellationToken;

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
        private FlowLayoutPanel _flpBoxContainer;

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
            this.Shown += MainForm_Shown;
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            ResizeDashboardPanels();
            await EnsureQueryModuleLoadedAsync(false).ConfigureAwait(true);
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
            _modbusService2.Config.BaudRate = 9600;
            _modbusService2.Config.Parity = Parity.Even;
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

            btnAddMeter.Enabled = true;
            btnSecondConnect.Enabled = true;
            btnAutoScanDashboard.Enabled = true;
            lblSecondStatusDashboard.Text = "● 已连接 (发现 " + discovered.Count + " 台)";
            lblSecondStatusDashboard.ForeColor = Color.Green;

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
                    lblSecondStatusDashboard.Text = "档案同步失败: " + ex.Message;
                    lblSecondStatusDashboard.ForeColor = Color.OrangeRed;
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
            if (!_modbusService.IsConnected && !_modbusService2.IsConnected) return;

            _isRefreshing = true;
            try
            {
                _refreshTickCount++;
                var readRealTime = _refreshTickCount % _realTimeIntervalSeconds == 0;
                var readEnergy = _refreshTickCount % _energyIntervalSeconds == 0;
                var readQuality = _refreshTickCount % _qualityIntervalSeconds == 0;
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
            var (statusText, statusColor, isOffline) = GetMeterDisplayStatus(meter);
            if (!isOffline && meter.RealTime != null)
            {
                float powerKw = meter.RealTime.ActivePowerTotal / 1000f;
                float maxCurrent = GetMaxCurrent(meter.RealTime);

                if (tags.PowerLabel.Text != powerKw.ToString("F1") + " kW")
                    tags.PowerLabel.Text = powerKw.ToString("F1") + " kW";
                if (tags.CurrentLabel.Text != maxCurrent.ToString("F1") + " A")
                    tags.CurrentLabel.Text = maxCurrent.ToString("F1") + " A";
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
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "MQTT 发布失败: " + ex.Message;
                    lblStatus.ForeColor = Color.OrangeRed;
                    throw;
                }
            }
        }

        private MeterTelemetryMessage BuildTelemetryMessage(MeterInfo meter, MeterPollResult result, DateTimeOffset collectTime, bool includeRealTime, bool includeEnergy, bool includeQuality)
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
                SampleType = BuildSampleType(includeRealTime, includeEnergy, includeQuality),
                RealTime = includeRealTime ? result.RealTime : null,
                Energy = includeEnergy ? result.Energy : null,
                Quality = includeQuality ? result.Quality : null
            };
        }

        private static int GetPositiveIntAppSetting(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
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
                var elapsed = DateTime.Now - meter.LastSuccessfulReadTime.Value;
                if (elapsed.TotalSeconds > _offlineTimeoutSeconds)
                {
                    return ("● 设备掉线", Color.OrangeRed, true);
                }
            }

            if (meter.RealTime != null)
            {
                var (text, color) = DetermineMeterStatus(meter.RealTime);
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
