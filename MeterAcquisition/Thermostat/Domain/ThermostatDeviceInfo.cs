using System;

namespace MeterAcquisition.Thermostat.Domain
{
    public sealed class ThermostatDeviceInfo
    {
        public ThermostatDeviceInfo(byte slaveId, string name, string groupName, bool isEnabled)
        {
            if (slaveId < 1 || slaveId > 99)
            {
                throw new ArgumentOutOfRangeException(nameof(slaveId), "温控器地址必须在 1 到 99 之间。");
            }

            SlaveId = slaveId;
            Name = string.IsNullOrWhiteSpace(name) ? string.Format("温控器 {0}", slaveId) : name.Trim();
            GroupName = groupName == null ? string.Empty : groupName.Trim();
            IsEnabled = isEnabled;
        }

        public byte SlaveId { get; }
        public string Name { get; }
        public string GroupName { get; }
        public bool IsEnabled { get; }
        public string DeviceKey => string.Format("direct:{0}", SlaveId);
    }
}
