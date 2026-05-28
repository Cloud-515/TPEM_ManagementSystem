using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MeterAcquisition
{
    public class MeterDetailsForm : Form
    {
        private readonly MeterManager _meterManager;
        private readonly string _meterId;
        private readonly Label _lblName;
        private readonly Label _lblAddress;
        private readonly Label _lblStatus;
        private readonly Label _lblPower;
        private readonly Label _lblCurrent;
        private readonly DataGridView _dgvRealTime;
        private readonly DataGridView _dgvEnergy;
        private readonly DataGridView _dgvQuality;

        public MeterDetailsForm(MeterManager meterManager, string meterId)
        {
            _meterManager = meterManager;
            _meterId = meterId;

            Text = "电表详情";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(960, 720);
            MinimumSize = new Size(840, 620);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                AutoSize = true,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12),
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.Controls.Add(header, 0, 0);

            _lblName = new Label
            {
                Font = new Font("微软雅黑", 13, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 20, 0),
            };
            header.Controls.Add(_lblName, 0, 0);

            _lblAddress = new Label
            {
                Font = new Font("微软雅黑", 10),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 20, 0),
            };
            header.Controls.Add(_lblAddress, 1, 0);

            _lblStatus = new Label
            {
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 20, 0),
            };
            header.Controls.Add(_lblStatus, 2, 0);

            _lblPower = new Label
            {
                Font = new Font("微软雅黑", 10),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 20, 0),
            };
            header.Controls.Add(_lblPower, 3, 0);

            _lblCurrent = new Label
            {
                Font = new Font("微软雅黑", 10),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 0, 0),
            };
            header.Controls.Add(_lblCurrent, 4, 0);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
            };
            root.Controls.Add(tabs, 0, 1);

            _dgvRealTime = CreateGrid();
            _dgvEnergy = CreateGrid();
            _dgvQuality = CreateGrid();

            tabs.TabPages.Add(CreateTabPage("实时数据", _dgvRealTime));
            tabs.TabPages.Add(CreateTabPage("电能数据", _dgvEnergy));
            tabs.TabPages.Add(CreateTabPage("电能质量", _dgvQuality));

            MeterGridRenderer.InitializeRealTimeGrid(_dgvRealTime);
            MeterGridRenderer.InitializeEnergyGrid(_dgvEnergy);
            MeterGridRenderer.InitializeQualityGrid(_dgvQuality);

            Load += MeterDetailsForm_Load;
            FormClosed += MeterDetailsForm_FormClosed;
        }

        private void MeterDetailsForm_Load(object sender, EventArgs e)
        {
            _meterManager.DataRefreshed += MeterManager_DataRefreshed;
            RefreshFromMeter();
        }

        private void MeterDetailsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _meterManager.DataRefreshed -= MeterManager_DataRefreshed;
        }

        private void MeterManager_DataRefreshed(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)RefreshFromMeter);
                return;
            }

            RefreshFromMeter();
        }

        public void RefreshFromMeter()
        {
            var meter = _meterManager.AllMeters.FirstOrDefault(m => m.Id == _meterId);
            if (meter == null)
            {
                _lblName.Text = "电表不存在";
                _lblAddress.Text = string.Empty;
                _lblStatus.Text = "● 无数据";
                _lblStatus.ForeColor = Color.Gray;
                _lblPower.Text = "功率: -- kW";
                _lblCurrent.Text = "电流: -- A";
                MeterGridRenderer.UpdateRealTimeGrid(_dgvRealTime, null);
                MeterGridRenderer.UpdateEnergyGrid(_dgvEnergy, null);
                MeterGridRenderer.UpdateQualityGrid(_dgvQuality, null);
                return;
            }

            _lblName.Text = string.IsNullOrWhiteSpace(meter.Name) ? "未命名" : meter.Name;
            _lblAddress.Text = "地址: " + meter.SlaveAddress;

            if (meter.RealTime != null)
            {
                float powerKw = meter.RealTime.ActivePowerTotal / 1000f;
                float maxCurrent = MainForm.GetMaxCurrent(meter.RealTime);
                _lblPower.Text = "功率: " + powerKw.ToString("F1") + " kW";
                _lblCurrent.Text = "电流: " + maxCurrent.ToString("F1") + " A";

                var (text, color) = MainForm.DetermineMeterStatus(meter.RealTime);
                _lblStatus.Text = text;
                _lblStatus.ForeColor = color;
            }
            else
            {
                _lblStatus.Text = "● 等待数据";
                _lblStatus.ForeColor = Color.Gray;
                _lblPower.Text = "功率: -- kW";
                _lblCurrent.Text = "电流: -- A";
            }

            MeterGridRenderer.UpdateRealTimeGrid(_dgvRealTime, meter.RealTime);
            MeterGridRenderer.UpdateEnergyGrid(_dgvEnergy, meter.Energy);
            MeterGridRenderer.UpdateQualityGrid(_dgvQuality, meter.Quality);
        }

        private static TabPage CreateTabPage(string title, Control content)
        {
            var page = new TabPage(title);
            page.Controls.Add(content);
            return page;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            };
            grid.Columns.Add("colParameter", "参数");
            grid.Columns.Add("colValue", "数值");
            grid.Columns.Add("colUnit", "单位");
            return grid;
        }
    }
}
