using System.Drawing;
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
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(8), Margin = Padding.Empty };
            _connectionBar = new ThermostatConnectionBar { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 4) };
            _controlBar = new ThermostatControlBar { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 4) };
            _statusBar = new ThermostatStatusBar { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 4) };
            _portComboBox = _connectionBar.PortComboBox; _startAddress = _connectionBar.StartAddress; _endAddress = _connectionBar.EndAddress;
            _modeComboBox = _controlBar.ModeComboBox; _fanSpeedComboBox = _controlBar.FanSpeedComboBox; _statusLabel = _statusBar.StatusLabel; _grid = CreateGrid();
            _contentPanel.Controls.Add(_grid); _contentPanel.Controls.Add(_toolbar);
            _toolbar.Controls.Add(_connectionBar); _toolbar.Controls.Add(_controlBar); _toolbar.Controls.Add(_statusBar);
            Controls.Add(_contentPanel); AutoScaleMode = AutoScaleMode.Font; Dock = DockStyle.Fill;
        }
    }
}