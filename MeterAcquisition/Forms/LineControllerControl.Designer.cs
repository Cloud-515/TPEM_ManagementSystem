using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition
{
    internal sealed partial class LineControllerControl
    {
        private TableLayoutPanel _rootLayout;
        private Panel _connectionBarHost;
        private Panel _groupControlBarHost;
        private TableLayoutPanel _summaryLayout;
        private TableLayoutPanel _cardsPanel;
        private Label _pageStatusLabel;
        private Label _connectionValueLabel;
        private Label _controllerCountValueLabel;
        private Label _moduleCountValueLabel;
        private Label _refreshValueLabel;
        private Label _selectionSummaryLabel;

        private void InitializeComponent()
        {
            _rootLayout = new TableLayoutPanel();
            _connectionBarHost = new Panel();
            _groupControlBarHost = new Panel();
            _summaryLayout = new TableLayoutPanel();
            _cardsPanel = new TableLayoutPanel();
            _pageStatusLabel = new Label();
            _connectionValueLabel = CreateSummaryValue("未连接");
            _controllerCountValueLabel = CreateSummaryValue("0");
            _moduleCountValueLabel = CreateSummaryValue("0");
            _refreshValueLabel = CreateSummaryValue("--");
            _selectionSummaryLabel = new Label();
            SuspendLayout();
            _rootLayout.ColumnCount = 1;
            _rootLayout.RowCount = 5;
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.Padding = new Padding(12);
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _connectionBarHost.Dock = DockStyle.Fill;
            _groupControlBarHost.Dock = DockStyle.Fill;
            _summaryLayout.ColumnCount = 4;
            _summaryLayout.RowCount = 1;
            _summaryLayout.Dock = DockStyle.Top;
            _summaryLayout.AutoSize = true;
            for (var i = 0; i < 4; i++) _summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            AddSummaryCard(_summaryLayout, 0, "连接状态", _connectionValueLabel);
            AddSummaryCard(_summaryLayout, 1, "控制器数量", _controllerCountValueLabel);
            AddSummaryCard(_summaryLayout, 2, "模块数量", _moduleCountValueLabel);
            AddSummaryCard(_summaryLayout, 3, "最近刷新", _refreshValueLabel);
            // UI-5：分组容器改成单列 TableLayoutPanel。
            // 原来是 FlowLayoutPanel(TopDown)，FlowLayout 不认 Dock，分组只能自己写死
            // Width = 1120 才有宽度；换成单列表格后宽度由列（100%）给，窗口多宽就多宽。
            _cardsPanel.AutoScroll = true;
            _cardsPanel.Dock = DockStyle.Fill;
            _cardsPanel.ColumnCount = 1;
            _cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _cardsPanel.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            _pageStatusLabel.AutoSize = true;
            _pageStatusLabel.Dock = DockStyle.Fill;
            _pageStatusLabel.ForeColor = Color.DimGray;
            _pageStatusLabel.Margin = new Padding(0, 12, 0, 0);
            _rootLayout.Controls.Add(_connectionBarHost, 0, 0);
            _rootLayout.Controls.Add(_groupControlBarHost, 0, 1);
            _rootLayout.Controls.Add(_summaryLayout, 0, 2);
            _rootLayout.Controls.Add(_cardsPanel, 0, 3);
            _rootLayout.Controls.Add(_pageStatusLabel, 0, 4);
            Controls.Add(_rootLayout);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            ResumeLayout(false);
        }

        /// <summary>
        /// 汇总卡片的数值标签：必须 Dock=Fill，不能 AutoSize。
        /// 原来是 AutoSize=true 且不设 Dock，标签会停在 padding 区左上角，
        /// 和下面那个 Dock=Top 的标题标签占同一块地方，两行字直接叠在一起（UI-2）。
        /// </summary>
        private static Label CreateSummaryValue(string text)
        {
            return new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = text,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
        }

        private static void AddSummaryCard(TableLayoutPanel summary, int column, string title, Label value)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(column == 0 ? 0 : 8, 0, 0, 0), Padding = new Padding(12), MinimumSize = new Size(0, 84) };
            // 先加 Dock=Fill 的数值，再加 Dock=Top 的标题：
            // WinForms 的停靠顺序是后加的先占位，标题因此在上、数值占剩下的空间。
            card.Controls.Add(value);
            card.Controls.Add(new Label { AutoSize = false, Dock = DockStyle.Top, Height = 24, Text = title, ForeColor = Color.DimGray });
            summary.Controls.Add(card, column, 0);
        }
    }
}