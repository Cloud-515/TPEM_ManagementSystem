namespace MeterAcquisition.HeatPump.Domain;

public sealed class WiredControllerInfo
{
    public required string Id { get; init; }

    public byte SlaveId { get; set; }

    public required string Name { get; set; }

    public bool IsSelected { get; set; }

    public bool IsOnline { get; set; }

    public bool ControllerLockEnabled { get; set; }

    public int Differential { get; set; } = 2;

    public List<HeatPumpModuleInfo> Modules { get; } = new();
}
