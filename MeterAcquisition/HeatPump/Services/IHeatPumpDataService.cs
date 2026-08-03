using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Services;

public interface IHeatPumpDataService : IDisposable
{
    bool IsConnected { get; }

    string ConnectionDescription { get; }

    Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(byte slaveId, CancellationToken cancellationToken = default);

    Task<HeatPumpTelemetry> ReadTelemetryAsync(byte slaveId, byte moduleIndex, CancellationToken cancellationToken = default);

    Task SetRunModeAsync(byte slaveId, HeatPumpRunMode mode, CancellationToken cancellationToken = default);

    Task SetTargetTemperatureAsync(byte slaveId, decimal temperature, CancellationToken cancellationToken = default);

    Task SetDifferentialAsync(byte slaveId, int differential, CancellationToken cancellationToken = default);

    Task SetControllerLockAsync(byte slaveId, bool locked, CancellationToken cancellationToken = default);

    Task ClearFaultAsync(byte slaveId, CancellationToken cancellationToken = default);
}
