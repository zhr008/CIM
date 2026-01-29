namespace WCFServices.Database
{
    public interface IMssqlRepository
    {
        Task<MessageModel?> GetMessageByIdAsync(string id);
        Task<IEnumerable<MessageModel>> GetAllMessagesAsync();
        Task<bool> InsertMessageAsync(MessageModel message);
        Task<bool> UpdateMessageAsync(MessageModel message);
        Task<bool> DeleteMessageAsync(string id);
        Task InitializeDatabaseAsync();
    }
}