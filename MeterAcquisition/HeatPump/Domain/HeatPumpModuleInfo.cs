namespace MeterAcquisition.HeatPump.Domain;

public sealed class HeatPumpModuleInfo
{
    public required string Id { get; init; }

    public byte SlaveId { get; set; }

    public byte ModuleIndex { get; init; }

    public required string Name { get; set; }

    public bool IsSelected { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public HeatPumpTelemetry Telemetry { get; set; } = new();
}
