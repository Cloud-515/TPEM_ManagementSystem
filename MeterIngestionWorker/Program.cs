using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Tpem.Diagnostics;
using Tpem.Thresholds;

namespace MeterIngestionWorker
{
    internal static class Program
    {
        /// <summary>
        /// P0-7: 队列必须有界。原来是无界 BlockingCollection —— PostgreSQL 一旦不可用，
        /// 消息会一直堆积直到进程 OOM，而且此前没有任何迹象。
        /// 队列满时丢弃**最旧**的消息：实时遥测的价值随时间衰减，保住最新数据更有意义。
        /// </summary>
        private static BlockingCollection<MqttEnvelope> MessageQueue;

        private static readonly ConcurrentDictionary<string, DateTime> RecentMessages = new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(10);
        private static long _droppedEnvelopes;
        private static long _processedEnvelopes;
        private static long _failedEnvelopes;

        /// <summary>
        /// 报警去抖计数器（P1-6）。键为 "meterId|alarmCode"，值为连续命中次数。
        /// 放内存即可：进程重启后最多重新累计一遍去抖，不会误报也不会漏报既有告警。
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> AlarmDebounce =
            new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>判定阈值提供者（P1-1）。与上位机读同一张 meter_threshold 表。</summary>
        private static ThresholdProvider _thresholdProvider;
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DateFormatString = "o",
            DateParseHandling = DateParseHandling.DateTimeOffset,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static TimeSpan RealtimeHistoryInterval;
        private static TimeSpan EnergyHistoryInterval;
        private static TimeSpan QualityHistoryInterval;
        private static TimeSpan OfflineTimeout;
        private static TimeSpan HeatPumpOfflineTimeout;
        private static float VoltageAbsoluteChangeThreshold;
        private static float VoltageRelativeChangeThreshold;
        private static float CurrentAbsoluteChangeThreshold;
        private static float CurrentRelativeChangeThreshold;
        private static float PowerAbsoluteChangeThreshold;
        private static float PowerRelativeChangeThreshold;
        private static float PowerFactorChangeThreshold;
        private static float FrequencyChangeThreshold;
        private static float AlarmVoltageLowThreshold;
        private static float AlarmVoltageHighThreshold;
        private static float AlarmCurrentHighThreshold;
        private static float AlarmPowerFactorLowThreshold;

        private static string _mqttHost;
        private static int _mqttPort;
        private static string _mqttClientId;
        private static string _mqttLegacyTopic;
        private static bool _subscribeLegacyTopic;
        /// <summary>X-5：温控器链路未接通，默认不订阅。见 ConnectAndSubscribeAsync 里的说明。</summary>
        private static bool _subscribeThermostatTopic;
        private static string _mqttTopicPattern;
        private static string _mqttRegistryTopicPattern;
        private static string _mqttHeatPumpTopicPattern;
        private static string _mqttThermostatTopicPattern;
        private static string _mqttUsername;
        private static string _mqttPassword;
        private static string _siteCode;
        private static string _mysqlConnectionString;
        private static int _processWorkers;
        private static int _maxQueueLength;
        private static TimeSpan _mqttReconnectMaxDelay;
        private static TimeSpan _heartbeatInterval;
        private static int _messageLogRetentionHours;

        private static async Task<int> Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                // 配置错误必须给出清楚的原因，而不是一个裸的未处理异常堆栈（P0-1）。
                Console.WriteLine("配置加载失败，服务无法启动: " + ex.Message);
                return 2;
            }

            AppLogger.Initialize(
                "ingestion",
                AppLogger.ParseLevel(ConfigurationManager.AppSettings["LogLevel"], Tpem.Diagnostics.LogLevel.Info),
                true);
            InstallGlobalExceptionHandlers();
            WarmUpThresholds();

            var exitCode = 0;

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    AppLogger.Info("Lifecycle", "收到停止信号，正在停止后台订阅服务...");
                };

                var workers = StartWorkers();
                var offlineMonitor = Task.Run(() => MonitorOfflineMetersAsync(cts.Token), cts.Token);
                var heartbeat = Task.Run(() => ReportHeartbeatAsync(cts.Token), cts.Token);
                try
                {
                    await RunMqttSubscriberAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    AppLogger.Info("Lifecycle", "服务已停止。");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Lifecycle", "服务异常退出。", ex);
                    exitCode = 1;

                    // 异常路径同样要取消：离线监控与心跳都以 !IsCancellationRequested 作为退出条件，
                    // 不取消就会留下"进程活着、既不再订阅也不退出"的状态，只能手工杀掉。
                    cts.Cancel();
                }
                finally
                {
                    MessageQueue.CompleteAdding();
                }

                try
                {
                    await Task.WhenAll(workers.Concat(new[] { offlineMonitor, heartbeat })).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 停止信号下后台循环以取消异常结束，属正常收尾。
                    // 原实现没有这层捕获：Ctrl+C 会让 OperationCanceledException 冒到 Main 之外，
                    // 走 AppDomain 未处理异常处理器记为"服务崩溃"、退出码非 0（作为服务托管时会被判定为崩溃），
                    // 并且下面的累计统计与 AppLogger.Shutdown() 一并被跳过。
                }
                AppLogger.Info(
                    "Lifecycle",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "累计处理 {0} 条，失败 {1} 条，因队列满丢弃 {2} 条。",
                        Interlocked.Read(ref _processedEnvelopes),
                        Interlocked.Read(ref _failedEnvelopes),
                        Interlocked.Read(ref _droppedEnvelopes)));
                AppLogger.Shutdown();
                return exitCode;
            }
        }

        private static void LoadConfiguration()
        {            _mqttHost = GetRequiredAppSetting("MqttHost");
            _mqttPort = int.Parse(GetAppSetting("MqttPort", "1883"), CultureInfo.InvariantCulture);
            _mqttClientId = GetAppSetting("MqttClientId", "meter-ingestion-worker");
            _mqttLegacyTopic = GetAppSetting("MqttLegacyTopic", "meter/data");
            _subscribeLegacyTopic = bool.TryParse(GetAppSetting("SubscribeLegacyTopic", "false"), out var subscribeLegacyTopic) && subscribeLegacyTopic;
            _subscribeThermostatTopic = bool.TryParse(GetAppSetting("SubscribeThermostatTopic", "false"), out var subscribeThermostat) && subscribeThermostat;
            _mqttTopicPattern = GetAppSetting("MqttTopicPattern", "meter/+/+/+");
            _mqttRegistryTopicPattern = GetAppSetting("MqttRegistryTopicPattern", "meter/registry/+/+");
            _mqttHeatPumpTopicPattern = GetAppSetting("HeatPumpMqttTopicPattern", "tpem/+/heatpump/+/telemetry");
            _mqttThermostatTopicPattern = GetAppSetting("ThermostatMqttTopicPattern", "tpem/+/thermostat/+/telemetry");
            _mqttUsername = GetAppSetting("MqttUsername", string.Empty);
            _mqttPassword = GetAppSetting("MqttPassword", string.Empty);
            _siteCode = GetAppSetting("SiteCode", string.Empty);
            _mysqlConnectionString = GetFirstAvailableConnectionString("MeterDb", "MeterAcquisition");
            _processWorkers = int.Parse(GetAppSetting("ProcessWorkers", "1"), CultureInfo.InvariantCulture);
            if (_processWorkers < 1)
            {
                _processWorkers = 1;
            }

            _maxQueueLength = GetPositiveIntAppSetting("MaxQueueLength", 50000);
            MessageQueue = new BlockingCollection<MqttEnvelope>(new ConcurrentQueue<MqttEnvelope>(), _maxQueueLength);
            _mqttReconnectMaxDelay = TimeSpan.FromSeconds(GetPositiveIntAppSetting("MqttReconnectMaxDelaySeconds", 60));
            _heartbeatInterval = TimeSpan.FromSeconds(GetPositiveIntAppSetting("HeartbeatIntervalSeconds", 300));
            _messageLogRetentionHours = GetPositiveIntAppSetting("MessageLogRetentionHours", 48);
            _thresholdProvider = new ThresholdProvider(
                _mysqlConnectionString,
                TimeSpan.FromSeconds(GetPositiveIntAppSetting("ThresholdRefreshSeconds", 300)));

            RealtimeHistoryInterval = TimeSpan.FromSeconds(GetPositiveIntAppSetting("RealtimeHistoryIntervalSeconds", 30));
            EnergyHistoryInterval = TimeSpan.FromMinutes(GetPositiveIntAppSetting("EnergyHistoryIntervalMinutes", 15));
            QualityHistoryInterval = TimeSpan.FromMinutes(GetPositiveIntAppSetting("QualityHistoryIntervalMinutes", 10));
            OfflineTimeout = TimeSpan.FromSeconds(GetPositiveIntAppSetting("OfflineTimeoutSeconds", 15));
            HeatPumpOfflineTimeout = TimeSpan.FromSeconds(GetPositiveIntAppSetting("HeatPumpOfflineTimeoutSeconds", 30));
            VoltageAbsoluteChangeThreshold = GetPositiveFloatAppSetting("VoltageAbsoluteChangeThreshold", 3f);
            VoltageRelativeChangeThreshold = GetPositiveFloatAppSetting("VoltageRelativeChangeThreshold", 0.015f);
            CurrentAbsoluteChangeThreshold = GetPositiveFloatAppSetting("CurrentAbsoluteChangeThreshold", 0.5f);
            CurrentRelativeChangeThreshold = GetPositiveFloatAppSetting("CurrentRelativeChangeThreshold", 0.05f);
            PowerAbsoluteChangeThreshold = GetPositiveFloatAppSetting("PowerAbsoluteChangeThreshold", 1000f);
            PowerRelativeChangeThreshold = GetPositiveFloatAppSetting("PowerRelativeChangeThreshold", 0.05f);
            PowerFactorChangeThreshold = GetPositiveFloatAppSetting("PowerFactorChangeThreshold", 0.03f);
            FrequencyChangeThreshold = GetPositiveFloatAppSetting("FrequencyChangeThreshold", 0.05f);
            AlarmVoltageLowThreshold = GetPositiveFloatAppSetting("AlarmVoltageLowThreshold", 180f);
            AlarmVoltageHighThreshold = GetPositiveFloatAppSetting("AlarmVoltageHighThreshold", 260f);
            AlarmCurrentHighThreshold = GetPositiveFloatAppSetting("AlarmCurrentHighThreshold", 400f);
            AlarmPowerFactorLowThreshold = GetPositiveFloatAppSetting("AlarmPowerFactorLowThreshold", 0.5f);
        }

        /// <summary>
        /// P0-1：后台服务的兜底。没有这两道，一个后台线程异常会直接终止进程，
        /// 而现场只会看到"服务窗口不见了"，没有任何原因记录。
        /// </summary>
        /// <summary>
        /// 启动时先加载一次判定阈值并把生效值写进日志（P1-1）。
        /// 现场排查"为什么这台表被判成异常"时，第一步就是确认当时用的是哪套阈值、
        /// 是来自数据库还是内置兜底。等到第一条遥测到达才加载会让这件事无从追溯。
        /// </summary>
        private static void WarmUpThresholds()
        {
            var set = _thresholdProvider.Resolve(_siteCode, null, null);
            var source = _thresholdProvider.UsingFallback ? "内置兜底值（数据库不可用）" : "meter_threshold 表";
            var voltage = set.Get(ThresholdSet.VoltagePhase);
            var pf = set.Get(ThresholdSet.PowerFactorTotal);
            AppLogger.Info(
                "Threshold",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "判定阈值来源: {0}；相电压 {1}~{2} V，总功率因数下限 {3}，带电门槛 {4} V。",
                    source,
                    voltage != null && voltage.MinValue.HasValue ? voltage.MinValue.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-",
                    voltage != null && voltage.MaxValue.HasValue ? voltage.MaxValue.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-",
                    pf != null && pf.MinValue.HasValue ? pf.MinValue.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-",
                    set.EnergizedThreshold.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        private static void InstallGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                AppLogger.Error(
                    "UnhandledDomain",
                    "未处理异常，IsTerminating=" + e.IsTerminating + "。",
                    e.ExceptionObject as Exception);
                AppLogger.Shutdown();
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                AppLogger.Error("UnobservedTask", "存在无人处理的 Task 异常。", e.Exception);
                e.SetObserved();
            };
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
                        ReceivedAt = GetBeijingNow().UtcDateTime
                    };

                    EnqueueEnvelope(envelope);
                    return Task.CompletedTask;
                };

                client.DisconnectedAsync += args =>
                {
                    // 这里只留痕，不做重连 —— 重连交给下面的主循环统一负责。
                    // P0-6: 原实现在这个回调里"延时 5 秒后重连一次"，一旦那次连接失败，
                    // 异常在事件回调里消失，就再也不会有第二次尝试，服务进程活着但永久掉线。
                    //
                    // 注意：MQTTnet 在 ConnectAsync 失败时也会触发本回调，此时 ClientWasConnected=false。
                    // 那种情况由重连循环负责记录（带堆栈），这里不重复输出，否则一次失败会刷两份完整堆栈。
                    if (args.ClientWasConnected && !cancellationToken.IsCancellationRequested)
                    {
                        AppLogger.Warn("Mqtt", "与 Broker 的连接已断开，原因: " + args.Reason + "。将由主循环退避重连。");
                    }

                    return Task.CompletedTask;
                };

                await MaintainConnectionAsync(client, cancellationToken).ConfigureAwait(false);

                if (client.IsConnected)
                {
                    await client.DisconnectAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// P0-6：带指数退避的连接维持循环。只要服务在跑，就会一直尝试恢复连接，
        /// 并且每次尝试都留日志，"活着但掉线"变成可观测状态。
        /// </summary>
        private static async Task MaintainConnectionAsync(IMqttClient client, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromSeconds(5);
            var announced = false;
            var stackLogged = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (client.IsConnected)
                {
                    if (!announced)
                    {
                        AppLogger.Info("Mqtt", "订阅服务已就绪，按 Ctrl+C 停止。");
                        announced = true;
                    }

                    delay = TimeSpan.FromSeconds(5);
                    stackLogged = false;
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                announced = false;
                try
                {
                    await ConnectAndSubscribeAsync(client, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var summary = string.Format(
                        CultureInfo.InvariantCulture,
                        "连接 Broker {0}:{1} 失败，{2} 秒后重试。",
                        _mqttHost, _mqttPort, delay.TotalSeconds);

                    // 只在本轮故障的第一次记录完整堆栈。Broker 长时间不可用时，
                    // 后续重试只留一行摘要，避免几十行堆栈把日志刷爆导致真正的问题被淹没。
                    if (!stackLogged)
                    {
                        AppLogger.Error("Mqtt", summary, ex);
                        stackLogged = true;
                    }
                    else
                    {
                        AppLogger.Warn("Mqtt", summary + " 原因: " + RootCauseMessage(ex));
                    }
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, _mqttReconnectMaxDelay.TotalSeconds));
            }
        }

        private static string RootCauseMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        /// <summary>
        /// 入队。队列满时丢弃最旧的一条再放入最新的（P0-7）。
        /// </summary>
        private static void EnqueueEnvelope(MqttEnvelope envelope)
        {
            if (MessageQueue.IsAddingCompleted)
            {
                return;
            }

            try
            {
                if (MessageQueue.TryAdd(envelope))
                {
                    return;
                }

                MqttEnvelope discarded;
                if (MessageQueue.TryTake(out discarded))
                {
                    var dropped = Interlocked.Increment(ref _droppedEnvelopes);
                    if (dropped == 1 || dropped % 1000 == 0)
                    {
                        AppLogger.Warn(
                            "Queue",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "待入库队列已满（上限 {0}），累计丢弃最旧消息 {1} 条。请检查 PostgreSQL 可用性与入库吞吐。",
                                _maxQueueLength, dropped));
                    }
                }

                if (!MessageQueue.TryAdd(envelope))
                {
                    Interlocked.Increment(ref _droppedEnvelopes);
                }
            }
            catch (InvalidOperationException)
            {
                // 队列已关闭（服务正在停止），直接丢弃。
            }
        }

        /// <summary>
        /// 心跳日志（P0-6/P0-8）：定期输出队列深度与处理计数，
        /// 让"进程还在但什么都没干"可以从日志里发现。
        /// </summary>
        private static async Task ReportHeartbeatAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_heartbeatInterval, cancellationToken).ConfigureAwait(false);
                    AppLogger.Info(
                        "Heartbeat",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "队列深度 {0}/{1}，已处理 {2}，失败 {3}，丢弃 {4}。",
                            MessageQueue.Count,
                            _maxQueueLength,
                            Interlocked.Read(ref _processedEnvelopes),
                            Interlocked.Read(ref _failedEnvelopes),
                            Interlocked.Read(ref _droppedEnvelopes)));

                    PruneMeterMessageLog();
                }
            }
            catch (OperationCanceledException)
            {
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
            AppLogger.Info("Mqtt", "已连接 Broker " + _mqttHost + ":" + _mqttPort + "，ClientId=" + _mqttClientId);

            var subscribeBuilder = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_mqttTopicPattern))
                .WithTopicFilter(f => f.WithTopic(_mqttRegistryTopicPattern))
                .WithTopicFilter(f => f.WithTopic(_mqttHeatPumpTopicPattern));

            // X-5：温控器链路两端都没接通 —— 上位机的 PublishThermostatTelemetryAsync 从无调用点，
            // 现网也没有 thermostat_* 四张表（迁移 002 未执行，且业务决定暂不上线）。
            // 因此默认不订阅：订阅了也收不到消息，一旦收到反而会因表不存在而每条都失败。
            // 温控器要上线时把 SubscribeThermostatTopic 改成 true，并同时执行迁移 002、补上发布端调用。
            if (_subscribeThermostatTopic)
            {
                subscribeBuilder.WithTopicFilter(f => f.WithTopic(_mqttThermostatTopicPattern));
            }

            if (_subscribeLegacyTopic && !string.IsNullOrWhiteSpace(_mqttLegacyTopic) && !string.Equals(_mqttLegacyTopic, _mqttTopicPattern, StringComparison.OrdinalIgnoreCase))
            {
                subscribeBuilder.WithTopicFilter(f => f.WithTopic(_mqttLegacyTopic));
            }

            var subscribeOptions = subscribeBuilder.Build();
            await client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

            var topics = new List<string>
            {
                _mqttTopicPattern,
                _mqttRegistryTopicPattern,
                _mqttHeatPumpTopicPattern
            };
            if (_subscribeThermostatTopic)
            {
                topics.Add(_mqttThermostatTopicPattern);
            }

            if (_subscribeLegacyTopic && !string.IsNullOrWhiteSpace(_mqttLegacyTopic) && !string.Equals(_mqttLegacyTopic, _mqttTopicPattern, StringComparison.OrdinalIgnoreCase))
            {
                topics.Add(_mqttLegacyTopic);
            }

            AppLogger.Info("Mqtt", "已订阅主题: " + string.Join(" , ", topics));
            if (!_subscribeThermostatTopic)
            {
                AppLogger.Info(
                    "Mqtt",
                    "温控器主题未订阅（SubscribeThermostatTopic=false）。该链路尚未接通：" +
                    "上位机未接入发布、迁移 002 未执行。要启用需三件事同时做到：执行迁移 002、" +
                    "补上位机发布端调用、把本配置改为 true。");
            }
        }

        private static void ProcessMessages()
        {
            foreach (var envelope in MessageQueue.GetConsumingEnumerable())
            {
                try
                {
                    HandleEnvelope(envelope);
                    Interlocked.Increment(ref _processedEnvelopes);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failedEnvelopes);
                    AppLogger.Error("Ingest", "处理消息失败，主题: " + (envelope?.Topic ?? "-"), ex);
                }
            }
        }

        private static void HandleEnvelope(MqttEnvelope envelope)
        {
            if (IsDuplicateEnvelope(envelope))
            {
                AppLogger.Debug("Dedup", "检测到重复消息，已跳过: " + envelope.Topic);
                return;
            }

            if (IsRegistryTopic(envelope.Topic))
            {
                HandleRegistryEnvelope(envelope);
                return;
            }

            if (IsHeatPumpTopic(envelope.Topic))
            {
                HandleHeatPumpTelemetryEnvelope(envelope);
                return;
            }

            if (IsThermostatTopic(envelope.Topic))
            {
                if (!_subscribeThermostatTopic)
                {
                    // X-5：链路未接通时不去写 thermostat_* 四张表（现网不存在），
                    // 否则每条消息都会变成一条失败记录。
                    AppLogger.Debug("Ingest", "温控器链路未启用，忽略消息: " + envelope.Topic);
                    return;
                }

                HandleThermostatTelemetryEnvelope(envelope);
                return;
            }

            HandleTelemetryEnvelope(envelope);
        }

        private static void HandleTelemetryEnvelope(MqttEnvelope envelope)
        {
            var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(envelope.PayloadJson, JsonSettings);
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
                        // P1-4：库级幂等。放在事务内 —— 若后续入库失败回滚，去重标记也一并回滚，
                        // 消息才有机会被重投重试；若放事务外，一条失败的消息会被永久标记为已处理。
                        if (!TryMarkMeterMessageProcessed(connection, transaction, envelope, telemetry))
                        {
                            transaction.Commit();
                            AppLogger.Debug("Dedup", "电表消息已处理过，跳过重复投递: " + envelope.Topic + " msgId=" + telemetry.MessageId);
                            MarkMessageLogProcessed(connection, null, logId);
                            return;
                        }

                        long meterId = GetMeterId(connection, transaction, telemetry.MeterCode, telemetry.SiteCode);

                        if (telemetry.RealTime != null)
                        {
                            UpsertRealtimeLatest(connection, transaction, meterId, telemetry);
                            if (ShouldWriteRealtimeHistory(connection, transaction, meterId, telemetry))
                            {
                                InsertRealtimeHistory(connection, transaction, meterId, telemetry);
                            }

                            EvaluateRealtimeAlarms(
                                connection,
                                transaction,
                                meterId,
                                telemetry,
                                _thresholdProvider.Resolve(telemetry.SiteCode, telemetry.BoxCode, telemetry.MeterCode));
                        }

                        if (telemetry.Energy != null)
                        {
                            UpsertEnergyLatest(connection, transaction, meterId, telemetry);
                            if (ShouldWriteEnergyHistory(connection, transaction, meterId, telemetry))
                            {
                                InsertEnergyHistory(connection, transaction, meterId, telemetry);
                            }
                        }

                        if (telemetry.Quality != null)
                        {
                            UpsertPowerQualityLatest(connection, transaction, meterId, telemetry);
                            if (ShouldWriteQualityHistory(connection, transaction, meterId, telemetry))
                            {
                                InsertPowerQualityHistory(connection, transaction, meterId, telemetry);
                            }
                        }

                        UpsertMeterStatus(connection, transaction, meterId, telemetry, null);
                        UpdateMeterLastSeen(connection, transaction, meterId, telemetry.CollectTime);
                        // P1-2：收到遥测说明设备已恢复，成对地把 offline 告警恢复掉，
                        // 让 alarm_event 里 offline 事件有明确的 start_time / end_time / duration。
                        ClearAlarm(connection, transaction, meterId, "offline", telemetry.CollectTime, null);
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

        private static void HandleHeatPumpTelemetryEnvelope(MqttEnvelope envelope)
        {
            var telemetry = JsonConvert.DeserializeObject<HeatPumpTelemetryMessage>(envelope.PayloadJson, JsonSettings);
            ValidateHeatPumpTelemetry(envelope.Topic, telemetry);

            using (var connection = new NpgsqlConnection(_mysqlConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (!TryInsertHeatPumpMessageLog(connection, transaction, envelope.Topic, telemetry.MessageId))
                    {
                        transaction.Commit();
                        AppLogger.Debug("Dedup", "热泵消息已处理，跳过重复投递: " + envelope.Topic);
                        return;
                    }

                    var deviceId = UpsertHeatPumpDevice(connection, transaction, telemetry);
                    InsertHeatPumpTelemetryHistory(connection, transaction, deviceId, telemetry);
                    UpsertHeatPumpDeviceState(connection, transaction, deviceId, telemetry);
                    UpdateHeatPumpAlarmState(connection, transaction, deviceId, telemetry);
                    RecoverHeatPumpOfflineAlarm(connection, transaction, deviceId, telemetry.ModuleIndex, telemetry.CollectedAt);
                    transaction.Commit();
                }
            }
        }

        private static void HandleThermostatTelemetryEnvelope(MqttEnvelope envelope)
        {
            var telemetry = JsonConvert.DeserializeObject<ThermostatTelemetryMessage>(envelope.PayloadJson, JsonSettings);
            ValidateThermostatTelemetry(envelope.Topic, telemetry);

            using (var connection = new NpgsqlConnection(_mysqlConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (!TryInsertThermostatMessageLog(connection, transaction, telemetry.MessageId))
                    {
                        transaction.Commit();
                        AppLogger.Debug("Dedup", "温控器消息已处理，跳过重复投递: " + envelope.Topic);
                        return;
                    }

                    var deviceId = UpsertThermostatDevice(connection, transaction, telemetry);
                    InsertThermostatTelemetryHistory(connection, transaction, deviceId, telemetry);
                    UpsertThermostatDeviceState(connection, transaction, deviceId, telemetry);
                    transaction.Commit();
                }
            }
        }

        private static void ValidateThermostatTelemetry(string topic, ThermostatTelemetryMessage telemetry)
        {
            if (telemetry == null)
            {
                throw new InvalidOperationException("温控器遥测消息为空或 JSON 反序列化失败。");
            }

            if (!string.Equals(telemetry.MessageType, "thermostat.telemetry.v1", StringComparison.Ordinal)
                || telemetry.MessageId == Guid.Empty
                || string.IsNullOrWhiteSpace(telemetry.SiteCode)
                || string.IsNullOrWhiteSpace(telemetry.DeviceKey)
                || telemetry.SlaveId < 1 || telemetry.SlaveId > 99
                || telemetry.CollectedAt == default(DateTimeOffset))
            {
                throw new InvalidOperationException("温控器消息缺少有效身份或采集时间。");
            }

            var segments = topic == null ? new string[0] : topic.Split('/');
            if (segments.Length != 5
                || !string.Equals(segments[0], "tpem", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[2], "thermostat", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[4], "telemetry", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[1], SanitizeTopicSegment(telemetry.SiteCode), StringComparison.Ordinal)
                || !string.Equals(segments[3], SanitizeTopicSegment(telemetry.DeviceKey), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("温控器主题与消息身份不一致。");
            }
        }

        private static bool TryInsertThermostatMessageLog(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid messageId)
        {
            const string sql = @"
INSERT INTO thermostat_message_log (message_id)
VALUES (@message_id)
ON CONFLICT (message_id) DO NOTHING;";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@message_id", messageId);
                return command.ExecuteNonQuery() == 1;
            }
        }

        private static long UpsertThermostatDevice(NpgsqlConnection connection, NpgsqlTransaction transaction, ThermostatTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO thermostat_device (site_code, device_key, slave_id, name, group_name)
VALUES (@site_code, @device_key, @slave_id, @name, @group_name)
ON CONFLICT (site_code, device_key) DO UPDATE SET
slave_id = EXCLUDED.slave_id,
name = EXCLUDED.name,
group_name = EXCLUDED.group_name,
updated_at = CURRENT_TIMESTAMP
RETURNING id;";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_code", telemetry.SiteCode.Trim());
                command.Parameters.AddWithValue("@device_key", telemetry.DeviceKey.Trim());
                command.Parameters.AddWithValue("@slave_id", telemetry.SlaveId);
                command.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(telemetry.Name) ? (object)DBNull.Value : telemetry.Name.Trim());
                command.Parameters.AddWithValue("@group_name", string.IsNullOrWhiteSpace(telemetry.GroupName) ? (object)DBNull.Value : telemetry.GroupName.Trim());
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void InsertThermostatTelemetryHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, ThermostatTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO thermostat_telemetry_history
(device_id, collected_at, is_online, room_temperature_celsius, set_temperature_celsius, power_state, mode, fan_speed)
VALUES
(@device_id, @collected_at, @is_online, @room_temperature_celsius, @set_temperature_celsius, @power_state, @mode, @fan_speed);";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillThermostatParameters(command, deviceId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertThermostatDeviceState(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, ThermostatTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO thermostat_device_state
(device_id, collected_at, is_online, room_temperature_celsius, set_temperature_celsius, power_state, mode, fan_speed)
VALUES
(@device_id, @collected_at, @is_online, @room_temperature_celsius, @set_temperature_celsius, @power_state, @mode, @fan_speed)
ON CONFLICT (device_id) DO UPDATE SET
collected_at = EXCLUDED.collected_at,
is_online = EXCLUDED.is_online,
room_temperature_celsius = EXCLUDED.room_temperature_celsius,
set_temperature_celsius = EXCLUDED.set_temperature_celsius,
power_state = EXCLUDED.power_state,
mode = EXCLUDED.mode,
fan_speed = EXCLUDED.fan_speed,
updated_at = CURRENT_TIMESTAMP
WHERE thermostat_device_state.collected_at <= EXCLUDED.collected_at;";
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillThermostatParameters(command, deviceId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void FillThermostatParameters(NpgsqlCommand command, long deviceId, ThermostatTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@device_id", deviceId);
            command.Parameters.AddWithValue("@collected_at", telemetry.CollectedAt.ToUniversalTime());
            command.Parameters.AddWithValue("@is_online", telemetry.IsOnline);
            command.Parameters.AddWithValue("@room_temperature_celsius", telemetry.RoomTemperatureCelsius.HasValue ? (object)telemetry.RoomTemperatureCelsius.Value : DBNull.Value);
            command.Parameters.AddWithValue("@set_temperature_celsius", telemetry.SetTemperatureCelsius.HasValue ? (object)telemetry.SetTemperatureCelsius.Value : DBNull.Value);
            command.Parameters.AddWithValue("@power_state", string.IsNullOrWhiteSpace(telemetry.PowerState) ? (object)DBNull.Value : telemetry.PowerState);
            command.Parameters.AddWithValue("@mode", string.IsNullOrWhiteSpace(telemetry.Mode) ? (object)DBNull.Value : telemetry.Mode);
            command.Parameters.AddWithValue("@fan_speed", string.IsNullOrWhiteSpace(telemetry.FanSpeed) ? (object)DBNull.Value : telemetry.FanSpeed);
        }

        private static void ValidateHeatPumpTelemetry(string topic, HeatPumpTelemetryMessage telemetry)
        {
            if (telemetry == null)
            {
                throw new InvalidOperationException("热泵遥测消息为空或 JSON 反序列化失败。");
            }

            if (!string.Equals(telemetry.MessageType, "heatpump.telemetry.v1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("热泵消息类型无效。");
            }

            if (telemetry.MessageId == Guid.Empty)
            {
                throw new InvalidOperationException("热泵消息缺少 MessageId。");
            }

            if (string.IsNullOrWhiteSpace(telemetry.SiteCode) || telemetry.ControllerSlaveId <= 0 || telemetry.ModuleIndex < 0)
            {
                throw new InvalidOperationException("热泵消息缺少有效站点、控制器或模块身份。");
            }

            if (telemetry.CollectedAt == default(DateTimeOffset))
            {
                throw new InvalidOperationException("热泵消息缺少采集时间。");
            }

            var segments = topic == null ? new string[0] : topic.Split('/');
            if (segments.Length != 5
                || !string.Equals(segments[0], "tpem", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[2], "heatpump", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[4], "telemetry", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(segments[1], SanitizeTopicSegment(telemetry.SiteCode), StringComparison.Ordinal)
                || !int.TryParse(segments[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var controllerSlaveId)
                || controllerSlaveId != telemetry.ControllerSlaveId)
            {
                throw new InvalidOperationException("热泵主题与消息身份不一致。");
            }
        }

        private static bool TryInsertHeatPumpMessageLog(NpgsqlConnection connection, NpgsqlTransaction transaction, string topic, Guid messageId)
        {
            const string sql = @"
INSERT INTO heat_pump_message_log (topic, message_id)
VALUES (@topic, @message_id)
ON CONFLICT (topic, message_id) DO NOTHING;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@topic", topic);
                command.Parameters.AddWithValue("@message_id", messageId);
                return command.ExecuteNonQuery() == 1;
            }
        }

        private static long UpsertHeatPumpDevice(NpgsqlConnection connection, NpgsqlTransaction transaction, HeatPumpTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO heat_pump_device (site_code, controller_address, controller_name)
VALUES (@site_code, @controller_address, @controller_name)
ON CONFLICT (site_code, controller_address) DO UPDATE SET
controller_name = EXCLUDED.controller_name,
updated_at = CURRENT_TIMESTAMP
RETURNING id;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_code", telemetry.SiteCode.Trim());
                command.Parameters.AddWithValue("@controller_address", telemetry.ControllerSlaveId);
                command.Parameters.AddWithValue("@controller_name", string.IsNullOrWhiteSpace(telemetry.GroupName) ? (object)DBNull.Value : telemetry.GroupName.Trim());
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void InsertHeatPumpTelemetryHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, HeatPumpTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO heat_pump_telemetry_history
(device_id, module_index, collected_at, message_id, module_name, enabled, state_code, run_mode,
 target_temperature, water_in_temperature, water_out_temperature, ambient_temperature, fault_code, protection_code)
VALUES
(@device_id, @module_index, @collected_at, @message_id, @module_name, @enabled, @state_code, @run_mode,
 @target_temperature, @water_in_temperature, @water_out_temperature, @ambient_temperature, @fault_code, @protection_code);";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillHeatPumpTelemetryParameters(command, deviceId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertHeatPumpDeviceState(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, HeatPumpTelemetryMessage telemetry)
        {
            const string sql = @"
INSERT INTO heat_pump_device_state
(device_id, module_index, module_name, last_collect_time, last_message_id, is_online, state_code, run_mode,
 target_temperature, water_in_temperature, water_out_temperature, ambient_temperature, fault_code, protection_code)
VALUES
(@device_id, @module_index, @module_name, @collected_at, @message_id, TRUE, @state_code, @run_mode,
 @target_temperature, @water_in_temperature, @water_out_temperature, @ambient_temperature, @fault_code, @protection_code)
ON CONFLICT (device_id, module_index) DO UPDATE SET
module_name = EXCLUDED.module_name,
last_collect_time = EXCLUDED.last_collect_time,
last_message_id = EXCLUDED.last_message_id,
is_online = TRUE,
state_code = EXCLUDED.state_code,
run_mode = EXCLUDED.run_mode,
target_temperature = EXCLUDED.target_temperature,
water_in_temperature = EXCLUDED.water_in_temperature,
water_out_temperature = EXCLUDED.water_out_temperature,
ambient_temperature = EXCLUDED.ambient_temperature,
fault_code = EXCLUDED.fault_code,
protection_code = EXCLUDED.protection_code,
updated_at = CURRENT_TIMESTAMP
WHERE heat_pump_device_state.last_collect_time <= EXCLUDED.last_collect_time;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                FillHeatPumpTelemetryParameters(command, deviceId, telemetry);
                command.ExecuteNonQuery();
            }
        }

        private static void FillHeatPumpTelemetryParameters(NpgsqlCommand command, long deviceId, HeatPumpTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@device_id", deviceId);
            command.Parameters.AddWithValue("@module_index", telemetry.ModuleIndex);
            command.Parameters.AddWithValue("@collected_at", telemetry.CollectedAt.ToUniversalTime());
            command.Parameters.AddWithValue("@message_id", telemetry.MessageId);
            command.Parameters.AddWithValue("@module_name", string.IsNullOrWhiteSpace(telemetry.ModuleName) ? (object)DBNull.Value : telemetry.ModuleName.Trim());
            command.Parameters.AddWithValue("@enabled", telemetry.IsEnabled);
            command.Parameters.AddWithValue("@state_code", telemetry.Status.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@run_mode", telemetry.RunMode);
            command.Parameters.AddWithValue("@target_temperature", telemetry.TargetTemperature);
            command.Parameters.AddWithValue("@water_in_temperature", telemetry.ReturnWaterTemperature);
            command.Parameters.AddWithValue("@water_out_temperature", telemetry.OutletWaterTemperature);
            command.Parameters.AddWithValue("@ambient_temperature", telemetry.AmbientTemperature);
            command.Parameters.AddWithValue("@fault_code", ParseHeatPumpCode(telemetry.FaultCode));
            command.Parameters.AddWithValue("@protection_code", 0);
        }

        private static int ParseHeatPumpCode(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) && code >= 0 ? code : 0;
        }

        private static void UpdateHeatPumpAlarmState(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, HeatPumpTelemetryMessage telemetry)
        {
            var faultCode = ParseHeatPumpCode(telemetry.FaultCode);
            if (faultCode > 0)
            {
                UpsertHeatPumpAlarm(connection, transaction, deviceId, telemetry.ModuleIndex, "fault", faultCode, telemetry.CollectedAt, "故障代码: " + faultCode.ToString(CultureInfo.InvariantCulture));
                return;
            }

            const string sql = @"
UPDATE heat_pump_alarm_event
SET recovered_at = @recovered_at,
    updated_at = CURRENT_TIMESTAMP
WHERE device_id = @device_id
  AND module_index = @module_index
  AND alarm_type = 'fault'
  AND recovered_at IS NULL;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@recovered_at", telemetry.CollectedAt.ToUniversalTime());
                command.Parameters.AddWithValue("@device_id", deviceId);
                command.Parameters.AddWithValue("@module_index", telemetry.ModuleIndex);
                command.ExecuteNonQuery();
            }
        }

        private static void RecoverHeatPumpOfflineAlarm(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, int moduleIndex, DateTimeOffset collectedAt)
        {
            const string sql = @"
UPDATE heat_pump_alarm_event
SET recovered_at = @recovered_at,
    updated_at = CURRENT_TIMESTAMP
WHERE device_id = @device_id
  AND module_index = @module_index
  AND alarm_type = 'offline'
  AND recovered_at IS NULL;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@recovered_at", collectedAt.ToUniversalTime());
                command.Parameters.AddWithValue("@device_id", deviceId);
                command.Parameters.AddWithValue("@module_index", moduleIndex);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertHeatPumpAlarm(NpgsqlConnection connection, NpgsqlTransaction transaction, long deviceId, int moduleIndex, string alarmType, int alarmCode, DateTimeOffset occurredAt, string message)
        {
            const string sql = @"
INSERT INTO heat_pump_alarm_event
(device_id, module_index, alarm_type, alarm_code, opened_at, last_seen_at, message)
VALUES
(@device_id, @module_index, @alarm_type, @alarm_code, @occurred_at, @occurred_at, @message)
ON CONFLICT (device_id, module_index, alarm_type, alarm_code) WHERE recovered_at IS NULL DO UPDATE SET
last_seen_at = EXCLUDED.last_seen_at,
message = EXCLUDED.message,
updated_at = CURRENT_TIMESTAMP;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@device_id", deviceId);
                command.Parameters.AddWithValue("@module_index", moduleIndex);
                command.Parameters.AddWithValue("@alarm_type", alarmType);
                command.Parameters.AddWithValue("@alarm_code", alarmCode);
                command.Parameters.AddWithValue("@occurred_at", occurredAt.ToUniversalTime());
                command.Parameters.AddWithValue("@message", message);
                command.ExecuteNonQuery();
            }
        }

        private static void HandleRegistryEnvelope(MqttEnvelope envelope)
        {
            var registry = JsonConvert.DeserializeObject<MeterRegistryMessage>(envelope.PayloadJson, JsonSettings);
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
            catch (Exception ex)
            {
                // 这里原先是个空 catch：数据库不可用时更新失败没人知道，
                // mqtt_message_log 那一行会永远停在 pending，排障时状态自相矛盾
                //（记录说"处理中"，实际早就失败了）。至少留下一行日志。
                AppLogger.Warn(
                    "Ingest",
                    "标记消息处理失败时自身失败（id=" + logId + "），该行可能仍停在 pending 状态。",
                    ex);
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
                // 空名称回退成 box_code，再按"同一站点内不重名"化解
                command.Parameters.AddWithValue("@box_name", ResolveBoxName(
                    connection,
                    transaction,
                    siteId,
                    boxCode,
                    string.IsNullOrWhiteSpace(boxName) ? boxCode : boxName));
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
(site_id, box_id, meter_code, meter_name, device_type, slave_address, location, is_enabled, is_toolbar, mqtt_topic, last_seen_time)
VALUES
(@site_id, @box_id, @meter_code, @meter_name, @device_type, @slave_address, @location, TRUE, @is_toolbar, @mqtt_topic, @last_seen_time)
ON CONFLICT (site_id, meter_code) DO UPDATE SET
box_id = EXCLUDED.box_id,
meter_name = EXCLUDED.meter_name,
device_type = EXCLUDED.device_type,
slave_address = EXCLUDED.slave_address,
location = EXCLUDED.location,
is_enabled = TRUE,
is_toolbar = EXCLUDED.is_toolbar,
mqtt_topic = EXCLUDED.mqtt_topic,
last_seen_time = EXCLUDED.last_seen_time,
updated_at = CURRENT_TIMESTAMP;";

            // 同一配电箱内电表名不能重复，落库前先化解
            string meterName = ResolveMeterName(
                connection,
                transaction,
                siteId,
                boxId,
                item.MeterCode,
                string.IsNullOrWhiteSpace(item.MeterName) ? item.MeterCode : item.MeterName);

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@box_id", boxId);
                command.Parameters.AddWithValue("@meter_code", item.MeterCode);
                command.Parameters.AddWithValue("@meter_name", meterName);
                command.Parameters.AddWithValue("@device_type", (object)(item.DeviceModel ?? "Legacy"));
                command.Parameters.AddWithValue("@slave_address", (int)item.SlaveAddress);
                command.Parameters.AddWithValue("@location", (object)(item.Location ?? registry.BoxName ?? string.Empty));
                command.Parameters.AddWithValue("@is_toolbar", item.IsToolbar);
                command.Parameters.AddWithValue("@mqtt_topic", BuildMeterTopic(registry.SiteCode, registry.BoxCode, item.MeterCode));
                command.Parameters.AddWithValue("@last_seen_time", registry.ScanTime.ToUniversalTime());
                command.ExecuteNonQuery();
            }

            return GetMeterId(connection, transaction, item.MeterCode, registry.SiteCode);
        }

        /// <summary>
        /// 同一配电箱内电表名不允许重复 —— 一个箱里两台同名电表，拓扑图和列表里都分不清是哪一台。
        /// 这里只做软处理，绝不阻断注册：注册不上那一台，它的遥测会因为找不到 meter 行而落不了库，比重名更糟。
        ///   原地改名且名字被占  → 拒绝这次改名，保留库里原来的名字；
        ///   新装 / 换箱且名字被占 → 换一个不冲突的名字（换箱时"保留原名"会在目标箱里造出重名，所以也只能改名）。
        /// 两种情况都记 Warn，方便有人去把上报配置里的重名改掉。
        /// </summary>
        private static string ResolveMeterName(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, long boxId, string meterCode, string wantedName)
        {
            const string sql = @"
SELECT meter_code, meter_name, box_id
FROM meter
WHERE site_id = @site_id AND (meter_code = @meter_code OR box_id = @box_id);";

            var rows = new List<KeyValuePair<string, KeyValuePair<string, long>>>();
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                command.Parameters.AddWithValue("@meter_code", meterCode);
                command.Parameters.AddWithValue("@box_id", boxId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new KeyValuePair<string, KeyValuePair<string, long>>(
                            reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                            new KeyValuePair<string, long>(
                                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                reader.IsDBNull(2) ? 0L : reader.GetInt64(2))));
                    }
                }
            }

            var clash = rows.FirstOrDefault(r => !string.Equals(r.Key, meterCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.Value.Key, wantedName, StringComparison.Ordinal));
            if (clash.Key == null)
            {
                return wantedName;
            }

            var own = rows.FirstOrDefault(r => string.Equals(r.Key, meterCode, StringComparison.OrdinalIgnoreCase));
            bool renamingInPlace = own.Key != null && own.Value.Value == boxId && !string.IsNullOrEmpty(own.Value.Key);
            if (renamingInPlace)
            {
                AppLogger.Warn("Registry", "本配电箱内已有同名电表「" + wantedName + "」(设备 " + clash.Key + ")，拒绝把 " + meterCode
                    + " 改成这个名字，保留原名「" + own.Value.Key + "」。请修正上报配置里的名称。");
                return own.Value.Key;
            }

            string unique = MakeUniqueName(wantedName, rows.Select(r => r.Value.Key));
            AppLogger.Warn("Registry", "本配电箱内已有同名电表「" + wantedName + "」(设备 " + clash.Key + ")，设备 " + meterCode
                + " 改用「" + unique + "」以免重名。请修正上报配置里的名称。");
            return unique;
        }

        /// <summary>
        /// 同一站点内配电箱名不允许重复（box_code 早有 uk_site_box，名称这一维之前没人管）。
        /// 箱体的"容器"是站点且不会变，所以只有原地改名一种情况：名字被占就保留原容器名，新箱体则换一个不冲突的名字。
        /// </summary>
        private static string ResolveBoxName(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, string boxCode, string wantedName)
        {
            const string sql = @"
SELECT box_code, box_name
FROM distribution_box
WHERE site_id = @site_id;";

            var rows = new List<KeyValuePair<string, string>>();
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@site_id", siteId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new KeyValuePair<string, string>(
                            reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                            reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
                    }
                }
            }

            var clash = rows.FirstOrDefault(r => !string.Equals(r.Key, boxCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.Value, wantedName, StringComparison.Ordinal));
            if (clash.Key == null)
            {
                return wantedName;
            }

            var own = rows.FirstOrDefault(r => string.Equals(r.Key, boxCode, StringComparison.OrdinalIgnoreCase));
            if (own.Key != null && !string.IsNullOrEmpty(own.Value))
            {
                AppLogger.Warn("Registry", "本站点内已有同名配电箱「" + wantedName + "」(箱体 " + clash.Key + ")，拒绝把 " + boxCode
                    + " 改成这个名字，保留原名「" + own.Value + "」。请修正上报配置里的名称。");
                return own.Value;
            }

            string unique = MakeUniqueName(wantedName, rows.Select(r => r.Value));
            AppLogger.Warn("Registry", "本站点内已有同名配电箱「" + wantedName + "」(箱体 " + clash.Key + ")，箱体 " + boxCode
                + " 改用「" + unique + "」以免重名。请修正上报配置里的名称。");
            return unique;
        }

        /// <summary>依次试「名字(2)」「名字(3)」……取第一个没被占用的。</summary>
        private static string MakeUniqueName(string wanted, IEnumerable<string> used)
        {
            var taken = new HashSet<string>(used ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            for (int index = 2; index <= 99; index++)
            {
                string candidate = wanted + "(" + index + ")";
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            return wanted + "(" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
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
                command.Parameters.AddWithValue("@last_seen_time", registry.ScanTime.ToUniversalTime());
                command.Parameters.AddWithValue("@id", meterId);
                command.ExecuteNonQuery();
            }

            UpsertMeterStatus(connection, transaction, meterId, registry.ScanTime, null, null, null, "disabled", false);
        }

        private static void MarkMissingMetersOffline(NpgsqlConnection connection, NpgsqlTransaction transaction, long siteId, long boxId, HashSet<long> activeMeterIds, DateTimeOffset scanTime)
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
            if (telemetry.RealTime == null)
            {
                return;
            }

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
updated_at = CURRENT_TIMESTAMP
-- P1-3 乱序保护：迟到的消息不得把更新的值覆盖回旧值。
-- 热泵与温控器链路早就有这个守卫，电表链路此前缺失，ProcessWorkers>1 时尤其容易命中。
WHERE meter_realtime_latest.collect_time <= EXCLUDED.collect_time;";

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
updated_at = CURRENT_TIMESTAMP
-- P1-3 乱序保护：迟到的消息不得把更新的值覆盖回旧值。
-- 热泵与温控器链路早就有这个守卫，电表链路此前缺失，ProcessWorkers>1 时尤其容易命中。
WHERE meter_energy_latest.collect_time <= EXCLUDED.collect_time;";

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
updated_at = CURRENT_TIMESTAMP
-- P1-3 乱序保护：迟到的消息不得把更新的值覆盖回旧值。
-- 热泵与温控器链路早就有这个守卫，电表链路此前缺失，ProcessWorkers>1 时尤其容易命中。
WHERE meter_power_quality_latest.collect_time <= EXCLUDED.collect_time;";

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

        private static bool ShouldWriteRealtimeHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            const string sql = @"
SELECT collect_time, voltage_a, voltage_b, voltage_c, current_a, current_b, current_c,
       active_power_total, power_factor_total, frequency
FROM meter_realtime_history
WHERE meter_id = @meter_id
ORDER BY collect_time DESC
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return true;
                    }

                    var lastCollectTime = ReadDateTimeOffset(reader, 0);
                    if (telemetry.CollectTime - lastCollectTime >= RealtimeHistoryInterval)
                    {
                        return true;
                    }

                    var lastRealtime = new RealTimeData
                    {
                        VoltageA = ReadFloat(reader, 1),
                        VoltageB = ReadFloat(reader, 2),
                        VoltageC = ReadFloat(reader, 3),
                        CurrentA = ReadFloat(reader, 4),
                        CurrentB = ReadFloat(reader, 5),
                        CurrentC = ReadFloat(reader, 6),
                        ActivePowerTotal = ReadFloat(reader, 7),
                        PowerFactorTotal = ReadFloat(reader, 8),
                        Frequency = ReadFloat(reader, 9)
                    };

                    return HasRealtimeSignificantChange(lastRealtime, telemetry.RealTime);
                }
            }
        }

        private static bool ShouldWriteEnergyHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            const string sql = @"
SELECT collect_time
FROM meter_energy_history
WHERE meter_id = @meter_id
ORDER BY collect_time DESC
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return true;
                }

                var lastCollectTime = ToDateTimeOffset(result);
                return telemetry.CollectTime - lastCollectTime >= EnergyHistoryInterval;
            }
        }

        private static bool ShouldWriteQualityHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry)
        {
            const string sql = @"
SELECT collect_time
FROM meter_power_quality_history
WHERE meter_id = @meter_id
ORDER BY collect_time DESC
LIMIT 1;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return true;
                }

                var lastCollectTime = ToDateTimeOffset(result);
                return telemetry.CollectTime - lastCollectTime >= QualityHistoryInterval;
            }
        }

        /// <summary>
        /// 报警评估（P1-1 + P1-6）。
        ///
        /// 改动要点：
        ///   1. 阈值不再来自 App.config，而是走 meter_threshold 表 + 共用判定引擎，
        ///      与上位机界面、网页端用同一套口径；
        ///   2. 只有连续命中 debounce_count 次才置位，避免单次抖动产生一条告警；
        ///   3. 恢复要满足滞回条件（低限需回升到 min+margin，高限需回落到 max-margin），
        ///      避免在阈值附近反复置位/恢复；
        ///   4. 未带电时跳过所有 require_energized 的判定项，停电不再被误报成
        ///      "电压过低 + 功率因数过低"。
        ///
        /// 改动前实测：现网 259 条 power_factor_low 全部来自同一台表，报警值密集分布在
        /// 0.499x（阈值 0.5 附近），其中 238 条持续时间不足 5 秒 —— 典型的缺去抖与滞回。
        /// </summary>
        private static void EvaluateRealtimeAlarms(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            MeterTelemetryMessage telemetry,
            ThresholdSet thresholds)
        {
            var sample = BuildSampleView(telemetry);
            if (sample == null)
            {
                return;
            }

            var hits = MeterQualityEvaluator.Evaluate(thresholds, sample);
            var hitByCode = new Dictionary<string, ThresholdHit>(StringComparer.OrdinalIgnoreCase);
            foreach (var hit in hits)
            {
                if (hit.IsAlarmEnabled)
                {
                    hitByCode[hit.AlarmCode] = hit;
                }
            }

            // 对"本次命中"的项累计去抖计数，达到门槛才置位。
            foreach (var pair in hitByCode)
            {
                var hit = pair.Value;
                var stateKey = meterId + "|" + hit.AlarmCode;
                var count = AlarmDebounce.AddOrUpdate(stateKey, 1, (k, v) => v + 1);
                if (count < hit.DebounceCount)
                {
                    AppLogger.Debug(
                        "Alarm",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "表 {0} 的 {1} 命中 {2}/{3} 次，未达去抖门槛，暂不置位（当前值 {4}）。",
                            meterId, hit.AlarmCode, count, hit.DebounceCount, hit.Value));
                    continue;
                }

                SetAlarmActive(connection, transaction, meterId, hit, telemetry.CollectTime);
            }

            // 去抖的口径是"**连续**命中 debounce_count 次"，所以本轮未命中的项必须清零。
            // 原实现只在"已激活且越过滞回带"时移除计数，未命中且当前无告警时既不归零也不递减，
            // 计数器的实际语义变成"任意时间跨度内命中的总次数" —— 阈值附近每 10 分钟蹭一次越限，
            // 50 分钟后照样置位一条告警，P1-6 想消除的误报只被削弱、没有消除。
            ResetDebounceCountersOfUnhitItems(meterId, thresholds, hitByCode);

            // 对"本次未命中"的已激活告警，检查是否满足滞回恢复条件。
            ResolveRecoveredAlarms(connection, transaction, meterId, thresholds, sample, hitByCode, telemetry.CollectTime);
        }

        /// <summary>
        /// 把本表"本轮未命中"的去抖计数清零。
        /// 只遍历阈值表里已绑定的告警代码（十来个），而不是遍历整个字典 ——
        /// 后者要按"所有表 × 所有告警码"扫描，表一多就成了每条消息都要付的开销。
        /// </summary>
        private static void ResetDebounceCountersOfUnhitItems(
            long meterId,
            ThresholdSet thresholds,
            Dictionary<string, ThresholdHit> hitByCode)
        {
            foreach (var binding in MeterQualityEvaluator.EnumerateAlarmCodes(thresholds, MeterQualityEvaluator.AllMetricCodes))
            {
                if (hitByCode.ContainsKey(binding.AlarmCode))
                {
                    continue;
                }

                int removed;
                AlarmDebounce.TryRemove(meterId + "|" + binding.AlarmCode, out removed);
            }
        }

        /// <summary>
        /// 把入库服务的 DTO 映射成判定引擎的输入视图。与上位机 MainForm.BuildSampleView 对应，
        /// 两端喂给引擎的是同一种形状，这是"判定口径唯一"的前提。
        /// </summary>
        private static MeterSampleView BuildSampleView(MeterTelemetryMessage telemetry)
        {
            if (telemetry == null || (telemetry.RealTime == null && telemetry.Quality == null))
            {
                return null;
            }

            var view = new MeterSampleView();
            var rt = telemetry.RealTime;
            if (rt != null)
            {
                view.VoltageA = rt.VoltageA;
                view.VoltageB = rt.VoltageB;
                view.VoltageC = rt.VoltageC;
                view.CurrentA = rt.CurrentA;
                view.CurrentB = rt.CurrentB;
                view.CurrentC = rt.CurrentC;
                view.PowerFactorTotal = rt.PowerFactorTotal;
                view.Frequency = rt.Frequency;
            }

            var q = telemetry.Quality;
            if (q != null)
            {
                view.VoltageThdA = q.VoltageTHDA;
                view.VoltageThdB = q.VoltageTHDB;
                view.VoltageThdC = q.VoltageTHDC;
                view.CurrentThdA = q.CurrentTHDA;
                view.CurrentThdB = q.CurrentTHDB;
                view.CurrentThdC = q.CurrentTHDC;
                view.VoltageUnbalance = q.VoltageUnbalance;
                view.CurrentUnbalance = q.CurrentUnbalance;
            }

            return view;
        }

        /// <summary>
        /// 置位或刷新一条告警。依赖迁移 003 建立的部分唯一索引
        /// uq_alarm_event_active (meter_id, alarm_code) WHERE status='active'，
        /// 因此一条 INSERT ... ON CONFLICT 就能完成，且并发下不会插出重复的 active 告警
        /// （原实现是"先 SELECT 再 INSERT/UPDATE"，ProcessWorkers&gt;1 时存在竞态）。
        /// </summary>
        private static void SetAlarmActive(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            ThresholdHit hit,
            DateTimeOffset collectTime)
        {
            const string sql = @"
INSERT INTO alarm_event
(meter_id, alarm_code, alarm_name, alarm_level, alarm_value, threshold_value, status, start_time)
VALUES
(@meter_id, @alarm_code, @alarm_name, @alarm_level, @alarm_value, @threshold_value, 'active', @start_time)
ON CONFLICT (meter_id, alarm_code) WHERE status = 'active' DO UPDATE SET
alarm_name = EXCLUDED.alarm_name,
alarm_level = EXCLUDED.alarm_level,
alarm_value = EXCLUDED.alarm_value,
threshold_value = EXCLUDED.threshold_value,
updated_at = CURRENT_TIMESTAMP
RETURNING (xmax = 0) AS inserted;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@alarm_code", hit.AlarmCode);
                command.Parameters.AddWithValue("@alarm_name", hit.AlarmName ?? hit.AlarmCode);
                command.Parameters.AddWithValue("@alarm_level", hit.AlarmLevel ?? "warning");
                command.Parameters.AddWithValue("@alarm_value", Convert.ToDecimal(hit.Value, CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@threshold_value", Convert.ToDecimal(hit.Threshold, CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@start_time", collectTime.ToUniversalTime());

                var inserted = command.ExecuteScalar();
                if (inserted is bool && (bool)inserted)
                {
                    AppLogger.Info(
                        "Alarm",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "置位告警 表{0} {1}（{2}），当前值 {3}，阈值 {4}，级别 {5}。",
                            meterId, hit.AlarmCode, hit.AlarmName, hit.Value, hit.Threshold, hit.AlarmLevel));
                }
            }
        }

        /// <summary>
        /// 对本次未命中的已激活告警做滞回恢复判定（P1-6）。
        /// 只处理属于当前阈值集合的告警代码 —— offline 之类由离线监控负责，
        /// 阈值表里已不存在的历史代码保持不动，避免静默改写历史。
        /// </summary>
        private static void ResolveRecoveredAlarms(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            ThresholdSet thresholds,
            MeterSampleView sample,
            Dictionary<string, ThresholdHit> currentHits,
            DateTimeOffset collectTime)
        {
            var bindings = MeterQualityEvaluator.EnumerateAlarmCodes(thresholds, MeterQualityEvaluator.AllMetricCodes);
            if (bindings.Count == 0)
            {
                return;
            }

            var activeCodes = LoadActiveAlarmCodes(connection, transaction, meterId);
            if (activeCodes.Count == 0)
            {
                return;
            }

            foreach (var binding in bindings)
            {
                if (!activeCodes.Contains(binding.AlarmCode) || currentHits.ContainsKey(binding.AlarmCode))
                {
                    continue;
                }

                var observed = MeterQualityEvaluator.GetObservedExtreme(sample, binding.Rule.MetricCode, binding.IsHigh);
                if (!MeterQualityEvaluator.IsRecovered(binding.IsHigh, binding.Threshold, binding.Rule.RecoverMargin, observed))
                {
                    // 已经回到阈值内但还没越过滞回带，保持激活，避免反复置位/恢复。
                    continue;
                }

                ClearAlarm(connection, transaction, meterId, binding.AlarmCode, collectTime, observed);
                int removed;
                AlarmDebounce.TryRemove(meterId + "|" + binding.AlarmCode, out removed);
            }
        }

        private static HashSet<string> LoadActiveAlarmCodes(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId)
        {
            const string sql = "SELECT alarm_code FROM alarm_event WHERE meter_id = @meter_id AND status = 'active';";
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        codes.Add(reader.GetString(0));
                    }
                }
            }

            return codes;
        }

        private static void ClearAlarm(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            string alarmCode,
            DateTimeOffset endTime,
            float? recoveredValue)
        {
            const string sql = @"
UPDATE alarm_event
SET status = 'cleared',
    end_time = @end_time,
    duration_seconds = GREATEST(0, CAST(EXTRACT(EPOCH FROM (@end_time - start_time)) AS bigint)),
    updated_at = CURRENT_TIMESTAMP
WHERE meter_id = @meter_id
  AND alarm_code = @alarm_code
  AND status = 'active';";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@end_time", endTime.ToUniversalTime());
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@alarm_code", alarmCode);
                if (command.ExecuteNonQuery() > 0)
                {
                    AppLogger.Info(
                        "Alarm",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "恢复告警 表{0} {1}，恢复值 {2}。",
                            meterId, alarmCode, recoveredValue.HasValue ? recoveredValue.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-"));
                }
            }
        }

        private static bool HasRealtimeSignificantChange(RealTimeData previous, RealTimeData current)
        {
            if (previous == null || current == null)
            {
                return true;
            }

            return HasAbsoluteOrRelativeChange(previous.VoltageA, current.VoltageA, VoltageAbsoluteChangeThreshold, VoltageRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.VoltageB, current.VoltageB, VoltageAbsoluteChangeThreshold, VoltageRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.VoltageC, current.VoltageC, VoltageAbsoluteChangeThreshold, VoltageRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.CurrentA, current.CurrentA, CurrentAbsoluteChangeThreshold, CurrentRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.CurrentB, current.CurrentB, CurrentAbsoluteChangeThreshold, CurrentRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.CurrentC, current.CurrentC, CurrentAbsoluteChangeThreshold, CurrentRelativeChangeThreshold)
                || HasAbsoluteOrRelativeChange(previous.ActivePowerTotal, current.ActivePowerTotal, PowerAbsoluteChangeThreshold, PowerRelativeChangeThreshold)
                || HasAbsoluteChange(previous.PowerFactorTotal, current.PowerFactorTotal, PowerFactorChangeThreshold)
                || HasAbsoluteChange(previous.Frequency, current.Frequency, FrequencyChangeThreshold);
        }

        /// <summary>
        /// P1-5：可空之后，"有值 → 无值"和"无值 → 有值"都算显著变化（该写一条历史，
        /// 否则数据中断这件事在历史表里看不出来）；两边都无值则不算变化。
        /// </summary>
        private static bool HasAbsoluteChange(float? previous, float? current, float threshold)
        {
            if (!previous.HasValue && !current.HasValue)
            {
                return false;
            }

            if (!previous.HasValue || !current.HasValue)
            {
                return true;
            }

            return Math.Abs(previous.Value - current.Value) >= threshold;
        }

        private static bool HasAbsoluteOrRelativeChange(float? previous, float? current, float absoluteThreshold, float relativeThreshold)
        {
            // P1-5：有值/无值之间的切换本身就是显著变化，必须留一条历史。
            if (!previous.HasValue && !current.HasValue)
            {
                return false;
            }

            if (!previous.HasValue || !current.HasValue)
            {
                return true;
            }

            var absoluteDelta = Math.Abs(current.Value - previous.Value);
            if (absoluteDelta >= absoluteThreshold)
            {
                return true;
            }

            var baseline = Math.Max(Math.Abs(previous.Value), 0.0001f);
            return absoluteDelta / baseline >= relativeThreshold;
        }

        /// <summary>
        /// P1-5：NULL 不再被读成 0f。数据库里的 NULL 表示"当时没采到"，
        /// 读成 0 会让变化检测与报警判定把缺数据当成真实的 0 值。
        /// </summary>
        private static float? ReadFloat(NpgsqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (float?)null : Convert.ToSingle(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTimeOffset ReadDateTimeOffset(NpgsqlDataReader reader, int ordinal)
        {
            return ToDateTimeOffset(reader.GetValue(ordinal));
        }

        private static DateTimeOffset ToDateTimeOffset(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                throw new InvalidOperationException("数据库时间字段为空，无法转换。");
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset;
            }

            if (value is DateTime dateTime)
            {
                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                }

                return new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero);
            }

            if (value is string text && !string.IsNullOrWhiteSpace(text))
            {
                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    return parsed;
                }
            }

            throw new InvalidCastException("无法将数据库值转换为 DateTimeOffset，实际类型: " + value.GetType().FullName);
        }

        private static async Task MonitorOfflineMetersAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_mysqlConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                        // P1-2：电表离线不再只改 meter_status，而是同时产生一条 offline 告警事件。
                        // 这是网页端"报警记录"此前只能靠当前状态临时拼凑、拿不到真实历史事件的根因：
                        // 后端从来没有为电表离线写过 alarm_event（热泵那条链路一直是写的）。
                        const string sql = @"
WITH newly_offline AS (
    UPDATE meter_status
    SET is_online = FALSE,
        status_code = 'offline',
        updated_at = CURRENT_TIMESTAMP
    WHERE is_online = TRUE
      AND last_collect_time IS NOT NULL
      AND last_collect_time < @deadline
    RETURNING meter_id, last_collect_time
)
INSERT INTO alarm_event
(meter_id, alarm_code, alarm_name, alarm_level, alarm_value, threshold_value, status, start_time)
SELECT meter_id, 'offline', '设备离线', 'critical', NULL, @timeout_seconds, 'active', COALESCE(last_collect_time, now())
FROM newly_offline
ON CONFLICT (meter_id, alarm_code) WHERE status = 'active' DO UPDATE SET
updated_at = CURRENT_TIMESTAMP
RETURNING meter_id;";

                        using (var command = new NpgsqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@deadline", GetBeijingNow().UtcDateTime - OfflineTimeout);
                            command.Parameters.AddWithValue("@timeout_seconds", Convert.ToDecimal(OfflineTimeout.TotalSeconds, CultureInfo.InvariantCulture));
                            using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                            {
                                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                {
                                    AppLogger.Warn(
                                        "OfflineMonitor",
                                        string.Format(
                                            CultureInfo.InvariantCulture,
                                            "表 {0} 超过 {1} 秒没有新采集时间，已标记离线并置位 offline 告警。",
                                            reader.GetInt64(0), OfflineTimeout.TotalSeconds));
                                }
                            }
                        }

                        const string heatPumpSql = @"
WITH newly_offline AS (
    UPDATE heat_pump_device_state
    SET is_online = FALSE,
        updated_at = CURRENT_TIMESTAMP
    WHERE is_online = TRUE
      AND last_collect_time < @deadline
    RETURNING device_id, module_index
)
INSERT INTO heat_pump_alarm_event
(device_id, module_index, alarm_type, alarm_code, opened_at, last_seen_at, message)
SELECT device_id, module_index, 'offline', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, '模块遥测超时'
FROM newly_offline
ON CONFLICT (device_id, module_index, alarm_type, alarm_code) WHERE recovered_at IS NULL DO UPDATE SET
last_seen_at = EXCLUDED.last_seen_at,
updated_at = CURRENT_TIMESTAMP;";

                        using (var command = new NpgsqlCommand(heatPumpSql, connection))
                        {
                            command.Parameters.AddWithValue("@deadline", GetBeijingNow().UtcDateTime - HeatPumpOfflineTimeout);
                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AppLogger.Error("OfflineMonitor", "离线监控轮次失败。", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }

        private static void UpsertMeterStatus(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, MeterTelemetryMessage telemetry, string errorMessage)
        {
            bool hasError = !string.IsNullOrWhiteSpace(errorMessage);
            DateTimeOffset? collectTime = telemetry?.CollectTime;
            UpsertMeterStatus(
                connection,
                transaction,
                meterId,
                collectTime,
                hasError ? (DateTimeOffset?)null : new DateTimeOffset(GetBeijingNow().UtcDateTime),
                hasError ? (DateTimeOffset?)new DateTimeOffset(GetBeijingNow().UtcDateTime) : null,
                hasError ? errorMessage : null,
                hasError ? "comm_error" : "online",
                !hasError);
        }

        private static void UpsertMeterStatus(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long meterId,
            DateTimeOffset? lastCollectTime,
            DateTimeOffset? lastPublishTime,
            DateTimeOffset? lastErrorTime,
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
last_collect_time = CASE
    WHEN EXCLUDED.last_collect_time IS NULL THEN meter_status.last_collect_time
    WHEN meter_status.last_collect_time IS NULL THEN EXCLUDED.last_collect_time
    WHEN EXCLUDED.last_collect_time >= meter_status.last_collect_time THEN EXCLUDED.last_collect_time
    ELSE meter_status.last_collect_time
END,
last_publish_time = CASE
    WHEN EXCLUDED.last_publish_time IS NULL THEN meter_status.last_publish_time
    WHEN meter_status.last_publish_time IS NULL THEN EXCLUDED.last_publish_time
    WHEN EXCLUDED.last_publish_time >= meter_status.last_publish_time THEN EXCLUDED.last_publish_time
    ELSE meter_status.last_publish_time
END,
last_error_time = COALESCE(EXCLUDED.last_error_time, meter_status.last_error_time),
last_error_message = EXCLUDED.last_error_message,
status_code = EXCLUDED.status_code,
updated_at = CASE
    WHEN meter_status.is_online IS DISTINCT FROM EXCLUDED.is_online
      OR meter_status.status_code IS DISTINCT FROM EXCLUDED.status_code
      OR meter_status.last_error_message IS DISTINCT FROM EXCLUDED.last_error_message
    THEN CURRENT_TIMESTAMP
    ELSE meter_status.updated_at
END;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@meter_id", meterId);
                command.Parameters.AddWithValue("@is_online", isOnline);
                command.Parameters.AddWithValue("@last_collect_time", lastCollectTime.HasValue ? (object)lastCollectTime.Value.ToUniversalTime() : DBNull.Value);
                command.Parameters.AddWithValue("@last_publish_time", lastPublishTime.HasValue ? (object)lastPublishTime.Value.ToUniversalTime() : DBNull.Value);
                command.Parameters.AddWithValue("@last_error_time", lastErrorTime.HasValue ? (object)lastErrorTime.Value.ToUniversalTime() : DBNull.Value);
                command.Parameters.AddWithValue("@last_error_message", string.IsNullOrWhiteSpace(lastErrorMessage) ? (object)DBNull.Value : Truncate(lastErrorMessage, 255));
                command.Parameters.AddWithValue("@status_code", statusCode ?? "offline");
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateMeterLastSeen(NpgsqlConnection connection, NpgsqlTransaction transaction, long meterId, DateTimeOffset collectTime)
        {
            // 与三张 meter_*_latest 表同样的单调守卫：迟到的旧消息不能把 last_seen_time 往回拨。
            // 没有这个守卫时，一条乱序消息会把时间戳回退，离线监控随即在 15 秒后
            // 把通讯正常的表标成离线并写一条 critical 级 alarm_event，下一条消息又恢复 —— 产生短命告警对。
            const string sql = @"
UPDATE meter
SET last_seen_time = @last_seen_time,
    updated_at = CURRENT_TIMESTAMP
WHERE id = @id
  AND (last_seen_time IS NULL OR last_seen_time <= @last_seen_time);";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@last_seen_time", collectTime.ToUniversalTime());
                command.Parameters.AddWithValue("@id", meterId);
                command.ExecuteNonQuery();
            }
        }

        private static void FillRealtimeParameters(NpgsqlCommand command, long meterId, MeterTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@meter_id", meterId);
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime.ToUniversalTime());
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
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime.ToUniversalTime());
            // P1-5：可空之后必须显式转成 DBNull，否则 null 会被 Npgsql 当成未设置值。
            command.Parameters.AddWithValue("@forward_active_energy", (object)telemetry.Energy.ForwardActiveEnergy ?? DBNull.Value);
            command.Parameters.AddWithValue("@reverse_active_energy", (object)telemetry.Energy.ReverseActiveEnergy ?? DBNull.Value);
            command.Parameters.AddWithValue("@forward_reactive_energy", (object)telemetry.Energy.ForwardReactiveEnergy ?? DBNull.Value);
            command.Parameters.AddWithValue("@reverse_reactive_energy", (object)telemetry.Energy.ReverseReactiveEnergy ?? DBNull.Value);
        }

        private static void FillQualityParameters(NpgsqlCommand command, long meterId, MeterTelemetryMessage telemetry)
        {
            command.Parameters.AddWithValue("@meter_id", meterId);
            command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime.ToUniversalTime());
            command.Parameters.AddWithValue("@current_thd_a", (object)telemetry.Quality.CurrentTHDA ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_thd_b", (object)telemetry.Quality.CurrentTHDB ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_thd_c", (object)telemetry.Quality.CurrentTHDC ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_thd_a", (object)telemetry.Quality.VoltageTHDA ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_thd_b", (object)telemetry.Quality.VoltageTHDB ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_thd_c", (object)telemetry.Quality.VoltageTHDC ?? DBNull.Value);
            command.Parameters.AddWithValue("@voltage_unbalance", (object)telemetry.Quality.VoltageUnbalance ?? DBNull.Value);
            command.Parameters.AddWithValue("@current_unbalance", (object)telemetry.Quality.CurrentUnbalance ?? DBNull.Value);
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

        private static bool IsHeatPumpTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            var segments = topic.Split('/');
            return segments.Length == 5
                && string.Equals(segments[0], "tpem", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[2], "heatpump", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[4], "telemetry", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsThermostatTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            var segments = topic.Split('/');
            return segments.Length == 5
                && string.Equals(segments[0], "tpem", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[2], "thermostat", StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[4], "telemetry", StringComparison.OrdinalIgnoreCase);
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

        private static int GetPositiveIntAppSetting(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : defaultValue;
        }

        private static float GetPositiveFloatAppSetting(string key, float defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0f ? parsed : defaultValue;
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

        private static long ToUnixMinute(DateTimeOffset time)
        {
            return time.ToUnixTimeSeconds() / 60;
        }

        private static DateTimeOffset GetBeijingNow()
        {
            return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        }

        private static DateTimeOffset ToUtcOffset(DateTime value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, TimeSpan.FromHours(8)).ToUniversalTime();
        }

        private static string TryExtractMeterCode(string payloadJson)
        {
            try
            {
                var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(payloadJson, JsonSettings);
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
                var telemetry = JsonConvert.DeserializeObject<MeterTelemetryMessage>(payloadJson, JsonSettings);
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

        /// <summary>
        /// P1-4：电表消息库级去重。返回 true 表示这是第一次见到这条消息，应当继续处理。
        ///
        /// 兼容说明：老版本上位机发出的消息没有 MessageId（Guid.Empty），此时退回到
        /// HandleEnvelope 里的内存指纹去重，直接返回 true 继续处理，不阻断升级过程中的混跑。
        /// </summary>
        private static bool TryMarkMeterMessageProcessed(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            MqttEnvelope envelope,
            MeterTelemetryMessage telemetry)
        {
            if (telemetry.MessageId == Guid.Empty)
            {
                return true;
            }

            const string sql = @"
INSERT INTO meter_message_log (message_id, topic, meter_code, message_type, collect_time)
VALUES (@message_id, @topic, @meter_code, @message_type, @collect_time)
ON CONFLICT (message_id) DO NOTHING;";

            using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@message_id", telemetry.MessageId);
                command.Parameters.AddWithValue("@topic", Truncate(envelope.Topic ?? string.Empty, 255));
                command.Parameters.AddWithValue("@meter_code", (object)Truncate(telemetry.MeterCode, 64) ?? DBNull.Value);
                command.Parameters.AddWithValue("@message_type", (object)Truncate(telemetry.MessageType, 64) ?? DBNull.Value);
                command.Parameters.AddWithValue("@collect_time", telemetry.CollectTime.ToUniversalTime());
                return command.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// 清理过期的幂等记录（P1-4）。去重只需要覆盖"可能被重投"的时间窗，
        /// 长期保留会让这张表变成第二个 mqtt_message_log。
        /// </summary>
        private static void PruneMeterMessageLog()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_mysqlConnectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "DELETE FROM meter_message_log WHERE received_at < now() - make_interval(hours => @hours);",
                        connection))
                    {
                        command.Parameters.AddWithValue("@hours", _messageLogRetentionHours);
                        var deleted = command.ExecuteNonQuery();
                        if (deleted > 0)
                        {
                            AppLogger.Info("Dedup", "清理过期幂等记录 " + deleted + " 条（保留 " + _messageLogRetentionHours + " 小时）。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Dedup", "清理 meter_message_log 失败。", ex);
            }
        }

        private static bool IsDuplicateEnvelope(MqttEnvelope envelope)
        {
            var key = ComputeEnvelopeFingerprint(envelope);
            var now = GetBeijingNow().UtcDateTime;
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
