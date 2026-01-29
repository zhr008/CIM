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
                _logger.LogDebug($"Received Tibrv message on subject: {e.Msg.Subject}");

                // Extract XML content from the message
                string xmlContent = ExtractXmlFromTibrvMessage(e.Msg);
                
                if (!string.IsNullOrEmpty(xmlContent))
                {
                    // Log the received message
                    _logger.LogInformation($"Received XML from Tibrv: {xmlContent.Substring(0, Math.Min(200, xmlContent.Length))}...");

                    // Forward the XML message to the WCF service
                    await ForwardXmlToWcfService(xmlContent);
                    
                    // Optionally save to database
                    await SaveReceivedMessageToDatabase(xmlContent, e.Msg.Subject);
                }
                else
                {
                    _logger.LogWarning("No XML content found in received Tibrv message.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling Tibrv message: {ex.Message}");
            }
        }

        private string ExtractXmlFromTibrvMessage(TIBCO.Rendezvous.Message message)
        {
            string xmlContent = string.Empty;

            // Try to find XML content in various possible fields
            if (message.Fields.Contains("XMLContent"))
            {
                xmlContent = message.Fields["XMLContent"].ToString();
            }
            else if (message.Fields.Contains("Data"))
            {
                xmlContent = message.Fields["Data"].ToString();
            }
            else if (message.Fields.Contains("Message"))
            {
                xmlContent = message.Fields["Message"].ToString();
            }
            else
            {
                // Look for any field that contains XML-like content
                foreach (System.Collections.DictionaryEntry entry in message.Fields)
                {
                    var value = entry.Value?.ToString();
                    if (value != null && (value.TrimStart().StartsWith("<") && value.Contains(">")))
                    {
                        xmlContent = value;
                        break;
                    }
                }
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