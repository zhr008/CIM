using Dapper;
using Microsoft.Extensions.Configuration;

namespace WCFServices.Database
{
    public class MssqlRepository : IMssqlRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public MssqlRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new ArgumentNullException("DefaultConnection");
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            
            // Create table if not exists
            var createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Messages' AND xtype='U')
                BEGIN
                    CREATE TABLE Messages (
                        Id NVARCHAR(50) PRIMARY KEY,
                        Content NVARCHAR(MAX) NOT NULL,
                        Timestamp DATETIME2 NOT NULL,
                        MessageType NVARCHAR(50) NOT NULL,
                        Source NVARCHAR(100) NULL
                    );
                    
                    CREATE INDEX IX_Messages_Timestamp ON Messages(Timestamp);
                    CREATE INDEX IX_Messages_MessageType ON Messages(MessageType);
                END";
            
            await connection.ExecuteAsync(createTableSql);
        }

        public async Task<MessageModel?> GetMessageByIdAsync(string id)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            var sql = "SELECT * FROM Messages WHERE Id = @Id";
            var message = await connection.QueryFirstOrDefaultAsync<MessageModel>(sql, new { Id = id });
            return message;
        }

        public async Task<IEnumerable<MessageModel>> GetAllMessagesAsync()
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            var sql = "SELECT * FROM Messages ORDER BY Timestamp DESC";
            return await connection.QueryAsync<MessageModel>(sql);
        }

        public async Task<bool> InsertMessageAsync(MessageModel message)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            var sql = @"INSERT INTO Messages (Id, Content, Timestamp, MessageType, Source) 
                        VALUES (@Id, @Content, @Timestamp, @MessageType, @Source)";
            
            var affectedRows = await connection.ExecuteAsync(sql, message);
            return affectedRows > 0;
        }

        public async Task<bool> UpdateMessageAsync(MessageModel message)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            var sql = @"UPDATE Messages 
                        SET Content = @Content, Timestamp = @Timestamp, MessageType = @MessageType, Source = @Source
                        WHERE Id = @Id";
            
            var affectedRows = await connection.ExecuteAsync(sql, message);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteMessageAsync(string id)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
            var sql = "DELETE FROM Messages WHERE Id = @Id";
            
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }
    }
}