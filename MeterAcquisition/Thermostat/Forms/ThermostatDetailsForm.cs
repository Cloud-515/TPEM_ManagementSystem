using System.Drawing;
using System.Windows.Forms;
using MeterAcquisition.Thermostat.Application;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatDetailsForm : Form
    {
        public ThermostatDetailsForm()
        {
            InitializeComponent();
        }

        public ThermostatDetailsForm(ThermostatDeviceSnapshot snapshot)
            : this()
        {
            BindSnapshot(snapshot);
        }

        private void BindSnapshot(ThermostatDeviceSnapshot snapshot)
        {
            Text = snapshot.Device.Name + " - 温控器详情";
            _titleLabel.Text = snapshot.Device.Name;
            var telemetry = snapshot.Telemetry;
            AddPair(_detailsCard, 0, "地址", snapshot.Device.SlaveId.ToString(), "分组", snapshot.Device.GroupName);
            AddPair(_detailsCard, 1, "通信状态", telemetry == null ? "离线" : "在线", "最后更新时间", telemetry == null ? string.Empty : telemetry.CollectedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            AddPair(_detailsCard, 2, "室温", telemetry == null ? string.Empty : telemetry.RoomTemperatureCelsius.ToString("0.0") + " °C", "设定温度", telemetry == null ? string.Empty : telemetry.SetTemperatureCelsius.ToString("0.0") + " °C");
            AddPair(_detailsCard, 3, "开关", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatPower(telemetry.PowerState), "运行模式", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatMode(telemetry.Mode));
            AddPair(_detailsCard, 4, "当前风速", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatCurrentFanSpeed(telemetry.CurrentFanSpeed), "风速设定", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatFanSpeedSetting(telemetry.FanSpeedSetting));
            AddPair(_detailsCard, 5, "阀1状态", telemetry == null ? string.Empty : ThermostatMonitorControl.FormatValve1State(telemetry.WaterValveOpen), "设备状态", snapshot.Device.IsEnabled ? "已启用" : "已停用");
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
