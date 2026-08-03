namespace MeterAcquisition.HeatPump.Application;

public sealed class HeatPumpWorkspaceConfiguration
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<PersistedControllerConfiguration> Controllers { get; set; } = new();

    public List<string> SelectedModuleKeys { get; set; } = new();
}

public sealed class PersistedControllerConfiguration
{
    public byte SlaveId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public int Differential { get; set; }

    public List<PersistedModuleConfiguration> Modules { get; set; } = new();
}

public sealed class PersistedModuleConfiguration
{
    public int ModuleIndex { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string GroupName { get; set; } = string.Empty;
}
