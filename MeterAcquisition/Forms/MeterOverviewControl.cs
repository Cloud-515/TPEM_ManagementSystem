using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MeterAcquisition
{
    internal sealed partial class MeterOverviewControl : UserControl
    {
        private readonly Dictionary<string, MeterCardBinding> _cards = new Dictionary<string, MeterCardBinding>();
        private readonly Func<MeterInfo, (string Text, Color Color, bool IsOffline)> _statusResolver;

        public event EventHandler<MeterSelectedEventArgs> MeterSelected;

        public MeterOverviewControl()
        {
            InitializeComponent();
            _sectionsPanel.SizeChanged += SectionsPanel_SizeChanged;
        }

        public MeterOverviewControl(Func<MeterInfo, (string Text, Color Color, bool IsOffline)> statusResolver)
            : this()
        {
            _statusResolver = statusResolver ?? throw new ArgumentNullException(nameof(statusResolver));
        }

        public void RebuildMeters(IEnumerable<MeterInfo> meters)
        {
            var allMeters = (meters ?? Enumerable.Empty<MeterInfo>())
                .Where(meter => meter != null && !string.IsNullOrWhiteSpace(meter.Id))
                .ToList();

            _sectionsPanel.SuspendLayout();
            _sectionsPanel.Controls.Clear();
            _cards.Clear();

            AddSection("外部串口设备", allMeters.Where(meter => meter.IsToolbar).ToList());
            AddSection("仪表盘设备", allMeters.Where(meter => !meter.IsToolbar).ToList());

            if (_cards.Count == 0)
            {
                _sectionsPanel.Controls.Add(new Label
                {
                    Text = "暂无电表设备",
                    AutoSize = false,
                    Height = 64,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("微软雅黑", 11),
                    ForeColor = Color.Gray,
                    BackColor = Color.White,
                    Margin = new Padding(0),
                    Padding = new Padding(12),
                });
            }

            _sectionsPanel.ResumeLayout(true);
            ResizeSections();
            RefreshMeterValues(allMeters);
        }

        public void RefreshMeterValues(IEnumerable<MeterInfo> meters)
        {
            if (meters == null || _cards.Count == 0)
            {
                return;
            }

            foreach (var meter in meters)
            {
                if (meter == null || string.IsNullOrWhiteSpace(meter.Id))
                {
                    continue;
                }

                if (_cards.TryGetValue(meter.Id, out MeterCardBinding card))
                {
                    UpdateCard(card, meter);
                }
            }
        }

        private void AddSection(string titleText, List<MeterInfo> meters)
        {
            if (meters.Count == 0)
            {
                return;
            }

            var section = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0),
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = titleText + "  " + meters.Count + " 台",
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 38,
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 80, 160),
                Padding = new Padding(10, 8, 0, 0),
                BackColor = Color.FromArgb(240, 248, 255),
                Margin = new Padding(0),
            };
            section.Controls.Add(title, 0, 0);

            var cardsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10),
                Margin = new Padding(0),
                BackColor = Color.WhiteSmoke,
            };
            foreach (var meter in meters)
            {
                cardsPanel.Controls.Add(BuildCard(meter));
            }

            section.Controls.Add(cardsPanel, 0, 1);
            _sectionsPanel.Controls.Add(section);
        }

        private Control BuildCard(MeterInfo meter)
        {
            var card = new Panel
            {
                Width = 300,
                Height = 190,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(14, 12, 14, 12),
                Cursor = Cursors.Hand,
                Tag = meter.Id,
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(layout);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var nameLabel = new Label
            {
                Text = meter.Name,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                AutoSize = true,
                MaximumSize = new Size(190, 0),
                ForeColor = Color.FromArgb(45, 45, 45),
                Margin = new Padding(0, 4, 8, 0),
            };
            header.Controls.Add(nameLabel, 0, 0);

            var addressLabel = new Label
            {
                Text = "地址 " + meter.SlaveAddress,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 5, 0, 0),
            };
            header.Controls.Add(addressLabel, 1, 0);
            layout.Controls.Add(header, 0, 0);

            var line = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = Color.FromArgb(230, 230, 230),
                Margin = new Padding(0, 10, 0, 10),
            };
            layout.Controls.Add(line, 0, 1);

            var metrics = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            metrics.Controls.Add(CreateMetricTitle("三相功率"), 0, 0);
            var powerLabel = new Label
            {
                Text = "-- kW",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 140, 255),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12),
            };
            metrics.Controls.Add(powerLabel, 0, 1);
            metrics.SetColumnSpan(powerLabel, 2);

            metrics.Controls.Add(CreateMetricTitle("最大电流"), 0, 2);
            var currentLabel = new Label
            {
                Text = "-- A",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleRight,
            };
            metrics.Controls.Add(currentLabel, 1, 2);
            layout.Controls.Add(metrics, 0, 2);

            var statusLabel = new Label
            {
                Text = "● 等待数据",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 12, 0, 0),
            };
            layout.Controls.Add(statusLabel, 0, 3);

            var binding = new MeterCardBinding(card, powerLabel, currentLabel, statusLabel);
            _cards[meter.Id] = binding;
            UpdateCard(binding, meter);
            RegisterCardClick(card, meter.Id);
            return card;
        }

        private static Label CreateMetricTitle(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 8, 0),
            };
        }

        private void UpdateCard(MeterCardBinding card, MeterInfo meter)
        {
            var status = _statusResolver == null
                ? (Text: "设计预览", Color: Color.FromArgb(108, 117, 125), IsOffline: false)
                : _statusResolver(meter);
            if (!status.IsOffline && meter.RealTime != null)
            {
                var powerKw = meter.RealTime.ActivePowerTotal / 1000f;
                var maxCurrent = Math.Max(meter.RealTime.CurrentA, Math.Max(meter.RealTime.CurrentB, meter.RealTime.CurrentC));
                card.PowerLabel.Text = powerKw.ToString("F1") + " kW";
                card.CurrentLabel.Text = maxCurrent.ToString("F1") + " A";
            }
            else
            {
                card.PowerLabel.Text = "-- kW";
                card.CurrentLabel.Text = "-- A";
            }

            card.StatusLabel.Text = status.Text;
            card.StatusLabel.ForeColor = status.Color;
        }

        private void RegisterCardClick(Control parent, string meterId)
        {
            parent.Click += (sender, args) => MeterSelected?.Invoke(this, new MeterSelectedEventArgs(meterId));
            foreach (Control child in parent.Controls)
            {
                RegisterCardClick(child, meterId);
            }
        }

        private void SectionsPanel_SizeChanged(object sender, EventArgs e)
        {
            ResizeSections();
        }

        private void ResizeSections()
        {
            var availableWidth = _sectionsPanel.ClientSize.Width - _sectionsPanel.Padding.Horizontal;
            if (availableWidth < 320)
            {
                availableWidth = 320;
            }

            _sectionsPanel.SuspendLayout();
            foreach (Control section in _sectionsPanel.Controls)
            {
                section.MinimumSize = new Size(availableWidth, 0);
                section.MaximumSize = new Size(availableWidth, 0);
                section.Width = availableWidth;

                if (section is TableLayoutPanel table && table.Controls.Count > 1 && table.Controls[1] is FlowLayoutPanel cardsPanel)
                {
                    cardsPanel.MinimumSize = new Size(availableWidth, 0);
                    cardsPanel.MaximumSize = new Size(availableWidth, 0);
                    cardsPanel.Width = availableWidth;
                }
            }
            _sectionsPanel.ResumeLayout(true);
        }

        private sealed class MeterCardBinding
        {
            public MeterCardBinding(Panel card, Label powerLabel, Label currentLabel, Label statusLabel)
            {
                Card = card;
                PowerLabel = powerLabel;
                CurrentLabel = currentLabel;
                StatusLabel = statusLabel;
            }

            public Panel Card { get; }
            public Label PowerLabel { get; }
            public Label CurrentLabel { get; }
            public Label StatusLabel { get; }
        }
    }

    internal sealed class MeterSelectedEventArgs : EventArgs
    {
        public MeterSelectedEventArgs(string meterId)
        {
            MeterId = meterId;
        }

        public string MeterId { get; }
    }
}
