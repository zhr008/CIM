using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace WCFServices.Services
{
    public class OracleDataAccess : IOracleDataAccess
    {
        private readonly string _connectionString;

        public OracleDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection") ?? 
                              configuration.GetValue<string>("OracleConfig:ConnectionString") ??
                              "Data Source=localhost:1521/XE;User Id=system;Password=oracle;Connection Timeout=30;";
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            using var connection = new OracleConnection(_connectionString);
            return await connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object parameters = null)
        {
            using var connection = new OracleConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            using var connection = new OracleConnection(_connectionString);
            return await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            var transaction = connection.BeginTransaction();
            return transaction;
        }
    }
}