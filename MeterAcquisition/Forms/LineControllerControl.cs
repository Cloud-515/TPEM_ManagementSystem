using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition
{
    internal sealed partial class LineControllerControl : UserControl
    {
        public LineControllerControl()
        {
            InitializeComponent();
        }

        internal Panel ConnectionBarHost => _connectionBarHost;
        internal Panel GroupControlBarHost => _groupControlBarHost;
        internal FlowLayoutPanel CardsPanel => _cardsPanel;
        internal Label PageStatusLabel => _pageStatusLabel;
        internal Label ConnectionValueLabel => _connectionValueLabel;
        internal Label ControllerCountValueLabel => _controllerCountValueLabel;
        internal Label ModuleCountValueLabel => _moduleCountValueLabel;
        internal Label RefreshValueLabel => _refreshValueLabel;
        internal Label SelectionSummaryLabel => _selectionSummaryLabel;
    }
}