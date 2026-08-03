using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Protocol;

namespace MeterAcquisition.HeatPump.Services;

public sealed class SharedBusHeatPumpDataService : IHeatPumpDataService
{
    private readonly ModbusService _modbusService;
    private readonly byte _scanStartAddress;
    private readonly byte _scanEndAddress;

    public SharedBusHeatPumpDataService(ModbusService modbusService, byte scanStartAddress, byte scanEndAddress)
    {
        if (scanStartAddress > scanEndAddress)
            throw new ArgumentOutOfRangeException(nameof(scanStartAddress));

        _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
        _scanStartAddress = scanStartAddress;
        _scanEndAddress = scanEndAddress;
    }

    public bool IsConnected => _modbusService.IsConnected;

    public string ConnectionDescription => _modbusService.IsConnected
        ? _modbusService.Config.BaudRate + " / " + _modbusService.Config.Parity + " / 8 / " + _modbusService.Config.StopBits
        : "未连接";

    public Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_modbusService.IsConnected);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default)
    {
        var controllers = new List<WiredControllerInfo>();
        for (byte slaveId = _scanStartAddress; slaveId <= _scanEndAddress; slaveId++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var registers = await ReadHoldingRegistersAsync(slaveId, HeatPumpRegisterMap.ControllerSettingsStart, HeatPumpRegisterMap.ControllerSettingsLength, cancellationToken).ConfigureAwait(false);
            if (registers == null)
                continue;

            controllers.Add(new WiredControllerInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                SlaveId = slaveId,
                Name = "线控器 " + slaveId + "#",
                IsOnline = true,
            });
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
                var telemetry = await ReadTelemetryAsync(slaveId, moduleIndex, cancellationToken).ConfigureAwait(false);
                if (telemetry.Status == HeatPumpUnitStatus.Offline)
                    continue;

                modules.Add(new HeatPumpModuleInfo
                {
                    Id = Guid.NewGuid().ToString("N"),
                    SlaveId = slaveId,
                    ModuleIndex = moduleIndex,
                    Name = moduleIndex == 0 ? "0#总机" : "模块 " + moduleIndex + "#",
                    Telemetry = telemetry,
                });
            }
            catch
            {
            }
        }

        return modules;
    }

    public async Task<HeatPumpTelemetry> ReadTelemetryAsync(byte slaveId, byte moduleIndex, CancellationToken cancellationToken = default)
    {
        var controllerRegisters = await ReadHoldingRegistersAsync(slaveId, HeatPumpRegisterMap.ControllerSettingsStart, HeatPumpRegisterMap.ControllerSettingsLength, cancellationToken).ConfigureAwait(false);
        var moduleBlockStart = (ushort)(moduleIndex * HeatPumpRegisterMap.ModuleBlockSize + HeatPumpRegisterMap.ModuleRealtimeBlockStart);
        var moduleRegisters = await ReadHoldingRegistersAsync(slaveId, moduleBlockStart, HeatPumpRegisterMap.ModuleRealtimeBlockLength, cancellationToken).ConfigureAwait(false);
        if (controllerRegisters == null || moduleRegisters == null)
            throw new TimeoutException("热泵 Modbus RTU 响应超时。");

        return HeatPumpTelemetryParser.Parse(controllerRegisters, moduleRegisters);
    }

    public Task SetRunModeAsync(byte slaveId, HeatPumpRunMode mode, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("共享总线热泵监测模式不支持控制命令。");
    }

    public Task SetTargetTemperatureAsync(byte slaveId, decimal temperature, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("共享总线热泵监测模式不支持控制命令。");
    }

    public Task SetDifferentialAsync(byte slaveId, int differential, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("共享总线热泵监测模式不支持控制命令。");
    }

    public Task SetControllerLockAsync(byte slaveId, bool locked, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("共享总线热泵监测模式不支持控制命令。");
    }

    public Task ClearFaultAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("共享总线热泵监测模式不支持控制命令。");
    }

    public void Dispose()
    {
    }

    private Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => _modbusService.ReadHoldingRegisterValues(slaveId, startAddress, quantity), cancellationToken);
    }
}
