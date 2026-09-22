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
            _portComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 130, Font = UiStyle.BodyFont };
            _portComboBox.Text = "COM3";
            _startAddress = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Width = 64, Font = UiStyle.BodyFont };
            _endAddress = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 99, Width = 64, Font = UiStyle.BodyFont };
            _connectButton = UiStyle.CreateButton("连接");
            _disconnectButton = UiStyle.CreateButton("断开");
            _scanButton = UiStyle.CreateButton("扫描");
            _refreshButton = UiStyle.CreateButton("刷新");

            var layout = UiStyle.CreateToolbar(
                UiStyle.Field("串口", _portComboBox),
                UiStyle.Field("地址", _startAddress),
                UiStyle.Field("至", _endAddress),
                _connectButton,
                _disconnectButton,
                _scanButton,
                _refreshButton);
            layout.Dock = DockStyle.Fill;
            layout.Padding = Padding.Empty; // 外层 _toolbar 已经留了内边距

            SuspendLayout();
            Controls.Add(layout);
            // UI-19：子容器不要自己算缩放基准。
            // AutoScaleMode.Font + 未声明 AutoScaleDimensions 会拿 6×13 当基准，
            // 而这里的环境字体是微软雅黑 9.75（实测 10×21），于是控件里所有像素常量被放大约 1.6 倍
            // —— 同一份 MinimumSize(76,32) 在这里画出 95×52，在 MainForm 里画出 76×32。
            // 改成 Inherit：整窗只由 MainForm 一处决定缩放，代码里的 1px 到哪个页面都是 1px。
            AutoScaleMode = AutoScaleMode.Inherit;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ResumeLayout(false);
        }
    }
}
