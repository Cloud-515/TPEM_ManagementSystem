using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Application;

public sealed class HeatPumpWorkspaceSnapshot
{
    public bool IsConnected { get; set; }

    public string ConnectionDescription { get; set; } = string.Empty;

    public CommSettings? Settings { get; set; }

    public IReadOnlyList<WiredControllerInfo> Controllers { get; set; } = Array.Empty<WiredControllerInfo>();

    public int ControllerCount { get; set; }

    public int ModuleCount { get; set; }

    public DateTime? LastRefreshAt { get; set; }
}
