namespace MeterAcquisition.HeatPump.Protocol;

public static class HeatPumpRegisterMap
{
    public const ushort ControllerModeSetAddress = 1601;
    public const ushort ControllerSetTemperatureAddress = 1602;
    public const ushort ControllerDiffAddress = 1603;
    public const ushort ControllerLockAddress = 1604;
    public const ushort ControllerClearFaultAddress = 1606;
    public const ushort ControllerSettingsStart = 1601;
    public const ushort ControllerSettingsLength = 4;

    public const ushort ModuleBlockSize = 100;
    public const ushort ModuleRealtimeBlockStart = 0;
    public const ushort ModuleRealtimeBlockLength = 15;

    public const int OnlineOffset = 0;
    public const int RunModeOffset = 1;
    public const int PlateOutletWaterTemperatureOffset = 2;
    public const int AmbientTemperatureOffset = 5;
    public const int PlateInletWaterTemperatureOffset = 6;
    public const int FaultStatusOffset = 12;
    public const int ProtectStatusOffset = 13;
    public const int StateFlagOffset = 14;
    public const ushort StateFlagElectricHeaterBit = 1 << 6;
    public const ushort StateFlagWaterPumpBit = 1 << 5;
    public const ushort StateFlagCompressorBBit = 1 << 4;
    public const ushort StateFlagFourWayValveABit = 1 << 3;
    public const ushort StateFlagFourWayValveBBit = 1 << 2;

    public const ushort RunModeDefrostBit = 1 << 4;
    public const ushort RunModeOffBit = 1 << 3;
    public const ushort RunModePumpBit = 1 << 2;
    public const ushort RunModeHeatingBit = 1 << 1;
    public const ushort RunModeCoolingBit = 1 << 0;
}
