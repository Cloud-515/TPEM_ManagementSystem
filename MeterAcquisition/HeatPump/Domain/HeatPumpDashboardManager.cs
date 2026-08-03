using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.HeatPump.Domain;

public sealed class HeatPumpDashboardManager
{
    private readonly List<WiredControllerInfo> _controllers = new();
    private IHeatPumpDataService _dataService;

    public HeatPumpDashboardManager(IHeatPumpDataService dataService)
    {
        _dataService = dataService;
        ResetToDemoControllers();
    }

    public IReadOnlyList<WiredControllerInfo> Controllers => _controllers;

    public WiredControllerInfo? GetController(byte slaveId)
    {
        return _controllers.FirstOrDefault(controller => controller.SlaveId == slaveId);
    }

    public bool HasDuplicateGroupName(byte slaveId, string groupName)
    {
        return _controllers.Any(controller => controller.SlaveId != slaveId
            && string.Equals(controller.Name?.Trim(), groupName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public WiredControllerInfo UpsertController(byte slaveId, string groupName)
    {
        var existing = GetController(slaveId);
        if (existing is not null)
        {
            existing.Name = groupName;
            return existing;
        }

        var controller = new WiredControllerInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            SlaveId = slaveId,
            Name = groupName,
            IsOnline = true,
        };

        controller.Modules.Add(CreateDemoModule(slaveId, 0, "0#总机", HeatPumpUnitStatus.Standby));
        _controllers.Add(controller);
        return controller;
    }

    public IReadOnlyList<HeatPumpModuleInfo> AllModules => _controllers.SelectMany(controller => controller.Modules).ToList();

    public IReadOnlyList<HeatPumpModuleInfo> AllUnits => AllModules;

    public IEnumerable<HeatPumpModuleInfo> PrimaryUnits => _controllers.SelectMany(controller => controller.Modules.Where(module => module.ModuleIndex == 0));

    public IEnumerable<HeatPumpModuleInfo> ScannedUnits => _controllers.SelectMany(controller => controller.Modules.Where(module => module.ModuleIndex != 0));

    public void SetDataService(IHeatPumpDataService dataService, bool loadDemoControllers)
    {
        _dataService = dataService;
        _controllers.Clear();
        if (loadDemoControllers)
        {
            SeedDemoControllers();
        }
    }

    public void ReplaceControllers(IEnumerable<WiredControllerInfo> controllers)
    {
        _controllers.Clear();
        foreach (var controller in controllers
            .GroupBy(item => item.SlaveId)
            .Select(group => group.First())
            .OrderBy(item => item.SlaveId))
        {
            var distinctModules = controller.Modules
                .GroupBy(module => module.ModuleIndex)
                .Select(group => group.First())
                .OrderBy(module => module.ModuleIndex)
                .ToList();
            controller.Modules.Clear();
            controller.Modules.AddRange(distinctModules);
            _controllers.Add(controller);
        }
    }

    public void ResetToDemoControllers()
    {
        _controllers.Clear();
        SeedDemoControllers();
    }

    public void MarkAllOffline()
    {
        foreach (var controller in _controllers)
        {
            controller.IsOnline = false;
            foreach (var module in controller.Modules)
            {
                module.Telemetry = CreateOfflineTelemetry();
            }
        }
    }

    public Task<IReadOnlyList<HeatPumpModuleInfo>> AutoScanAsync(byte startAddress, byte endAddress, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HeatPumpModuleInfo>>(AllModules.Where(module => module.ModuleIndex >= startAddress && module.ModuleIndex <= endAddress).ToList());
    }

    public Task<HeatPumpModuleInfo> AddManualUnitAsync(byte preferredAddress, CancellationToken cancellationToken = default)
    {
        var existing = AllModules.FirstOrDefault(module => module.ModuleIndex == preferredAddress);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var controller = _controllers.First();
        var module = CreateDemoModule(controller.SlaveId, preferredAddress, $"模块 {preferredAddress}#", HeatPumpUnitStatus.Standby);
        controller.Modules.Add(module);
        return Task.FromResult(module);
    }

    public Task RefreshTelemetryAsync(CancellationToken cancellationToken = default)
    {
        foreach (var module in AllModules)
        {
            if (_dataService is MockHeatPumpDataService mockService)
            {
                mockService.UpdateTelemetry(module);
            }
        }

        return Task.CompletedTask;
    }

    public void RemoveUnit(string unitId)
    {
        foreach (var controller in _controllers)
        {
            var module = controller.Modules.FirstOrDefault(item => item.Id == unitId && item.ModuleIndex != 0);
            if (module is not null)
            {
                controller.Modules.Remove(module);
                return;
            }
        }
    }

    private void SeedDemoControllers()
    {
        _controllers.Add(CreateDemoController(1, "一楼线控器", [
            CreateDemoModule(1, 0, "0#总机", HeatPumpUnitStatus.Running),
            CreateDemoModule(1, 1, "模块 1#", HeatPumpUnitStatus.Standby),
            CreateDemoModule(1, 2, "模块 2#", HeatPumpUnitStatus.Running)
        ]));

        _controllers.Add(CreateDemoController(2, "二楼线控器", [
            CreateDemoModule(2, 0, "0#总机", HeatPumpUnitStatus.Running),
            CreateDemoModule(2, 1, "模块 1#", HeatPumpUnitStatus.Alarm),
            CreateDemoModule(2, 3, "模块 3#", HeatPumpUnitStatus.Standby)
        ]));
    }

    private WiredControllerInfo CreateDemoController(byte slaveId, string name, IEnumerable<HeatPumpModuleInfo> modules)
    {
        var controller = new WiredControllerInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            SlaveId = slaveId,
            Name = name,
            IsOnline = true,
        };

        controller.Modules.AddRange(modules);
        return controller;
    }

    private HeatPumpModuleInfo CreateDemoModule(byte slaveId, byte moduleIndex, string name, HeatPumpUnitStatus status)
    {
        var telemetry = _dataService is MockHeatPumpDataService mockService
            ? mockService.CreateTelemetry(status)
            : CreateOfflineTelemetry();

        return new HeatPumpModuleInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            SlaveId = slaveId,
            ModuleIndex = moduleIndex,
            Name = name,
            Telemetry = telemetry,
        };
    }

    private static HeatPumpTelemetry CreateOfflineTelemetry()
    {
        return new HeatPumpTelemetry
        {
            Status = HeatPumpUnitStatus.Offline,
            RunMode = HeatPumpRunMode.Off,
            LastUpdatedAt = DateTime.Now,
        };
    }
}
