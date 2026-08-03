using System;

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

        private readonly ModbusService _modbusService;

        public Amc96lE4MeterDataReader(ModbusService modbusService)
        {
            _modbusService = modbusService;
        }

        public string DeviceModel => "AMC96L-E4";

        public bool Probe(byte slaveAddress)
        {
            var registers = _modbusService.ReadHoldingRegisterValues(slaveAddress, RealtimeStartAddress, 2);
            return registers != null && IsFinite(ParseFloat(registers, 0));
        }

        public RealTimeData ReadRealTimeData()
        {
            return ReadRealTimeData(_modbusService.Config.SlaveAddress);
        }

        public RealTimeData ReadRealTimeData(byte slaveAddress)
        {
            var registers = _modbusService.ReadHoldingRegisterValues(slaveAddress, RealtimeStartAddress, RealtimeRegisterCount);
            if (registers == null)
            {
                return null;
            }

            return new RealTimeData
            {
                VoltageA = ParseFloat(registers, 0),
                VoltageB = ParseFloat(registers, 2),
                VoltageC = ParseFloat(registers, 4),
                VoltageAB = ParseFloat(registers, 6),
                VoltageBC = ParseFloat(registers, 8),
                VoltageCA = ParseFloat(registers, 10),
                CurrentA = ParseFloat(registers, 12),
                CurrentB = ParseFloat(registers, 14),
                CurrentC = ParseFloat(registers, 16),
                ActivePowerTotal = ParseFloat(registers, 26) * 1000f,
                ReactivePowerTotal = ParseFloat(registers, 34) * 1000f,
                ApparentPowerTotal = ParseFloat(registers, 42) * 1000f,
                PowerFactorTotal = ParseFloat(registers, 50),
                Frequency = ParseFloat(registers, 52)
            };
        }

        public EnergyData ReadEnergyData()
        {
            return ReadEnergyData(_modbusService.Config.SlaveAddress);
        }

        public EnergyData ReadEnergyData(byte slaveAddress)
        {
            var registers = _modbusService.ReadHoldingRegisterValues(slaveAddress, EnergyStartAddress, EnergyRegisterCount);
            if (registers == null)
            {
                return null;
            }

            return new EnergyData
            {
                ForwardActiveEnergy = ParseFloat(registers, 0),
                ReverseActiveEnergy = ParseFloat(registers, 2),
                ForwardReactiveEnergy = ParseFloat(registers, 6),
                ReverseReactiveEnergy = ParseFloat(registers, 8)
            };
        }

        public PowerQualityData ReadPowerQualityData()
        {
            return ReadPowerQualityData(_modbusService.Config.SlaveAddress);
        }

        public PowerQualityData ReadPowerQualityData(byte slaveAddress)
        {
            var harmonicRegisters = _modbusService.ReadHoldingRegisterValues(slaveAddress, QualityStartAddress, QualityRegisterCount);
            var unbalanceRegisters = _modbusService.ReadHoldingRegisterValues(slaveAddress, UnbalanceStartAddress, UnbalanceRegisterCount);
            if (harmonicRegisters == null || unbalanceRegisters == null)
            {
                return null;
            }

            return new PowerQualityData
            {
                VoltageTHDA = harmonicRegisters[0] / 100f,
                VoltageTHDB = harmonicRegisters[1] / 100f,
                VoltageTHDC = harmonicRegisters[2] / 100f,
                CurrentTHDA = harmonicRegisters[3] / 100f,
                CurrentTHDB = harmonicRegisters[4] / 100f,
                CurrentTHDC = harmonicRegisters[5] / 100f,
                VoltageUnbalance = unbalanceRegisters[0] / 10f,
                CurrentUnbalance = unbalanceRegisters[1] / 10f
            };
        }

        private static float ParseFloat(ushort[] registers, int offset)
        {
            var raw = ((uint)registers[offset] << 16) | registers[offset + 1];
            var bytes = BitConverter.GetBytes(raw);
            return BitConverter.ToSingle(bytes, 0);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
