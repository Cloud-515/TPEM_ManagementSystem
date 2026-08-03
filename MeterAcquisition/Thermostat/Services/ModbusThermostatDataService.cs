using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Services;
using MeterAcquisition.Thermostat.Domain;
using MeterAcquisition.Thermostat.Protocol;

namespace MeterAcquisition.Thermostat.Services
{
    public sealed class ModbusThermostatDataService : IThermostatDataService
    {
        private readonly ModbusRtuClient _client;

        public ModbusThermostatDataService()
            : this(new ModbusRtuClient())
        {
        }

        internal ModbusThermostatDataService(ModbusRtuClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool IsConnected => _client.IsOpen;

        public Task ConnectAsync(CommSettings settings, CancellationToken cancellationToken)
        {
            return _client.ConnectAsync(settings, cancellationToken);
        }

        public Task DisconnectAsync()
        {
            return _client.DisconnectAsync();
        }

        public async Task<IReadOnlyList<byte>> ScanAsync(byte startSlaveId, byte endSlaveId, CancellationToken cancellationToken)
        {
            if (startSlaveId < 1 || endSlaveId > 99 || startSlaveId > endSlaveId)
            {
                throw new ArgumentOutOfRangeException(nameof(startSlaveId), "温控器扫描地址必须在 1 到 99 之间。");
            }

            var devices = new List<byte>();
            for (var slaveId = startSlaveId; slaveId <= endSlaveId; slaveId++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await ReadTelemetryAsync((byte)slaveId, cancellationToken).ConfigureAwait(false);
                    devices.Add((byte)slaveId);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }

            return devices;
        }

        public async Task<ThermostatTelemetry> ReadTelemetryAsync(byte slaveId, CancellationToken cancellationToken)
        {
            ValidateSlaveId(slaveId);
            var registers = await _client.ReadHoldingRegistersAsync(
                slaveId,
                ThermostatRegisterMap.TelemetryStart,
                ThermostatRegisterMap.TelemetryCount,
                cancellationToken).ConfigureAwait(false);
            return ThermostatTelemetryParser.Parse(slaveId, registers, DateTime.Now);
        }

        public Task<ThermostatTelemetry> SetPowerAsync(byte slaveId, ThermostatPowerState powerState, CancellationToken cancellationToken)
        {
            if (powerState == ThermostatPowerState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(powerState));
            }

            return WriteAndReadAsync(slaveId, ThermostatRegisterMap.Power, (ushort)powerState, cancellationToken);
        }

        public Task<ThermostatTelemetry> SetModeAsync(byte slaveId, ThermostatMode mode, CancellationToken cancellationToken)
        {
            if (mode == ThermostatMode.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            return WriteAndReadAsync(slaveId, ThermostatRegisterMap.Mode, (ushort)mode, cancellationToken);
        }

        public Task<ThermostatTelemetry> SetFanSpeedAsync(byte slaveId, ThermostatFanSpeedSetting fanSpeed, CancellationToken cancellationToken)
        {
            if (fanSpeed == ThermostatFanSpeedSetting.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(fanSpeed));
            }

            return WriteAndReadAsync(slaveId, ThermostatRegisterMap.FanSpeedSetting, (ushort)fanSpeed, cancellationToken);
        }

        public async Task<ThermostatFanControlDiagnostic> DiagnoseFanControlAsync(byte slaveId, CancellationToken cancellationToken)
        {
            var telemetry = await ReadTelemetryAsync(slaveId, cancellationToken).ConfigureAwait(false);
            return new ThermostatFanControlDiagnostic(telemetry, _client.GetRecentFrames());
        }

        public Task<ThermostatTelemetry> SetTemperatureAsync(byte slaveId, decimal temperatureCelsius, CancellationToken cancellationToken)
        {
            if (temperatureCelsius < 5m || temperatureCelsius > 35m)
            {
                throw new ArgumentOutOfRangeException(nameof(temperatureCelsius), "设定温度必须在 5 到 35°C 之间。");
            }

            return WriteAndReadAsync(slaveId, ThermostatRegisterMap.SetTemperature, ThermostatTelemetryParser.ToTemperatureRegister(temperatureCelsius), cancellationToken);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private async Task<ThermostatTelemetry> WriteAndReadAsync(byte slaveId, ushort registerAddress, ushort value, CancellationToken cancellationToken)
        {
            ValidateSlaveId(slaveId);
            await _client.WriteSingleRegisterAsync(slaveId, registerAddress, value, cancellationToken).ConfigureAwait(false);
            await VerifyRegisterValueAsync(slaveId, registerAddress, value, cancellationToken).ConfigureAwait(false);

            return await ReadTelemetryAsync(slaveId, cancellationToken).ConfigureAwait(false);
        }

        private async Task VerifyRegisterValueAsync(byte slaveId, ushort registerAddress, ushort expectedValue, CancellationToken cancellationToken)
        {
            const int attempts = 3;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var values = await _client.ReadHoldingRegistersAsync(slaveId, registerAddress, 1, cancellationToken).ConfigureAwait(false);
                if (values.Length == 1 && values[0] == expectedValue)
                {
                    return;
                }

                if (attempt < attempts - 1)
                {
                    await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("温控器已响应写入，但控制寄存器回读值与目标值不一致。");
        }

        private static void ValidateSlaveId(byte slaveId)
        {
            if (slaveId < 1 || slaveId > 99)
            {
                throw new ArgumentOutOfRangeException(nameof(slaveId), "温控器地址必须在 1 到 99 之间。");
            }
        }
    }
}
