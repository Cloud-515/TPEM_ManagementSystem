using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

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
        private readonly string _username;
        private readonly string _password;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private readonly IMqttClient _client;
        private bool _disposed;

        public MqttPublisherService()
        {
            _host = GetAppSetting("MqttHost", "127.0.0.1");
            _port = int.TryParse(GetAppSetting("MqttPort", "2883"), out var port) ? port : 2883;
            _clientId = GetAppSetting("MqttClientId", "meter-acquisition-winform");
            _topicTemplate = GetAppSetting("MqttTopicTemplate", "meter/{site}/{box}/{meter}");
            _legacyTopic = GetAppSetting("MqttLegacyTopic", "meter/data");
            _publishLegacyTopic = bool.TryParse(GetAppSetting("PublishLegacyTopic", "true"), out var publishLegacyTopic) && publishLegacyTopic;
            _registryTopicTemplate = GetAppSetting("MqttRegistryTopicTemplate", "meter/registry/{site}/{box}");
            _username = GetAppSetting("MqttUsername", string.Empty);
            _password = GetAppSetting("MqttPassword", string.Empty);
            _jsonSettings = new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss",
                NullValueHandling = NullValueHandling.Ignore
            };

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
            _client.DisconnectedAsync += args => Task.CompletedTask;
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
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            await _client.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);
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
