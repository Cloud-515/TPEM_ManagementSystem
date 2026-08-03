using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.Thermostat.Domain;
using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.Thermostat.Services
{
    public interface IThermostatDataService : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(CommSettings settings, CancellationToken cancellationToken);
        Task DisconnectAsync();
        Task<IReadOnlyList<byte>> ScanAsync(byte startSlaveId, byte endSlaveId, CancellationToken cancellationToken);
        Task<ThermostatTelemetry> ReadTelemetryAsync(byte slaveId, CancellationToken cancellationToken);
        Task<ThermostatTelemetry> SetPowerAsync(byte slaveId, ThermostatPowerState powerState, CancellationToken cancellationToken);
        Task<ThermostatTelemetry> SetModeAsync(byte slaveId, ThermostatMode mode, CancellationToken cancellationToken);
        Task<ThermostatTelemetry> SetFanSpeedAsync(byte slaveId, ThermostatFanSpeedSetting fanSpeed, CancellationToken cancellationToken);
        Task<ThermostatTelemetry> SetTemperatureAsync(byte slaveId, decimal temperatureCelsius, CancellationToken cancellationToken);
        Task<ThermostatFanControlDiagnostic> DiagnoseFanControlAsync(byte slaveId, CancellationToken cancellationToken);
    }
}
