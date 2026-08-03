using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Protocol;

namespace MeterAcquisition.HeatPump.Services;

public sealed class ModbusHeatPumpDataService : IHeatPumpDataService
{
    private readonly ModbusRtuClient _client;
    private CommSettings _settings;

    public ModbusHeatPumpDataService()
        : this(new ModbusRtuClient())
    {
    }

    public ModbusHeatPumpDataService(ModbusRtuClient client)
    {
        _client = client;
    }

    public bool IsConnected => _client.IsOpen;

    public string ConnectionDescription => _client.IsOpen
        ? string.Format("{0} / {1} / {2}{3}{4}", _client.PortName, _settings.BaudRate, _settings.DataBits, _settings.Parity, _settings.StopBits)
        : "未连接";

    public async Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
    {
        var connected = await _client.ConnectAsync(settings, cancellationToken).ConfigureAwait(false);
        if (connected)
        {
            _settings = settings;
        }

        return connected;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return _client.DisconnectAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default)
    {
        var controllers = new List<WiredControllerInfo>();

        var scanStart = _settings != null ? _settings.ControllerScanStartAddress : (byte)1;
        var scanEnd = _settings != null ? _settings.ControllerScanEndAddress : (byte)16;
        if (scanStart > scanEnd)
        {
            throw new InvalidOperationException("热泵控制器扫描地址范围无效。");
        }

        for (var address = (int)scanStart; address <= scanEnd; address++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slaveId = (byte)address;

            try
            {
                await _client.ReadHoldingRegistersAsync(
                    slaveId,
                    HeatPumpRegisterMap.ControllerSettingsStart,
                    HeatPumpRegisterMap.ControllerSettingsLength,
                    cancellationToken);

                controllers.Add(new WiredControllerInfo
                {
                    Id = Guid.NewGuid().ToString("N"),
                    SlaveId = slaveId,
                    Name = $"线控器 {slaveId}#",
                    IsOnline = true,
                });
            }
            catch (TimeoutException)
            {
            }
            catch
            {
            }
        }

        return controllers;
    }

    public async Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        var modules = new List<HeatPumpModuleInfo>();

        for (byte moduleIndex = 0; moduleIndex <= 15; moduleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var telemetry = await ReadTelemetryAsync(slaveId, moduleIndex, cancellationToken);
                if (telemetry.Status == HeatPumpUnitStatus.Offline)
                {
                    continue;
                }

                modules.Add(new HeatPumpModuleInfo
                {
                    Id = Guid.NewGuid().ToString("N"),
                    SlaveId = slaveId,
                    ModuleIndex = moduleIndex,
                    Name = moduleIndex == 0 ? "0#总机" : $"模块 {moduleIndex}#",
                    Telemetry = telemetry,
                });
            }
            catch (TimeoutException)
            {
            }
            catch
            {
            }
        }

        return modules;
    }

    public async Task<HeatPumpTelemetry> ReadTelemetryAsync(byte slaveId, byte moduleIndex, CancellationToken cancellationToken = default)
    {
        var controllerRegisters = await _client.ReadHoldingRegistersAsync(
            slaveId,
            HeatPumpRegisterMap.ControllerSettingsStart,
            HeatPumpRegisterMap.ControllerSettingsLength,
            cancellationToken);

        var moduleBlockStart = (ushort)(moduleIndex * HeatPumpRegisterMap.ModuleBlockSize + HeatPumpRegisterMap.ModuleRealtimeBlockStart);
        var moduleRegisters = await _client.ReadHoldingRegistersAsync(
            slaveId,
            moduleBlockStart,
            HeatPumpRegisterMap.ModuleRealtimeBlockLength,
            cancellationToken);

        return HeatPumpTelemetryParser.Parse(controllerRegisters, moduleRegisters);
    }

    public Task SetRunModeAsync(byte slaveId, HeatPumpRunMode mode, CancellationToken cancellationToken = default)
    {
        return _client.WriteSingleRegisterAsync(slaveId, HeatPumpRegisterMap.ControllerModeSetAddress, ToModeRegisterValue(mode), cancellationToken);
    }

    public Task SetTargetTemperatureAsync(byte slaveId, decimal temperature, CancellationToken cancellationToken = default)
    {
        return _client.WriteSingleRegisterAsync(slaveId, HeatPumpRegisterMap.ControllerSetTemperatureAddress, (ushort)Math.Round(temperature), cancellationToken);
    }

    public Task SetDifferentialAsync(byte slaveId, int differential, CancellationToken cancellationToken = default)
    {
        return _client.WriteSingleRegisterAsync(slaveId, HeatPumpRegisterMap.ControllerDiffAddress, (ushort)differential, cancellationToken);
    }

    public Task SetControllerLockAsync(byte slaveId, bool locked, CancellationToken cancellationToken = default)
    {
        return _client.WriteSingleRegisterAsync(slaveId, HeatPumpRegisterMap.ControllerLockAddress, locked ? (ushort)1 : (ushort)0, cancellationToken);
    }

    public Task ClearFaultAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        return _client.WriteSingleRegisterAsync(slaveId, HeatPumpRegisterMap.ControllerClearFaultAddress, 1, cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static ushort ToModeRegisterValue(HeatPumpRunMode mode)
    {
        return mode switch
        {
            HeatPumpRunMode.Off => 0x08,
            HeatPumpRunMode.Cooling => 0x01,
            HeatPumpRunMode.Heating => 0x02,
            HeatPumpRunMode.Pump => 0x04,
            _ => throw new InvalidOperationException("当前模式不支持直接写入线控器寄存器。"),
        };
    }
}
