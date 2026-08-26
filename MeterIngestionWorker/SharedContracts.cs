using System;
using System.Collections.Generic;

namespace MeterIngestionWorker
{
    public class RealTimeData
    {
        public float? VoltageA { get; set; }
        public float? VoltageB { get; set; }
        public float? VoltageC { get; set; }
        public float? VoltageAB { get; set; }
        public float? VoltageBC { get; set; }
        public float? VoltageCA { get; set; }
        public float? CurrentA { get; set; }
        public float? CurrentB { get; set; }
        public float? CurrentC { get; set; }
        public float? ActivePowerTotal { get; set; }
        public float? ReactivePowerTotal { get; set; }
        public float? ApparentPowerTotal { get; set; }
        public float? PowerFactorTotal { get; set; }
        public float? Frequency { get; set; }
    }

    public class EnergyData
    {
        public float? ForwardActiveEnergy { get; set; }
        public float? ReverseActiveEnergy { get; set; }
        public float? ForwardReactiveEnergy { get; set; }
        public float? ReverseReactiveEnergy { get; set; }
    }

    public class PowerQualityData
    {
        public float? CurrentTHDA { get; set; }
        public float? CurrentTHDB { get; set; }
        public float? CurrentTHDC { get; set; }
        public float? VoltageTHDA { get; set; }
        public float? VoltageTHDB { get; set; }
        public float? VoltageTHDC { get; set; }
        public float? VoltageUnbalance { get; set; }
        public float? CurrentUnbalance { get; set; }
    }

    public class ThermostatTelemetryMessage
{
    public string MessageType { get; set; }
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
        public string MessageType { get; set; }
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
        /// <summary>P1-4：消息类型与版本号。此前电表消息完全没有版本字段，改结构无兼容手段。</summary>
        public string MessageType { get; set; }

        /// <summary>P1-4：消息唯一标识，供入库服务做库级幂等去重（与热泵/温控器链路一致）。</summary>
        public Guid MessageId { get; set; }

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
        /// <summary>P1-8：改为 DateTimeOffset，与遥测的 CollectTime 统一时间基准。
        /// 原为 DateTime（Kind=Local），机器时区一旦不是 +08:00 就与遥测时间错位。</summary>
        public DateTimeOffset ScanTime { get; set; }
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
