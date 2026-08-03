namespace MeterAcquisition.Thermostat.Protocol
{
    public static class ThermostatRegisterMap
    {
        public const ushort RoomTemperature = 0;
        public const ushort SetTemperature = 1;
        public const ushort Power = 2;
        public const ushort Mode = 3;
        // 点表 Modbus 地址 255/256 对应设备 PDU 映射地址 4/5；客户端传入的是 PDU 地址。
        // 点表地址 255/256 为 Modbus 文档地址；ModbusRtuClient 使用去掉文档偏移后的 PDU 映射地址 4/5。
        public const ushort CurrentFanSpeedPointAddress = 255;
        public const ushort FanSpeedSettingPointAddress = 256;
        public const ushort CurrentFanSpeed = 4;
        public const ushort FanSpeedSetting = 5;
        public const ushort WaterValve = 6;
        public const ushort TelemetryStart = 0;
        public const ushort TelemetryCount = 11;
        public const decimal TemperatureScale = 0.1m;
    }
}
