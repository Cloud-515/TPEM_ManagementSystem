using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    partial class ThermostatConnectionBar
    {
        private ComboBox _portComboBox;
        private NumericUpDown _startAddress;
        private NumericUpDown _endAddress;
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _scanButton;
        private Button _refreshButton;

        private void InitializeComponent()
        {
            _portComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 130, MinimumSize = new Size(130, 32) };
            _startAddress = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Width = 60, MinimumSize = new Size(60, 32) };
            _endAddress = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 99, Width = 60, MinimumSize = new Size(60, 32) };
            _portComboBox.Text = "COM3";
            _portComboBox.Text = "COM3";
            _connectButton = CreateButton("连接"); _disconnectButton = CreateButton("断开"); _scanButton = CreateButton("扫描"); _refreshButton = CreateButton("刷新");
            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0), Margin = Padding.Empty };
            layout.Controls.Add(CreateField("串口", _portComboBox)); layout.Controls.Add(CreateField("地址", _startAddress)); layout.Controls.Add(CreateField("至", _endAddress)); layout.Controls.Add(_connectButton); layout.Controls.Add(_disconnectButton); layout.Controls.Add(_scanButton); layout.Controls.Add(_refreshButton);
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