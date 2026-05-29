using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace MeterAcquisition
{
    internal sealed class HistoryQueryService
    {
        private static readonly IReadOnlyList<HistoryMetricDefinition> MetricDefinitions = new List<HistoryMetricDefinition>
        {
            new HistoryMetricDefinition("voltage_a", "A相电压", "meter_realtime_history", "voltage_a", "V", true),
            new HistoryMetricDefinition("voltage_b", "B相电压", "meter_realtime_history", "voltage_b", "V", true),
            new HistoryMetricDefinition("voltage_c", "C相电压", "meter_realtime_history", "voltage_c", "V", true),
            new HistoryMetricDefinition("current_a", "A相电流", "meter_realtime_history", "current_a", "A", true),
            new HistoryMetricDefinition("current_b", "B相电流", "meter_realtime_history", "current_b", "A", true),
            new HistoryMetricDefinition("current_c", "C相电流", "meter_realtime_history", "current_c", "A", true),
            new HistoryMetricDefinition("active_power_total", "总有功功率", "meter_realtime_history", "active_power_total", "W", true),
            new HistoryMetricDefinition("reactive_power_total", "总无功功率", "meter_realtime_history", "reactive_power_total", "var", true),
            new HistoryMetricDefinition("apparent_power_total", "总视在功率", "meter_realtime_history", "apparent_power_total", "VA", true),
            new HistoryMetricDefinition("power_factor_total", "总功率因数", "meter_realtime_history", "power_factor_total", string.Empty, true),
            new HistoryMetricDefinition("frequency", "频率", "meter_realtime_history", "frequency", "Hz", true),
            new HistoryMetricDefinition("forward_active_energy", "正向有功电能", "meter_energy_history", "forward_active_energy", "kWh", false),
            new HistoryMetricDefinition("reverse_active_energy", "反向有功电能", "meter_energy_history", "reverse_active_energy", "kWh", false),
            new HistoryMetricDefinition("forward_reactive_energy", "正向无功电能", "meter_energy_history", "forward_reactive_energy", "kvarh", false),
            new HistoryMetricDefinition("reverse_reactive_energy", "反向无功电能", "meter_energy_history", "reverse_reactive_energy", "kvarh", false),
            new HistoryMetricDefinition("current_thd_a", "A相电流谐波", "meter_power_quality_history", "current_thd_a", "%", false),
            new HistoryMetricDefinition("current_thd_b", "B相电流谐波", "meter_power_quality_history", "current_thd_b", "%", false),
            new HistoryMetricDefinition("current_thd_c", "C相电流谐波", "meter_power_quality_history", "current_thd_c", "%", false),
            new HistoryMetricDefinition("voltage_thd_a", "A相电压谐波", "meter_power_quality_history", "voltage_thd_a", "%", false),
            new HistoryMetricDefinition("voltage_thd_b", "B相电压谐波", "meter_power_quality_history", "voltage_thd_b", "%", false),
            new HistoryMetricDefinition("voltage_thd_c", "C相电压谐波", "meter_power_quality_history", "voltage_thd_c", "%", false),
            new HistoryMetricDefinition("voltage_unbalance", "电压不平衡度", "meter_power_quality_history", "voltage_unbalance", "%", false),
            new HistoryMetricDefinition("current_unbalance", "电流不平衡度", "meter_power_quality_history", "current_unbalance", "%", false)
        };

        private readonly string _connectionString;

        public HistoryQueryService()
        {
            _connectionString = GetConnectionString();
        }

        public IReadOnlyList<HistoryMetricDefinition> GetMetricDefinitions()
        {
            return MetricDefinitions;
        }

        public async Task<List<MeterLookupItem>> GetMetersAsync()
        {
            const string sql = @"
SELECT m.id,
       m.meter_code,
       m.meter_name,
       b.box_code,
       b.box_name,
       s.site_code,
       s.site_name
FROM meter m
JOIN distribution_box b ON b.id = m.box_id
JOIN site s ON s.id = m.site_id
WHERE COALESCE(m.is_enabled, TRUE) = TRUE
ORDER BY s.site_code, b.box_code, m.meter_code;";

            var meters = new List<MeterLookupItem>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new NpgsqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var item = new MeterLookupItem
                        {
                            MeterId = reader.GetInt64(0),
                            MeterCode = reader.GetString(1),
                            MeterName = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                            BoxCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            BoxName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            SiteCode = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            SiteName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                        };
                        item.DisplayText = string.Format(CultureInfo.InvariantCulture, "{0} ({1}) - {2}/{3}", item.MeterName, item.MeterCode, item.SiteCode, item.BoxCode);
                        meters.Add(item);
                    }
                }
            }

            return meters;
        }

        public async Task<List<SiteTreeItem>> GetMeterTreeAsync()
        {
            var meters = await GetMetersAsync().ConfigureAwait(false);
            var sites = meters
                .GroupBy(m => new { m.SiteCode, m.SiteName })
                .Select(siteGroup => new SiteTreeItem
                {
                    SiteCode = siteGroup.Key.SiteCode,
                    SiteName = siteGroup.Key.SiteName,
                    Boxes = siteGroup
                        .GroupBy(m => new { m.BoxCode, m.BoxName })
                        .Select(boxGroup => new BoxTreeItem
                        {
                            BoxCode = boxGroup.Key.BoxCode,
                            BoxName = boxGroup.Key.BoxName,
                            Meters = boxGroup.OrderBy(m => m.MeterCode).ToList()
                        })
                        .OrderBy(b => b.BoxCode)
                        .ToList()
                })
                .OrderBy(s => s.SiteCode)
                .ToList();

            return sites;
        }

        public async Task<QuickHistoryResult> QueryQuickHistoryAsync(long meterId, string metricKey, DateTime startTime, DateTime endTime, int pageNumber, int pageSize)
        {
            var metric = GetMetricDefinition(metricKey);
            var totalRows = 0L;
            var chartPoints = new List<HistoryPoint>();
            var rows = new List<HistoryGridRow>();
            var startBeijing = ToBeijingOffset(startTime);
            var endBeijing = ToBeijingOffset(endTime);

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                totalRows = await QueryRowCountAsync(connection, metric, meterId, startBeijing, endBeijing).ConfigureAwait(false);
                chartPoints = await QuerySeriesAsync(connection, metric, meterId, startBeijing, endBeijing, totalRows).ConfigureAwait(false);
                rows = await QueryQuickRowsAsync(connection, metric, meterId, startBeijing, endBeijing, pageNumber, pageSize).ConfigureAwait(false);
            }

            return new QuickHistoryResult
            {
                Metric = metric,
                TotalRows = totalRows,
                ChartPoints = chartPoints,
                Rows = rows
            };
        }

        public async Task<List<ComparisonSeriesResult>> QueryComparisonAsync(IReadOnlyList<MeterLookupItem> meters, string metricKey, DateTime startTime, DateTime endTime)
        {
            var metric = GetMetricDefinition(metricKey);
            var startBeijing = ToBeijingOffset(startTime);
            var endBeijing = ToBeijingOffset(endTime);
            var tasks = meters.Select(m => QueryComparisonSeriesAsync(m, metric, startBeijing, endBeijing)).ToArray();
            var series = await Task.WhenAll(tasks).ConfigureAwait(false);
            return series.Where(s => s != null).ToList();
        }

        private async Task<ComparisonSeriesResult> QueryComparisonSeriesAsync(MeterLookupItem meter, HistoryMetricDefinition metric, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var totalRows = await QueryRowCountAsync(connection, metric, meter.MeterId, startTime, endTime).ConfigureAwait(false);
                var points = await QuerySeriesAsync(connection, metric, meter.MeterId, startTime, endTime, totalRows).ConfigureAwait(false);
                return new ComparisonSeriesResult
                {
                    MeterId = meter.MeterId,
                    MeterName = meter.MeterName,
                    MeterCode = meter.MeterCode,
                    Metric = metric,
                    Points = points
                };
            }
        }

        private async Task<long> QueryRowCountAsync(NpgsqlConnection connection, HistoryMetricDefinition metric, long meterId, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            var sql = string.Format(CultureInfo.InvariantCulture,
                "SELECT COUNT(*) FROM public.{0} WHERE meter_id = @meter_id AND collect_time BETWEEN @start_time AND @end_time AND {1} IS NOT NULL;",
                metric.TableName,
                metric.ColumnName);

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@start_time", startTime.ToUniversalTime());
                command.Parameters.AddWithValue("@end_time", endTime.ToUniversalTime());
                var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
                return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
        }

        private async Task<List<HistoryPoint>> QuerySeriesAsync(NpgsqlConnection connection, HistoryMetricDefinition metric, long meterId, DateTimeOffset startTime, DateTimeOffset endTime, long totalRows)
        {
            var bucketSeconds = GetBucketSeconds(startTime, endTime, totalRows);
            string sql;
            if (bucketSeconds > 1)
            {
                sql = string.Format(CultureInfo.InvariantCulture, @"
SELECT MIN(collect_time) AS collect_time,
       AVG(CAST({1} AS double precision)) AS metric_value
FROM public.{0}
WHERE meter_id = @meter_id
  AND collect_time BETWEEN @start_time AND @end_time
  AND {1} IS NOT NULL
GROUP BY FLOOR(EXTRACT(EPOCH FROM collect_time) / @bucket_seconds)
ORDER BY MIN(collect_time);",
                    metric.TableName,
                    metric.ColumnName);
            }
            else
            {
                sql = string.Format(CultureInfo.InvariantCulture, @"
SELECT collect_time,
       CAST({1} AS double precision) AS metric_value
FROM public.{0}
WHERE meter_id = @meter_id
  AND collect_time BETWEEN @start_time AND @end_time
  AND {1} IS NOT NULL
ORDER BY collect_time;",
                    metric.TableName,
                    metric.ColumnName);
            }

            var points = new List<HistoryPoint>();
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@start_time", startTime.ToUniversalTime());
                command.Parameters.AddWithValue("@end_time", endTime.ToUniversalTime());
                if (bucketSeconds > 1)
                {
                    command.Parameters.AddWithValue("@bucket_seconds", bucketSeconds);
                }

                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        if (reader.IsDBNull(1))
                        {
                            continue;
                        }

                        points.Add(new HistoryPoint
                        {
                            CollectTime = ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(0)),
                            Value = reader.GetDouble(1)
                        });
                    }
                }
            }

            return points;
        }

        private async Task<List<HistoryGridRow>> QueryQuickRowsAsync(NpgsqlConnection connection, HistoryMetricDefinition metric, long meterId, DateTimeOffset startTime, DateTimeOffset endTime, int pageNumber, int pageSize)
        {
            var sourceSql = metric.SupportsSourceColumn ? "source" : "NULL::text AS source";
            var sql = string.Format(CultureInfo.InvariantCulture, @"
SELECT collect_time,
       CAST({1} AS double precision) AS metric_value,
       {2}
FROM public.{0}
WHERE meter_id = @meter_id
  AND collect_time BETWEEN @start_time AND @end_time
  AND {1} IS NOT NULL
ORDER BY collect_time ASC
LIMIT @limit OFFSET @offset;",
                metric.TableName,
                metric.ColumnName,
                sourceSql);

            var rows = new List<HistoryGridRow>();
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@start_time", startTime.ToUniversalTime());
                command.Parameters.AddWithValue("@end_time", endTime.ToUniversalTime());
                command.Parameters.AddWithValue("@limit", pageSize);
                command.Parameters.AddWithValue("@offset", Math.Max(0, pageNumber - 1) * pageSize);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        rows.Add(new HistoryGridRow
                        {
                            CollectTime = ToBeijingTime(reader.GetFieldValue<DateTimeOffset>(0)),
                            Value = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1),
                            Unit = metric.Unit,
                            Source = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                        });
                    }
                }
            }

            return rows;
        }

        private static int GetBucketSeconds(DateTimeOffset startTime, DateTimeOffset endTime, long totalRows)
        {
            if (totalRows <= 1500)
            {
                return 1;
            }

            var seconds = Math.Max(1d, (endTime - startTime).TotalSeconds);
            return Math.Max(1, (int)Math.Ceiling(seconds / 1500d));
        }

        private static DateTimeOffset ToBeijingOffset(DateTime value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, TimeSpan.FromHours(8));
        }

        private static DateTime ToBeijingTime(DateTimeOffset value)
        {
            return value.ToOffset(TimeSpan.FromHours(8)).DateTime;
        }

        private static HistoryMetricDefinition GetMetricDefinition(string metricKey)
        {
            var metric = MetricDefinitions.FirstOrDefault(m => string.Equals(m.Key, metricKey, StringComparison.OrdinalIgnoreCase));
            if (metric == null)
            {
                throw new InvalidOperationException("未知查询维度: " + metricKey);
            }

            return metric;
        }

        private static string GetConnectionString()
        {
            foreach (var name in new[] { "MeterDb", "MeterAcquisition" })
            {
                var settings = ConfigurationManager.ConnectionStrings[name];
                if (settings != null && !string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    return settings.ConnectionString;
                }
            }

            throw new ConfigurationErrorsException("缺少 connectionStrings: MeterDb, MeterAcquisition");
        }
    }

    internal sealed class HistoryMetricDefinition
    {
        public HistoryMetricDefinition(string key, string displayName, string tableName, string columnName, string unit, bool supportsSourceColumn)
        {
            Key = key;
            DisplayName = displayName;
            TableName = tableName;
            ColumnName = columnName;
            Unit = unit;
            SupportsSourceColumn = supportsSourceColumn;
        }

        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public string TableName { get; private set; }
        public string ColumnName { get; private set; }
        public string Unit { get; private set; }
        public bool SupportsSourceColumn { get; private set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal sealed class MeterLookupItem
    {
        public long MeterId { get; set; }
        public string MeterCode { get; set; }
        public string MeterName { get; set; }
        public string BoxCode { get; set; }
        public string BoxName { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public string DisplayText { get; set; }

        public override string ToString()
        {
            return DisplayText ?? (MeterName ?? MeterCode ?? base.ToString());
        }
    }

    internal sealed class SiteTreeItem
    {
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public List<BoxTreeItem> Boxes { get; set; }
    }

    internal sealed class BoxTreeItem
    {
        public string BoxCode { get; set; }
        public string BoxName { get; set; }
        public List<MeterLookupItem> Meters { get; set; }
    }

    internal sealed class HistoryPoint
    {
        public DateTime CollectTime { get; set; }
        public double Value { get; set; }
    }

    internal sealed class HistoryGridRow
    {
        public DateTime CollectTime { get; set; }
        public double? Value { get; set; }
        public string Unit { get; set; }
        public string Source { get; set; }
    }

    internal sealed class QuickHistoryResult
    {
        public HistoryMetricDefinition Metric { get; set; }
        public long TotalRows { get; set; }
        public List<HistoryPoint> ChartPoints { get; set; }
        public List<HistoryGridRow> Rows { get; set; }
    }

    internal sealed class ComparisonSeriesResult
    {
        public long MeterId { get; set; }
        public string MeterCode { get; set; }
        public string MeterName { get; set; }
        public HistoryMetricDefinition Metric { get; set; }
        public List<HistoryPoint> Points { get; set; }
    }
}
