using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Services;

public sealed class MockHeatPumpDataService : IHeatPumpDataService
{
    private readonly Random _random = new();
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public string ConnectionDescription => _isConnected ? "模拟模式" : "未连接";

    public Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WiredControllerInfo> controllers =
        [
            new WiredControllerInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                SlaveId = 1,
                Name = "一楼线控器",
                IsOnline = true,
            },
            new WiredControllerInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                SlaveId = 2,
                Name = "二楼线控器",
                IsOnline = true,
            }
        ];

        return Task.FromResult(controllers);
    }

    public Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HeatPumpModuleInfo> modules = slaveId switch
        {
            1 =>
            [
                CreateModule(1, 0, "0#总机", HeatPumpUnitStatus.Running),
                CreateModule(1, 1, "模块 1#", HeatPumpUnitStatus.Standby),
                CreateModule(1, 2, "模块 2#", HeatPumpUnitStatus.Running),
            ],
            2 =>
            [
                CreateModule(2, 0, "0#总机", HeatPumpUnitStatus.Running),
                CreateModule(2, 1, "模块 1#", HeatPumpUnitStatus.Alarm),
                CreateModule(2, 3, "模块 3#", HeatPumpUnitStatus.Standby),
            ],
            _ => []
        };

        return Task.FromResult(modules);
    }

    public Task<HeatPumpTelemetry> ReadTelemetryAsync(byte slaveId, byte moduleIndex, CancellationToken cancellationToken = default)
    {
        var telemetry = CreateTelemetry(moduleIndex == 0 ? HeatPumpUnitStatus.Running : PickStatus());
        return Task.FromResult(telemetry);
    }

    public Task SetRunModeAsync(byte slaveId, HeatPumpRunMode mode, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetTargetTemperatureAsync(byte slaveId, decimal temperature, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetDifferentialAsync(byte slaveId, int differential, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetControllerLockAsync(byte slaveId, bool locked, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ClearFaultAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public HeatPumpTelemetry CreateTelemetry(HeatPumpUnitStatus status)
    {
        var mode = status switch
        {
            HeatPumpUnitStatus.Running => Pick(HeatPumpRunMode.Heating, HeatPumpRunMode.Cooling, HeatPumpRunMode.Pump),
            HeatPumpUnitStatus.Standby => HeatPumpRunMode.Off,
            HeatPumpUnitStatus.Alarm => Pick(HeatPumpRunMode.Heating, HeatPumpRunMode.Cooling),
            _ => HeatPumpRunMode.Off,
        };

        var outlet = status == HeatPumpUnitStatus.Offline ? 0m : NextDecimal(34m, 52m);
        var target = status == HeatPumpUnitStatus.Offline ? 0m : Clamp(outlet + NextDecimal(-3m, 4m), 30m, 55m);
        var returnWater = status == HeatPumpUnitStatus.Offline ? 0m : Clamp(outlet - NextDecimal(1m, 5m), 24m, 48m);
        var ambient = status == HeatPumpUnitStatus.Offline ? 0m : NextDecimal(14m, 31m);

        return new HeatPumpTelemetry
        {
            OutletWaterTemperature = outlet,
            ReturnWaterTemperature = returnWater,
            TargetTemperature = target,
            AmbientTemperature = ambient,
            RunMode = mode,
            Status = status,
            CompressorOn = status == HeatPumpUnitStatus.Running,
            PumpOn = status is HeatPumpUnitStatus.Running or HeatPumpUnitStatus.Alarm,
            ElectricHeaterOn = status == HeatPumpUnitStatus.Running && _random.NextDouble() > 0.65,
            LastUpdatedAt = DateTime.Now,
        };
    }

    public void UpdateTelemetry(HeatPumpModuleInfo module)
    {
        if (module.Telemetry.Status == HeatPumpUnitStatus.Offline)
        {
            if (_random.NextDouble() > 0.85)
            {
                module.Telemetry = CreateTelemetry(HeatPumpUnitStatus.Standby);
            }

            return;
        }

        if (_random.NextDouble() > 0.96)
        {
            module.Telemetry.Status = HeatPumpUnitStatus.Alarm;
        }
        else if (module.Telemetry.Status == HeatPumpUnitStatus.Alarm && _random.NextDouble() > 0.72)
        {
            module.Telemetry.Status = HeatPumpUnitStatus.Running;
        }
        else if (_random.NextDouble() > 0.97)
        {
            module.Telemetry.Status = HeatPumpUnitStatus.Offline;
        }
        else if (_random.NextDouble() > 0.89)
        {
            module.Telemetry.Status = module.Telemetry.Status == HeatPumpUnitStatus.Running
                ? HeatPumpUnitStatus.Standby
                : HeatPumpUnitStatus.Running;
        }

        if (module.Telemetry.Status == HeatPumpUnitStatus.Offline)
        {
            module.Telemetry.OutletWaterTemperature = 0m;
            module.Telemetry.ReturnWaterTemperature = 0m;
            module.Telemetry.TargetTemperature = 0m;
            module.Telemetry.AmbientTemperature = 0m;
            module.Telemetry.CompressorOn = false;
            module.Telemetry.PumpOn = false;
            module.Telemetry.ElectricHeaterOn = false;
            module.Telemetry.LastUpdatedAt = DateTime.Now;
            return;
        }

        module.Telemetry.RunMode = module.Telemetry.Status == HeatPumpUnitStatus.Standby
            ? HeatPumpRunMode.Off
            : Pick(HeatPumpRunMode.Heating, HeatPumpRunMode.Cooling, HeatPumpRunMode.Pump);

        module.Telemetry.OutletWaterTemperature = Clamp(module.Telemetry.OutletWaterTemperature + NextDecimal(-0.8m, 0.8m), 28m, 55m);
        module.Telemetry.ReturnWaterTemperature = Clamp(module.Telemetry.ReturnWaterTemperature + NextDecimal(-0.6m, 0.6m), 24m, 50m);
        module.Telemetry.TargetTemperature = Clamp(module.Telemetry.TargetTemperature + NextDecimal(-0.4m, 0.4m), 30m, 55m);
        module.Telemetry.AmbientTemperature = Clamp(module.Telemetry.AmbientTemperature + NextDecimal(-0.5m, 0.5m), 12m, 32m);
        module.Telemetry.CompressorOn = module.Telemetry.Status == HeatPumpUnitStatus.Running;
        module.Telemetry.PumpOn = module.Telemetry.Status is HeatPumpUnitStatus.Running or HeatPumpUnitStatus.Alarm;
        module.Telemetry.ElectricHeaterOn = module.Telemetry.Status == HeatPumpUnitStatus.Running && _random.NextDouble() > 0.68;
        module.Telemetry.LastUpdatedAt = DateTime.Now;
    }

    public void Dispose()
    {
    }

    private HeatPumpModuleInfo CreateModule(byte slaveId, byte moduleIndex, string name, HeatPumpUnitStatus status)
    {
        return new HeatPumpModuleInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            SlaveId = slaveId,
            ModuleIndex = moduleIndex,
            Name = name,
            Telemetry = CreateTelemetry(status),
        };
    }

    private HeatPumpUnitStatus PickStatus()
    {
        return Pick(HeatPumpUnitStatus.Running, HeatPumpUnitStatus.Standby, HeatPumpUnitStatus.Offline);
    }

    private decimal NextDecimal(decimal min, decimal max)
    {
        return min + (decimal)_random.NextDouble() * (max - min);
    }

    private T Pick<T>(params T[] values)
    {
        return values[_random.Next(values.Length)];
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
