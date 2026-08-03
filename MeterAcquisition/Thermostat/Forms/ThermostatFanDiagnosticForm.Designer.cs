using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatFanDiagnosticForm
    {
        private TextBox _summaryTextBox;
        private ListView _stateListView;
        private ListView _framesListView;

        private void InitializeComponent()
        {
            _summaryTextBox = new TextBox { Dock = DockStyle.Top, Height = 62, ReadOnly = true, Multiline = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Padding = new Padding(10) };
            _stateListView = new ListView { Dock = DockStyle.Top, Height = 95, View = View.Details, FullRowSelect = true, GridLines = true };
            _stateListView.Columns.Add("开关", 90); _stateListView.Columns.Add("模式", 150); _stateListView.Columns.Add("阀1", 70); _stateListView.Columns.Add("目标风速", 100); _stateListView.Columns.Add("当前风速", 100);
            _framesListView = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            _framesListView.Columns.Add("时间", 105); _framesListView.Columns.Add("方向", 60); _framesListView.Columns.Add("原始 Modbus 帧", 680);
            SuspendLayout(); Controls.Add(_framesListView); Controls.Add(_stateListView); Controls.Add(_summaryTextBox); AutoScaleMode = AutoScaleMode.Font; MinimumSize = new Size(760, 430); Size = new Size(900, 560); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; ResumeLayout(false);
        }
    }
}