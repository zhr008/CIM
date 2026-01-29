using WCFServices.Database;

namespace WCFServices.Services
{
    public interface IMessageService
    {
        Task<bool> PublishMessageAsync(MessageModel message);
        Task<bool> ProcessXmlMessageAsync(MessageModel message);
        Task StartListeningAsync();
        Task StopListeningAsync();
    }
}