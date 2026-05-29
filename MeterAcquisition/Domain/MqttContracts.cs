using System;
using System.Collections.Generic;

namespace MeterAcquisition
{
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
