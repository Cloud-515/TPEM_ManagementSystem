using System;
using System.Collections.Generic;

namespace MeterAcquisition
{
    public class ThermostatTelemetryMessage
    {
        public string MessageType { get; set; } = "thermostat.telemetry.v1";
        public Guid MessageId { get; set; }
        public string SiteCode { get; set; }
        public string DeviceKey { get; set; }
        public int SlaveId { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public DateTimeOffset CollectedAt { get; set; }
        public bool IsOnline { get; set; }
        public decimal? RoomTemperatureCelsius { get; set; }
        public decimal? SetTemperatureCelsius { get; set; }
        public string PowerState { get; set; }
        public string Mode { get; set; }
        public string FanSpeed { get; set; }
    }

    public class HeatPumpTelemetryMessage
    {
        public string MessageType { get; set; } = "heatpump.telemetry.v1";
        public Guid MessageId { get; set; }
        public string SiteCode { get; set; }
        public int ControllerSlaveId { get; set; }
        public int ModuleIndex { get; set; }
        public string ModuleName { get; set; }
        public string GroupName { get; set; }
        public DateTimeOffset CollectedAt { get; set; }
        public bool IsEnabled { get; set; }
        public int RunMode { get; set; }
        public int Status { get; set; }
        public decimal OutletWaterTemperature { get; set; }
        public decimal ReturnWaterTemperature { get; set; }
        public decimal TargetTemperature { get; set; }
        public decimal AmbientTemperature { get; set; }
        public bool CompressorOn { get; set; }
        public bool PumpOn { get; set; }
        public bool ElectricHeaterOn { get; set; }
        public string FaultCode { get; set; }
    }

    public class MeterTelemetryMessage
    {
        public string SiteCode { get; set; }
        public string BoxCode { get; set; }
        public string MeterCode { get; set; }
        public string MeterName { get; set; }
        public string Location { get; set; }
        public byte SlaveAddress { get; set; }
        public bool IsToolbar { get; set; }
        public string DeviceModel { get; set; }
        public string Source { get; set; }
        public DateTimeOffset CollectTime { get; set; }
        public string SampleType { get; set; }
        public RealTimeData RealTime { get; set; }
        public EnergyData Energy { get; set; }
        public PowerQualityData Quality { get; set; }
    }

    public class MeterRegistryMessage
    {
        public string SiteCode { get; set; }
        public string BoxCode { get; set; }
        public string BoxName { get; set; }
        public string Action { get; set; }
        public DateTime ScanTime { get; set; }
        public List<MeterRegistryItem> Meters { get; set; } = new List<MeterRegistryItem>();
    }

    public class MeterRegistryItem
    {
        public string MeterCode { get; set; }
        public string MeterName { get; set; }
        public string Location { get; set; }
        public byte SlaveAddress { get; set; }
        public bool IsToolbar { get; set; }
        public string DeviceModel { get; set; }
    }
}
