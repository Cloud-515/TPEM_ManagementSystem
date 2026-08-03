namespace MeterAcquisition.HeatPump.Domain;

public sealed class HeatPumpUnitInfo
{
    public required string Id { get; init; }

    public required string Name { get; set; }

    public byte DeviceAddress { get; set; }

    public bool IsPrimaryConnection { get; set; }

    public bool IsScannedUnit { get; set; }

    public bool IsSelected { get; set; }

    public HeatPumpTelemetry Telemetry { get; set; } = new();
}
