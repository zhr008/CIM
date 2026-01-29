using Common.Services;
using WCFServices.Contracts;
using WCFServices.Database;
using Microsoft.Extensions.Logging;

namespace WCFServices.Adapters
{
    public class TibrvToWcfAdapter
    {
        private readonly IWcfService _wcfService;
        private readonly IMssqlRepository _repository;
        private readonly ILogger<TibrvToWcfAdapter> _logger;
        private readonly TibrvService _tibrvService;

        public TibrvToWcfAdapter(IWcfService wcfService, IMssqlRepository repository, ILogger<TibrvToWcfAdapter> logger)
        {
            _wcfService = wcfService;
            _repository = repository;
            _logger = logger;
            
            // We'll initialize the TibrvService separately and pass it here
            _tibrvService = null; // Will be set later
        }

        public TibrvToWcfAdapter(IWcfService wcfService, IMssqlRepository repository, ILogger<TibrvToWcfAdapter> logger, TibrvService tibrvService)
        {
            _wcfService = wcfService;
            _repository = repository;
            _logger = logger;
            _tibrvService = tibrvService;
        }

        public void StartListeningForTibrvMessages()
        {
            if (_tibrvService == null)
            {
                _logger.LogError("TibrvService is not initialized.");
                return;
            }

            // Subscribe to the message received event
            _tibrvService.messageReceivedHandler += HandleTibrvMessageReceived;
            
            _logger.LogInformation("Started listening for Tibrv messages to forward to WCF service.");
        }

        public void StopListeningForTibrvMessages()
        {
            if (_tibrvService != null)
            {
                _tibrvService.messageReceivedHandler -= HandleTibrvMessageReceived;
                _logger.LogInformation("Stopped listening for Tibrv messages.");
            }
        }

        private async void HandleTibrvMessageReceived(object sender, TIBCO.Rendezvous.MessageReceivedEventArgs e)
        {
            try
            {
                // 修复CS1061: MessageReceivedEventArgs 没有 Msg 属性，应该用 Message
                // 修复CA2254: 日志消息模板应为常量字符串
                _logger.LogDebug("Received Tibrv message on subject: {Subject}", e.Message);

                // Extract XML content from the message
                string xmlContent = ExtractXmlFromTibrvMessage(e.Message);
                
                if (!string.IsNullOrEmpty(xmlContent))
                {
                    // 日志模板应为常量字符串
                    _logger.LogInformation("Received XML from Tibrv: {XmlPreview}...", xmlContent.Substring(0, Math.Min(200, xmlContent.Length)));

                    // Forward the XML message to the WCF service
                    await ForwardXmlToWcfService(xmlContent);
                    
                    // Optionally save to database
                    await SaveReceivedMessageToDatabase(xmlContent, e.Message.ReplySubject);
                }
                else
                {
                    _logger.LogWarning("No XML content found in received Tibrv message.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Tibrv message: {Message}", ex.Message);
            }
        }

        private string ExtractXmlFromTibrvMessage(TIBCO.Rendezvous.Message message)
        {
            string xmlContent = string.Empty;

            // 尝试通过 Message 的 GetField/GetXmlAsString 方法获取 XML 内容
            // 优先查找常见字段名
            string[] possibleFieldNames = { "XMLContent", "Data", "Message" };
            foreach (var fieldName in possibleFieldNames)
            {
                try
                {
                    var field = message.GetField(fieldName);
                    if (field != null && field.Value != null)
                    {
                        xmlContent = field.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(xmlContent))
                            return xmlContent;
                    }
                }
                catch
                {
                    // 字段不存在时忽略异常
                }
            }

            // 尝试查找 XML 字符串字段
            for (uint i = 0; i < message.FieldCount; i++)
            {
                try
                {
                    var field = message.GetFieldByIndex(i);
                    var value = field?.Value?.ToString();
                    if (!string.IsNullOrEmpty(value) && value.TrimStart().StartsWith("<") && value.Contains(">"))
                    {
                        xmlContent = value;
                        break;
                    }
                }
                catch
                {
                    // 忽略异常
                }
            }

            // 尝试通过 GetXmlAsString 获取
            try
            {
                for (uint i = 0; i < message.FieldCount; i++)
                {
                    var xml = message.GetXmlAsStringByIndex(i);
                    if (!string.IsNullOrWhiteSpace(xml))
                    {
                        xmlContent = xml;
                        break;
                    }
                }
            }
            catch
            {
                // 忽略异常
            }

            return xmlContent;
        }

        private async Task ForwardXmlToWcfService(string xmlContent)
        {
            try
            {
                // Call the WCF service to process the XML message
                bool success = await _wcfService.ProcessXmlMessageAsync(xmlContent);
                
                if (success)
                {
                    _logger.LogInformation("Successfully forwarded XML to WCF service.");
                }
                else
                {
                    _logger.LogWarning("Failed to forward XML to WCF service.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error forwarding XML to WCF service: {ex.Message}");
            }
        }

        private async Task SaveReceivedMessageToDatabase(string xmlContent, string subject)
        {
            try
            {
                var messageRecord = new MessageModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = xmlContent,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "XML_FROM_TIBRV",
                    Source = subject
                };

                await _repository.InsertMessageAsync(messageRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving message to database: {ex.Message}");
            }
        }
    }
}