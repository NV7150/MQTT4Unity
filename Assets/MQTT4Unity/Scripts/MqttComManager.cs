using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;

namespace MQTT4Unity {

    public delegate void SubscribeDebugCallback(string topic, string payload);
    public delegate void SubscribeCallback(string payload);
    
    public delegate void OnConnectedCallback();
    public delegate void OnDisconnectedCallback();
    
    public delegate void LogCallback(string msg);

    /// <summary>
    /// This is a simple wrapper of MQTTnet for Unity that supports wildcard topics.
    /// </summary>
    public class MqttComManager : MonoBehaviour {
        [SerializeField] private string brokerDomain;
        [SerializeField] private int port = 8883;
        [SerializeField] private UserAuthentication userAuth;
        [SerializeField] private ClientIdAuthentication clientIdAuth;
        [SerializeField] private MqttQualityOfServiceLevel defaultQos;
        [SerializeField] private bool connectOnAwake = true;

        /// <summary>
        /// For Debug: Returns raw topic and message
        /// </summary>
        [Obsolete("This event is deprecated. Use Subscribe() instead.")]
        public event SubscribeDebugCallback OnRecvDebug;

        public event OnConnectedCallback OnConnected;
        public event OnDisconnectedCallback OnDisconnected;

        private IMqttClient _client;
        private bool _isStartUp = false;
        private bool _isConnected = false;
        private readonly MqttSubscribePool _subscribePool = new();

        private readonly ConcurrentQueue<MqttApplicationMessage> _processQueue = new();
        private readonly ConcurrentQueue<string> _mqttLogs = new();

        private bool _endProcess = false;
        
        private MqttNetEventLogger _logger;

        delegate void DelayedPublish();
        DelayedPublish _delayedPublish;
        delegate void DelayedSubscribe();
        DelayedSubscribe _delayedSubscribe;
        
        public event LogCallback OnLog;

        /// <summary>
        /// True if the connection established
        /// </summary>
        public bool IsConnected => _isConnected;

        private void Awake() {
            if (connectOnAwake)
                ConnectStartUp();
        }

        /// <summary>
        /// Start up the connection
        /// This will call automatically if you check "connectOnAwake" on Awake Process
        /// </summary>
        public void ConnectStartUp() {
            if (_isStartUp)
                return;
            _isStartUp = true;

            var option = new MqttClientOptionsBuilder();
            if (userAuth.EnableAuth)
                option.WithCredentials(userAuth.UserName, userAuth.Password);
            
            if (clientIdAuth.EnableAuth)
                option.WithClientId(clientIdAuth.ClientId);
    
            option.WithTcpServer(brokerDomain, port);
            
            _logger = new MqttNetEventLogger();
            _logger.LogMessagePublished += (s, e) => {
                var trace = $">> [{e.LogMessage.Level}] {e.LogMessage.Source}: {e.LogMessage.Message}";
                if (e.LogMessage.Exception != null) {
                    trace += Environment.NewLine + e.LogMessage.Exception;
                }

                _mqttLogs.Enqueue(trace);
            };
            
            var builder = new MqttFactory(_logger);
            _client = builder.CreateMqttClient();
            _client.ConnectedAsync += Connected;
            _client.DisconnectedAsync += Disconnected;
            _client.ApplicationMessageReceivedAsync += OnRecv;
            
            _ = Connect(option.Build());
            
            StartCoroutine(ResolveSubscribes());
            StartCoroutine(ResolveLogs());
        }

        // Run subscribe callback with coroutine in main thread
        // Because some Unity functionalities cannot run in another thread...
        private IEnumerator ResolveSubscribes() {
            yield return new WaitUntil(() => IsConnected);
            
            OnConnected?.Invoke();
            
            while (!_endProcess) {
                if (!IsConnected) {
                    OnDisconnected?.Invoke();
                    yield return new WaitUntil(() => IsConnected);
                    OnConnected?.Invoke();
                }
                
                while(_processQueue.TryDequeue(out var msg)) {
                    var content = Encoding.UTF8.GetString(msg.PayloadSegment);
                    OnRecvDebug?.Invoke(msg.Topic, content);
                    var callbacks = _subscribePool.GetCallbacks(msg.Topic);
                    foreach (var callback in callbacks) {
                        callback.Invoke(content);
                    }
                }
                yield return null;
            }
        }

        private IEnumerator ResolveLogs() {
            while (!_endProcess) {
                while (_mqttLogs.TryDequeue(out var msg)) {
                    OnLog?.Invoke(msg);
                }

                yield return null;
            }
        }

        async Task Connect(MqttClientOptions options) {
            await _client.ConnectAsync(options, CancellationToken.None);
        }

        Task Connected(MqttClientConnectedEventArgs args) {
            _isConnected = true;

            _delayedSubscribe?.Invoke();
            _delayedSubscribe = null;
            _delayedPublish?.Invoke();
            _delayedPublish = null;
            

            return Task.CompletedTask;
        }

        Task Disconnected(MqttClientDisconnectedEventArgs args) {
            _isConnected = false;
            _isStartUp = false;
            
            return Task.CompletedTask;
        }

        Task OnRecv(MqttApplicationMessageReceivedEventArgs args) {
            var msg = args.ApplicationMessage;
            // Only Queue the message
            _processQueue.Enqueue(msg);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publish the payload to the specified topic
        /// If isConnected == false, the task will be proceeded after connected
        /// </summary>
        /// <param name="topic">target topic</param>
        /// <param name="payload">payload</param>
        /// <param name="level">the service level if you want to specify: default is defaultQoS</param>
        public void Publish(string topic, string payload, MqttQualityOfServiceLevel? level = null) {
            if (!IsConnected) {
                _delayedPublish += () => Publish(topic, payload, level);
                return;
            }

            var trueLevel = level ?? defaultQos;
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(trueLevel)
                .Build();
            _ = _client.PublishAsync(message, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribe to the topic
        /// If isConnected == false, the task will be proceeded after connected
        /// </summary>
        /// <param name="topic">target topic</param>
        /// <param name="callback">callback delegate</param>
        public void Subscribe(string topic, SubscribeCallback callback) {
            if (!IsConnected) {
                _delayedSubscribe += () => Subscribe(topic, callback);
                return;
            }

            bool isFirstSubscription = !_subscribePool.HasSubscriptions(topic);
            _subscribePool.AddSubscription(topic, callback);

            if (isFirstSubscription) {
                var builder = new MqttFactory();
                var subscribeOpt = builder.CreateSubscribeOptionsBuilder().WithTopicFilter(
                    f => {
                        f.WithTopic(topic);
                    });
                _ = _client.SubscribeAsync(subscribeOpt.Build(), CancellationToken.None);
            }
        }

        /// <summary>
        /// Unregister the callback
        /// </summary>
        /// <param name="topic">target topic</param>
        /// <param name="callback">callback delegate that want to unsubscribe</param>
        public void Unsubscribe(string topic, SubscribeCallback callback) {
            if (!IsConnected)
                return;

            _subscribePool.RemoveSubscription(topic, callback);

            if (!_subscribePool.HasSubscriptions(topic)) {
                // Unsubscribe from broker
                var builder = new MqttFactory();
                var unsubscribeOpt = builder.CreateUnsubscribeOptionsBuilder().WithTopicFilter(topic);
                _ = _client.UnsubscribeAsync(unsubscribeOpt.Build(), CancellationToken.None);
            }
        }

        /// <summary>
        /// Unsubscribe the topic and release all delegates
        /// </summary>
        /// <param name="topic">target topic</param>
        public void UnsubscribeAll(string topic) {
            if (!IsConnected)
                return;

            _subscribePool.RemoveAllSubscriptions(topic);

            // Unsubscribe from broker
            var builder = new MqttFactory();
            var unsubscribeOpt = builder.CreateUnsubscribeOptionsBuilder().WithTopicFilter(topic);
            _ = _client.UnsubscribeAsync(unsubscribeOpt.Build(), CancellationToken.None);
        }

        private async void OnDestroy() {
            _endProcess = true;
            await _client.DisconnectAsync();
        }

    }

    [Serializable]
    public class UserAuthentication {
        [SerializeField] private bool enableAuth = false;
        [SerializeField] private string userName;
        [SerializeField] private string password;

        public bool EnableAuth => enableAuth;

        public string UserName => userName;

        public string Password => password;
    }

    [Serializable]
    public class ClientIdAuthentication {
        [SerializeField] private bool enableAuth = false;
        [SerializeField] private string clientId;

        public bool EnableAuth => enableAuth;

        public string ClientId => clientId;
    }
}
