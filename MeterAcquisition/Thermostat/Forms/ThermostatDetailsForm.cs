using System.Drawing;
using System.Windows.Forms;
using MeterAcquisition.Thermostat.Application;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed class ThermostatDetailsForm : Form
    {
        public ThermostatDetailsForm(ThermostatDeviceSnapshot snapshot)
        {
            Text = snapshot.Device.Name + " - 温控器详情";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(500, 430);
            MinimumSize = new Size(516, 469);
            BackColor = Color.White;

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(20, 14, 20, 0),
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                Text = snapshot.Device.Name
            };
            Controls.Add(title);

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                ColumnCount = 4,
                RowCount = 6
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            for (var index = 0; index < 6; index++)
            {
                card.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
            }

            var telemetry = snapshot.Telemetry;
            AddPair(card, 0, "地址", snapshot.Device.SlaveId.ToString(), "分组", snapshot.Device.GroupName);
            AddPair(card, 1, "通信状态", telemetry == null ? "离线" : "在线", "最后更新时间", telemetry == null ? string.Empty : telemetry.CollectedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            AddPair(card, 2, "室温", telemetry == null ? string.Empty : telemetry.RoomTemperatureCelsius.ToString("0.0") + " °C", "设定温度", telemetry == null ? string.Empty : telemetry.SetTemperatureCelsius.ToString("0.0") + " °C");
            AddPair(card, 3, "开关", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatPower(telemetry.PowerState), "运行模式", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatMode(telemetry.Mode));
            AddPair(card, 4, "当前风速", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatCurrentFanSpeed(telemetry.CurrentFanSpeed), "风速设定", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatFanSpeedSetting(telemetry.FanSpeedSetting));
            AddPair(card, 5, "阀1状态", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatValve1State(telemetry.WaterValveOpen), "设备状态", snapshot.Device.IsEnabled ? "已启用" : "已停用");
            Controls.Add(card);
        }

        private static void AddPair(TableLayoutPanel card, int row, string leftTitle, string leftValue, string rightTitle, string rightValue)
        {
            card.Controls.Add(CreateLabel(leftTitle, true), 0, row);
            card.Controls.Add(CreateLabel(leftValue, false), 1, row);
            card.Controls.Add(CreateLabel(rightTitle, true), 2, row);
            card.Controls.Add(CreateLabel(rightValue, false), 3, row);
        }

        private static Label CreateLabel(string text, bool isTitle)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = isTitle ? Color.DimGray : Color.FromArgb(35, 35, 35),
                Font = new Font(SystemFonts.DefaultFont, isTitle ? FontStyle.Regular : FontStyle.Bold),
                BorderStyle = isTitle ? BorderStyle.None : BorderStyle.FixedSingle,
                Padding = isTitle ? new Padding(0) : new Padding(8, 0, 4, 0)
            };
        }
    }
}
