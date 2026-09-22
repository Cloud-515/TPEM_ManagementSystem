using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    partial class ThermostatStatusBar
    {
        private Label _statusLabel;

        private void InitializeComponent()
        {
            _statusLabel = new Label
            {
                AutoSize = true,
                MinimumSize = new Size(280, 0),
                Text = "未连接",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray,
                Font = UiStyle.BodyFont,
            };
            // 和另外两条工具条用同一套结构（工具条 + 固定行高条目），
            // 三条的高度才会一致；否则同一行里状态条比控制条矮几像素，中线对不上。
            var layout = UiStyle.CreateToolbar(UiStyle.Cell(_statusLabel));
            layout.Dock = DockStyle.Fill;
            layout.Padding = Padding.Empty;

            SuspendLayout();
            Controls.Add(layout);
            // UI-19：缩放基准统一交给 MainForm，见 ThermostatConnectionBar.Designer.cs 的说明。
            AutoScaleMode = AutoScaleMode.Inherit;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ResumeLayout(false);
        }
    }
}
