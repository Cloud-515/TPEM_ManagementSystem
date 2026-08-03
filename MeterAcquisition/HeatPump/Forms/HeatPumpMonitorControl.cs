using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MeterAcquisition.HeatPump.Application;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.HeatPump.Forms
{
    internal sealed class HeatPumpMonitorControl : UserControl
    {
        private readonly HeatPumpHistoryQueryService _historyService;
        private ComboBox _historyDeviceComboBox;
        private CheckedListBox _comparisonDevicesList;
        private ComboBox _metricComboBox;
        private DateTimePicker _startPicker;
        private DateTimePicker _endPicker;
        private Button _queryButton;
        private Button _compareButton;
        private Label _historyStatusLabel;
        private Chart _historyChart;
        private Panel _chartHostPanel;
        private Label _summaryLabel;
        private DataGridView _eventGrid;
        private bool _historyLoaded;

        public HeatPumpMonitorControl()
        {
            _historyService = new HeatPumpHistoryQueryService();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 247, 250);
            Controls.Add(CreateHistoryPage());
            Load += async (sender, args) =>
            {
                EnsureHistoryChart();
                if (!_historyLoaded)
                {
                    await LoadHistoryDevicesAsync();
                }
            };
            SizeChanged += (sender, args) => EnsureHistoryChart();
        }

        private Control CreateHistoryPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = BackColor, Padding = new Padding(8) };
            var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(6), BackColor = Color.White, WrapContents = true };
            filterPanel.Controls.Add(CreateFilterLabel("模块"));
            _historyDeviceComboBox = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _historyDeviceComboBox.SelectedIndexChanged += SelectHistoryDevice;
            filterPanel.Controls.Add(_historyDeviceComboBox);
            filterPanel.Controls.Add(CreateFilterLabel("指标"));
            _metricComboBox = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _metricComboBox.DataSource = _historyService.GetMetrics();
            filterPanel.Controls.Add(_metricComboBox);
            filterPanel.Controls.Add(CreateFilterLabel("开始"));
            _startPicker = CreateDatePicker(DateTime.Today.AddDays(-1));
            filterPanel.Controls.Add(_startPicker);
            filterPanel.Controls.Add(CreateFilterLabel("结束"));
            _endPicker = CreateDatePicker(DateTime.Now);
            filterPanel.Controls.Add(_endPicker);
            _queryButton = new Button { Text = "查询趋势", Width = 88, Height = 26, Margin = new Padding(10, 5, 3, 3) };
            _queryButton.Click += async (sender, args) => await QueryHistoryAsync();
            filterPanel.Controls.Add(_queryButton);
            _compareButton = new Button { Text = "模块对比", Width = 88, Height = 26, Margin = new Padding(3, 5, 3, 3) };
            _compareButton.Click += async (sender, args) => await QueryComparisonAsync();
            filterPanel.Controls.Add(_compareButton);
            _historyStatusLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(12, 9, 3, 3) };
            filterPanel.Controls.Add(_historyStatusLabel);

            _summaryLabel = new Label { Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 4), BackColor = Color.White, ForeColor = Color.FromArgb(45, 45, 45) };
            _chartHostPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _eventGrid = CreateEventGrid();
            _comparisonDevicesList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle };
            var comparisonPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            comparisonPanel.Controls.Add(_comparisonDevicesList);
            comparisonPanel.Controls.Add(new Label { Text = "选择模块进行对比", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) });

            var eventPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            eventPanel.Controls.Add(_eventGrid);
            eventPanel.Controls.Add(new Label { Text = "告警事件", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) });
            var chartPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            chartPanel.Controls.Add(_chartHostPanel);

            var analysisPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(0) };
            analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228F));
            analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            analysisPanel.Controls.Add(filterPanel, 0, 0);
            analysisPanel.SetColumnSpan(filterPanel, 2);
            analysisPanel.Controls.Add(_summaryLabel, 0, 1);
            analysisPanel.SetColumnSpan(_summaryLabel, 2);
            analysisPanel.Controls.Add(comparisonPanel, 0, 2);
            analysisPanel.Controls.Add(chartPanel, 1, 2);
            analysisPanel.Controls.Add(eventPanel, 0, 3);
            analysisPanel.SetColumnSpan(eventPanel, 2);
            page.Controls.Add(analysisPanel);
            return page;
        }

        private async Task LoadHistoryDevicesAsync()
        {
            try
            {
                SetHistoryBusy(true, "正在加载热泵模块...");
                var devices = await _historyService.GetDevicesAsync().ConfigureAwait(true);
                _historyDeviceComboBox.DataSource = devices;
                _comparisonDevicesList.Items.Clear();
                foreach (var device in devices)
                {
                    _comparisonDevicesList.Items.Add(device, false);
                }

                if (devices.Count > 0)
                {
                    _historyDeviceComboBox.SelectedIndex = 0;
                    _comparisonDevicesList.SetItemChecked(0, true);
                    _historyStatusLabel.Text = "请选择范围后查询。";
                }
                else
                {
                    _historyStatusLabel.Text = "暂无已入库的热泵模块。";
                }

                _historyLoaded = true;
            }
            catch (Exception ex)
            {
                _historyStatusLabel.Text = "加载模块失败: " + ex.Message;
            }
            finally
            {
                SetHistoryBusy(false, null);
            }
        }

        private void SelectHistoryDevice(object sender, EventArgs args)
        {
            var selected = _historyDeviceComboBox.SelectedItem as HeatPumpDeviceLookupItem;
            if (selected == null)
            {
                return;
            }

            for (var index = 0; index < _comparisonDevicesList.Items.Count; index++)
            {
                var device = _comparisonDevicesList.Items[index] as HeatPumpDeviceLookupItem;
                if (device != null && device.DeviceId == selected.DeviceId)
                {
                    _comparisonDevicesList.SetItemChecked(index, true);
                    break;
                }
            }
        }

        private async Task QueryHistoryAsync()
        {
            var device = _historyDeviceComboBox.SelectedItem as HeatPumpDeviceLookupItem;
            var metric = _metricComboBox.SelectedItem as HeatPumpHistoryMetricDefinition;
            if (device == null || metric == null)
            {
                _historyStatusLabel.Text = "请选择热泵模块和指标。";
                return;
            }

            try
            {
                SetHistoryBusy(true, "正在查询趋势...");
                var result = await _historyService.QueryHistoryAsync(device.DeviceId, metric.Key, _startPicker.Value, _endPicker.Value).ConfigureAwait(true);
                DrawSeries(new[] { new HeatPumpComparisonSeries { Device = device, Metric = metric, Points = result.Points } });
                DrawSummary(result.Summary, result.PointCount, result.Points.Count);
                BindEvents(result.Events);
                _historyStatusLabel.Text = result.PointCount > result.Points.Count ? "已按时间桶聚合 " + result.PointCount + " 条原始数据。" : "查询到 " + result.Points.Count + " 个数据点。";
            }
            catch (Exception ex)
            {
                _historyStatusLabel.Text = "查询失败: " + ex.Message;
            }
            finally
            {
                SetHistoryBusy(false, null);
            }
        }

        private async Task QueryComparisonAsync()
        {
            var metric = _metricComboBox.SelectedItem as HeatPumpHistoryMetricDefinition;
            var devices = _comparisonDevicesList.CheckedItems.Cast<HeatPumpDeviceLookupItem>().Take(6).ToList();
            if (metric == null || devices.Count < 2)
            {
                _historyStatusLabel.Text = "请至少选择两个模块进行对比。";
                return;
            }

            try
            {
                SetHistoryBusy(true, "正在比较模块...");
                var series = await _historyService.QueryComparisonAsync(devices, metric.Key, _startPicker.Value, _endPicker.Value).ConfigureAwait(true);
                DrawSeries(series);
                _summaryLabel.Text = "模块对比: " + string.Join("、", series.Select(item => item.Device.DisplayText)) + "\r\n指标: " + metric.DisplayName + "。每条曲线按自身采样时间绘制。";
                _eventGrid.DataSource = null;
                _historyStatusLabel.Text = "已对比 " + series.Count + " 个模块。";
            }
            catch (Exception ex)
            {
                _historyStatusLabel.Text = "对比失败: " + ex.Message;
            }
            finally
            {
                SetHistoryBusy(false, null);
            }
        }

        private void DrawSeries(IEnumerable<HeatPumpComparisonSeries> allSeries)
        {
            EnsureHistoryChart();
            if (_historyChart == null)
            {
                return;
            }

            var chartArea = _historyChart.ChartAreas[0];
            _historyChart.Series.Clear();
            chartArea.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisY.Title = string.Empty;
            var colors = new[] { Color.FromArgb(0, 120, 212), Color.FromArgb(0, 153, 102), Color.FromArgb(217, 83, 79), Color.FromArgb(153, 102, 204), Color.FromArgb(230, 145, 56), Color.FromArgb(70, 130, 180) };
            var index = 0;
            foreach (var item in allSeries)
            {
                var series = new Series(item.Device.DisplayText)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    Color = colors[index % colors.Length],
                    XValueType = ChartValueType.DateTime,
                    ToolTip = "#SERIESNAME\n#VALX{yyyy-MM-dd HH:mm:ss}\n#VALY{F2} " + item.Metric.Unit
                };
                foreach (var point in item.Points)
                {
                    series.Points.AddXY(point.CollectedAt, point.Value);
                }

                _historyChart.Series.Add(series);
                chartArea.AxisY.Title = item.Metric.DisplayName + (string.IsNullOrWhiteSpace(item.Metric.Unit) ? string.Empty : " (" + item.Metric.Unit + ")");
                index++;
            }

            _historyChart.Legends[0].Enabled = _historyChart.Series.Count > 1;
        }

        private void DrawSummary(HeatPumpHistorySummary summary, long rawPointCount, int displayedPointCount)
        {
            _summaryLabel.Text = string.Format(CultureInfo.InvariantCulture,
                "采样 {0} 条，显示 {1} 点；进水均/低/高: {2} / {3} / {4} °C；出水均/低/高: {5} / {6} / {7} °C；平均温差: {8} °C；运行样本: {9}；故障样本: {10}",
                rawPointCount,
                displayedPointCount,
                FormatNumber(summary.AverageInletTemperature), FormatNumber(summary.MinimumInletTemperature), FormatNumber(summary.MaximumInletTemperature),
                FormatNumber(summary.AverageOutletTemperature), FormatNumber(summary.MinimumOutletTemperature), FormatNumber(summary.MaximumOutletTemperature),
                FormatNumber(summary.AverageTemperatureDelta), summary.RunningSampleCount, summary.FaultSampleCount);
        }

        private void BindEvents(List<HeatPumpAlarmEvent> events)
        {
            _eventGrid.DataSource = events.Select(item => new
            {
                类型 = item.EventType,
                代码 = item.EventCode,
                首次发现 = item.FirstSeenAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                最后发现 = item.LastSeenAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                恢复时间 = item.RecoveredAt.HasValue ? item.RecoveredAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "未恢复",
                详情 = item.Detail
            }).ToList();
        }

        private void SetHistoryBusy(bool busy, string status)
        {
            _queryButton.Enabled = !busy;
            _compareButton.Enabled = !busy;
            if (!string.IsNullOrWhiteSpace(status))
            {
                _historyStatusLabel.Text = status;
            }
        }

        private static Label CreateFilterLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(5, 9, 2, 3) };
        }

        private static DateTimePicker CreateDatePicker(DateTime value)
        {
            return new DateTimePicker { Width = 148, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", ShowUpDown = true, Value = value };
        }

        private void EnsureHistoryChart()
        {
            if (_historyChart != null || _chartHostPanel == null || _chartHostPanel.Width <= 0 || _chartHostPanel.Height <= 0)
            {
                return;
            }

            _historyChart = CreateHistoryChart();
            _chartHostPanel.Controls.Add(_historyChart);
        }

        private static Chart CreateHistoryChart()
        {
            var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White, BorderlineColor = Color.Gainsboro, BorderlineDashStyle = ChartDashStyle.Solid };
            var area = new ChartArea("History") { BackColor = Color.White };
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            area.AxisX.LabelStyle.Angle = -35;
            area.AxisY.IsStartedFromZero = false;
            chart.ChartAreas.Add(area);
            chart.Legends.Add(new Legend { Docking = Docking.Top, Alignment = StringAlignment.Center, Enabled = false });
            return chart;
        }

        private static DataGridView CreateEventGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false
            };
        }

        private static string FormatNumber(double? value)
        {
            return value.HasValue ? value.Value.ToString("F1", CultureInfo.InvariantCulture) : "-";
        }
    }
}
