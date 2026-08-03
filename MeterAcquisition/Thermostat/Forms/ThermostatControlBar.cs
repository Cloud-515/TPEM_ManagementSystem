using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public partial class ThermostatControlBar : UserControl
    {
        public ThermostatControlBar()
        {
            InitializeComponent();
        }

        public ComboBox ModeComboBox => _modeComboBox;
        public ComboBox FanSpeedComboBox => _fanSpeedComboBox;
        public Button PowerButton => _powerButton;
        public Button TemperatureButton => _temperatureButton;
        public Button ApplyModeButton => _applyModeButton;
        public Button ApplyFanSpeedButton => _applyFanSpeedButton;
        public Button FanDiagnosticButton => _fanDiagnosticButton;
    }
}