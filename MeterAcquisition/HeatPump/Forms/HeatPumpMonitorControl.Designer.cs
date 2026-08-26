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
            _historyDeviceComboBox = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _historyDeviceComboBox.Items.Add("示例热泵模块"); _historyDeviceComboBox.SelectedIndex = 0;
            _metricComboBox = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _startPicker = CreateDatePicker(System.DateTime.Today.AddDays(-1));
            _endPicker = CreateDatePicker(System.DateTime.Now);
            _queryButton = new Button { Text = "查询趋势", AutoSize = true, MinimumSize = new Size(88, 30) };
            _compareButton = new Button { Text = "模块对比", AutoSize = true, MinimumSize = new Size(88, 30) };
            _historyStatusLabel = new Label { AutoSize = true, ForeColor = Color.DimGray };
            _summaryLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, MinimumSize = new Size(0, 40), Padding = new Padding(10, 6, 10, 4), BackColor = Color.White, ForeColor = Color.FromArgb(45, 45, 45) };
            _chartHostPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _eventGrid = CreateEventGrid();
            _comparisonDevicesList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle };
            _analysisPanel.SuspendLayout(); _filterPanel.SuspendLayout(); _comparisonPanel.SuspendLayout(); _eventPanel.SuspendLayout();
            SuspendLayout();
            _filterPanel.Dock = DockStyle.Fill; _filterPanel.AutoSize = true; _filterPanel.Padding = new Padding(6); _filterPanel.BackColor = Color.White; _filterPanel.WrapContents = true;
            _filterPanel.Controls.Add(CreateFilterLabel("模块")); _filterPanel.Controls.Add(_historyDeviceComboBox); _filterPanel.Controls.Add(CreateFilterLabel("指标")); _filterPanel.Controls.Add(_metricComboBox); _filterPanel.Controls.Add(CreateFilterLabel("开始")); _filterPanel.Controls.Add(_startPicker); _filterPanel.Controls.Add(CreateFilterLabel("结束")); _filterPanel.Controls.Add(_endPicker); _filterPanel.Controls.Add(_queryButton); _filterPanel.Controls.Add(_compareButton); _filterPanel.Controls.Add(_historyStatusLabel);
            _comparisonPanel.Dock = DockStyle.Fill; _comparisonPanel.Padding = new Padding(0, 0, 8, 0); _comparisonPanel.Controls.Add(_comparisonDevicesList); _comparisonPanel.Controls.Add(_comparisonTitleLabel); _comparisonTitleLabel.Text = "选择模块进行对比"; _comparisonTitleLabel.Dock = DockStyle.Top; _comparisonTitleLabel.Height = 25; _comparisonTitleLabel.TextAlign = ContentAlignment.MiddleLeft; _comparisonTitleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _chartPanel.Dock = DockStyle.Fill; _chartPanel.Padding = new Padding(0, 8, 0, 0); _chartPanel.Controls.Add(_chartHostPanel);
            _eventPanel.Dock = DockStyle.Fill; _eventPanel.Padding = new Padding(0, 8, 0, 0); _eventPanel.Controls.Add(_eventGrid); _eventPanel.Controls.Add(_eventTitleLabel); _eventTitleLabel.Text = "告警事件"; _eventTitleLabel.Dock = DockStyle.Top; _eventTitleLabel.Height = 25; _eventTitleLabel.TextAlign = ContentAlignment.MiddleLeft; _eventTitleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            // UI-4：筛选行和摘要行原来是 Absolute 86F / 48F 固定高。
            // 筛选条 WrapContents=true 一换行就被切；摘要标签遇到两行文字（例如
            // "加载模块失败: 42703: 字段 controller_code 不存在" + "POSITION: 24"）第二行只剩上半截。
            // 两行都改成 AutoSize，由内容决定高度。
            _analysisPanel.Dock = DockStyle.Fill; _analysisPanel.ColumnCount = 2; _analysisPanel.RowCount = 4; _analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228F)); _analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); _analysisPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); _analysisPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); _analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); _analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F)); _analysisPanel.Controls.Add(_filterPanel, 0, 0); _analysisPanel.SetColumnSpan(_filterPanel, 2); _analysisPanel.Controls.Add(_summaryLabel, 0, 1); _analysisPanel.SetColumnSpan(_summaryLabel, 2); _analysisPanel.Controls.Add(_comparisonPanel, 0, 2); _analysisPanel.Controls.Add(_chartPanel, 1, 2); _analysisPanel.Controls.Add(_eventPanel, 0, 3); _analysisPanel.SetColumnSpan(_eventPanel, 2);
            Controls.Add(_analysisPanel); AutoScaleMode = AutoScaleMode.Font; BackColor = Color.FromArgb(244, 247, 250); Dock = DockStyle.Fill;
            _analysisPanel.ResumeLayout(false); _filterPanel.ResumeLayout(false); _filterPanel.PerformLayout(); _comparisonPanel.ResumeLayout(false); _eventPanel.ResumeLayout(false); ResumeLayout(false);
        }
    }
}