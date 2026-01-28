using Common.Services;
using Microsoft.Extensions.Logging;
using TIBCO.Rendezvous;
using WCFServices.Models;

namespace WCFServices.Services
{
    public class TibcoSubscriberService
    {
        private readonly TibrvService _tibcoService;
        private readonly IMesBusinessService _businessService;
        private readonly ILogger<TibcoSubscriberService> _logger;

        public TibcoSubscriberService(
            TibrvService tibcoService,
            IMesBusinessService businessService,
            ILogger<TibcoSubscriberService> logger)
        {
            _tibcoService = tibcoService;
            _businessService = businessService;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("Initializing TIBCO subscriber service...");

                // 初始化TIBCO服务
                _tibcoService.Service = "7500";
                _tibcoService.Network = "";
                _tibcoService.Daemon = "tcp:7500";
                _tibcoService.ListenSubject = "MES.SUBSCRIBE>";
                _tibcoService.TargetSubject = "MES.REPLY";

                // 设置事件处理器
                _tibcoService.ErrorMessageHandler += OnErrorMessage;
                _tibcoService.ConnectedStatusHandler += OnConnectedStatus;
                _tibcoService.ListenedStatusHandler += OnListenedStatus;
                
                // 设置消息接收处理器
                _tibcoService.messageReceivedHandler += OnMessageReceived;

                // 打开并连接
                _tibcoService.Open();
                _tibcoService.StartConnect();

                _logger.LogInformation("TIBCO subscriber service initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting TIBCO subscriber service");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _logger.LogInformation("Stopping TIBCO subscriber service...");
                _tibcoService.DisConnected();
                _logger.LogInformation("TIBCO subscriber service stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TIBCO subscriber service");
                throw;
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Received TIBCO message on subject: {e.Message.SendSubject}");

                // 将TIBCO消息转换为MesMessage
                var mesMessage = ConvertTibcoMessageToMesMessage(e.Message);

                // 在后台任务中处理消息，避免阻塞消息接收
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 通过业务服务处理消息
                        var response = await _businessService.ProcessTibcoMessageAsync(mesMessage);
                        
                        _logger.LogInformation($"Processed TIBCO message: {mesMessage.MessageId}, Success: {response.Success}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing received TIBCO message: {mesMessage.MessageId}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling received TIBCO message");
            }
        }

        private MesMessage ConvertTibcoMessageToMesMessage(TIBCO.Rendezvous.Message tibcoMessage)
        {
            var mesMessage = new MesMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = tibcoMessage.SendSubject,
                MessageType = ExtractMessageType(tibcoMessage.SendSubject),
                Timestamp = DateTime.UtcNow,
                Fields = new Dictionary<string, object>(),
                CorrelationId = "",
                ReplySubject = tibcoMessage.ReplySubject
            };

            // 将TIBCO消息字段转换为MesMessage字段
            foreach (TIBCO.Rendezvous.Field field in tibcoMessage.Fields)
            {
                mesMessage.Fields[field.Name] = field.Value?.ToString() ?? "";
            }

            return mesMessage;
        }

        private string ExtractMessageType(string subject)
        {
            // 从主题中提取消息类型，例如从 "MES.EQUIPMENT.STATUS.UPDATE" 提取 "EQUIPMENT_STATUS"
            if (string.IsNullOrEmpty(subject))
                return "UNKNOWN";

            var parts = subject.Split('.');
            if (parts.Length >= 2)
            {
                var typeParts = parts.Skip(1).Take(2).ToArray(); // 取第二和第三个部分
                return string.Join("_", typeParts).ToUpper();
            }

            return "GENERIC";
        }

        private void OnErrorMessage(object sender, string message)
        {
            _logger.LogError($"TIBCO Error: {message}");
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