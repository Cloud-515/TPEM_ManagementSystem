using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Protocol;

public static class HeatPumpTelemetryParser
{
    public static HeatPumpTelemetry Parse(
        IReadOnlyList<ushort> controllerRegisters,
        IReadOnlyList<ushort> moduleRegisters)
    {
        if (controllerRegisters.Count < HeatPumpRegisterMap.ControllerSettingsLength)
        {
            throw new ArgumentException("线控器设置寄存器长度不足。", nameof(controllerRegisters));
        }

        if (moduleRegisters.Count < HeatPumpRegisterMap.ModuleRealtimeBlockLength)
        {
            throw new ArgumentException("热泵状态寄存器长度不足。", nameof(moduleRegisters));
        }

        var online = moduleRegisters[HeatPumpRegisterMap.OnlineOffset] == 1;
        var modeBits = moduleRegisters[HeatPumpRegisterMap.RunModeOffset];
        var faultBits = moduleRegisters[HeatPumpRegisterMap.FaultStatusOffset];
        var protectBits = moduleRegisters[HeatPumpRegisterMap.ProtectStatusOffset];
        var stateFlags = moduleRegisters[HeatPumpRegisterMap.StateFlagOffset];
        var runMode = ParseMode(modeBits);

        var status = !online
            ? HeatPumpUnitStatus.Offline
            : (faultBits != 0 || protectBits != 0)
                ? HeatPumpUnitStatus.Alarm
                : runMode == HeatPumpRunMode.Off
                    ? HeatPumpUnitStatus.Standby
                    : HeatPumpUnitStatus.Running;

        var plateOutlet = GetSignedTenths(moduleRegisters[HeatPumpRegisterMap.PlateOutletWaterTemperatureOffset]);
        var plateInlet = GetSignedTenths(moduleRegisters[HeatPumpRegisterMap.PlateInletWaterTemperatureOffset]);

        return new HeatPumpTelemetry
        {
            OutletWaterTemperature = plateOutlet,
            ReturnWaterTemperature = plateInlet,
            TargetTemperature = controllerRegisters[1] / 10m,
            AmbientTemperature = GetSignedTenths(moduleRegisters[HeatPumpRegisterMap.AmbientTemperatureOffset]),
            RunMode = runMode,
            Status = status,
            CompressorOn = status == HeatPumpUnitStatus.Running && (
                (stateFlags & HeatPumpRegisterMap.StateFlagCompressorBBit) != 0 ||
                runMode is HeatPumpRunMode.Cooling or HeatPumpRunMode.Heating or HeatPumpRunMode.Defrost),
            PumpOn = (stateFlags & HeatPumpRegisterMap.StateFlagWaterPumpBit) != 0,
            ElectricHeaterOn = (stateFlags & HeatPumpRegisterMap.StateFlagElectricHeaterBit) != 0,
            LastUpdatedAt = DateTime.Now,
        };
    }

    private static decimal GetSignedTenths(ushort rawValue)
    {
        return unchecked((short)rawValue) / 10m;
    }

    private static HeatPumpRunMode ParseMode(ushort modeBits)
    {
        if ((modeBits & HeatPumpRegisterMap.RunModeDefrostBit) != 0)
        {
            return HeatPumpRunMode.Defrost;
        }

        if ((modeBits & HeatPumpRegisterMap.RunModeCoolingBit) != 0)
        {
            return HeatPumpRunMode.Cooling;
        }

        if ((modeBits & HeatPumpRegisterMap.RunModeHeatingBit) != 0)
        {
            return HeatPumpRunMode.Heating;
        }

        if ((modeBits & HeatPumpRegisterMap.RunModePumpBit) != 0)
        {
            return HeatPumpRunMode.Pump;
        }

        if ((modeBits & HeatPumpRegisterMap.RunModeOffBit) != 0)
        {
            return HeatPumpRunMode.Off;
        }

        return HeatPumpRunMode.Auto;
    }
}
