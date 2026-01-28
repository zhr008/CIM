using Common.Services;
using Common.Models;
using log4net;

namespace CIMMonitor.Services
{
    /// <summary>
    /// TIBCO Rendezvous服务封装类
    /// </summary>
    public class TibcoService : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TibcoService));
        private static TibcoService? _instance;
        private static readonly object _lock = new object();
        
        private readonly TibrvRendezvousService _tibcoService;
        private readonly List<TibcoMessage> _messages = new();
        private readonly Random _random = new();
        
        // 用于UI更新的事件
        public event Action<TibcoMessage>? OnMessageReceived;
        
        public class TibcoMessage
        {
            public string Subject { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string SenderId { get; set; } = string.Empty;
        }

        private TibcoService()
        {
            _tibcoService = new TibrvRendezvousService();
            _tibcoService.OnMessageReceived += OnTibcoMessageReceived;
            _tibcoService.OnError += OnTibcoError;
        }

        public static TibcoService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TibcoService();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 初始化TIBCO连接
        /// </summary>
        public async Task<bool> InitializeAsync(string networkInterface = ";", string service = "7500", string daemon = "tcp:7500")
        {
            log.Info("正在初始化TIBCO Rendezvous服务...");
            
            var result = await _tibcoService.InitializeAsync(networkInterface, service, daemon);
            
            if (result)
            {
                log.Info("TIBCO Rendezvous服务初始化成功");
            }
            else
            {
                log.Error("TIBCO Rendezvous服务初始化失败");
            }
            
            return result;
        }

        /// <summary>
        /// 底层TIBCO服务的消息接收事件处理
        /// </summary>
        private void OnTibcoMessageReceived(object sender, EquipmentMessage equipmentMessage)
        {
            var tibcoMessage = new TibcoMessage
            {
                Subject = "DEFAULT.SUBJECT", // 可以从设备消息中提取主题信息
                Content = equipmentMessage.MessageContent,
                SenderId = equipmentMessage.EquipmentID,
                Timestamp = equipmentMessage.Timestamp
            };

            // 添加到本地消息缓存
            lock (_messages)
            {
                _messages.Add(tibcoMessage);
                // 限制消息数量，保留最新的20条
                if (_messages.Count > 20)
                {
                    _messages.RemoveAt(0);
                }
            }

            log.Info($"接收到TIBCO消息: {tibcoMessage.Subject} - {tibcoMessage.Content}");
            
            // 触发UI更新事件
            OnMessageReceived?.Invoke(tibcoMessage);
        }

        /// <summary>
        /// 底层TIBCO服务的错误事件处理
        /// </summary>
        private void OnTibcoError(object sender, string errorMessage)
        {
            log.Error($"TIBCO服务错误: {errorMessage}");
        }

        public List<TibcoMessage> GetRecentMessages()
        {
            lock (_messages)
            {
                return _messages.OrderByDescending(m => m.Timestamp).Take(20).ToList();
            }
        }

        public async Task<bool> SendMessageAsync(string subject, string content, string senderId = "CIM")
        {
            if (!_tibcoService.IsConnected)
            {
                log.Warn("TIBCO服务未连接，无法发送消息");
                return false;
            }

            var equipmentMessage = new EquipmentMessage
            {
                EquipmentID = senderId,
                MessageType = "TIBCO_MESSAGE",
                MessageContent = content,
                Timestamp = DateTime.Now
            };

            var result = await _tibcoService.SendMessageAsync(subject, equipmentMessage);
            
            if (result)
            {
                // 添加到本地消息缓存
                var tibcoMessage = new TibcoMessage
                {
                    Subject = subject,
                    Content = content,
                    SenderId = senderId,
                    Timestamp = DateTime.Now
                };
                
                lock (_messages)
                {
                    _messages.Add(tibcoMessage);
                    // 限制消息数量，保留最新的20条
                    if (_messages.Count > 20)
                    {
                        _messages.RemoveAt(0);
                    }
                }
                
                log.Info($"TIBCO消息发送成功: {subject}");
            }
            else
            {
                log.Error($"TIBCO消息发送失败: {subject}");
            }
            
            return result;
        }

        public List<string> GetSubjects()
        {
            return new List<string>
            {
                "PRODUCTION.DATA",
                "PRODUCTION.COMMAND",
                "DEVICE.CONTROL",
                "DEVICE.STATUS",
                "ALARM.EVENT",
                "ALARM.ACK",
                "ORDER.UPDATE",
                "ORDER.COMPLETE",
                "SYSTEM.HEARTBEAT"
            };
        }

        public void SubscribeToSubject(string subject)
        {
            if (_tibcoService.IsConnected)
            {
                _ = _tibcoService.SubscribeAsync(subject);
            }
        }

        public bool IsConnected => _tibcoService.IsConnected;

        public void Dispose()
        {
            _tibcoService.OnMessageReceived -= OnTibcoMessageReceived;
            _tibcoService.OnError -= OnTibcoError;
            _tibcoService.Disconnect();
            _tibcoService.Dispose();
        }
    }
}
