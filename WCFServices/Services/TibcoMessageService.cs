using Common.Services;
using Microsoft.Extensions.Logging;
using TIBCO.Rendezvous;
using WCFServices.Models;

namespace WCFServices.Services
{
    public interface ITibcoMessageService
    {
        Task<bool> InitializeAsync(string service, string network, string daemon, string listenSubject, string targetSubject);
        Task<bool> SendMessageAsync(string subject, Dictionary<string, object> fields);
        Task<bool> SubscribeAsync(string subject, Action<MessageReceivedEventArgs> messageHandler);
        Task<bool> UnsubscribeAsync(string subject);
        Task StartListeningAsync();
        Task StopListeningAsync();
    }

    public interface ITibcoMessageSender
    {
        Task<bool> SendMessageAsync(string subject, Dictionary<string, object> fields);
    }

    public interface ITibcoMessageListener
    {
        Task HandleMessageAsync(MesMessage message);
    }

    public interface ITibcoAdapter
    {
        Task StartAsync();
        Task StopAsync();
    }

    public class TibcoMessageService : ITibcoMessageService, ITibcoMessageSender
    {
        private readonly ILogger<TibcoMessageService> _logger;
        private readonly TibrvService _tibcoService;
        private readonly IMesService _mesService;
        
        public TibcoMessageService(
            ILogger<TibcoMessageService> logger, 
            TibrvService tibcoService)
        {
            _logger = logger;
            _tibcoService = tibcoService;
        }

        public Task<bool> InitializeAsync(string service, string network, string daemon, string listenSubject, string targetSubject)
        {
            try
            {
                _tibcoService.Service = service;
                _tibcoService.Network = network;
                _tibcoService.Daemon = daemon;
                _tibcoService.ListenSubject = listenSubject;
                _tibcoService.TargetSubject = targetSubject;
                
                _tibcoService.ErrorMessageHandler += OnErrorMessage;
                _tibcoService.ConnectedStatusHandler += OnConnectedStatus;
                
                _tibcoService.Open();
                _tibcoService.StartConnect();
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TIBCO service");
                return Task.FromResult(false);
            }
        }

        public Task<bool> SendMessageAsync(string subject, Dictionary<string, object> fields)
        {
            try
            {
                // 使用TIBCO服务发送消息
                var message = new TIBCO.Rendezvous.Message();
                message.SendSubject = subject;
                
                foreach (var field in fields)
                {
                    message.AddField(field.Key, field.Value?.ToString());
                }
                
                _tibcoService.Transport?.Send(message);
                
                _logger.LogInformation($"Sent TIBCO message to subject: {subject}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send TIBCO message to subject: {subject}");
                return Task.FromResult(false);
            }
        }

        public Task<bool> SubscribeAsync(string subject, Action<MessageReceivedEventArgs> messageHandler)
        {
            try
            {
                _tibcoService.messageReceivedHandler += (sender, args) =>
                {
                    if (args.Message.SendSubject.StartsWith(subject))
                    {
                        messageHandler(args);
                    }
                };
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to subscribe to TIBCO subject: {subject}");
                return Task.FromResult(false);
            }
        }

        public Task<bool> UnsubscribeAsync(string subject)
        {
            try
            {
                // 实现取消订阅逻辑
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to unsubscribe from TIBCO subject: {subject}");
                return Task.FromResult(false);
            }
        }

        public Task StartListeningAsync()
        {
            try
            {
                _tibcoService.ListenedStatusHandler += OnListenedStatus;
                _tibcoService.OnConnectCallBack(this, _tibcoService.IsConnected);
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start listening");
                return Task.FromResult(false);
            }
        }

        public Task StopListeningAsync()
        {
            try
            {
                _tibcoService.DisConnected();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop listening");
                return Task.FromResult(false);
            }
        }

        private void OnErrorMessage(object sender, string message)
        {
            _logger.LogInformation($"TIBCO Error: {message}");
        }

        private void OnConnectedStatus(object sender, bool isConnected)
        {
            _logger.LogInformation($"TIBCO Connection Status: {isConnected}");
        }

        private void OnListenedStatus(object sender, bool isListened)
        {
            _logger.LogInformation($"TIBCO Listening Status: {isListened}");
        }
    }
}