using Common.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WCFServices.Models;

namespace WCFServices.Services
{
    /// <summary>
    /// TIBCO Rendezvous消息发送服务
    /// 集成真实TIBCO Rendezvous功能
    /// </summary>
    public interface ITibcoMessageService
    {
        Task<bool> InitializeAsync(TibcoConnectionInfo connectionInfo);
        Task<bool> StartListeningAsync(string subject);
        Task<bool> StopListeningAsync();
        bool SendMessage(TibcoMessage message);
        Task<bool> SendMessageAsync(TibcoMessage message);
        event EventHandler<TibcoMessage>? MessageReceived;
        TibcoConnectionStatus GetConnectionStatus();
    }

    /// <summary>
    /// TIBCO消息服务实现
    /// </summary>
    public class TibcoMessageService : ITibcoMessageService, ITibcoMessageSender, IDisposable
    {
        private readonly ILogger<TibcoMessageService> _logger;
        private TibcoConnectionInfo? _connectionInfo;
        private TibcoConnectionStatus _connectionStatus = TibcoConnectionStatus.Disconnected;
        private Timer? _listenerTimer;
        private string? _currentSubject;
        private readonly object _lock = new object();

        // 依赖注入TibrvRendezvousService
        private readonly TibrvRendezvousService _tibcoService;

        public event EventHandler<TibcoMessage>? MessageReceived;

        public TibcoMessageService(ILogger<TibcoMessageService> logger, TibrvRendezvousService tibcoService)
        {
            _logger = logger;
            _tibcoService = tibcoService;
            
            // 订阅底层TIBCO服务事件
            _tibcoService.OnMessageReceived += OnTibcoMessageReceived;
            _tibcoService.OnError += OnTibcoError;
        }

        /// <summary>
        /// 初始化TIBCO连接
        /// </summary>
        public async Task<bool> InitializeAsync(TibcoConnectionInfo connectionInfo)
        {
            try
            {
                _logger.LogInformation($"初始化TIBCO连接: Service={connectionInfo.Service}, Network={connectionInfo.Network}, Daemon={connectionInfo.Daemon}");

                _connectionInfo = connectionInfo;
                _connectionStatus = TibcoConnectionStatus.Connecting;

                // 使用真实的TIBCO服务进行连接
                var result = await _tibcoService.InitializeAsync(
                    connectionInfo.Network, 
                    connectionInfo.Service, 
                    connectionInfo.Daemon);

                if (result)
                {
                    _connectionStatus = TibcoConnectionStatus.Connected;
                    _logger.LogInformation("TIBCO连接已建立");
                }
                else
                {
                    _connectionStatus = TibcoConnectionStatus.Error;
                    _logger.LogError("TIBCO连接建立失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化TIBCO连接失败");
                _connectionStatus = TibcoConnectionStatus.Error;
                return false;
            }
        }

        /// <summary>
        /// 开始监听主题
        /// </summary>
        public async Task<bool> StartListeningAsync(string subject)
        {
            try
            {
                if (_connectionStatus != TibcoConnectionStatus.Connected)
                {
                    _logger.LogWarning("TIBCO连接未建立");
                    return false;
                }

                _logger.LogInformation($"开始监听主题: {subject}");
                _currentSubject = subject;
                
                // 使用真实的TIBCO服务订阅主题
                var result = await _tibcoService.SubscribeAsync(subject);
                
                if (result)
                {
                    _logger.LogInformation($"成功订阅主题: {subject}");
                }
                else
                {
                    _logger.LogError($"订阅主题失败: {subject}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始监听失败");
                return false;
            }
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public async Task<bool> StopListeningAsync()
        {
            try
            {
                _logger.LogInformation("停止监听");
                
                // 在真实实现中，这里会取消订阅主题
                // 目前先简单地停止定时器
                _listenerTimer?.Dispose();
                _listenerTimer = null;
                _currentSubject = null;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止监听失败");
                return false;
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public bool SendMessage(TibcoMessage message)
        {
            try
            {
                if (_connectionStatus != TibcoConnectionStatus.Connected)
                {
                    _logger.LogWarning("TIBCO连接未建立");
                    return false;
                }

                _logger.LogInformation($"发送TIBCO消息: Subject={message.Subject}, Type={message.MessageType}");
                _logger.LogInformation($"消息内容: {message.ToJson()}");

                // 这里需要将TibcoMessage转换为EquipmentMessage并发送
                var equipmentMessage = new Common.Models.EquipmentMessage
                {
                    EquipmentID = message.Source,
                    MessageType = message.MessageType,
                    MessageContent = message.ToJson(),
                    Timestamp = DateTime.Now
                };

                // 使用底层TIBCO服务发送消息
                var task = _tibcoService.SendMessageAsync(message.Subject, equipmentMessage);
                var result = task.Result; // 注意：在实际生产环境中应避免使用.Result
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败");
                return false;
            }
        }

        /// <summary>
        /// 异步发送消息
        /// </summary>
        public async Task<bool> SendMessageAsync(TibcoMessage message)
        {
            try
            {
                if (_connectionStatus != TibcoConnectionStatus.Connected)
                {
                    _logger.LogWarning("TIBCO连接未建立");
                    return false;
                }

                _logger.LogInformation($"发送TIBCO消息: Subject={message.Subject}, Type={message.MessageType}");
                _logger.LogInformation($"消息内容: {message.ToJson()}");

                // 这里需要将TibcoMessage转换为EquipmentMessage并发送
                var equipmentMessage = new Common.Models.EquipmentMessage
                {
                    EquipmentID = message.Source,
                    MessageType = message.MessageType,
                    MessageContent = message.ToJson(),
                    Timestamp = DateTime.Now
                };

                // 使用底层TIBCO服务发送消息
                var result = await _tibcoService.SendMessageAsync(message.Subject, equipmentMessage);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步发送消息失败");
                return false;
            }
        }

        /// <summary>
        /// 获取连接状态
        /// </summary>
        public TibcoConnectionStatus GetConnectionStatus()
        {
            return _connectionStatus;
        }

        /// <summary>
        /// 底层TIBCO服务的消息接收事件处理器
        /// </summary>
        private void OnTibcoMessageReceived(object sender, Common.Models.EquipmentMessage equipmentMessage)
        {
            try
            {
                // 将EquipmentMessage转换为TibcoMessage
                var tibcoMessage = new TibcoMessage
                {
                    Subject = _currentSubject ?? "DEFAULT.SUBJECT",
                    Source = equipmentMessage.EquipmentID,
                    MessageType = equipmentMessage.MessageType,
                    Content = equipmentMessage.MessageContent,
                    Timestamp = equipmentMessage.Timestamp
                };

                _logger.LogInformation($"接收到TIBCO消息: {tibcoMessage.MessageType}");
                
                // 触发上层事件
                MessageReceived?.Invoke(this, tibcoMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理底层TIBCO消息时出错");
            }
        }

        /// <summary>
        /// 底层TIBCO服务的错误事件处理器
        /// </summary>
        private void OnTibcoError(object sender, string errorMessage)
        {
            _logger.LogError($"底层TIBCO服务错误: {errorMessage}");
            
            // 更新连接状态
            _connectionStatus = TibcoConnectionStatus.Error;
        }

        public void Dispose()
        {
            // 取消事件订阅
            if (_tibcoService != null)
            {
                _tibcoService.OnMessageReceived -= OnTibcoMessageReceived;
                _tibcoService.OnError -= OnTibcoError;
            }
            
            _listenerTimer?.Dispose();
        }
    }

    /// <summary>
    /// TIBCO消息监听器包装器
    /// </summary>
    public class TibcoMessageListener : ITibcoMessageListener
    {
        private readonly IMesBusinessService _businessService;
        private readonly ILogger<TibcoMessageListener> _logger;

        public TibcoMessageListener(
            IMesBusinessService businessService,
            ILogger<TibcoMessageListener> logger)
        {
            _businessService = businessService;
            _logger = logger;
        }

        public void OnMessageReceived(TibcoMessage message)
        {
            _logger.LogInformation($"监听器收到消息: {message.MessageType}");

            try
            {
                _ = Task.Run(async () => await _businessService.ProcessMessageAsync(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理消息失败");
            }
        }

        public void OnError(Exception ex)
        {
            _logger.LogError(ex, "监听器发生错误");
        }
    }

    /// <summary>
    /// TIBCO适配器
    /// 负责协调TIBCO消息服务和业务服务
    /// </summary>
    public interface ITibcoAdapter
    {
        Task<bool> StartAsync();
        Task<bool> StopAsync();
    }

    public class TibcoAdapter : ITibcoAdapter
    {
        private readonly ITibcoMessageService _messageService;
        private readonly IMesBusinessService _businessService;
        private readonly ILogger<TibcoAdapter> _logger;

        public TibcoAdapter(
            ITibcoMessageService messageService,
            IMesBusinessService businessService,
            ILogger<TibcoAdapter> logger)
        {
            _messageService = messageService;
            _businessService = businessService;
            _logger = logger;
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                _logger.LogInformation("启动TIBCO适配器");

                // 初始化连接
                var connectionInfo = new TibcoConnectionInfo
                {
                    Service = "7500",
                    Network = ";",
                    Daemon = "tcp:7500"
                };

                if (!await _messageService.InitializeAsync(connectionInfo))
                {
                    _logger.LogError("TIBCO连接初始化失败");
                    return false;
                }

                // 注册消息监听器
                _messageService.MessageReceived += OnMessageReceived;

                // 开始监听主要主题
                var subjects = new[]
                {
                    "EQUIPMENT.STATUS.>",
                    "LOT.TRACKING.>",
                    "PROCESS.DATA.>",
                    "ALARM.>"
                };

                foreach (var subject in subjects)
                {
                    if (await _messageService.StartListeningAsync(subject))
                    {
                        _logger.LogInformation($"开始监听主题: {subject}");
                    }
                }

                _logger.LogInformation("TIBCO适配器启动成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TIBCO适配器启动失败");
                return false;
            }
        }

        public async Task<bool> StopAsync()
        {
            try
            {
                _logger.LogInformation("停止TIBCO适配器");

                _messageService.MessageReceived -= OnMessageReceived;
                await _messageService.StopListeningAsync();

                _logger.LogInformation("TIBCO适配器已停止");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TIBCO适配器停止失败");
                return false;
            }
        }

        private void OnMessageReceived(object? sender, TibcoMessage message)
        {
            _ = Task.Run(async () => await _businessService.ProcessMessageAsync(message));
        }
    }
}
