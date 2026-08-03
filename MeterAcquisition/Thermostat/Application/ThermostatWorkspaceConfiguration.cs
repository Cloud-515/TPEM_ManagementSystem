using System.Collections.Generic;

namespace MeterAcquisition.Thermostat.Application
{
    public sealed class ThermostatWorkspaceConfiguration
    {
        public int Version { get; set; } = 1;
        public List<ThermostatDeviceConfiguration> Devices { get; set; } = new List<ThermostatDeviceConfiguration>();
    }

    public sealed class ThermostatDeviceConfiguration
    {
        public byte SlaveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }
}
