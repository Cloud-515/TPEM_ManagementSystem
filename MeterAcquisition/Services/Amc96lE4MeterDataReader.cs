using System;
using Tpem.Protocol;

namespace MeterAcquisition
{
    public sealed class Amc96lE4MeterDataReader : IMeterDataReader
    {
        private const ushort RealtimeStartAddress = 0x2000;
        private const ushort RealtimeRegisterCount = 54;
        private const ushort EnergyStartAddress = 0x3082;
        private const ushort EnergyRegisterCount = 10;
        private const ushort QualityStartAddress = 0x0400;
        private const ushort QualityRegisterCount = 6;
        private const ushort UnbalanceStartAddress = 0x0700;
        private const ushort UnbalanceRegisterCount = 2;

        /// <summary>
        /// AMC96L-E4 实时数据映射（P2-1）。
        /// 偏移是"字节偏移"，等于原来的寄存器下标 × 2。
        /// 功率类原始值单位是 kW/kvar/kVA，乘 1000 统一成 W/var/VA ——
        /// 这个 ×1000 原来是写在赋值行末尾的裸字面量，现在是映射表里的 Scale 列。
        /// </summary>
        private static readonly RegisterField<RealTimeData>[] RealtimeMap =
        {
            new RegisterField<RealTimeData>("VoltageA", 0, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageA = v),
            new RegisterField<RealTimeData>("VoltageB", 4, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageB = v),
            new RegisterField<RealTimeData>("VoltageC", 8, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageC = v),
            new RegisterField<RealTimeData>("VoltageAB", 12, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageAB = v),
            new RegisterField<RealTimeData>("VoltageBC", 16, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageBC = v),
            new RegisterField<RealTimeData>("VoltageCA", 20, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageCA = v),
            new RegisterField<RealTimeData>("CurrentA", 24, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentA = v),
            new RegisterField<RealTimeData>("CurrentB", 28, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentB = v),
            new RegisterField<RealTimeData>("CurrentC", 32, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentC = v),
            new RegisterField<RealTimeData>("ActivePowerTotal", 52, RegisterValueType.Float32, 1000f, "W", (d, v) => d.ActivePowerTotal = v),
            new RegisterField<RealTimeData>("ReactivePowerTotal", 68, RegisterValueType.Float32, 1000f, "var", (d, v) => d.ReactivePowerTotal = v),
            new RegisterField<RealTimeData>("ApparentPowerTotal", 84, RegisterValueType.Float32, 1000f, "VA", (d, v) => d.ApparentPowerTotal = v),
            new RegisterField<RealTimeData>("PowerFactorTotal", 100, RegisterValueType.Float32, 1f, string.Empty, (d, v) => d.PowerFactorTotal = v),
            new RegisterField<RealTimeData>("Frequency", 104, RegisterValueType.Float32, 1f, "Hz", (d, v) => d.Frequency = v)
        };

        /// <summary>AMC96L-E4 电能映射。原始为 32 位浮点，单位已是 kWh/kvarh。</summary>
        private static readonly RegisterField<EnergyData>[] EnergyMap =
        {
            new RegisterField<EnergyData>("ForwardActiveEnergy", 0, RegisterValueType.Float32, 1f, "kWh", (d, v) => d.ForwardActiveEnergy = v),
            new RegisterField<EnergyData>("ReverseActiveEnergy", 4, RegisterValueType.Float32, 1f, "kWh", (d, v) => d.ReverseActiveEnergy = v),
            new RegisterField<EnergyData>("ForwardReactiveEnergy", 12, RegisterValueType.Float32, 1f, "kvarh", (d, v) => d.ForwardReactiveEnergy = v),
            new RegisterField<EnergyData>("ReverseReactiveEnergy", 16, RegisterValueType.Float32, 1f, "kvarh", (d, v) => d.ReverseReactiveEnergy = v)
        };

        /// <summary>
        /// AMC96L-E4 谐波映射：单寄存器整数，0.01% 一档，故 Scale=0.01 换成 %。
        /// 原写法是 `harmonicRegisters[0] / 100f`。
        /// </summary>
        private static readonly RegisterField<PowerQualityData>[] HarmonicMap =
        {
            new RegisterField<PowerQualityData>("VoltageTHDA", 0, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.VoltageTHDA = v),
            new RegisterField<PowerQualityData>("VoltageTHDB", 2, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.VoltageTHDB = v),
            new RegisterField<PowerQualityData>("VoltageTHDC", 4, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.VoltageTHDC = v),
            new RegisterField<PowerQualityData>("CurrentTHDA", 6, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.CurrentTHDA = v),
            new RegisterField<PowerQualityData>("CurrentTHDB", 8, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.CurrentTHDB = v),
            new RegisterField<PowerQualityData>("CurrentTHDC", 10, RegisterValueType.UInt16, 0.01f, "%", (d, v) => d.CurrentTHDC = v)
        };

        /// <summary>不平衡度：0.1% 一档，故 Scale=0.1。原写法是 `unbalanceRegisters[0] / 10f`。</summary>
        private static readonly RegisterField<PowerQualityData>[] UnbalanceMap =
        {
            new RegisterField<PowerQualityData>("VoltageUnbalance", 0, RegisterValueType.UInt16, 0.1f, "%", (d, v) => d.VoltageUnbalance = v),
            new RegisterField<PowerQualityData>("CurrentUnbalance", 2, RegisterValueType.UInt16, 0.1f, "%", (d, v) => d.CurrentUnbalance = v)
        };

        internal static RegisterField<RealTimeData>[] RealtimeFields => RealtimeMap;
        internal static RegisterField<EnergyData>[] EnergyFields => EnergyMap;
        internal static RegisterField<PowerQualityData>[] HarmonicFields => HarmonicMap;
        internal static RegisterField<PowerQualityData>[] UnbalanceFields => UnbalanceMap;

        private readonly ModbusService _modbusService;

        public Amc96lE4MeterDataReader(ModbusService modbusService)
        {
            _modbusService = modbusService;
        }

        public string DeviceModel => "AMC96L-E4";

        public bool Probe(byte slaveAddress)
        {
            var data = _modbusService.ReadHoldingRegisters(slaveAddress, RealtimeStartAddress, 2);
            return ModbusCodec.ReadFloat32(data, 0, Modbus32BitOrder.Abcd).HasValue;
        }

        public RealTimeData ReadRealTimeData()
        {
            return ReadRealTimeData(_modbusService.Config.SlaveAddress);
        }

        public RealTimeData ReadRealTimeData(byte slaveAddress)
        {
            var data = _modbusService.ReadHoldingRegisters(slaveAddress, RealtimeStartAddress, RealtimeRegisterCount);
            if (!MeterDataService.IsResponseUsable(data, RegisterMapReader.RequiredLength(RealtimeMap), slaveAddress, "实时数据", RealtimeStartAddress))
            {
                return null;
            }

            var result = new RealTimeData();
            RegisterMapReader.Apply(RealtimeMap, data, result);
            return result;
        }

        public EnergyData ReadEnergyData()
        {
            return ReadEnergyData(_modbusService.Config.SlaveAddress);
        }

        public EnergyData ReadEnergyData(byte slaveAddress)
        {
            var data = _modbusService.ReadHoldingRegisters(slaveAddress, EnergyStartAddress, EnergyRegisterCount);
            if (!MeterDataService.IsResponseUsable(data, RegisterMapReader.RequiredLength(EnergyMap), slaveAddress, "电能数据", EnergyStartAddress))
            {
                return null;
            }

            var result = new EnergyData();
            RegisterMapReader.Apply(EnergyMap, data, result);
            return result;
        }

        public PowerQualityData ReadPowerQualityData()
        {
            return ReadPowerQualityData(_modbusService.Config.SlaveAddress);
        }

        public PowerQualityData ReadPowerQualityData(byte slaveAddress)
        {
            var harmonic = _modbusService.ReadHoldingRegisters(slaveAddress, QualityStartAddress, QualityRegisterCount);
            var unbalance = _modbusService.ReadHoldingRegisters(slaveAddress, UnbalanceStartAddress, UnbalanceRegisterCount);

            var harmonicOk = MeterDataService.IsResponseUsable(
                harmonic, RegisterMapReader.RequiredLength(HarmonicMap), slaveAddress, "谐波", QualityStartAddress);
            var unbalanceOk = MeterDataService.IsResponseUsable(
                unbalance, RegisterMapReader.RequiredLength(UnbalanceMap), slaveAddress, "不平衡度", UnbalanceStartAddress);

            // P1-5：两个块相互独立，其中一个失败不再把整份数据丢掉，
            // 只让失败那部分字段留 null。原实现是任一块失败就整体返回 null。
            if (!harmonicOk && !unbalanceOk)
            {
                return null;
            }

            var result = new PowerQualityData();
            if (harmonicOk)
            {
                RegisterMapReader.Apply(HarmonicMap, harmonic, result);
            }

            if (unbalanceOk)
            {
                RegisterMapReader.Apply(UnbalanceMap, unbalance, result);
            }

            return result;
        }
    }
}
