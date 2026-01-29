using Common.Services;
using WCFServices.Database;
using TIBCO.Rendezvous;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections;

namespace WCFServices.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMssqlRepository _repository;
        private readonly ILogger<MessageService> _logger;
        private TibcoRV _tibcoRV;
        private string _ServiceName;
        private string _Network;
        private string _Daemon;
        private string _listenSubject;
        private string _targetSubject;

        public MessageService(IMssqlRepository repository, ILogger<MessageService> logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            
            // Load configuration for 
            _ServiceName = configuration.GetValue<string>("TibcoRV:Service") ?? "";
            _Network = configuration.GetValue<string>("TibcoRV:Network") ?? "";
            _Daemon = configuration.GetValue<string>("TibcoRV:Daemon") ?? "";
            _listenSubject = configuration.GetValue<string>(":ListenSubject") ?? "";
            _targetSubject = configuration.GetValue<string>(":TargetSubject") ?? "";

            // Initialize TibcoRV service
            _tibcoRV = new TibcoRV(_ServiceName, _Network, _Daemon, _listenSubject, _targetSubject);
            
            // Subscribe to events
            _tibcoRV.ErrorMessageHandler += OnErrorMessage;
            _tibcoRV.ConnectedStatusHandler += OnConnectedStatusChanged;
            _tibcoRV.ListenedStatusHandler += OnListenedStatusChanged;
            _tibcoRV.messageReceivedHandler += OnMessageReceived;
        }

        public async Task StartListeningAsync()
        {
            try
            {
                _logger.LogInformation("Starting to listen for TIBRV messages...");

                // Initialize the TIBRV environment
                var openResult = await Task.Run(() => _tibcoRV.Open());
                if (!openResult)
                {
                    _logger.LogError("Failed to open TIBRV environment");
                    return;
                }
                _tibcoRV.messageReceivedHandler += OnMessageReceived;

                // Connect to TIBRV
                await Task.Run(() => _tibcoRV.StartConnect());

                _logger.LogInformation("TIBRV listener started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting TIBRV listener: {ex.Message}");
            }
        }

        public async Task StopListeningAsync()
        {
            try
            {
                _logger.LogInformation("Stopping TIBRV listener...");
                await Task.Run(() => _tibcoRV.DisConnected());
                _logger.LogInformation("TIBRV listener stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping TIBRV listener: {ex.Message}");
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Received message from TIBRV on subject: {e.Message.SendSubject}");

                // Extract the XML content from the message
                string xmlContent = string.Empty;

                // Try to find the XML content in the message fields --zhr
                //if (e.Message.Fields.Contains("XMLContent"))
                //{
                //    xmlContent = e.Message.Fields["XMLContent"].ToString();
                //}
                //else if (e.Message.Fields.Contains("Data"))
                //{
                //    xmlContent = e.Message.Fields["Data"].ToString();
                //}
                //else
                //{
                //    // If no specific field found, try to get the first field
                //    foreach (DictionaryEntry field in e.Message.Fields)
                //    {
                //        if (field.Value is string strVal && (strVal.StartsWith("<?xml") || (strVal.Contains("<") && strVal.Contains(">"))))
                //        {
                //            xmlContent = strVal;
                //            break;
                //        }
                //    }
                //}

                if (!string.IsNullOrEmpty(xmlContent))
                {
                    // Create a message record
                    var messageRecord = new MessageModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = xmlContent,
                        Timestamp = DateTime.UtcNow,
                        MessageType = "XML",
                        Source = $"TIBRV_{e.Message.SendSubject}"
                    };

                    // Process the received XML message
                    _ = Task.Run(async () =>
                    {
                        await ProcessXmlMessageAsync(messageRecord);

                        // Push the received XML message to the WCF service
                        await PushXmlToWcfService(xmlContent);
                    });

                    _logger.LogInformation($"Processed received XML message with ID: {messageRecord.Id}");
                }
                else
                {
                    _logger.LogWarning($"Received message from TIBRV but no XML content found in fields: {string.Join(", ", e.Message)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling received message: {ex.Message}");
            }
        }

        private void OnErrorMessage(object sender, string errorMessage)
        {
            _logger.LogError($"TIBRV Error: {errorMessage}");
        }

        private void OnConnectedStatusChanged(object sender, bool isConnected)
        {
            _logger.LogInformation($"TIBRV Connection Status: {(isConnected ? "Connected" : "Disconnected")}");
        }

        private void OnListenedStatusChanged(object sender, bool isListened)
        {
            _logger.LogInformation($"TIBRV Listen Status: {(isListened ? "Listening" : "Not Listening")}");
        }

        public async Task<bool> PublishMessageAsync(MessageModel message)
        {
            try
            {
                if (_tibcoRV.IsConnected)
                {
                    await _tibcoRV.SendXmlMessageAsync(_targetSubject, message.Content);
                    _logger.LogInformation($"Published message to TIBRV: {message.Id}");
                    return true;
                }
                else
                {
                    _logger.LogWarning("TIBRV service is not connected. Cannot publish message.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing message to TIBRV: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProcessXmlMessageAsync(MessageModel message)
        {
            try
            {
                // Process XML message - could involve validation, transformation, etc.
                _logger.LogInformation($"Processing XML message: {message.Id}");

                // Store the processed message in database
                await _repository.InsertMessageAsync(message);

                // Optionally forward to other systems via TIBRV
                await PublishMessageAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing XML message: {ex.Message}");
                return false;
            }
        }

        private async Task PushXmlToWcfService(string xmlContent)
        {
            try
            {
                // In a real implementation, this would call the WCF service to process the XML
                // For now, we'll just log that the push was attempted
                _logger.LogInformation($"Pushed XML message to WCF service: {xmlContent.Substring(0, Math.Min(100, xmlContent.Length))}...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pushing XML to WCF service: {ex.Message}");
            }
        }


        public void Dispose()
        {
            _tibcoRV?.DisConnected();
            _tibcoRV?.Dispose();
        }
    }
}