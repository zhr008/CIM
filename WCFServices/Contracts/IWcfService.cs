using CoreWCF;
using System.ServiceModel;

namespace WCFServices.Contracts
{
    [ServiceContract]
    public interface IWcfService
    {
        [OperationContract]
        Task<bool> SendMessageAsync(string message);

        [OperationContract]
        Task<string> GetMessageAsync(string id);

        [OperationContract]
        Task<bool> ProcessXmlMessageAsync(string xmlContent);

        [OperationContract]
        Task<List<string>> GetAllMessagesAsync();
    }
}