using System.Drawing;
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
            _modeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, MinimumSize = new Size(120, 32) };
            _fanSpeedComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, MinimumSize = new Size(90, 32) };
            _modeComboBox.Items.AddRange(new object[] { "制冷", "制热", "送风", "自动" }); _modeComboBox.SelectedIndex = 0;
            _fanSpeedComboBox.Items.AddRange(new object[] { "自动", "低速", "中速", "高速" }); _fanSpeedComboBox.SelectedIndex = 0;
            _powerButton = CreateButton("切换开关"); _temperatureButton = CreateButton("设温"); _applyModeButton = CreateButton("写入模式"); _applyFanSpeedButton = CreateButton("写入风速"); _fanDiagnosticButton = CreateButton("风机诊断");
            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0), Margin = Padding.Empty };
            layout.Controls.Add(_powerButton); layout.Controls.Add(_temperatureButton); layout.Controls.Add(CreateField("目标模式", _modeComboBox)); layout.Controls.Add(_applyModeButton); layout.Controls.Add(CreateField("目标风速", _fanSpeedComboBox)); layout.Controls.Add(_applyFanSpeedButton); layout.Controls.Add(_fanDiagnosticButton);
            SuspendLayout(); Controls.Add(layout); AutoScaleMode = AutoScaleMode.Font; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; ResumeLayout(false);
        }

        private static FlowLayoutPanel CreateField(string text, Control input)
        {
            var field = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 8, 0) };
            field.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 4, 0) }); input.Margin = new Padding(0, 2, 0, 2); field.Controls.Add(input); return field;
        }

        private static Button CreateButton(string text) => new Button { Text = text, AutoSize = true, MinimumSize = new Size(76, 32), Margin = new Padding(6, 2, 6, 2), Padding = new Padding(12, 6, 12, 6) };
    }
}