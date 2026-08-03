using MeterAcquisition.HeatPump.Domain;
using MeterAcquisition.HeatPump.Services;

namespace MeterAcquisition.HeatPump.Application;

public sealed class HeatPumpWorkspaceService : IDisposable
{
    private readonly Func<IHeatPumpDataService> _dataServiceFactory;
    private bool _disposed;
    private IHeatPumpDataService _dataService;
    private HeatPumpWorkspaceConfiguration _configuration = new HeatPumpWorkspaceConfiguration();
    private CommSettings? _settings;

    public HeatPumpWorkspaceService()
        : this(() => new ModbusHeatPumpDataService())
    {
    }

    public HeatPumpWorkspaceService(IHeatPumpDataService dataService)
        : this(() => dataService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        Dashboard = new HeatPumpDashboardManager(_dataService);
        Dashboard.SetDataService(_dataService, loadDemoControllers: false);
    }

    public HeatPumpWorkspaceService(Func<IHeatPumpDataService> dataServiceFactory)
    {
        _dataServiceFactory = dataServiceFactory ?? throw new ArgumentNullException(nameof(dataServiceFactory));
        _dataService = new MockHeatPumpDataService();
        Dashboard = new HeatPumpDashboardManager(_dataService);
        Dashboard.SetDataService(_dataService, loadDemoControllers: false);
    }

    public HeatPumpDashboardManager Dashboard { get; }

    public bool IsConnected => _dataService.IsConnected;

    public CommSettings? Settings => _settings;

    public void ApplyConfiguration(HeatPumpWorkspaceConfiguration configuration)
    {
        ThrowIfDisposed();
        _configuration = configuration ?? new HeatPumpWorkspaceConfiguration();
        ApplyControllerConfiguration(Dashboard.Controllers);
    }

    public HeatPumpWorkspaceConfiguration ExportConfiguration(IEnumerable<string> selectedModuleKeys)
    {
        ThrowIfDisposed();
        var configuredControllers = _configuration.Controllers.ToDictionary(controller => controller.SlaveId);
        foreach (var controller in Dashboard.Controllers)
        {
            configuredControllers[controller.SlaveId] = new PersistedControllerConfiguration
            {
                SlaveId = controller.SlaveId,
                Name = controller.Name,
                IsLocked = controller.ControllerLockEnabled,
                Differential = controller.Differential,
                Modules = controller.Modules
                    .OrderBy(module => module.DisplayOrder)
                    .ThenBy(module => module.ModuleIndex)
                    .Select(module => new PersistedModuleConfiguration
                    {
                        ModuleIndex = module.ModuleIndex,
                        Name = module.Name,
                        IsEnabled = module.IsEnabled,
                        DisplayOrder = module.DisplayOrder,
                        GroupName = module.GroupName
                    })
                    .ToList()
            };
        }

        _configuration = new HeatPumpWorkspaceConfiguration
        {
            SchemaVersion = HeatPumpWorkspaceConfiguration.CurrentSchemaVersion,
            Controllers = configuredControllers.Values.OrderBy(controller => controller.SlaveId).ToList(),
            SelectedModuleKeys = (selectedModuleKeys ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToList()
        };
        return _configuration;
    }

    public async Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);

        var dataService = _dataServiceFactory();
        var connected = false;
        try
        {
            connected = await dataService.ConnectAsync(settings, cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                dataService.Dispose();
                return false;
            }

            _dataService = dataService;
            _settings = settings;
            Dashboard.SetDataService(_dataService, loadDemoControllers: false);
            return true;
        }
        catch
        {
            dataService.Dispose();
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            await _dataService.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!ReferenceEquals(_dataService, DashboardDataServicePlaceholder.Instance))
            {
                _dataService.Dispose();
            }

            _dataService = DashboardDataServicePlaceholder.Instance;
            _settings = null;
            Dashboard.SetDataService(_dataService, loadDemoControllers: false);
        }
    }

    public async Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_dataService.IsConnected)
        {
            throw new InvalidOperationException("Heat pump workspace is not connected.");
        }

        var controllers = await _dataService.ScanControllersAsync(cancellationToken).ConfigureAwait(false);
        Dashboard.ReplaceControllers(controllers);
        ApplyControllerConfiguration(Dashboard.Controllers);
        return Dashboard.Controllers.ToList();
    }

    public async Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(WiredControllerInfo controller, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        var modules = await _dataService.ScanModulesAsync(controller.SlaveId, cancellationToken).ConfigureAwait(false);

        controller.Modules.Clear();
        foreach (var module in modules.OrderBy(m => m.DisplayOrder).ThenBy(m => m.ModuleIndex))
        {
            controller.Modules.Add(module);
        }

        ApplyModuleConfiguration(new[] { controller });
        return controller.Modules.ToList();
    }

    public async Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(IEnumerable<WiredControllerInfo> controllers, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controllers == null)
        {
            throw new ArgumentNullException(nameof(controllers));
        }

        var modules = new List<HeatPumpModuleInfo>();
        foreach (var controller in controllers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            modules.AddRange(await ScanModulesAsync(controller, cancellationToken).ConfigureAwait(false));
        }

        return modules;
    }

    private void ApplyControllerConfiguration(IEnumerable<WiredControllerInfo> controllers)
    {
        var configurations = _configuration.Controllers.ToDictionary(controller => controller.SlaveId);
        foreach (var controller in controllers)
        {
            if (!configurations.TryGetValue(controller.SlaveId, out var configuration))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(configuration.Name))
            {
                controller.Name = configuration.Name;
            }

            controller.ControllerLockEnabled = configuration.IsLocked;
            controller.Differential = configuration.Differential;
        }
    }

    private void ApplyModuleConfiguration(IEnumerable<WiredControllerInfo> controllers)
    {
        var controllerConfigurations = _configuration.Controllers.ToDictionary(controller => controller.SlaveId);
        foreach (var controller in controllers)
        {
            if (!controllerConfigurations.TryGetValue(controller.SlaveId, out var controllerConfiguration))
            {
                continue;
            }

            var moduleConfigurations = controllerConfiguration.Modules.ToDictionary(module => module.ModuleIndex);
            foreach (var module in controller.Modules)
            {
                if (!moduleConfigurations.TryGetValue(module.ModuleIndex, out var configuration))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(configuration.Name))
                {
                    module.Name = configuration.Name;
                }

                module.IsEnabled = configuration.IsEnabled;
                module.DisplayOrder = configuration.DisplayOrder;
                module.GroupName = configuration.GroupName;
            }
        }

        foreach (var controller in controllers)
        {
            var orderedModules = controller.Modules.OrderBy(module => module.DisplayOrder).ThenBy(module => module.ModuleIndex).ToList();
            controller.Modules.Clear();
            controller.Modules.AddRange(orderedModules);
        }
    }

    public async Task<IReadOnlyList<HeatPumpModuleInfo>> RefreshTelemetryAsync(WiredControllerInfo controller, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        foreach (var module in controller.Modules.OrderBy(m => m.DisplayOrder).ThenBy(m => m.ModuleIndex))
        {
            module.Telemetry = await _dataService.ReadTelemetryAsync(controller.SlaveId, module.ModuleIndex, cancellationToken).ConfigureAwait(false);
        }

        return controller.Modules.ToList();
    }

    public async Task<IReadOnlyList<HeatPumpTelemetryRefreshResult>> RefreshTelemetryAsync(IEnumerable<WiredControllerInfo> controllers, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controllers == null)
        {
            throw new ArgumentNullException(nameof(controllers));
        }

        EnsureConnected();
        var results = new List<HeatPumpTelemetryRefreshResult>();
        foreach (var controller in controllers)
        {
            foreach (var module in controller.Modules.OrderBy(item => item.DisplayOrder).ThenBy(item => item.ModuleIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    module.Telemetry = await _dataService.ReadTelemetryAsync(controller.SlaveId, module.ModuleIndex, cancellationToken).ConfigureAwait(false);
                    results.Add(new HeatPumpTelemetryRefreshResult(module));
                }
                catch (Exception ex)
                {
                    results.Add(new HeatPumpTelemetryRefreshResult(module, ex));
                }
            }
        }

        return results;
    }

    public async Task SetRunModeAsync(WiredControllerInfo controller, HeatPumpRunMode mode, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        await _dataService.SetRunModeAsync(controller.SlaveId, mode, cancellationToken).ConfigureAwait(false);
        foreach (var module in controller.Modules)
        {
            module.Telemetry.RunMode = mode;
            module.Telemetry.LastUpdatedAt = DateTime.Now;
        }
    }

    public async Task SetTargetTemperatureAsync(WiredControllerInfo controller, decimal temperature, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        await _dataService.SetTargetTemperatureAsync(controller.SlaveId, temperature, cancellationToken).ConfigureAwait(false);
        foreach (var module in controller.Modules)
        {
            module.Telemetry.TargetTemperature = temperature;
            module.Telemetry.LastUpdatedAt = DateTime.Now;
        }
    }

    public async Task SetDifferentialAsync(WiredControllerInfo controller, int differential, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        await _dataService.SetDifferentialAsync(controller.SlaveId, differential, cancellationToken).ConfigureAwait(false);
        controller.Differential = differential;
    }

    public async Task SetControllerLockAsync(WiredControllerInfo controller, bool locked, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        await _dataService.SetControllerLockAsync(controller.SlaveId, locked, cancellationToken).ConfigureAwait(false);
        controller.ControllerLockEnabled = locked;
    }

    public async Task ClearFaultAsync(WiredControllerInfo controller, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        EnsureConnected();

        await _dataService.ClearFaultAsync(controller.SlaveId, cancellationToken).ConfigureAwait(false);
        foreach (var module in controller.Modules)
        {
            module.Telemetry.LastUpdatedAt = DateTime.Now;
        }
    }

    public HeatPumpWorkspaceSnapshot CreateSnapshot()
    {
        ThrowIfDisposed();

        var controllers = Dashboard.Controllers.ToList();
        var modules = controllers.SelectMany(controller => controller.Modules).ToList();
        var lastRefreshAt = modules
            .Select(module => module.Telemetry?.LastUpdatedAt)
            .Where(timestamp => timestamp.HasValue)
            .Select(timestamp => timestamp.Value)
            .DefaultIfEmpty()
            .Max();

        return new HeatPumpWorkspaceSnapshot
        {
            IsConnected = _dataService.IsConnected,
            ConnectionDescription = _dataService.ConnectionDescription,
            Settings = _settings,
            Controllers = controllers,
            ControllerCount = controllers.Count,
            ModuleCount = modules.Count,
            LastRefreshAt = lastRefreshAt == default(DateTime) ? (DateTime?)null : lastRefreshAt
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dataService.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeatPumpWorkspaceService));
        }
    }

    private void EnsureConnected()
    {
        if (!_dataService.IsConnected)
        {
            throw new InvalidOperationException("Heat pump workspace is not connected.");
        }
    }

    private sealed class DashboardDataServicePlaceholder : IHeatPumpDataService
    {
        public static readonly DashboardDataServicePlaceholder Instance = new();

        public bool IsConnected => false;

        public string ConnectionDescription => "未连接";

        public Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WiredControllerInfo>> ScanControllersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<HeatPumpModuleInfo>> ScanModulesAsync(byte slaveId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<HeatPumpTelemetry> ReadTelemetryAsync(byte slaveId, byte moduleIndex, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SetRunModeAsync(byte slaveId, HeatPumpRunMode mode, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SetTargetTemperatureAsync(byte slaveId, decimal targetTemperature, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SetDifferentialAsync(byte slaveId, int differential, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SetControllerLockAsync(byte slaveId, bool enabled, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ClearFaultAsync(byte slaveId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
