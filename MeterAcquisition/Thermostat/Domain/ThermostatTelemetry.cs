using System;

namespace MeterAcquisition.Thermostat.Domain
{
    public enum ThermostatPowerState
    {
        Unknown = -1,
        Off = 0,
        On = 1
    }

    public enum ThermostatMode
    {
        Unknown = -1,
        FloorHeating = 0,
        FanCoilHeating = 1,
        CombinedHeating = 2,
        Cooling = 3,
        FloorCooling = 4,
        CombinedCooling = 5,
        Ventilation = 9
    }

    public enum ThermostatCurrentFanSpeed
    {
        Unknown = -1,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum ThermostatFanSpeedSetting
    {
        Unknown = -1,
        Low = 1,
        Medium = 2,
        High = 3,
        Auto = 4
    }

    public sealed class ThermostatTelemetry
    {
        public ThermostatTelemetry(
            byte slaveId,
            decimal setTemperatureCelsius,
            decimal roomTemperatureCelsius,
            ThermostatPowerState powerState,
            ThermostatMode mode,
            ThermostatCurrentFanSpeed currentFanSpeed,
            ThermostatFanSpeedSetting fanSpeedSetting,
            bool? waterValveOpen,
            ushort[] rawRegisters,
            DateTime collectedAt)
        {
            SlaveId = slaveId;
            SetTemperatureCelsius = setTemperatureCelsius;
            RoomTemperatureCelsius = roomTemperatureCelsius;
            PowerState = powerState;
            Mode = mode;
            CurrentFanSpeed = currentFanSpeed;
            FanSpeedSetting = fanSpeedSetting;
            WaterValveOpen = waterValveOpen;
            RawRegisters = rawRegisters ?? throw new ArgumentNullException(nameof(rawRegisters));
            CollectedAt = collectedAt;
        }

        public byte SlaveId { get; }
        public decimal SetTemperatureCelsius { get; }
        public decimal RoomTemperatureCelsius { get; }
        public ThermostatPowerState PowerState { get; }
        public ThermostatMode Mode { get; }
        public ThermostatCurrentFanSpeed CurrentFanSpeed { get; }
        public ThermostatFanSpeedSetting FanSpeedSetting { get; }
        public bool? WaterValveOpen { get; }
        public ushort[] RawRegisters { get; }
        public DateTime CollectedAt { get; }
    }
}
