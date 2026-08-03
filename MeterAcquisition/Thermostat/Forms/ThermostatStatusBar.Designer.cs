using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    partial class ThermostatStatusBar
    {
        private Label _statusLabel;

        private void InitializeComponent()
        {
            _statusLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, MinimumSize = new Size(280, 32), Text = "未连接", TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray };
            SuspendLayout(); Controls.Add(_statusLabel); AutoScaleMode = AutoScaleMode.Font; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; ResumeLayout(false);
        }
    }
}