using System;
using System.Collections.Generic;
using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.Thermostat.Domain
{
    public sealed class ThermostatFanControlDiagnostic
    {
        public ThermostatFanControlDiagnostic(ThermostatTelemetry telemetry, IReadOnlyList<ModbusFrameRecord> frames)
        {
            Telemetry = telemetry;
            Frames = frames;
            Summary = BuildSummary(telemetry);
        }

        public ThermostatTelemetry Telemetry { get; }
        public IReadOnlyList<ModbusFrameRecord> Frames { get; }
        public string Summary { get; }

        private static string BuildSummary(ThermostatTelemetry telemetry)
        {
            if (telemetry == null)
            {
                return "未读取到温控器状态。";
            }

            if (telemetry.PowerState != ThermostatPowerState.On)
            {
                return "温控器未处于开机状态，风机可能不会运行。";
            }

            if (telemetry.WaterValveOpen != true)
            {
                return "阀1未开，设备策略可能不会启动风机。";
            }

            if (telemetry.Mode == ThermostatMode.FloorHeating || telemetry.Mode == ThermostatMode.FloorCooling)
            {
                return "当前为地板模式，风机可能不参与运行。";
            }

            if (telemetry.FanSpeedSetting == ThermostatFanSpeedSetting.Unknown)
            {
                return "未读到风速设定寄存器。";
            }

            if (telemetry.CurrentFanSpeed == ThermostatCurrentFanSpeed.Unknown)
            {
                return "风速设定已读取，但设备未报告实际风速；请检查风机电源、接线或设备联锁。";
            }

            return "风速设定与实际风速均已读取。";
        }
    }
}
