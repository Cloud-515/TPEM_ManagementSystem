using System;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public partial class ThermostatConnectionBar : UserControl
    {
        public ThermostatConnectionBar()
        {
            InitializeComponent();
        }

        public ComboBox PortComboBox => _portComboBox;
        public NumericUpDown StartAddress => _startAddress;
        public NumericUpDown EndAddress => _endAddress;
        public Button ConnectButton => _connectButton;
        public Button DisconnectButton => _disconnectButton;
        public Button ScanButton => _scanButton;
        public Button RefreshButton => _refreshButton;
    }
}