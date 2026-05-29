using System;
using System.Collections.Generic;

namespace MeterIngestionWorker
{
    public class RealTimeData
    {
        public float VoltageA { get; set; }
        public float VoltageB { get; set; }
        public float VoltageC { get; set; }
        public float VoltageAB { get; set; }
        public float VoltageBC { get; set; }
        public float VoltageCA { get; set; }
        public float CurrentA { get; set; }
        public float CurrentB { get; set; }
        public float CurrentC { get; set; }
        public float ActivePowerTotal { get; set; }
        public float ReactivePowerTotal { get; set; }
        public float ApparentPowerTotal { get; set; }
        public float PowerFactorTotal { get; set; }
        public float Frequency { get; set; }
    }

    public class EnergyData
    {
        public float ForwardActiveEnergy { get; set; }
        public float ReverseActiveEnergy { get; set; }
        public float ForwardReactiveEnergy { get; set; }
        public float ReverseReactiveEnergy { get; set; }
    }

    public class PowerQualityData
    {
        public float CurrentTHDA { get; set; }
        public float CurrentTHDB { get; set; }
        public float CurrentTHDC { get; set; }
        public float VoltageTHDA { get; set; }
        public float VoltageTHDB { get; set; }
        public float VoltageTHDC { get; set; }
        public float VoltageUnbalance { get; set; }
        public float CurrentUnbalance { get; set; }
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
    }
}
