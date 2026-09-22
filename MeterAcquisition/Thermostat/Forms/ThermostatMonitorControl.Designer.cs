using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatMonitorControl
    {
        private Panel _contentPanel;
        private FlowLayoutPanel _toolbar;
        private ThermostatConnectionBar _connectionBar;
        private ThermostatControlBar _controlBar;
        private ThermostatStatusBar _statusBar;

        private void InitializeComponent()
        {
            _contentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top };
            _connectionBar = new ThermostatConnectionBar();
            _controlBar = new ThermostatControlBar();
            _statusBar = new ThermostatStatusBar();
            _portComboBox = _connectionBar.PortComboBox;
            _startAddress = _connectionBar.StartAddress;
            _endAddress = _connectionBar.EndAddress;
            _modeComboBox = _controlBar.ModeComboBox;
            _fanSpeedComboBox = _controlBar.FanSpeedComboBox;
            _statusLabel = _statusBar.StatusLabel;
            _grid = CreateGrid();
            _contentPanel.Controls.Add(_grid);
            _contentPanel.Controls.Add(_toolbar);
            // 三条子工具条按统一间距排布；每条自己不换行，换行只发生在三条之间。
            UiStyle.RebuildToolbar(_toolbar, _connectionBar, _controlBar, _statusBar);
            Controls.Add(_contentPanel);
            // UI-19：缩放基准统一交给 MainForm，见 ThermostatConnectionBar.Designer.cs 的说明。
            AutoScaleMode = AutoScaleMode.Inherit;
            Dock = DockStyle.Fill;
        }
    }
}
