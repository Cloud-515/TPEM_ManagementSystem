using System.Drawing;
using System.Windows.Forms;
using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class HeatPumpDetailsForm : Form
{
    private readonly Label _lblName;
    private readonly Label _lblAddress;
    private readonly Label _lblMode;
    private readonly Label _lblStatus;
    private readonly Label _lblOutlet;
    private readonly Label _lblReturn;
    private readonly Label _lblTarget;
    private readonly Label _lblAmbient;
    private readonly Label _lblComponents;
    private readonly Label _lblUpdatedAt;

    public HeatPumpDetailsForm()
    {
        Text = "机组详情";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 420);
        BackColor = Color.WhiteSmoke;
        Font = new Font("微软雅黑", 10.5F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16),
            BackColor = Color.WhiteSmoke,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(layout);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(18, 16, 18, 16),
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 12),
        };
        layout.Controls.Add(header, 0, 0);

        _lblName = new Label
        {
            AutoSize = true,
            Font = new Font("微软雅黑", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 80, 160),
            Location = new Point(18, 14),
        };
        header.Controls.Add(_lblName);

        _lblAddress = new Label
        {
            AutoSize = true,
            Font = new Font("微软雅黑", 10F),
            ForeColor = Color.DimGray,
            Location = new Point(18, 48),
        };
        header.Controls.Add(_lblAddress);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.White,
            Padding = new Padding(18),
            Margin = new Padding(0),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        for (var index = 0; index < 6; index++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.Controls.Add(grid, 0, 1);

        _lblMode = AddValueRow(grid, 0, 0, "运行模式");
        _lblStatus = AddValueRow(grid, 1, 0, "当前状态");
        _lblOutlet = AddValueRow(grid, 0, 1, "出水温度");
        _lblReturn = AddValueRow(grid, 1, 1, "回水温度");
        _lblTarget = AddValueRow(grid, 0, 2, "设定温度");
        _lblAmbient = AddValueRow(grid, 1, 2, "环境温度");
        _lblComponents = AddValueRow(grid, 0, 3, "部件状态");
        _lblUpdatedAt = AddValueRow(grid, 1, 3, "最近更新");
    }

    public void UpdateModule(HeatPumpModuleInfo module)
    {
        Text = $"机组详情 - {module.Name}";
        _lblName.Text = module.Name;
        _lblAddress.Text = $"控制器站号: {module.SlaveId} / 模块编号: {module.ModuleIndex + 1}";
        _lblMode.Text = FormatMode(module.Telemetry.RunMode);
        _lblStatus.Text = FormatStatus(module.Telemetry.Status);
        _lblOutlet.Text = $"{module.Telemetry.OutletWaterTemperature:F1} °C";
        _lblReturn.Text = $"{module.Telemetry.ReturnWaterTemperature:F1} °C";
        _lblTarget.Text = $"{module.Telemetry.TargetTemperature:F1} °C";
        _lblAmbient.Text = $"{module.Telemetry.AmbientTemperature:F1} °C";
        _lblComponents.Text = $"压缩机 {(module.Telemetry.CompressorOn ? "开" : "关")} / 水泵 {(module.Telemetry.PumpOn ? "开" : "关")} / 电辅热 {(module.Telemetry.ElectricHeaterOn ? "开" : "关")}";
        _lblUpdatedAt.Text = module.Telemetry.LastUpdatedAt == default
            ? "--"
            : module.Telemetry.LastUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static Label AddValueRow(TableLayoutPanel parent, int column, int row, string title)
    {
        var host = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            Margin = new Padding(0, 0, 12, 12),
            Padding = new Padding(0),
        };

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("微软雅黑", 9.5F),
            ForeColor = Color.Gray,
            Location = new Point(0, 0),
        };
        host.Controls.Add(titleLabel);

        var valueLabel = new Label
        {
            AutoSize = true,
            Font = new Font("微软雅黑", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 50, 50),
            Location = new Point(0, 28),
        };
        host.Controls.Add(valueLabel);

        parent.Controls.Add(host, column, row);
        return valueLabel;
    }

    private static string FormatMode(HeatPumpRunMode mode)
    {
        return mode switch
        {
            HeatPumpRunMode.Off => "关机",
            HeatPumpRunMode.Heating => "制热",
            HeatPumpRunMode.Cooling => "制冷",
            HeatPumpRunMode.Pump => "水泵",
            HeatPumpRunMode.Defrost => "化霜",
            _ => "未知",
        };
    }

    private static string FormatStatus(HeatPumpUnitStatus status)
    {
        return status switch
        {
            HeatPumpUnitStatus.Running => "运行中",
            HeatPumpUnitStatus.Standby => "待机",
            HeatPumpUnitStatus.Alarm => "告警",
            _ => "离线",
        };
    }
}
