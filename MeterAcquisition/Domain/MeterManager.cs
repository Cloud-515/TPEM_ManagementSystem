using System;
using System.Collections.Generic;
using System.Linq;
using Tpem.Diagnostics;

namespace MeterAcquisition
{
    public class MeterManager
    {
        private readonly IMeterDataReader _toolbarService;
        private readonly IMeterDataReader _dashboardService;

        public event EventHandler DataRefreshed;

        public List<DistributionBox> Boxes { get; }
        public List<MeterInfo> AllMeters => Boxes.SelectMany(b => b.Meters).ToList();

        public MeterManager(IMeterDataReader toolbarService, IMeterDataReader dashboardService)
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
                DeviceModel = _toolbarService.DeviceModel
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

        public List<MeterPollResult> PollAll(bool readRealTime, bool readEnergy, bool readQuality)
        {
            var results = new List<MeterPollResult>();
            if (!readRealTime && !readEnergy && !readQuality)
            {
                // 本轮不需要读任何类别。若继续往下走，HasAnyData 必为 false，
                // 会被误记成一次采集失败。
                DataRefreshed?.Invoke(this, EventArgs.Empty);
                return results;
            }

            var meters = AllMeters;
            var failed = 0;

            foreach (var meter in meters)
            {
                var svc = meter.IsToolbar ? _toolbarService : _dashboardService;
                var result = new MeterPollResult { Meter = meter };
                try
                {
                    if (readRealTime)
                    {
                        result.RealTime = svc.ReadRealTimeData(meter.SlaveAddress);
                        meter.RealTime = result.RealTime;
                    }

                    if (readEnergy)
                    {
                        result.Energy = svc.ReadEnergyData(meter.SlaveAddress);
                        meter.Energy = result.Energy;
                    }

                    if (readQuality)
                    {
                        result.Quality = svc.ReadPowerQualityData(meter.SlaveAddress);
                        meter.Quality = result.Quality;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // 串口已关闭/正在关闭。属于常见竞态，Debug 级别即可。
                    failed++;
                    AppLogger.Debug("Poll", "轮询 " + meter.Id + "(从站 " + meter.SlaveAddress + ") 时串口不可用: " + ex.Message);
                }
                catch (Exception ex)
                {
                    // P0-2 / P0-8: 原来是 `catch { }`。报文解析越界、字段异常等问题在这里被彻底吞掉，
                    // 现场表现为"数据不刷新但没有任何提示"，无法定位。
                    failed++;
                    AppLogger.Error(
                        "Poll",
                        "轮询 " + meter.Id + "(从站 " + meter.SlaveAddress + ", 型号 " + (meter.DeviceModel ?? "-") + ") 失败。",
                        ex);
                }

                if (result.HasAnyData)
                {
                    meter.LastSuccessfulReadTime = DateTimeOffset.UtcNow;
                    results.Add(result);
                }
                else
                {
                    meter.ConsecutiveFailureCount++;

                    // 只在 1 / 10 / 100 / 1000... 次时输出，避免一台离线的表每秒刷一行日志，
                    // 同时又能看出"从什么时候开始读不到、已经持续多久"。
                    if (IsMilestone(meter.ConsecutiveFailureCount))
                    {
                        AppLogger.Warn(
                            "Poll",
                            "从站 " + meter.SlaveAddress + "(" + meter.Id + ") 连续 " +
                            meter.ConsecutiveFailureCount + " 次未取到数据。");
                    }

                    continue;
                }

                if (meter.ConsecutiveFailureCount > 0)
                {
                    AppLogger.Info(
                        "Poll",
                        "从站 " + meter.SlaveAddress + " 恢复通讯（此前连续失败 " + meter.ConsecutiveFailureCount + " 次）。");
                    meter.ConsecutiveFailureCount = 0;
                }
            }

            if (failed > 0)
            {
                AppLogger.Warn("Poll", "本轮轮询 " + meters.Count + " 台，其中 " + failed + " 台抛出异常。");
            }

            DataRefreshed?.Invoke(this, EventArgs.Empty);
            return results;
        }

        /// <summary>1, 10, 100, 1000 … 用于把"持续失败"压缩成对数级别的日志量。</summary>
        private static bool IsMilestone(int count)
        {
            if (count < 1)
            {
                return false;
            }

            var threshold = 1;
            while (threshold <= count)
            {
                if (threshold == count)
                {
                    return true;
                }

                threshold *= 10;
            }

            return false;
        }

        public void ClearData()
        {
            foreach (var meter in AllMeters)
            {
                meter.LastSuccessfulReadTime = null;
                meter.ConsecutiveFailureCount = 0;
                meter.RealTime = null;
                meter.Energy = null;
                meter.Quality = null;
            }
        }
    }
}
