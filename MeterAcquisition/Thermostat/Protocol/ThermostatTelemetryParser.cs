using System;
using MeterAcquisition.Thermostat.Domain;

namespace MeterAcquisition.Thermostat.Protocol
{
    public static class ThermostatTelemetryParser
    {
        public static ThermostatTelemetry Parse(byte slaveId, ushort[] registers, DateTime collectedAt)
        {
            if (registers == null)
            {
                throw new ArgumentNullException(nameof(registers));
            }

            if (registers.Length < ThermostatRegisterMap.TelemetryCount)
            {
                throw new ArgumentException("温控器遥测寄存器数量不足。", nameof(registers));
            }

            var snapshot = new ushort[ThermostatRegisterMap.TelemetryCount];
            Array.Copy(registers, snapshot, snapshot.Length);

            return new ThermostatTelemetry(
                slaveId,
                ToTemperature(snapshot[ThermostatRegisterMap.SetTemperature]),
                ToTemperature(snapshot[ThermostatRegisterMap.RoomTemperature]),
                ToPowerState(snapshot[ThermostatRegisterMap.Power]),
                ToMode(snapshot[ThermostatRegisterMap.Mode]),
                ToCurrentFanSpeed(snapshot[ThermostatRegisterMap.CurrentFanSpeed]),
                ToFanSpeedSetting(snapshot[ThermostatRegisterMap.FanSpeedSetting]),
                ToWaterValveState(snapshot[ThermostatRegisterMap.WaterValve]),
                snapshot,
                collectedAt);
        }

        public static ushort ToTemperatureRegister(decimal temperatureCelsius)
        {
            var value = decimal.Round(temperatureCelsius / ThermostatRegisterMap.TemperatureScale, 0, MidpointRounding.AwayFromZero);
            if (value < 0 || value > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(temperatureCelsius));
            }

            return decimal.ToUInt16(value);
        }

        private static decimal ToTemperature(ushort value)
        {
            return value * ThermostatRegisterMap.TemperatureScale;
        }

        private static ThermostatPowerState ToPowerState(ushort value)
        {
            return value == 0 ? ThermostatPowerState.Off : value == 1 ? ThermostatPowerState.On : ThermostatPowerState.Unknown;
        }

        private static ThermostatMode ToMode(ushort value)
        {
            switch (value)
            {
                case 0: return ThermostatMode.FloorHeating;
                case 1: return ThermostatMode.FanCoilHeating;
                case 2: return ThermostatMode.CombinedHeating;
                case 3: return ThermostatMode.Cooling;
                case 4: return ThermostatMode.FloorCooling;
                case 5: return ThermostatMode.CombinedCooling;
                case 9: return ThermostatMode.Ventilation;
                default: return ThermostatMode.Unknown;
            }
        }

        private static ThermostatCurrentFanSpeed ToCurrentFanSpeed(ushort value)
        {
            return value == 1 ? ThermostatCurrentFanSpeed.Low : value == 2 ? ThermostatCurrentFanSpeed.Medium : value == 3 ? ThermostatCurrentFanSpeed.High : ThermostatCurrentFanSpeed.Unknown;
        }

        private static ThermostatFanSpeedSetting ToFanSpeedSetting(ushort value)
        {
            return value == 1 ? ThermostatFanSpeedSetting.Low : value == 2 ? ThermostatFanSpeedSetting.Medium : value == 3 ? ThermostatFanSpeedSetting.High : value == 4 ? ThermostatFanSpeedSetting.Auto : ThermostatFanSpeedSetting.Unknown;
        }

        private static bool? ToWaterValveState(ushort value)
        {
            return value == 0 ? (bool?)false : value == 1 ? true : null;
        }
    }
}
