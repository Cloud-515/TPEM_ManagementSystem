using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition.Thermostat.Forms
{
    public sealed partial class ThermostatDetailsForm
    {
        private Label _titleLabel;
        private TableLayoutPanel _detailsCard;

        private void InitializeComponent()
        {
            _titleLabel = new Label { Dock = DockStyle.Top, Height = 58, Padding = new Padding(20, 14, 20, 0), Font = new Font(Font.FontFamily, 14, FontStyle.Bold) };
            _detailsCard = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 20), ColumnCount = 4, RowCount = 6 };
            _detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22)); _detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28)); _detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22)); _detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            for (var index = 0; index < 6; index++) _detailsCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
            SuspendLayout();
            Controls.Add(_detailsCard); Controls.Add(_titleLabel);
            AutoScaleMode = AutoScaleMode.Font; BackColor = Color.White; ClientSize = new Size(500, 430); MinimumSize = new Size(516, 469); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            ResumeLayout(false);
        }
    }
}