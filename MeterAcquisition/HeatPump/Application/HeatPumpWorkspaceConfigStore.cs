using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace MeterAcquisition.HeatPump.Application;

public sealed class HeatPumpWorkspaceConfigStore
{
    private const string FileName = "heatpump-workspace.json";
    private readonly string _filePath;

    public HeatPumpWorkspaceConfigStore(string filePath = null)
    {
        _filePath = filePath ?? Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, FileName);
    }

    public HeatPumpWorkspaceConfiguration Load(out string warning)
    {
        warning = string.Empty;
        if (!File.Exists(_filePath))
        {
            return new HeatPumpWorkspaceConfiguration();
        }

        try
        {
            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            var configuration = JsonConvert.DeserializeObject<HeatPumpWorkspaceConfiguration>(json);
            if (configuration == null || configuration.SchemaVersion > HeatPumpWorkspaceConfiguration.CurrentSchemaVersion)
            {
                warning = "热泵本地配置版本无效，已使用空配置。";
                return new HeatPumpWorkspaceConfiguration();
            }

            return Normalize(configuration);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            warning = "热泵本地配置无法读取，已使用空配置: " + ex.Message;
            return new HeatPumpWorkspaceConfiguration();
        }
    }

    public void Save(HeatPumpWorkspaceConfiguration configuration)
    {
        var normalized = Normalize(configuration ?? new HeatPumpWorkspaceConfiguration());
        var directory = Path.GetDirectoryName(_filePath);
        Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";
        var json = JsonConvert.SerializeObject(normalized, Formatting.Indented);
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(true);
        }

        if (File.Exists(_filePath))
        {
            File.Replace(temporaryPath, _filePath, null);
        }
        else
        {
            File.Move(temporaryPath, _filePath);
        }
    }

    private static HeatPumpWorkspaceConfiguration Normalize(HeatPumpWorkspaceConfiguration configuration)
    {
        var controllers = (configuration.Controllers ?? new List<PersistedControllerConfiguration>())
            .Where(controller => controller != null && controller.SlaveId > 0)
            .GroupBy(controller => controller.SlaveId)
            .Select(group => group.First())
            .OrderBy(controller => controller.SlaveId)
            .Select(controller => new PersistedControllerConfiguration
            {
                SlaveId = controller.SlaveId,
                Name = controller.Name?.Trim() ?? string.Empty,
                IsLocked = controller.IsLocked,
                Differential = controller.Differential,
                Modules = (controller.Modules ?? new List<PersistedModuleConfiguration>())
                    .Where(module => module != null && module.ModuleIndex >= 0)
                    .GroupBy(module => module.ModuleIndex)
                    .Select(group => group.First())
                    .OrderBy(module => module.DisplayOrder)
                    .ThenBy(module => module.ModuleIndex)
                    .Select(module => new PersistedModuleConfiguration
                    {
                        ModuleIndex = module.ModuleIndex,
                        Name = module.Name?.Trim() ?? string.Empty,
                        IsEnabled = module.IsEnabled,
                        DisplayOrder = module.DisplayOrder,
                        GroupName = module.GroupName?.Trim() ?? string.Empty
                    })
                    .ToList()
            })
            .ToList();

        return new HeatPumpWorkspaceConfiguration
        {
            SchemaVersion = HeatPumpWorkspaceConfiguration.CurrentSchemaVersion,
            Controllers = controllers,
            SelectedModuleKeys = (configuration.SelectedModuleKeys ?? new List<string>())
                .Where(IsModuleKey)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList()
        };
    }

    private static bool IsModuleKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var parts = key.Split(':');
        return parts.Length == 2 &&
            byte.TryParse(parts[0], out var slaveId) && slaveId > 0 &&
            int.TryParse(parts[1], out var moduleIndex) && moduleIndex >= 0;
    }
}
