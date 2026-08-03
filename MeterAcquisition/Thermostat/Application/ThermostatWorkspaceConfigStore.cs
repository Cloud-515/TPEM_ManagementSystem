using System;
using System.IO;
using Newtonsoft.Json;

namespace MeterAcquisition.Thermostat.Application
{
    public sealed class ThermostatWorkspaceConfigStore
    {
        private readonly string _filePath;

        public ThermostatWorkspaceConfigStore()
            : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "thermostat-workspace.json"))
        {
        }

        internal ThermostatWorkspaceConfigStore(string filePath)
        {
            _filePath = filePath;
        }

        public ThermostatWorkspaceConfiguration Load(out string warning)
        {
            warning = string.Empty;
            if (!File.Exists(_filePath))
            {
                return new ThermostatWorkspaceConfiguration();
            }

            try
            {
                var content = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<ThermostatWorkspaceConfiguration>(content) ?? new ThermostatWorkspaceConfiguration();
            }
            catch (Exception ex)
            {
                warning = "温控器工作区配置读取失败: " + ex.Message;
                return new ThermostatWorkspaceConfiguration();
            }
        }

        public void Save(ThermostatWorkspaceConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
            File.Copy(temporaryPath, _filePath, true);
            File.Delete(temporaryPath);
        }
    }
}
