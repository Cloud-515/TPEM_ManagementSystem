using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public partial class ThermostatStatusBar : UserControl
    {
        public ThermostatStatusBar()
        {
            InitializeComponent();
        }

        public Label StatusLabel => _statusLabel;
    }
}