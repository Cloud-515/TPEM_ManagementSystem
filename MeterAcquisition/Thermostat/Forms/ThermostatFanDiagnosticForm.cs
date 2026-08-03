using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MeterAcquisition.Thermostat.Domain;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed class ThermostatFanDiagnosticForm : Form
    {
        public ThermostatFanDiagnosticForm(ThermostatFanControlDiagnostic diagnostic)
        {
            Text = "风机命令诊断";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(760, 430);
            Size = new Size(900, 560);

            var summary = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 62,
                ReadOnly = true,
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = diagnostic.Summary,
                Padding = new Padding(10)
            };
            Controls.Add(summary);

            var state = new ListView { Dock = DockStyle.Top, Height = 95, View = View.Details, FullRowSelect = true, GridLines = true };
            state.Columns.Add("开关", 90); state.Columns.Add("模式", 150); state.Columns.Add("阀1", 70); state.Columns.Add("目标风速", 100); state.Columns.Add("当前风速", 100);
            var telemetry = diagnostic.Telemetry;
            state.Items.Add(new ListViewItem(new[]
            {
                ThermostatMonitorControl.FormatPower(telemetry.PowerState), ThermostatMonitorControl.FormatMode(telemetry.Mode),
                ThermostatMonitorControl.FormatValve1State(telemetry.WaterValveOpen), ThermostatMonitorControl.FormatFanSpeedSetting(telemetry.FanSpeedSetting),
                ThermostatMonitorControl.FormatCurrentFanSpeed(telemetry.CurrentFanSpeed)
            }));
            Controls.Add(state);

            var frames = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            frames.Columns.Add("时间", 105); frames.Columns.Add("方向", 60); frames.Columns.Add("原始 Modbus 帧", 680);
            foreach (var frame in diagnostic.Frames.Reverse())
            {
                frames.Items.Add(new ListViewItem(new[] { frame.Timestamp.ToString("HH:mm:ss.fff"), frame.Direction, frame.Hex }));
            }
            Controls.Add(frames);
        }
    }
}
