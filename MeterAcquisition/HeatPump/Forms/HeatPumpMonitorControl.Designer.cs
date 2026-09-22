using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.HeatPump.Forms
{
    internal sealed partial class HeatPumpMonitorControl
    {
        private TableLayoutPanel _analysisPanel;
        private FlowLayoutPanel _filterPanel;
        private Panel _comparisonPanel;
        private Panel _chartPanel;
        private Panel _eventPanel;
        private Label _comparisonTitleLabel;
        private Label _eventTitleLabel;

        private void InitializeComponent()
        {
            _analysisPanel = new TableLayoutPanel();
            _filterPanel = new FlowLayoutPanel();
            _comparisonPanel = new Panel();
            _chartPanel = new Panel();
            _eventPanel = new Panel();
            _comparisonTitleLabel = new Label();
            _eventTitleLabel = new Label();
            _historyDeviceComboBox = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Font = UiStyle.BodyFont };
            _historyDeviceComboBox.Items.Add("示例热泵模块"); _historyDeviceComboBox.SelectedIndex = 0;
            _metricComboBox = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Font = UiStyle.BodyFont };
            _startPicker = CreateDatePicker(System.DateTime.Today.AddDays(-1));
            _endPicker = CreateDatePicker(System.DateTime.Now);
            _queryButton = UiStyle.CreateButton("查询趋势");
            _compareButton = UiStyle.CreateButton("模块对比");
            // 状态文字可能是两行的报错，原来它和筛选控件挤在同一条 FlowLayoutPanel 里，
            // 一出现长文本就把"查询趋势/模块对比"顶到下一行（UI-18）。现在单独占一行。
            _historyStatusLabel = new Label { AutoSize = true, ForeColor = Color.DimGray, Font = UiStyle.BodyFont, MinimumSize = new Size(0, 24), Margin = new Padding(UiStyle.ToolbarPad, 0, UiStyle.ToolbarPad, UiStyle.RowGap) };
            _summaryLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, MinimumSize = new Size(0, 40), Padding = new Padding(10, 6, 10, 4), BackColor = Color.White, ForeColor = Color.FromArgb(45, 45, 45), Font = UiStyle.BodyFont };
            _chartHostPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _eventGrid = CreateEventGrid();
            _comparisonDevicesList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle, Font = UiStyle.BodyFont };
            _analysisPanel.SuspendLayout(); _comparisonPanel.SuspendLayout(); _eventPanel.SuspendLayout();
            SuspendLayout();
            _filterPanel.Dock = DockStyle.Fill; _filterPanel.BackColor = Color.White;
            // 每个"标签 + 输入框"打包成固定行高的字段组：换行只在字段之间发生，标签不会被甩到上一行。
            UiStyle.RebuildToolbar(
                _filterPanel,
                UiStyle.Field("模块", _historyDeviceComboBox),
                UiStyle.Field("指标", _metricComboBox),
                UiStyle.Field("开始", _startPicker),
                UiStyle.Field("结束", _endPicker),
                _queryButton,
                _compareButton);
            _comparisonPanel.Dock = DockStyle.Fill; _comparisonPanel.Padding = new Padding(0, 0, 8, 0); _comparisonPanel.Controls.Add(_comparisonDevicesList); _comparisonPanel.Controls.Add(_comparisonTitleLabel); _comparisonTitleLabel.Text = "选择模块进行对比"; _comparisonTitleLabel.Dock = DockStyle.Top; _comparisonTitleLabel.AutoSize = true; _comparisonTitleLabel.TextAlign = ContentAlignment.MiddleLeft; _comparisonTitleLabel.Font = UiStyle.SectionFont; _comparisonTitleLabel.Padding = new Padding(0, 0, 0, UiStyle.RowGap);
            _chartPanel.Dock = DockStyle.Fill; _chartPanel.Padding = new Padding(0, 8, 0, 0); _chartPanel.Controls.Add(_chartHostPanel);
            _eventPanel.Dock = DockStyle.Fill; _eventPanel.Padding = new Padding(0, 8, 0, 0); _eventPanel.Controls.Add(_eventGrid); _eventPanel.Controls.Add(_eventTitleLabel); _eventTitleLabel.Text = "告警事件"; _eventTitleLabel.Dock = DockStyle.Top; _eventTitleLabel.AutoSize = true; _eventTitleLabel.TextAlign = ContentAlignment.MiddleLeft; _eventTitleLabel.Font = UiStyle.SectionFont; _eventTitleLabel.Padding = new Padding(0, 0, 0, UiStyle.RowGap);
            // UI-4：筛选行和摘要行原来是 Absolute 86F / 48F 固定高，文字一换行就被切，已改 AutoSize。
            // UI-13：左列 Absolute 228F、告警行 Absolute 180F 也换成百分比 —— 写死像素在放大字号或小屏下同样会切内容。
            _analysisPanel.Dock = DockStyle.Fill; _analysisPanel.ColumnCount = 2; _analysisPanel.RowCount = 5;
            _analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            _analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76F));
            _analysisPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _analysisPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _analysisPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            _analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            _analysisPanel.Controls.Add(_filterPanel, 0, 0); _analysisPanel.SetColumnSpan(_filterPanel, 2);
            _analysisPanel.Controls.Add(_historyStatusLabel, 0, 1); _analysisPanel.SetColumnSpan(_historyStatusLabel, 2);
            _analysisPanel.Controls.Add(_summaryLabel, 0, 2); _analysisPanel.SetColumnSpan(_summaryLabel, 2);
            _analysisPanel.Controls.Add(_comparisonPanel, 0, 3); _analysisPanel.Controls.Add(_chartPanel, 1, 3);
            _analysisPanel.Controls.Add(_eventPanel, 0, 4); _analysisPanel.SetColumnSpan(_eventPanel, 2);
            Controls.Add(_analysisPanel);
            // UI-19：缩放基准统一交给 MainForm，见 Thermostat/Forms/ThermostatConnectionBar.Designer.cs 的说明。
            AutoScaleMode = AutoScaleMode.Inherit;
            BackColor = Color.FromArgb(244, 247, 250); Dock = DockStyle.Fill;
            _analysisPanel.ResumeLayout(false); _comparisonPanel.ResumeLayout(false); _eventPanel.ResumeLayout(false); ResumeLayout(false);
        }
    }
}
