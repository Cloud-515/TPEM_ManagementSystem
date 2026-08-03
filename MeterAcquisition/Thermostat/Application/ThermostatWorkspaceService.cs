using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.Thermostat.Domain;
using MeterAcquisition.Thermostat.Services;

namespace MeterAcquisition.Thermostat.Application
{
    public sealed class ThermostatWorkspaceService : IDisposable
    {
        private readonly IThermostatDataService _dataService;
        private readonly Dictionary<byte, ThermostatDeviceInfo> _devices = new Dictionary<byte, ThermostatDeviceInfo>();
        private readonly Dictionary<byte, ThermostatTelemetry> _telemetry = new Dictionary<byte, ThermostatTelemetry>();

        public ThermostatWorkspaceService()
            : this(new ModbusThermostatDataService())
        {
        }

        internal ThermostatWorkspaceService(IThermostatDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public bool IsConnected => _dataService.IsConnected;

        public async Task<bool> ConnectAsync(CommSettings settings)
        {
            try
            {
                await _dataService.ConnectAsync(settings, CancellationToken.None).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task DisconnectAsync()
        {
            return _dataService.DisconnectAsync();
        }

        public void ApplyConfiguration(ThermostatWorkspaceConfiguration configuration)
        {
            _devices.Clear();
            _telemetry.Clear();
            if (configuration == null || configuration.Devices == null)
            {
                return;
            }

            foreach (var device in configuration.Devices.Where(item => item != null && item.SlaveId >= 1 && item.SlaveId <= 99).GroupBy(item => item.SlaveId).Select(group => group.First()))
            {
                _devices[device.SlaveId] = new ThermostatDeviceInfo(device.SlaveId, device.Name, device.GroupName, device.IsEnabled);
            }
        }

        public ThermostatWorkspaceConfiguration ExportConfiguration()
        {
            return new ThermostatWorkspaceConfiguration
            {
                Devices = _devices.Values.OrderBy(device => device.SlaveId).Select(device => new ThermostatDeviceConfiguration
                {
                    SlaveId = device.SlaveId,
                    Name = device.Name,
                    GroupName = device.GroupName,
                    IsEnabled = device.IsEnabled
                }).ToList()
            };
        }

        public async Task<IReadOnlyList<byte>> ScanAsync(byte startSlaveId, byte endSlaveId)
        {
            var found = await _dataService.ScanAsync(startSlaveId, endSlaveId, CancellationToken.None).ConfigureAwait(false);
            foreach (var slaveId in found)
            {
                if (!_devices.ContainsKey(slaveId))
                {
                    _devices[slaveId] = new ThermostatDeviceInfo(slaveId, null, null, true);
                }
            }

            return found;
        }

        public async Task<IReadOnlyList<ThermostatRefreshResult>> RefreshTelemetryAsync()
        {
            var results = new List<ThermostatRefreshResult>();
            foreach (var device in _devices.Values.Where(item => item.IsEnabled).OrderBy(item => item.SlaveId))
            {
                try
                {
                    var telemetry = await _dataService.ReadTelemetryAsync(device.SlaveId, CancellationToken.None).ConfigureAwait(false);
                    _telemetry[device.SlaveId] = telemetry;
                    results.Add(ThermostatRefreshResult.Success(device, telemetry));
                }
                catch (Exception ex)
                {
                    results.Add(ThermostatRefreshResult.Failure(device, ex.Message));
                }
            }

            return results;
        }

        public async Task<ThermostatTelemetry> SetPowerAsync(byte slaveId, ThermostatPowerState powerState)
        {
            var telemetry = await _dataService.SetPowerAsync(slaveId, powerState, CancellationToken.None).ConfigureAwait(false);
            _telemetry[slaveId] = telemetry;
            return telemetry;
        }

        public async Task<ThermostatTelemetry> SetModeAsync(byte slaveId, ThermostatMode mode)
        {
            var telemetry = await _dataService.SetModeAsync(slaveId, mode, CancellationToken.None).ConfigureAwait(false);
            _telemetry[slaveId] = telemetry;
            return telemetry;
        }

        public async Task<ThermostatTelemetry> SetFanSpeedAsync(byte slaveId, ThermostatFanSpeedSetting fanSpeed)
        {
            var telemetry = await _dataService.SetFanSpeedAsync(slaveId, fanSpeed, CancellationToken.None).ConfigureAwait(false);
            _telemetry[slaveId] = telemetry;
            return telemetry;
        }

        public Task<ThermostatFanControlDiagnostic> DiagnoseFanControlAsync(byte slaveId)
        {
            return _dataService.DiagnoseFanControlAsync(slaveId, CancellationToken.None);
        }

        public async Task<ThermostatTelemetry> SetTemperatureAsync(byte slaveId, decimal temperatureCelsius)
        {
            var telemetry = await _dataService.SetTemperatureAsync(slaveId, temperatureCelsius, CancellationToken.None).ConfigureAwait(false);
            _telemetry[slaveId] = telemetry;
            return telemetry;
        }

        public ThermostatWorkspaceSnapshot CreateSnapshot()
        {
            return new ThermostatWorkspaceSnapshot(
                IsConnected,
                _devices.Values.OrderBy(device => device.GroupName).ThenBy(device => device.SlaveId).Select(device => new ThermostatDeviceSnapshot(device, _telemetry.TryGetValue(device.SlaveId, out var telemetry) ? telemetry : null)).ToList());
        }

        public void Dispose()
        {
            _dataService.Dispose();
        }
    }

    public sealed class ThermostatWorkspaceSnapshot
    {
        public ThermostatWorkspaceSnapshot(bool isConnected, IReadOnlyList<ThermostatDeviceSnapshot> devices)
        {
            IsConnected = isConnected;
            Devices = devices;
        }

        public bool IsConnected { get; }
        public IReadOnlyList<ThermostatDeviceSnapshot> Devices { get; }
    }

    public sealed class ThermostatDeviceSnapshot
    {
        public ThermostatDeviceSnapshot(ThermostatDeviceInfo device, ThermostatTelemetry telemetry)
        {
            Device = device;
            Telemetry = telemetry;
        }

        public ThermostatDeviceInfo Device { get; }
        public ThermostatTelemetry Telemetry { get; }
    }

    public sealed class ThermostatRefreshResult
    {
        private ThermostatRefreshResult(ThermostatDeviceInfo device, ThermostatTelemetry telemetry, string errorMessage)
        {
            Device = device;
            Telemetry = telemetry;
            ErrorMessage = errorMessage;
        }

        public ThermostatDeviceInfo Device { get; }
        public ThermostatTelemetry Telemetry { get; }
        public string ErrorMessage { get; }
        public bool IsSuccess => Telemetry != null;

        public static ThermostatRefreshResult Success(ThermostatDeviceInfo device, ThermostatTelemetry telemetry)
        {
            return new ThermostatRefreshResult(device, telemetry, string.Empty);
        }

        public static ThermostatRefreshResult Failure(ThermostatDeviceInfo device, string errorMessage)
        {
            return new ThermostatRefreshResult(device, null, errorMessage ?? string.Empty);
        }
    }
}
