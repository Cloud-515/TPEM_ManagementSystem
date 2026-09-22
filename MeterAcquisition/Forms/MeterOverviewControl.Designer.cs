using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition
{
    internal sealed partial class MeterOverviewControl
    {
        private FlowLayoutPanel _sectionsPanel;

        private void InitializeComponent()
        {
            _sectionsPanel = new FlowLayoutPanel();
            SuspendLayout();
            //
            // _sectionsPanel
            //
            _sectionsPanel.AutoScroll = true;
            _sectionsPanel.BackColor = Color.Transparent;
            _sectionsPanel.Dock = DockStyle.Fill;
            _sectionsPanel.FlowDirection = FlowDirection.TopDown;
            _sectionsPanel.Padding = new Padding(0, 0, 12, 0);
            _sectionsPanel.WrapContents = false;
            //
            // MeterOverviewControl
            //
            AutoScaleMode = AutoScaleMode.Inherit; // UI-19：缩放基准统一交给 MainForm
            BackColor = Color.FromArgb(245, 247, 250);
            Controls.Add(_sectionsPanel);
            Dock = DockStyle.Fill;
            Padding = new Padding(12);
            ResumeLayout(false);
        }
    }
}