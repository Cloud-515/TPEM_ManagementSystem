using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Npgsql;
using NpgsqlTypes;
using Newtonsoft.Json;

namespace MeterIngestionWorker
{
    internal static class Program
    {
        private static readonly BlockingCollection<MqttEnvelope> MessageQueue = new BlockingCollection<MqttEnvelope>(new ConcurrentQueue<MqttEnvelope>());
        private static readonly ConcurrentDictionary<string, DateTime> RecentMessages = new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(10);
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            NullValueHandling = NullValueHandling.Ignore
        };

        private static string _mqttHost;
        private static int _mqttPort;
        private static string _mqttClientId;
        private static string _mqttLegacyTopic;
        private static bool _subscribeLegacyTopic;
        private static string _mqttTopicPattern;
        private static string _mqttRegistryTopicPattern;
        private static string _mqttUsername;
        private static string _mqttPassword;
        private static string _siteCode;
        private static string _mysqlConnectionString;
        private static int _processWorkers;

        private static async Task<int> Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            LoadConfiguration();
            var exitCode = 0;

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("正在停止后台订阅服务...");
                };

                var workers = StartWorkers();
                try
                {
                    await RunMqttSubscriberAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("服务已停止。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("服务异常退出: " + ex);
                    exitCode = 1;
                }
                finally
                {
                    MessageQueue.CompleteAdding();
                }

                await Task.WhenAll(workers).ConfigureAwait(false);
                return exitCode;
            }
        }

        private static void LoadConfiguration()
        {
            _mqttHost = GetRequiredAppSetting("MqttHost");
            _mqttPort = int.Parse(GetAppSetting("MqttPort", "2883"), CultureInfo.InvariantCulture);
            _mqttClientId = GetAppSetting("MqttClientId", "meter-ingestion-worker");
            _mqttLegacyTopic = GetAppSetting("MqttLegacyTopic", "meter/data");
            _subscribeLegacyTopic = bool.TryParse(GetAppSetting("SubscribeLegacyTopic", "false"), out var subscribeLegacyTopic) && subscribeLegacyTopic;
            _mqttTopicPattern = GetAppSetting("MqttTopicPattern", "meter/+/+/+");
            _mqttRegistryTopicPattern = GetAppSetting("MqttRegistryTopicPattern", "meter/registry/+/+");
            _mqttUsername = GetAppSetting("MqttUsername", string.Empty);
            _mqttPassword = GetAppSetting("MqttPassword", string.Empty);
            _siteCode = GetAppSetting("SiteCode", string.Empty);
            _mysqlConnectionString = GetFirstAvailableConnectionString("MeterDb", "MeterAcquisition");
            _processWorkers = int.Parse(GetAppSetting("ProcessWorkers", "1"), CultureInfo.InvariantCulture);
            if (_processWorkers < 1)
            {
                _processWorkers = 1;
            }
        }

        private static Task[] StartWorkers()
        {
            var workers = new Task[_processWorkers];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(() => ProcessMessages());
            }

            return workers;
        }

        private static async Task RunMqttSubscriberAsync(CancellationToken cancellationToken)
        {
            var mqttFactory = new MqttFactory();
            using (var client = mqttFactory.CreateMqttClient())
            {
                client.ApplicationMessageReceivedAsync += args =>
                {
                    var segment = args.ApplicationMessage.PayloadSegment;
                    var payload = segment.Array == null
                        ? string.Empty
                        : Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);

                    var envelope = new MqttEnvelope
                    {
                        ClientId = TryExtractPublisherClientId(payload) ?? args.ClientId ?? _mqttClientId,
                        Topic = args.ApplicationMessage.Topic,
                        Qos = (byte)args.ApplicationMessage.QualityOfServiceLevel,
                        PayloadJson = payload,
                        ReceivedAt = DateTime.Now
                    };

                    if (!MessageQueue.IsAddingCompleted)
                    {
                        MessageQueue.Add(envelope, cancellationToken);
                    }

                    return Task.CompletedTask;
                };

                client.DisconnectedAsync += async args =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    Console.WriteLine("MQTT 已断开，5 秒后重连。");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    await ConnectAndSubscribeAsync(client, cancellationToken).ConfigureAwait(false);
                };

                await ConnectAndSubscribeAsync(client, cancellationToken).ConfigureAwait(false);
                Console.WriteLine("订阅服务已启动，按 Ctrl+C 停止。");

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }

                if (client.IsConnected)
                {
                    await client.DisconnectAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task ConnectAndSubscribeAsync(IMqttClient client, CancellationToken cancellationToken)
        {
            if (client.IsConnected)
            {
                return;
            }

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttHost, _mqttPort)
                .WithClientId(_mqttClientId)
                .WithCleanSession();

            if (!string.IsNullOrWhiteSpace(_mqttUsername))
            {
                builder.WithCredentials(_mqttUsername, _mqttPassword);
            }

            await client.ConnectAsync(builder.Build(), cancellationToken).ConfigureAwait(false);
            Console.WriteLine("MQTT 已连接: " + _mqttHost + ":" + _mqttPort);

            var subscribeBuilder = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_mqttTopicPattern))
                .WithTopicFilter(f => f.WithTopic(_mqttRegistryTopicPattern));

            if (_subscribeLegacyTopic && !string.IsNullOrWhiteSpace(_mqttLegacyTopic) && !string.Equals(_mqttLegacyTopic, _mqttTopicPattern, StringComparison.OrdinalIgnoreCase))
            {
                subscribeBuilder.WithTopicFilter(f => f.WithTopic(_mqttLegacyTopic));
            }

            var subscribeOptions = subscribeBuilder.Build();
            await client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

            var topics = new System.Collections.Generic.List<string>
            {
                _mqttTopicPattern,
                _mqttRegistryTopicPattern
            };
            if (_subscribeLegacyTopic && !string.IsNullOrWhiteSpace(_mqttLegacyTopic) && !string.Equals(_mqttLegacyTopic, _mqttTopicPattern, StringComparison.OrdinalIgnoreCase))
            {
                topics.Add(_mqttLegacyTopic);
            }

            Console.WriteLine("已订阅主题: " + string.Join(" , ", topics));
        }

        private static void ProcessMessages()
        {
            foreach (var envelope in MessageQueue.GetConsumingEnumerable())
            {
                try
                {
                    HandleEnvelope(envelope);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("处理消息失败: " + ex.Message);
                }
            }
        }

        private static void HandleEnvelope(MqttEnvelope envelope)
        {
            if (IsDuplicateEnvelope(envelope))
            {
                Console.WriteLine("检测到重复消息，已跳过: " + envelope.Topic);
                return;
            }

            if (IsRegistryTopic(envelope.Topic))
            {
                HandleRegistryEnvelope(envelope);
                return;
            }

            HandleTelemetryEnvelope(envelope);
        }

        private static void HandleTelemetryEnvelope(MqttEnvelope envelope)
        {
            var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(envelope.PayloadJson);
            if (telemetry == null)
            {
                throw new InvalidOperationException("MQTT 消息为空或 JSON 反序列化失败。");
            }

            ApplyTopicMetadata(envelope.Topic, telemetry);

            if (string.IsNullOrWhiteSpace(telemetry.MeterCode))
            {
                throw new InvalidOperationException("消息缺少 meter_code。");
            }

            if (string.IsNullOrWhiteSpace(telemetry.SiteCode))
            {
                telemetry.SiteCode = _siteCode;
            }

            long logId;
            using (var logConnection = new NpgsqlConnection(_mysqlConnectionString))
            {
                logConnection.Open();
                logId = InsertMessageLog(logConnection, null, envelope, telemetry.MeterCode);
            }

            try
            {
                using (var connection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        long meterId = GetMeterId(connection, transaction, telemetry.MeterCode, telemetry.SiteCode);

                        UpsertRealtimeLatest(connection, transaction, meterId, telemetry);
                        InsertRealtimeHistory(connection, transaction, meterId, telemetry);
                        UpsertEnergyLatest(connection, transaction, meterId, telemetry);
                        InsertEnergyHistory(connection, transaction, meterId, telemetry);
                        UpsertPowerQualityLatest(connection, transaction, meterId, telemetry);
                        InsertPowerQualityHistory(connection, transaction, meterId, telemetry);
                        UpsertMeterStatus(connection, transaction, meterId, telemetry, null);
                        UpdateMeterLastSeen(connection, transaction, meterId, telemetry.CollectTime);
                        transaction.Commit();
                    }
                }

                using (var logConnection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    logConnection.Open();
                    MarkMessageLogProcessed(logConnection, null, logId);
                }
            }
            catch (Exception ex)
            {
                MarkLogFailed(logId, envelope, ex.Message);
                throw;
            }
        }

        private static void HandleRegistryEnvelope(MqttEnvelope envelope)
        {
            var registry = JsonConvert.DeserializeObject<MeterRegistryMessage>(envelope.PayloadJson);
            if (registry == null)
            {
                throw new InvalidOperationException("档案同步消息为空或 JSON 反序列化失败。");
            }

            ApplyRegistryTopicMetadata(envelope.Topic, registry);

            if (string.IsNullOrWhiteSpace(registry.SiteCode))
            {
                registry.SiteCode = _siteCode;
            }

            if (string.IsNullOrWhiteSpace(registry.BoxCode))
            {
                throw new InvalidOperationException("档案同步消息缺少 box_code。");
            }

            long logId;
            using (var logConnection = new NpgsqlConnection(_mysqlConnectionString))
            {
                logConnection.Open();
                logId = InsertMessageLog(logConnection, null, envelope, GetRegistryLogMeterCode(registry));
            }

            try
            {
                using (var connection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        ProcessRegistryMessage(connection, transaction, registry);
                        transaction.Commit();
                    }
                }

                using (var logConnection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    logConnection.Open();
                    MarkMessageLogProcessed(logConnection, null, logId);
                }
            }
            catch (Exception ex)
            {
                MarkLogFailed(logId, envelope, ex.Message);
                throw;
            }
        }

        private static long InsertMessageLog(NpgsqlConnection connection, NpgsqlTransaction transaction, MqttEnvelope envelope, string meterCode)
        {
            const string insertSql = @"
INSERT INTO mqtt_message_log
(client_id, topic, qos, payload_json, meter_code, received_at, process_status)
VALUES
(@client_id, @topic, @qos, @payload_json, @meter_code, @received_at, 'pending')
RETURNING id;";

            using (var command = new NpgsqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@client_id", envelope.ClientId ?? string.Empty);
                command.Parameters.AddWithValue("@topic", envelope.Topic ?? string.Empty);
                command.Parameters.AddWithValue("@qos", (short)envelope.Qos);
                command.Parameters.Add("@payload_json", NpgsqlDbType.Jsonb).Value = envelope.PayloadJson ?? "{}";
                command.Parameters.AddWithValue("@meter_code", (object)meterCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@received_at", envelope.ReceivedAt);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void MarkMessageLogProcessed(NpgsqlConnection connection, NpgsqlTransaction transaction, long logId)
        {
            const string sql = @"
UPDATE mqtt_message_log
SET process_status = 'success', error_message = NULL
WHERE id = @id;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@id", logId);
                command.ExecuteNonQuery();
            }
        }

        private static void MarkLogFailed(long logId, MqttEnvelope envelope, string errorMessage)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    connection.Open();
                    const string sql = @"
UPDATE mqtt_message_log
SET process_status = 'failed', error_message = @error_message
WHERE id = @id;";

                    var meterCode = TryExtractMeterCode(envelope.PayloadJson);
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@error_message", Truncate(errorMessage, 255));
                        command.Parameters.AddWithValue("@id", logId);
                        command.ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(meterCode))
                    {
                        long meterId = GetMeterId(connection, null, meterCode, null, false);
                        if (meterId > 0)
                        {
                            UpsertMeterStatus(connection, null, meterId, null, errorMessage);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static long GetMeterId(NpgsqlConnection connection, NpgsqlTransaction transaction, string meterCode, string siteCode, bool throwIfMissing = true)
        {
            const string sql = @"
SELECT m.id
FROM meter m
LEFT JOIN site s ON s.id = m.site_id
WHERE m.meter_code = @meter_code
  AND (@site_code = '' OR s.site_code = @site_code)
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_code", meterCode);
                command.Parameters.AddWithValue("@site_code", siteCode ?? string.Empty);
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    if (throwIfMissing)
                    {
                        throw new InvalidOperationException("未在 meter 表中找到档案: " + meterCode);
                    }

                    return 0;
                }

                return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
        }

        private static void ProcessRegistryMessage(NpgsqlConnection connection, NpgsqlTransaction transaction, MeterRegistryMessage registry)
        {
            long siteId = UpsertSite(connection, transaction, registry.SiteCode);
            long boxId = UpsertDistributionBox(connection, transaction, siteId, registry.BoxCode, registry.BoxName);
            var action = (registry.Action ?? string.Empty).Trim().ToLowerInvariant();

            if (action == "disable")
            {
                foreach (var item in registry.Meters ?? new System.Collections.Generic.List<MeterRegistryItem>())
                {
                    DisableMeter(connection, transaction, siteId, boxId, registry, item);
                }
                return;
            }

            var activeMeterIds = new System.Collections.Generic.HashSet<long>();
            foreach (var item in registry.Meters ?? new System.Collections.Generic.List<MeterRegistryItem>())
            {
                if (string.IsNullOrWhiteSpace(item?.MeterCode))
                {
                    continue;
                }

                long meterId = UpsertMeter(connection, transaction, siteId, boxId, registry, item);
                activeMeterIds.Add(meterId);
                UpsertMeterStatus(connection, transaction, meterId, registry.ScanTime, registry.ScanTime, null, null, "online", true);
                UpdateMeterLastSeen(connection, transaction, meterId, registry.ScanTime);
            }

            MarkMissingMetersOffline(connection, transaction, siteId, boxId, activeMeterIds, registry.ScanTime);
        }

        private static long UpsertSite(NpgsqlConnection connection, NpgsqlTransaction transaction, string siteCode)
        {
            const string sql = @"
INSERT INTO site
(site_code, site_name)
VALUES
(@site_code, @site_name)
ON CONFLICT (site_code) DO UPDATE SET
site_name = EXCLUDED.site_name,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_code", siteCode);
                command.Parameters.AddWithValue("@site_name", siteCode);
                command.ExecuteNonQuery();
            }

            return GetSiteId(connection, transaction, siteCode);
        }

        private static long GetSiteId(NpgsqlConnection connection, NpgsqlTransaction transaction, string siteCode)
        {
            const string sql = @"
SELECT id
FROM site
WHERE site_code = @site_code
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_code", siteCode);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static long UpsertDistributionBox(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, string boxCode, string boxName)
        {
            const string sql = @"
INSERT INTO distribution_box
(site_id, box_code, box_name, location)
VALUES
(@site_id, @box_code, @box_name, @location)
ON CONFLICT (site_id, box_code) DO UPDATE SET
box_name = EXCLUDED.box_name,
location = EXCLUDED.location,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@box_code", boxCode);
                command.Parameters.AddWithValue("@box_name", string.IsNullOrWhiteSpace(boxName) ? boxCode : boxName);
                command.Parameters.AddWithValue("@location", string.IsNullOrWhiteSpace(boxName) ? (object)DBNull.Value : boxName);
                command.ExecuteNonQuery();
            }

            return GetDistributionBoxId(connection, transaction, siteId, boxCode);
        }

        private static long GetDistributionBoxId(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, string boxCode)
        {
            const string sql = @"
SELECT id
FROM distribution_box
WHERE site_id = @site_id AND box_code = @box_code
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@box_code", boxCode);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static long UpsertMeter(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, long boxId, MeterRegistryMessage registry, MeterRegistryItem item)
        {
            const string sql = @"
INSERT INTO meter
(site_id, box_id, meter_code, meter_name, slave_address, location, is_enabled, is_toolbar, mqtt_topic, last_seen_time)
VALUES
(@site_id, @box_id, @meter_code, @meter_name, @slave_address, @location, TRUE, @is_toolbar, @mqtt_topic, @last_seen_time)
ON CONFLICT (site_id, meter_code) DO UPDATE SET
box_id = EXCLUDED.box_id,
meter_name = EXCLUDED.meter_name,
slave_address = EXCLUDED.slave_address,
location = EXCLUDED.location,
is_enabled = TRUE,
is_toolbar = EXCLUDED.is_toolbar,
mqtt_topic = EXCLUDED.mqtt_topic,
last_seen_time = EXCLUDED.last_seen_time,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@box_id", boxId);
                command.Parameters.AddWithValue("@meter_code", item.MeterCode);
                command.Parameters.AddWithValue("@meter_name", string.IsNullOrWhiteSpace(item.MeterName) ? item.MeterCode : item.MeterName);
                command.Parameters.AddWithValue("@slave_address", (int)item.SlaveAddress);
                command.Parameters.AddWithValue("@location", (object)(item.Location ?? registry.BoxName ?? string.Empty));
                command.Parameters.AddWithValue("@is_toolbar", item.IsToolbar);
                command.Parameters.AddWithValue("@mqtt_topic", BuildMeterTopic(registry.SiteCode, registry.BoxCode, item.MeterCode));
                command.Parameters.AddWithValue("@last_seen_time", registry.ScanTime);
                command.ExecuteNonQuery();
            }

            return GetMeterId(connection, transaction, item.MeterCode, registry.SiteCode);
        }

        private static void DisableMeter(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, long boxId, MeterRegistryMessage registry, MeterRegistryItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.MeterCode))
            {
                return;
            }

            long meterId = UpsertMeter(connection, transaction, siteId, boxId, registry, item);
            const string sql = @"
UPDATE meter
SET is_enabled = FALSE,
    last_seen_time = @last_seen_time,
    updated_at = CURRENT_TIMESTAMP
WHERE id = @id;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@last_seen_time", registry.ScanTime);
                command.Parameters.AddWithValue("@id", meterId);
                command.ExecuteNonQuery();
            }

            UpsertMeterStatus(connection, transaction, meterId, registry.ScanTime, null, null, null, "disabled", false);
        }

        private static void MarkMissingMetersOffline(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, long boxId, System.Collections.Generic.HashSet<long> activeMeterIds, DateTime scanTime)
        {
            const string sql = @"
SELECT id
FROM meter
WHERE site_id = @site_id
  AND box_id = @box_id
  AND COALESCE(is_enabled, TRUE) = TRUE;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@box_id", boxId);
                using (var reader = command.ExecuteReader())
                {
                    var meterIds = new System.Collections.Generic.List<long>();
                    while (reader.Read())
                    {
                        meterIds.Add(reader.GetInt64(0));
                    }
                    reader.Close();

                    foreach (var meterId in meterIds)
                    {
                        if (activeMeterIds.Contains(meterId))
                        {
                            continue;
                        }

                        UpsertMeterStatus(connection, transaction, meterId, scanTime, null, null, null, "offline", false);
                    }
                }
            }
        }

        private static void UpsertRealtimeLatest(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO meter_realtime_latest
(meter_id, collect_time, voltage_a, voltage_b, voltage_c, voltage_ab, voltage_bc, voltage_ca,
 current_a, current_b, current_c, active_power_total, reactive_power_total, apparent_power_total,
 power_factor_total, frequency, data_quality)
VALUES
(@meter_id, @collect_time, @voltage_a, @voltage_b, @voltage_c, @voltage_ab, @voltage_bc, @voltage_ca,
 @current_a, @current_b, @current_c, @active_power_total, @reactive_power_total, @apparent_power_total,
 @power_factor_total, @frequency, 1)
ON CONFLICT (meter_id) DO UPDATE SET
collect_time = EXCLUDED.collect_time,
voltage_a = EXCLUDED.voltage_a,
voltage_b = EXCLUDED.voltage_b,
voltage_c = EXCLUDED.voltage_c,
voltage_ab = EXCLUDED.voltage_ab,
voltage_bc = EXCLUDED.voltage_bc,
voltage_ca = EXCLUDED.voltage_ca,
current_a = EXCLUDED.current_a,
current_b = EXCLUDED.current_b,
current_c = EXCLUDED.current_c,
active_power_total = EXCLUDED.active_power_total,
reactive_power_total = EXCLUDED.reactive_power_total,
apparent_power_total = EXCLUDED.apparent_power_total,
power_factor_total = EXCLUDED.power_factor_total,
frequency = EXCLUDED.frequency,
data_quality = EXCLUDED.data_quality,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillRealtimeParameters(command, meterId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertRealtimeHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO meter_realtime_history
(meter_id, collect_time, ts_minute, voltage_a, voltage_b, voltage_c, voltage_ab, voltage_bc, voltage_ca,
 current_a, current_b, current_c, active_power_total, reactive_power_total, apparent_power_total,
 power_factor_total, frequency, source)
VALUES
(@meter_id, @collect_time, @ts_minute, @voltage_a, @voltage_b, @voltage_c, @voltage_ab, @voltage_bc, @voltage_ca,
 @current_a, @current_b, @current_c, @active_power_total, @reactive_power_total, @apparent_power_total,
 @power_factor_total, @frequency, @source)
ON CONFLICT (meter_id, collect_time) DO NOTHING;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillRealtimeParameters(command, meterId, telemetry);
                command.Parameters.AddWithValue("@ts_minute", ToUnixMinute(telemetry.CollectTime));
                command.Parameters.AddWithValue("@source", telemetry.Source ?? "mqtt");
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertEnergyLatest(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            if (telemetry.Energy == null)
            {
                return;
            }

            const string sql = @"
INSERT INTO meter_energy_latest
(meter_id, collect_time, forward_active_energy, reverse_active_energy, forward_reactive_energy, reverse_reactive_energy)
VALUES
(@meter_id, @collect_time, @forward_active_energy, @reverse_active_energy, @forward_reactive_energy, @reverse_reactive_energy)
ON CONFLICT (meter_id) DO UPDATE SET
collect_time = EXCLUDED.collect_time,
forward_active_energy = EXCLUDED.forward_active_energy,
reverse_active_energy = EXCLUDED.reverse_active_energy,
forward_reactive_energy = EXCLUDED.forward_reactive_energy,
reverse_reactive_energy = EXCLUDED.reverse_reactive_energy,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillEnergyParameters(command, meterId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertEnergyHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            if (telemetry.Energy == null)
            {
                return;
            }

            const string sql = @"
INSERT INTO meter_energy_history
(meter_id, collect_time, forward_active_energy, reverse_active_energy, forward_reactive_energy, reverse_reactive_energy)
VALUES
(@meter_id, @collect_time, @forward_active_energy, @reverse_active_energy, @forward_reactive_energy, @reverse_reactive_energy)
ON CONFLICT (meter_id, collect_time) DO NOTHING;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillEnergyParameters(command, meterId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertPowerQualityLatest(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            if (telemetry.Quality == null)
            {
                return;
            }

            const string sql = @"
INSERT INTO meter_power_quality_latest
(meter_id, collect_time, current_thd_a, current_thd_b, current_thd_c, voltage_thd_a, voltage_thd_b, voltage_thd_c,
 voltage_unbalance, current_unbalance)
VALUES
(@meter_id, @collect_time, @current_thd_a, @current_thd_b, @current_thd_c, @voltage_thd_a, @voltage_thd_b, @voltage_thd_c,
 @voltage_unbalance, @current_unbalance)
ON CONFLICT (meter_id) DO UPDATE SET
collect_time = EXCLUDED.collect_time,
current_thd_a = EXCLUDED.current_thd_a,
current_thd_b = EXCLUDED.current_thd_b,
current_thd_c = EXCLUDED.current_thd_c,
voltage_thd_a = EXCLUDED.voltage_thd_a,
voltage_thd_b = EXCLUDED.voltage_thd_b,
voltage_thd_c = EXCLUDED.voltage_thd_c,
voltage_unbalance = EXCLUDED.voltage_unbalance,
current_unbalance = EXCLUDED.current_unbalance,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillQualityParameters(command, meterId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertPowerQualityHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            if (telemetry.Quality == null)
            {
                return;
            }

            const string sql = @"
INSERT INTO meter_power_quality_history
(meter_id, collect_time, current_thd_a, current_thd_b, current_thd_c, voltage_thd_a, voltage_thd_b, voltage_thd_c,
 voltage_unbalance, current_unbalance)
VALUES
(@meter_id, @collect_time, @current_thd_a, @current_thd_b, @current_thd_c, @voltage_thd_a, @voltage_thd_b, @voltage_thd_c,
 @voltage_unbalance, @current_unbalance)
ON CONFLICT (meter_id, collect_time) DO NOTHING;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillQualityParameters(command, meterId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertMeterStatus(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry, string errorMessage)
        {
            bool hasError = !string.IsNullOrWhiteSpace(errorMessage);
            DateTime? collectTime = telemetry?.CollectTime;
            UpsertMeterStatus(
                connection,
                transaction,
                meterId,
                collectTime,
                hasError ? (DateTime?)null : DateTime.Now,
                hasError ? (DateTime?)DateTime.Now : null,
                hasError ? errorMessage : null,
                hasError ? "comm_error" : "online",
                !hasError);
        }

        private static void UpsertMeterStatus(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            DateTime? lastCollectTime,
            DateTime? lastPublishTime,
            DateTime? lastErrorTime,
            string lastErrorMessage,
            string statusCode,
            bool isOnline)
        {
            const string sql = @"
INSERT INTO meter_status
(meter_id, is_online, last_collect_time, last_publish_time, last_error_time, last_error_message, status_code)
VALUES
(@meter_id, @is_online, @last_collect_time, @last_publish_time, @last_error_time, @last_error_message, @status_code)
ON CONFLICT (meter_id) DO UPDATE SET
is_online = EXCLUDED.is_online,
last_collect_time = EXCLUDED.last_collect_time,
last_publish_time = EXCLUDED.last_publish_time,
last_error_time = EXCLUDED.last_error_time,
last_error_message = EXCLUDED.last_error_message,
status_code = EXCLUDED.status_code,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@is_online", isOnline);
                command.Parameters.AddWithValue("@last_collect_time", (object)lastCollectTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@last_publish_time", (object)lastPublishTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@last_error_time", (object)lastErrorTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@last_error_message", string.IsNullOrWhiteSpace(lastErrorMessage) ? (object)DBNull.Value : Truncate(lastErrorMessage, 255));
                command.Parameters.AddWithValue("@status_code", statusCode ?? "offline");
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateMeterLastSeen(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, DateTime collectTime)
        {
            const string sql = @"
UPDATE meter
SET last_seen_time = @last_seen_time,
    updated_at = CURRENT_TIMESTAMP
WHERE id = @id;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@last_seen_time", collectTime);
                command.Parameters.AddWithValue("@id", meterId);
                command.ExecuteNonQuery();
            }
        }

        private static void FillRealtimeParameters(NpgsqlCommand command, long meterId, MeterTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@meter_id", meterId);
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime);
            command.Parameters.AddWithValue("@voltage_a", (object)telemetry.RealTime?.VoltageA ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_b", (object)telemetry.RealTime?.VoltageB ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_c", (object)telemetry.RealTime?.VoltageC ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_ab", (object)telemetry.RealTime?.VoltageAB ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_bc", (object)telemetry.RealTime?.VoltageBC ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_ca", (object)telemetry.RealTime?.VoltageCA ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_a", (object)telemetry.RealTime?.CurrentA ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_b", (object)telemetry.RealTime?.CurrentB ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_c", (object)telemetry.RealTime?.CurrentC ?? DBNull.Value);
            command.Parameters.AddWithValue("@active_power_total", (object)telemetry.RealTime?.ActivePowerTotal ?? DBNull.Value);
            command.Parameters.AddWithValue("@reactive_power_total", (object)telemetry.RealTime?.ReactivePowerTotal ?? DBNull.Value);
            command.Parameters.AddWithValue("@apparent_power_total", (object)telemetry.RealTime?.ApparentPowerTotal ?? DBNull.Value);
            command.Parameters.AddWithValue("@power_factor_total", (object)telemetry.RealTime?.PowerFactorTotal ?? DBNull.Value);
            command.Parameters.AddWithValue("@frequency", (object)telemetry.RealTime?.Frequency ?? DBNull.Value);
        }

        private static void FillEnergyParameters(NpgsqlCommand command, long meterId, MeterTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@meter_id", meterId);
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime);
            command.Parameters.AddWithValue("@forward_active_energy", telemetry.Energy.ForwardActiveEnergy);
            command.Parameters.AddWithValue("@reverse_active_energy", telemetry.Energy.ReverseActiveEnergy);
            command.Parameters.AddWithValue("@forward_reactive_energy", telemetry.Energy.ForwardReactiveEnergy);
            command.Parameters.AddWithValue("@reverse_reactive_energy", telemetry.Energy.ReverseReactiveEnergy);
        }

        private static void FillQualityParameters(NpgsqlCommand command, long meterId, MeterTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@meter_id", meterId);
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime);
            command.Parameters.AddWithValue("@current_thd_a", telemetry.Quality.CurrentTHDA);
            command.Parameters.AddWithValue("@current_thd_b", telemetry.Quality.CurrentTHDB);
            command.Parameters.AddWithValue("@current_thd_c", telemetry.Quality.CurrentTHDC);
            command.Parameters.AddWithValue("@voltage_thd_a", telemetry.Quality.VoltageTHDA);
            command.Parameters.AddWithValue("@voltage_thd_b", telemetry.Quality.VoltageTHDB);
            command.Parameters.AddWithValue("@voltage_thd_c", telemetry.Quality.VoltageTHDC);
            command.Parameters.AddWithValue("@voltage_unbalance", telemetry.Quality.VoltageUnbalance);
            command.Parameters.AddWithValue("@current_unbalance", telemetry.Quality.CurrentUnbalance);
        }

        private static void ApplyTopicMetadata(string topic, MeterTelemetryMessage telemetry)
        {
            if (string.IsNullOrWhiteSpace(topic) || telemetry == null)
            {
                return;
            }

            var segments = topic.Split('/');
            if (segments.Length != 4 || !string.Equals(segments[0], "meter", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(segments[1], "registry", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(telemetry.SiteCode))
            {
                telemetry.SiteCode = segments[1];
            }

            if (string.IsNullOrWhiteSpace(telemetry.BoxCode))
            {
                telemetry.BoxCode = segments[2];
            }

            if (string.IsNullOrWhiteSpace(telemetry.MeterCode))
            {
                telemetry.MeterCode = segments[3];
            }
        }

        private static bool IsRegistryTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            var segments = topic.Split('/');
            return segments.Length == 4
                && string.Equals(segments[0], "meter", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[1], "registry", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyRegistryTopicMetadata(string topic, MeterRegistryMessage registry)
        {
            if (!IsRegistryTopic(topic) || registry == null)
            {
                return;
            }

            var segments = topic.Split('/');
            if (string.IsNullOrWhiteSpace(registry.SiteCode))
            {
                registry.SiteCode = segments[2];
            }

            if (string.IsNullOrWhiteSpace(registry.BoxCode))
            {
                registry.BoxCode = segments[3];
            }
        }

        private static string GetRegistryLogMeterCode(MeterRegistryMessage registry)
        {
            var item = registry?.Meters?.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m?.MeterCode));
            return item?.MeterCode;
        }

        private static string BuildMeterTopic(string siteCode, string boxCode, string meterCode)
        {
            return "meter/"
                + SanitizeTopicSegment(siteCode)
                + "/"
                + SanitizeTopicSegment(boxCode)
                + "/"
                + SanitizeTopicSegment(meterCode);
        }

        private static string SanitizeTopicSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim().Replace("/", "_").Replace("+", "_").Replace("#", "_");
        }

        private static string GetRequiredAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException("缺少 appSettings: " + key);
            }

            return value;
        }

        private static string GetAppSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static string GetFirstAvailableConnectionString(params string[] names)
        {
            foreach (var name in names)
            {
                var settings = ConfigurationManager.ConnectionStrings[name];
                if (settings != null && !string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    return settings.ConnectionString;
                }
            }

            throw new ConfigurationErrorsException("缺少 connectionStrings: " + string.Join(", ", names));
        }

        private static long ToUnixMinute(DateTime time)
        {
            return new DateTimeOffset(time).ToUnixTimeSeconds() / 60;
        }

        private static string TryExtractMeterCode(string payloadJson)
        {
            try
            {
                var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(payloadJson);
                return telemetry?.MeterCode;
            }
            catch
            {
                return null;
            }
        }

        private static string TryExtractPublisherClientId(string payloadJson)
        {
            try
            {
                var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(payloadJson);
                if (telemetry == null || string.IsNullOrWhiteSpace(telemetry.MeterCode))
                {
                    return null;
                }

                return "publisher-" + telemetry.MeterCode;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsDuplicateEnvelope(MqttEnvelope envelope)
        {
            var key = ComputeEnvelopeFingerprint(envelope);
            var now = DateTime.UtcNow;
            CleanupRecentMessages(now);

            while (true)
            {
                if (!RecentMessages.TryGetValue(key, out var existing))
                {
                    if (RecentMessages.TryAdd(key, now))
                    {
                        return false;
                    }

                    continue;
                }

                if (now - existing > DuplicateWindow)
                {
                    if (RecentMessages.TryUpdate(key, now, existing))
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static string ComputeEnvelopeFingerprint(MqttEnvelope envelope)
        {
            var payload = envelope?.PayloadJson ?? string.Empty;
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                var hash = sha256.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static void CleanupRecentMessages(DateTime now)
        {
            foreach (var pair in RecentMessages)
            {
                if (now - pair.Value > DuplicateWindow)
                {
                    RecentMessages.TryRemove(pair.Key, out _);
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private sealed class MqttEnvelope
        {
            public string ClientId { get; set; }
            public string Topic { get; set; }
            public byte Qos { get; set; }
            public string PayloadJson { get; set; }
            public DateTime ReceivedAt { get; set; }
        }
    }
}
