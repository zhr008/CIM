using System.ServiceModel;
using WCFServices.Contracts;
using WCFServices.Database;
using WCFServices.Services;

namespace WCFServices.Implementations
{
    [ServiceBehavior]
    public class WcfServiceImpl : IWcfService
    {
        private readonly IMessageService _messageService;
        private readonly IMssqlRepository _repository;

        public WcfServiceImpl(IMessageService messageService, IMssqlRepository repository)
        {
            _messageService = messageService;
            _repository = repository;
        }

        public async Task<bool> SendMessageAsync(string message)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                var messageRecord = new MessageModel
                {
                    Id = messageId,
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "General"
                };

                await _repository.InsertMessageAsync(messageRecord);
                
                // Also publish to Tibrv if needed
                await _messageService.PublishMessageAsync(messageRecord);
                
                return true;
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Error in SendMessageAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetMessageAsync(string id)
        {
            try
            {
                var message = await _repository.GetMessageByIdAsync(id);
                return message?.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMessageAsync: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> ProcessXmlMessageAsync(string xmlContent)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                var messageRecord = new MessageModel
                {
                    Id = messageId,
                    Content = xmlContent,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "XML"
                };

                await _repository.InsertMessageAsync(messageRecord);
                
                // Process the XML message
                await _messageService.ProcessXmlMessageAsync(messageRecord);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessXmlMessageAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAllMessagesAsync()
        {
            try
            {
                var messages = await _repository.GetAllMessagesAsync();
                return messages.Select(m => m.Content).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllMessagesAsync: {ex.Message}");
                return new List<string>();
            }
        }
    }
}