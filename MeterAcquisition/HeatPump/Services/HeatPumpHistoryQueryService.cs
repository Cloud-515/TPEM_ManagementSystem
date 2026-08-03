using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace MeterAcquisition.HeatPump.Services
{
    internal sealed class HeatPumpHistoryQueryService
    {
        private const int MaximumChartPoints = 1500;
        private static readonly IReadOnlyList<HeatPumpHistoryMetricDefinition> Metrics = new List<HeatPumpHistoryMetricDefinition>
        {
            new HeatPumpHistoryMetricDefinition("set_temperature", "设定温度", "set_temperature", "°C"),
            new HeatPumpHistoryMetricDefinition("inlet_temperature", "进水温度", "inlet_temperature", "°C"),
            new HeatPumpHistoryMetricDefinition("outlet_temperature", "出水温度", "outlet_temperature", "°C"),
            new HeatPumpHistoryMetricDefinition("ambient_temperature", "环境温度", "ambient_temperature", "°C"),
            new HeatPumpHistoryMetricDefinition("temperature_delta", "进出水温差", "outlet_temperature - inlet_temperature", "°C"),
            new HeatPumpHistoryMetricDefinition("run_mode", "运行模式", "run_mode", string.Empty),
            new HeatPumpHistoryMetricDefinition("state_code", "运行状态", "state_code", string.Empty),
            new HeatPumpHistoryMetricDefinition("fault_code", "故障代码", "fault_code", string.Empty)
        };

        private readonly string _connectionString;

        public HeatPumpHistoryQueryService()
        {
            _connectionString = GetConnectionString();
        }

        public IReadOnlyList<HeatPumpHistoryMetricDefinition> GetMetrics()
        {
            return Metrics;
        }

        public async Task<List<HeatPumpDeviceLookupItem>> GetDevicesAsync()
        {
            const string sql = @"
SELECT id, site_code, controller_code, module_address, device_name
FROM heat_pump_device
WHERE is_enabled = TRUE
ORDER BY site_code, controller_code, module_address;";

            var devices = new List<HeatPumpDeviceLookupItem>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new NpgsqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var item = new HeatPumpDeviceLookupItem
                        {
                            DeviceId = reader.GetInt64(0),
                            SiteCode = reader.GetString(1),
                            ControllerCode = reader.GetString(2),
                            ModuleAddress = reader.GetInt32(3),
                            DeviceName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                        };
                        item.DisplayText = string.Format(CultureInfo.InvariantCulture, "{0} / {1} / 模块 {2}", item.SiteCode, item.ControllerCode, item.ModuleAddress);
                        devices.Add(item);
                    }
                }
            }

            return devices;
        }

        public async Task<HeatPumpHistoryResult> QueryHistoryAsync(long deviceId, string metricKey, DateTime startTime, DateTime endTime)
        {
            ValidateRange(startTime, endTime);
            var metric = GetMetric(metricKey);
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var pointCount = await QueryPointCountAsync(connection, deviceId, metric, startTime, endTime).ConfigureAwait(false);
                var points = await QuerySeriesAsync(connection, deviceId, metric, startTime, endTime, pointCount).ConfigureAwait(false);
                var summary = await QuerySummaryAsync(connection, deviceId, startTime, endTime).ConfigureAwait(false);
                var events = await QueryEventsAsync(connection, deviceId, startTime, endTime).ConfigureAwait(false);
                return new HeatPumpHistoryResult
                {
                    Metric = metric,
                    PointCount = pointCount,
                    Points = points,
                    Summary = summary,
                    Events = events
                };
            }
        }

        public async Task<List<HeatPumpComparisonSeries>> QueryComparisonAsync(IReadOnlyList<HeatPumpDeviceLookupItem> devices, string metricKey, DateTime startTime, DateTime endTime)
        {
            ValidateRange(startTime, endTime);
            var metric = GetMetric(metricKey);
            var tasks = devices.Select(device => QueryComparisonSeriesAsync(device, metric, startTime, endTime)).ToArray();
            var series = await Task.WhenAll(tasks).ConfigureAwait(false);
            return series.ToList();
        }

        private async Task<HeatPumpComparisonSeries> QueryComparisonSeriesAsync(HeatPumpDeviceLookupItem device, HeatPumpHistoryMetricDefinition metric, DateTime startTime, DateTime endTime)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var pointCount = await QueryPointCountAsync(connection, device.DeviceId, metric, startTime, endTime).ConfigureAwait(false);
                return new HeatPumpComparisonSeries
                {
                    Device = device,
                    Metric = metric,
                    Points = await QuerySeriesAsync(connection, device.DeviceId, metric, startTime, endTime, pointCount).ConfigureAwait(false)
                };
            }
        }

        private static async Task<long> QueryPointCountAsync(NpgsqlConnection connection, long deviceId, HeatPumpHistoryMetricDefinition metric, DateTime startTime, DateTime endTime)
        {
            var sql = string.Format(CultureInfo.InvariantCulture, @"
SELECT COUNT(*)
FROM heat_pump_telemetry_history
WHERE device_id = @device_id
  AND collected_at BETWEEN @start_time AND @end_time
  AND ({0}) IS NOT NULL;", metric.Expression);
            using (var command = CreateTimeRangeCommand(sql, connection, deviceId, startTime, endTime))
            {
                return Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false), CultureInfo.InvariantCulture);
            }
        }

        private static async Task<List<HeatPumpHistoryPoint>> QuerySeriesAsync(NpgsqlConnection connection, long deviceId, HeatPumpHistoryMetricDefinition metric, DateTime startTime, DateTime endTime, long pointCount)
        {
            var bucketSeconds = GetBucketSeconds(startTime, endTime, pointCount);
            var select = bucketSeconds > 1
                ? string.Format(CultureInfo.InvariantCulture, "MIN(collected_at), AVG(CAST(({0}) AS double precision))", metric.Expression)
                : string.Format(CultureInfo.InvariantCulture, "collected_at, CAST(({0}) AS double precision)", metric.Expression);
            var groupBy = bucketSeconds > 1 ? "GROUP BY FLOOR(EXTRACT(EPOCH FROM collected_at) / @bucket_seconds)" : string.Empty;
            var sql = string.Format(CultureInfo.InvariantCulture, @"
SELECT {0}
FROM heat_pump_telemetry_history
WHERE device_id = @device_id
  AND collected_at BETWEEN @start_time AND @end_time
  AND ({1}) IS NOT NULL
{2}
ORDER BY 1;", select, metric.Expression, groupBy);

            var points = new List<HeatPumpHistoryPoint>();
            using (var command = CreateTimeRangeCommand(sql, connection, deviceId, startTime, endTime))
            {
                if (bucketSeconds > 1)
                {
                    command.Parameters.AddWithValue("@bucket_seconds", bucketSeconds);
                }

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        if (!reader.IsDBNull(1))
                        {
                            points.Add(new HeatPumpHistoryPoint
                            {
                                CollectedAt = ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(0)),
                                Value = reader.GetDouble(1)
                            });
                        }
                    }
                }
            }

            return points;
        }

        private static async Task<HeatPumpHistorySummary> QuerySummaryAsync(NpgsqlConnection connection, long deviceId, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
SELECT COUNT(*),
       MIN(collected_at),
       MAX(collected_at),
       AVG(inlet_temperature), MIN(inlet_temperature), MAX(inlet_temperature),
       AVG(outlet_temperature), MIN(outlet_temperature), MAX(outlet_temperature),
       AVG(outlet_temperature - inlet_temperature),
       COUNT(*) FILTER (WHERE COALESCE(state_code, 0) <> 0),
       COUNT(*) FILTER (WHERE COALESCE(fault_code, 0) <> 0)
FROM heat_pump_telemetry_history
WHERE device_id = @device_id
  AND collected_at BETWEEN @start_time AND @end_time;";
            using (var command = CreateTimeRangeCommand(sql, connection, deviceId, startTime, endTime))
            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                await reader.ReadAsync().ConfigureAwait(false);
                return new HeatPumpHistorySummary
                {
                    SampleCount = reader.GetInt64(0),
                    FirstCollectedAt = reader.IsDBNull(1) ? (DateTime?)null : ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(1)),
                    LastCollectedAt = reader.IsDBNull(2) ? (DateTime?)null : ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(2)),
                    AverageInletTemperature = GetNullableDouble(reader, 3),
                    MinimumInletTemperature = GetNullableDouble(reader, 4),
                    MaximumInletTemperature = GetNullableDouble(reader, 5),
                    AverageOutletTemperature = GetNullableDouble(reader, 6),
                    MinimumOutletTemperature = GetNullableDouble(reader, 7),
                    MaximumOutletTemperature = GetNullableDouble(reader, 8),
                    AverageTemperatureDelta = GetNullableDouble(reader, 9),
                    RunningSampleCount = reader.GetInt64(10),
                    FaultSampleCount = reader.GetInt64(11)
                };
            }
        }

        private static async Task<List<HeatPumpAlarmEvent>> QueryEventsAsync(NpgsqlConnection connection, long deviceId, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
SELECT event_type, event_code, first_seen_at, last_seen_at, recovered_at, detail
FROM heat_pump_alarm_event
WHERE device_id = @device_id
  AND first_seen_at <= @end_time
  AND COALESCE(recovered_at, last_seen_at) >= @start_time
ORDER BY first_seen_at DESC
LIMIT 200;";
            var events = new List<HeatPumpAlarmEvent>();
            using (var command = CreateTimeRangeCommand(sql, connection, deviceId, startTime, endTime))
            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    events.Add(new HeatPumpAlarmEvent
                    {
                        EventType = reader.GetString(0),
                        EventCode = reader.GetInt32(1),
                        FirstSeenAt = ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(2)),
                        LastSeenAt = ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(3)),
                        RecoveredAt = reader.IsDBNull(4) ? (DateTime?)null : ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(4)),
                        Detail = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    });
                }
            }

            return events;
        }

        private static NpgsqlCommand CreateTimeRangeCommand(string sql, NpgsqlConnection connection, long deviceId, DateTime startTime, DateTime endTime)
        {
            var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@device_id", deviceId);
            command.Parameters.AddWithValue("@start_time", ToBeijingOffset(startTime).ToUniversalTime());
            command.Parameters.AddWithValue("@end_time", ToBeijingOffset(endTime).ToUniversalTime());
            return command;
        }

        private static HeatPumpHistoryMetricDefinition GetMetric(string metricKey)
        {
            var metric = Metrics.FirstOrDefault(item => string.Equals(item.Key, metricKey, StringComparison.OrdinalIgnoreCase));
            if (metric == null)
            {
                throw new InvalidOperationException("未知热泵查询指标: " + metricKey);
            }

            return metric;
        }

        private static int GetBucketSeconds(DateTime startTime, DateTime endTime, long pointCount)
        {
            if (pointCount <= MaximumChartPoints)
            {
                return 1;
            }

            return Math.Max(1, (int)Math.Ceiling((endTime - startTime).TotalSeconds / MaximumChartPoints));
        }

        private static void ValidateRange(DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException("结束时间必须晚于开始时间。");
            }

            if ((endTime - startTime).TotalDays > 366)
            {
                throw new ArgumentException("单次查询范围不能超过 366 天。");
            }
        }

        private static double? GetNullableDouble(NpgsqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
        }

        private static DateTimeOffset ToBeijingOffset(DateTime value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, TimeSpan.FromHours(8));
        }

        private static DateTime ToBeijingTime(DateTimeOffset value)
        {
            return value.ToOffset(TimeSpan.FromHours(8)).DateTime;
        }

        private static string GetConnectionString()
        {
            foreach (var name in new[] { "MeterDb", "MeterAcquisition" })
            {
                var setting = ConfigurationManager.ConnectionStrings[name];
                if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
                {
                    return setting.ConnectionString;
                }
            }

            throw new ConfigurationErrorsException("缺少 connectionStrings: MeterDb, MeterAcquisition");
        }
    }

    internal sealed class HeatPumpHistoryMetricDefinition
    {
        public HeatPumpHistoryMetricDefinition(string key, string displayName, string expression, string unit)
        {
            Key = key;
            DisplayName = displayName;
            Expression = expression;
            Unit = unit;
        }

        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public string Expression { get; private set; }
        public string Unit { get; private set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal sealed class HeatPumpDeviceLookupItem
    {
        public long DeviceId { get; set; }
        public string SiteCode { get; set; }
        public string ControllerCode { get; set; }
        public int ModuleAddress { get; set; }
        public string DeviceName { get; set; }
        public string DisplayText { get; set; }

        public override string ToString()
        {
            return DisplayText ?? ControllerCode;
        }
    }

    internal sealed class HeatPumpHistoryPoint
    {
        public DateTime CollectedAt { get; set; }
        public double Value { get; set; }
    }

    internal sealed class HeatPumpHistorySummary
    {
        public long SampleCount { get; set; }
        public DateTime? FirstCollectedAt { get; set; }
        public DateTime? LastCollectedAt { get; set; }
        public double? AverageInletTemperature { get; set; }
        public double? MinimumInletTemperature { get; set; }
        public double? MaximumInletTemperature { get; set; }
        public double? AverageOutletTemperature { get; set; }
        public double? MinimumOutletTemperature { get; set; }
        public double? MaximumOutletTemperature { get; set; }
        public double? AverageTemperatureDelta { get; set; }
        public long RunningSampleCount { get; set; }
        public long FaultSampleCount { get; set; }
    }

    internal sealed class HeatPumpAlarmEvent
    {
        public string EventType { get; set; }
        public int EventCode { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? RecoveredAt { get; set; }
        public string Detail { get; set; }
    }

    internal sealed class HeatPumpHistoryResult
    {
        public HeatPumpHistoryMetricDefinition Metric { get; set; }
        public long PointCount { get; set; }
        public List<HeatPumpHistoryPoint> Points { get; set; }
        public HeatPumpHistorySummary Summary { get; set; }
        public List<HeatPumpAlarmEvent> Events { get; set; }
    }

    internal sealed class HeatPumpComparisonSeries
    {
        public HeatPumpDeviceLookupItem Device { get; set; }
        public HeatPumpHistoryMetricDefinition Metric { get; set; }
        public List<HeatPumpHistoryPoint> Points { get; set; }
    }
}
