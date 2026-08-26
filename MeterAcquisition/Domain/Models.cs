using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace MeterAcquisition
{
    public class CommunicationConfig
    {
        public byte SlaveAddress { get; set; } = 95;
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.Even;
        public StopBits StopBits { get; set; } = StopBits.One;
        public int DataBits { get; set; } = 8;
    }

    public class BasicConfig
    {
        public ushort WiringMode { get; set; } = 5;
        public uint PT { get; set; } = 1;
        public uint CT { get; set; } = 1;
        public ushort PowerFactorAlgorithm { get; set; } = 0;
        public ushort ApparentPowerMethod { get; set; } = 0;
        public ushort THDAlgorithm { get; set; } = 0;
        public ushort BacklightTime { get; set; } = 5;
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string PortName { get; set; }
        public string Message { get; set; }
    }

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

    public class MeterInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public byte SlaveAddress { get; set; }
        public bool IsToolbar { get; set; }
        public string DeviceModel { get; set; }
        public DateTime? LastSuccessfulReadTime { get; set; }

        /// <summary>连续采集失败次数。P0-8：让"某台表一直读不到"从日志里可见。</summary>
        public int ConsecutiveFailureCount { get; set; }

        public RealTimeData RealTime { get; set; }
        public EnergyData Energy { get; set; }
        public PowerQualityData Quality { get; set; }
    }

    public class MeterPollResult
    {
        public MeterInfo Meter { get; set; }
        public RealTimeData RealTime { get; set; }
        public EnergyData Energy { get; set; }
        public PowerQualityData Quality { get; set; }

        public bool HasAnyData => RealTime != null || Energy != null || Quality != null;
    }

    public class DistributionBox
    {
        public string Name { get; set; }
        public List<MeterInfo> Meters { get; set; } = new List<MeterInfo>();
    }

    public class DeviceInfo
    {
        public string DeviceType { get; set; }
        public ushort ProgramVersion { get; set; }
        public ushort ProtocolVersion { get; set; }
        public string VersionDate { get; set; }
        public uint SerialNumber { get; set; }
        public ushort IoConfig { get; set; }
    }
}
