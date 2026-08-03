using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MeterAcquisition.Thermostat.Domain;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatFanDiagnosticForm : Form
    {
        public ThermostatFanDiagnosticForm()
        {
            InitializeComponent();
        }

        public ThermostatFanDiagnosticForm(ThermostatFanControlDiagnostic diagnostic)
            : this()
        {
            Text = "风机命令诊断";
            _summaryTextBox.Text = diagnostic.Summary;
            var telemetry = diagnostic.Telemetry;
            _stateListView.Items.Add(new ListViewItem(new[]
            {
                ThermostatMonitorControl.FormatPower(telemetry.PowerState), ThermostatMonitorControl.FormatMode(telemetry.Mode),
                ThermostatMonitorControl.FormatValve1State(telemetry.WaterValveOpen), ThermostatMonitorControl.FormatFanSpeedSetting(telemetry.FanSpeedSetting),
                ThermostatMonitorControl.FormatCurrentFanSpeed(telemetry.CurrentFanSpeed)
            }));
            foreach (var frame in diagnostic.Frames.Reverse())
            {
                _framesListView.Items.Add(new ListViewItem(new[] { frame.Timestamp.ToString("HH:mm:ss.fff"), frame.Direction, frame.Hex }));
            }
        }
    }
}
