using System;
using System.Collections.Generic;
using System.Linq;

namespace MeterAcquisition
{
    public class MeterManager
    {
        private readonly MeterDataService _toolbarService;
        private readonly MeterDataService _dashboardService;

        public event EventHandler DataRefreshed;

        public List<DistributionBox> Boxes { get; }
        public List<MeterInfo> AllMeters => Boxes.SelectMany(b => b.Meters).ToList();

        public MeterManager(MeterDataService toolbarService, MeterDataService dashboardService)
        {
            _toolbarService = toolbarService;
            _dashboardService = dashboardService;
            Boxes = new List<DistributionBox>();
        }

        public DistributionBox AddBox(string name)
        {
            var box = new DistributionBox { Name = name };
            Boxes.Add(box);
            return box;
        }

        public void RemoveBox(DistributionBox box)
        {
            Boxes.Remove(box);
        }

        public MeterInfo AddMeterToBox(DistributionBox box, string id, string name, string location, byte slaveAddress)
        {
            var meter = new MeterInfo
            {
                Id = id,
                Name = name,
                Location = location,
                SlaveAddress = slaveAddress,
            };
            box.Meters.Add(meter);
            return meter;
        }

        public void RemoveMeter(string meterId)
        {
            foreach (var box in Boxes)
            {
                var meter = box.Meters.FirstOrDefault(m => m.Id == meterId);
                if (meter != null)
                {
                    box.Meters.Remove(meter);
                    return;
                }
            }
        }

        public void PollAll()
        {
            foreach (var meter in AllMeters)
            {
                var svc = meter.IsToolbar ? _toolbarService : _dashboardService;
                try
                {
                    meter.RealTime = svc.ReadRealTimeData(meter.SlaveAddress);
                    meter.Energy = svc.ReadEnergyData(meter.SlaveAddress);
                    meter.Quality = svc.ReadPowerQualityData(meter.SlaveAddress);
                }
                catch (System.InvalidOperationException)
                {
                }
                catch
                {
                }
            }

            DataRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public void ClearData()
        {
            foreach (var meter in AllMeters)
            {
                meter.RealTime = null;
                meter.Energy = null;
                meter.Quality = null;
            }
        }
    }
}
