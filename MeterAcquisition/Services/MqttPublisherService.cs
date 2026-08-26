using System;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Tpem.Diagnostics;

namespace MeterAcquisition
{
    public sealed class MqttPublisherService : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _clientId;
        private readonly string _topicTemplate;
        private readonly string _legacyTopic;
        private readonly bool _publishLegacyTopic;
        private readonly string _registryTopicTemplate;
        private readonly string _heatPumpTopicTemplate;
        private readonly string _thermostatTopicTemplate;
        private readonly string _username;
        private readonly string _password;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private readonly IMqttClient _client;
        private bool _disposed;

        public MqttPublisherService()
        {
            _host = GetAppSetting("MqttHost", "127.0.0.1");
            _port = int.TryParse(GetAppSetting("MqttPort", "1883"), out var port) ? port : 1883;
            _clientId = BuildUniqueClientId(GetAppSetting("MqttClientId", "meter-acquisition-winform"));
            _topicTemplate = GetAppSetting("MqttTopicTemplate", "meter/{site}/{box}/{meter}");
            _legacyTopic = GetAppSetting("MqttLegacyTopic", "meter/data");
            _publishLegacyTopic = bool.TryParse(GetAppSetting("PublishLegacyTopic", "true"), out var publishLegacyTopic) && publishLegacyTopic;
            _registryTopicTemplate = GetAppSetting("MqttRegistryTopicTemplate", "meter/registry/{site}/{box}");
            _heatPumpTopicTemplate = GetAppSetting("HeatPumpMqttTopicTemplate", "tpem/{site}/heatpump/{controller}/telemetry");
            _thermostatTopicTemplate = GetAppSetting("ThermostatMqttTopicTemplate", "tpem/{site}/thermostat/{device}/telemetry");
            _username = GetAppSetting("MqttUsername", string.Empty);
            _password = GetAppSetting("MqttPassword", string.Empty);
            _jsonSettings = new JsonSerializerSettings
            {
                DateFormatString = "o",
                NullValueHandling = NullValueHandling.Ignore
            };

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
            _client.ConnectedAsync += args =>
            {
                AppLogger.Info("Mqtt", "已连接 Broker " + _host + ":" + _port + "，ClientId=" + _clientId);
                return Task.CompletedTask;
            };
            _client.DisconnectedAsync += args =>
            {
                // 发布路径是"用时才连"，这里只需留痕；重连由 EnsureConnectedAsync 在下次发布时完成。
                AppLogger.Warn(
                    "Mqtt",
                    "与 Broker 断开，原因: " + args.Reason + "。下次发布时会自动重连。",
                    args.Exception);
                return Task.CompletedTask;
            };
        }

        public async Task PublishAsync(MeterTelemetryMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null)
            {
                return;
            }

            await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                var payload = JsonConvert.SerializeObject(message, _jsonSettings);
                await PublishToTopicAsync(BuildTopic(message), payload, cancellationToken).ConfigureAwait(false);

                if (_publishLegacyTopic && !string.IsNullOrWhiteSpace(_legacyTopic))
                {
                    await PublishToTopicAsync(_legacyTopic, payload, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task PublishHeatPumpTelemetryAsync(HeatPumpTelemetryMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null)
            {
                return;
            }

            await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                var payload = JsonConvert.SerializeObject(message, _jsonSettings);
                await PublishToTopicAsync(BuildHeatPumpTopic(message), payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task PublishThermostatTelemetryAsync(ThermostatTelemetryMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null)
            {
                return;
            }

            await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                var payload = JsonConvert.SerializeObject(message, _jsonSettings);
                await PublishToTopicAsync(BuildThermostatTopic(message), payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task PublishRegistryAsync(MeterRegistryMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null)
            {
                return;
            }

            await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                var payload = JsonConvert.SerializeObject(message, _jsonSettings);
                await PublishToTopicAsync(BuildRegistryTopic(message), payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task PublishToTopicAsync(string topic, string payload, CancellationToken cancellationToken)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);
        }

        private string BuildHeatPumpTopic(HeatPumpTelemetryMessage message)
        {
            return _heatPumpTopicTemplate
                .Replace("{site}", SanitizeTopicSegment(message.SiteCode))
                .Replace("{controller}", message.ControllerSlaveId.ToString(CultureInfo.InvariantCulture));
        }

        private string BuildThermostatTopic(ThermostatTelemetryMessage message)
        {
            return _thermostatTopicTemplate
                .Replace("{site}", SanitizeTopicSegment(message.SiteCode))
                .Replace("{device}", SanitizeTopicSegment(message.DeviceKey));
        }

        private string BuildTopic(MeterTelemetryMessage message)
        {
            return _topicTemplate
                .Replace("{site}", SanitizeTopicSegment(message.SiteCode))
                .Replace("{box}", SanitizeTopicSegment(message.BoxCode))
                .Replace("{meter}", SanitizeTopicSegment(message.MeterCode));
        }

        private string BuildRegistryTopic(MeterRegistryMessage message)
        {
            return _registryTopicTemplate
                .Replace("{site}", SanitizeTopicSegment(message.SiteCode))
                .Replace("{box}", SanitizeTopicSegment(message.BoxCode));
        }

        private static string SanitizeTopicSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim().Replace("/", "_").Replace("+", "_").Replace("#", "_");
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_client.IsConnected)
            {
                return;
            }

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId(_clientId)
                .WithCleanSession();

            if (!string.IsNullOrWhiteSpace(_username))
            {
                builder.WithCredentials(_username, _password);
            }

            await _client.ConnectAsync(builder.Build(), cancellationToken).ConfigureAwait(false);
        }

        private static string GetAppSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        /// <summary>
        /// 给配置里的 ClientId 追加机器名后缀（P0-7）。
        /// 配置项是写死的常量，多机部署或误开两个实例时会用同一个 ClientId 连接同一个 Broker，
        /// 按 MQTT 规范后连的会把先连的顶下线，双方各自重连 → 无限重连风暴、数据双向丢失。
        /// </summary>
        private static string BuildUniqueClientId(string configured)
        {
            var baseId = string.IsNullOrWhiteSpace(configured) ? "meter-acquisition" : configured.Trim();
            string machine;
            try
            {
                machine = Environment.MachineName;
            }
            catch
            {
                machine = "host";
            }

            var suffix = SanitizeTopicSegment(machine);
            var clientId = baseId + "-" + suffix;

            // MQTT 3.1.1 服务端可只保证 23 字符，超长时退回哈希后缀以保持唯一且稳定。
            if (clientId.Length > 23)
            {
                var hash = (uint)StringComparer.Ordinal.GetHashCode(suffix);
                var shortSuffix = "-" + hash.ToString("x8", CultureInfo.InvariantCulture);
                var keep = Math.Max(1, 23 - shortSuffix.Length);
                clientId = baseId.Substring(0, Math.Min(baseId.Length, keep)) + shortSuffix;
            }

            return clientId;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                if (_client.IsConnected)
                {
                    _client.DisconnectAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
            }

            _client?.Dispose();
            _syncLock.Dispose();
        }
    }
}
