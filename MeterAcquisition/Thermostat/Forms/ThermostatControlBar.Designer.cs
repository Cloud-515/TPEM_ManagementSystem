using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    partial class ThermostatControlBar
    {
        private ComboBox _modeComboBox;
        private ComboBox _fanSpeedComboBox;
        private Button _powerButton;
        private Button _temperatureButton;
        private Button _applyModeButton;
        private Button _applyFanSpeedButton;
        private Button _fanDiagnosticButton;

        private void InitializeComponent()
        {
            _modeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = UiStyle.BodyFont };
            _fanSpeedComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, Font = UiStyle.BodyFont };
            _modeComboBox.Items.AddRange(new object[] { "制冷", "制热", "送风", "自动" });
            _modeComboBox.SelectedIndex = 0;
            _fanSpeedComboBox.Items.AddRange(new object[] { "自动", "低速", "中速", "高速" });
            _fanSpeedComboBox.SelectedIndex = 0;
            _powerButton = UiStyle.CreateButton("切换开关");
            _temperatureButton = UiStyle.CreateButton("设温");
            _applyModeButton = UiStyle.CreateButton("写入模式");
            _applyFanSpeedButton = UiStyle.CreateButton("写入风速");
            _fanDiagnosticButton = UiStyle.CreateButton("风机诊断");

            var layout = UiStyle.CreateToolbar(
                _powerButton,
                _temperatureButton,
                UiStyle.Field("目标模式", _modeComboBox),
                _applyModeButton,
                UiStyle.Field("目标风速", _fanSpeedComboBox),
                _applyFanSpeedButton,
                _fanDiagnosticButton);
            layout.Dock = DockStyle.Fill;
            layout.Padding = Padding.Empty; // 外层 _toolbar 已经留了内边距

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
