using System.Data;

namespace WCFServices.Services
{
    public interface IOracleDataAccess
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null);
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object parameters = null);
        Task<int> ExecuteAsync(string sql, object parameters = null);
        Task<IDbTransaction> BeginTransactionAsync();
    }
}