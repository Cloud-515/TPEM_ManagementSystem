namespace MeterAcquisition.HeatPump.Domain;

public enum HeatPumpRunMode
{
    Off,
    Cooling,
    Heating,
    Pump,
    Defrost,
    Auto
}

public enum HeatPumpUnitStatus
{
    Running,
    Standby,
    Alarm,
    Offline
}

public sealed class HeatPumpTelemetry
{
    public decimal OutletWaterTemperature { get; set; }

    public decimal ReturnWaterTemperature { get; set; }

    public decimal TargetTemperature { get; set; }

    public decimal AmbientTemperature { get; set; }

    public HeatPumpRunMode RunMode { get; set; }

    public HeatPumpUnitStatus Status { get; set; }

    public bool CompressorOn { get; set; }

    public bool PumpOn { get; set; }

    public bool ElectricHeaterOn { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
