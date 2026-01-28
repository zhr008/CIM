using Common.Services;
using Microsoft.Extensions.Logging;
using TIBCO.Rendezvous;
using WCFServices.Models;

namespace WCFServices.Services
{
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

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("Starting TIBCO adapter...");
                
                // 初始化TIBCO服务
                await _messageService.InitializeAsync(
                    "7500",  // service
                    "",      // network  
                    "tcp:7500", // daemon
                    "MES.>",  // listen subject
                    "MES.REPLY" // target subject
                );

                // 订阅TIBCO消息
                await _messageService.SubscribeAsync("MES.", async (messageArgs) =>
                {
                    try
                    {
                        // 将TIBCO消息转换为内部MesMessage格式
                        var mesMessage = ConvertTibcoMessageToMesMessage(messageArgs.Message);
                        
                        // 处理消息
                        await _businessService.ProcessTibcoMessageAsync(mesMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing TIBCO message");
                    }
                });

                // 开始监听
                await _messageService.StartListeningAsync();

                _logger.LogInformation("TIBCO adapter started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting TIBCO adapter");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _logger.LogInformation("Stopping TIBCO adapter...");
                await _messageService.StopListeningAsync();
                _logger.LogInformation("TIBCO adapter stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TIBCO adapter");
                throw;
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
                ReplySubject = ""
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
    }
}