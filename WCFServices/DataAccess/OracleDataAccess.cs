using Oracle.ManagedDataAccess.Client;
using System.Data;
using WCFServices.Models;

namespace WCFServices.DataAccess
{
    /// <summary>
    /// Oracle数据库连接配置
    /// </summary>
    public class OracleConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// 连接池最大大小
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 连接池最小大小
        /// </summary>
        public int MinPoolSize { get; set; } = 5;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;
    }

    /// <summary>
    /// Oracle数据访问层
    /// </summary>
    public interface IOracleDataAccess : IDisposable
    {
        Task<WaferLot?> GetLotByIdAsync(string lotId);
        Task<bool> InsertLotAsync(WaferLot lot);
        Task<bool> UpdateLotStatusAsync(string lotId, string status, string equipmentId);
        Task<List<WaferLot>> GetLotsByEquipmentAsync(string equipmentId);
        Task<bool> InsertProductionDataAsync(ProductionData data);
        Task<bool> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId);
        Task<Equipment?> GetEquipmentByIdAsync(string equipmentId);
        Task<bool> InsertAlarmAsync(Alarm alarm);
        Task<List<Alarm>> GetAlarmsByEquipmentAsync(string equipmentId, DateTime startTime, DateTime endTime);
        Task<bool> InsertLotTrackingAsync(LotTracking tracking);
        Task<List<LotTracking>> GetLotTrackingAsync(string lotId);
        Task<bool> InsertQualityDataAsync(QualityData qualityData);
        Task<Dictionary<string, double>> GetYieldReportAsync(string lotId);
    }

    /// <summary>
    /// Oracle数据访问实现
    /// </summary>
    public class OracleDataAccess : IOracleDataAccess
    {
        private readonly OracleConfig _config;
        private readonly SemaphoreSlim _connectionSemaphore;
        private OracleConnection? _cachedConnection;
        private readonly object _lockObject = new object();

        public OracleDataAccess(OracleConfig config)
        {
            _config = config;
            // 使用信号量限制并发连接数
            _connectionSemaphore = new SemaphoreSlim(
                _config.MaxPoolSize,
                _config.MaxPoolSize);
        }

        /// <summary>
        /// 获取数据库连接（支持连接池）
        /// </summary>
        private OracleConnection CreateConnection()
        {
            var connection = new OracleConnection(_config.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 获取连接（使用连接池）
        /// </summary>
        public async Task<OracleConnection> GetConnectionAsync()
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                var connection = CreateConnection();
                return connection;
            }
            catch
            {
                _connectionSemaphore.Release();
                throw;
            }
        }

        /// <summary>
        /// 释放连接回连接池
        /// </summary>
        private void ReleaseConnection(OracleConnection? connection)
        {
            if (connection != null)
            {
                try
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing connection: {ex.Message}");
                }
                finally
                {
                    connection.Dispose();
                    _connectionSemaphore.Release();
                }
            }
        }

        public async Task<WaferLot?> GetLotByIdAsync(string lotId)
        {
            OracleConnection? conn = null;
            try
            {
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT LOT_ID, PRODUCT_ID, PRODUCT_NAME, WAFER_COUNT, STATUS,
                             CREATED_TIME, CURRENT_EQUIPMENT, CURRENT_PROCESS
                      FROM MES_WAFER_LOTS WHERE LOT_ID = :lotId", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, lotId, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new WaferLot
                    {
                        LotId = reader.GetString(0),
                        ProductId = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        WaferCount = reader.GetInt32(3),
                        Status = reader.GetString(4),
                        CreatedTime = reader.GetDateTime(5),
                        CurrentEquipment = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        CurrentProcess = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLotByIdAsync: {ex.Message}");
                throw;
            }
            finally
            {
                ReleaseConnection(conn);
            }
        }

        public async Task<bool> InsertLotAsync(WaferLot lot)
        {
            OracleConnection? conn = null;
            try
            {
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"INSERT INTO MES_WAFER_LOTS
                      (LOT_ID, PRODUCT_ID, PRODUCT_NAME, WAFER_COUNT, STATUS, CREATED_TIME)
                      VALUES (:lotId, :productId, :productName, :waferCount, :status, :createdTime)", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, lot.LotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("productId", OracleDbType.Varchar2, lot.ProductId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("productName", OracleDbType.Varchar2, lot.ProductName, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("waferCount", OracleDbType.Int32, lot.WaferCount, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, lot.Status, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("createdTime", OracleDbType.TimeStamp, lot.CreatedTime, ParameterDirection.Input));

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertLotAsync: {ex.Message}");
                return false;
            }
            finally
            {
                ReleaseConnection(conn);
            }
        }

        public async Task<bool> UpdateLotStatusAsync(string lotId, string status, string equipmentId)
        {
            OracleConnection? conn = null;
            try
            {
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"UPDATE MES_WAFER_LOTS
                      SET STATUS = :status, CURRENT_EQUIPMENT = :equipmentId,
                          LAST_UPDATE_TIME = SYSTIMESTAMP
                      WHERE LOT_ID = :lotId", conn);

                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, status, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, equipmentId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, lotId, ParameterDirection.Input));

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateLotStatusAsync: {ex.Message}");
                return false;
            }
            finally
            {
                ReleaseConnection(conn);
            }
        }

        public async Task<List<WaferLot>> GetLotsByEquipmentAsync(string equipmentId)
        {
            var lots = new List<WaferLot>();
            OracleConnection? conn = null;
            try
            {
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT LOT_ID, PRODUCT_ID, PRODUCT_NAME, WAFER_COUNT, STATUS
                      FROM MES_WAFER_LOTS
                      WHERE CURRENT_EQUIPMENT = :equipmentId AND STATUS = 'IN_PROCESS'", conn);

                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, equipmentId, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    lots.Add(new WaferLot
                    {
                        LotId = reader.GetString(0),
                        ProductId = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        WaferCount = reader.GetInt32(3),
                        Status = reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLotsByEquipmentAsync: {ex.Message}");
            }
            finally
            {
                ReleaseConnection(conn);
            }
            return lots;
        }

        public async Task<bool> InsertProductionDataAsync(ProductionData data)
        {
            OracleConnection? conn = null;
            try
            {
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"INSERT INTO MES_PRODUCTION_DATA
                      (LOT_ID, EQUIPMENT_ID, PROCESS_STEP_ID, START_TIME, END_TIME, STATUS, RESULT, OPERATOR_ID)
                      VALUES (:lotId, :equipmentId, :processStepId, :startTime, :endTime, :status, :result, :operatorId)", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, data.LotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, data.EquipmentId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("processStepId", OracleDbType.Varchar2, data.ProcessStepId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("startTime", OracleDbType.TimeStamp, data.StartTime, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("endTime", OracleDbType.TimeStamp, data.EndTime ?? DateTime.Now, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, data.Status, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("result", OracleDbType.Varchar2, data.Result, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("operatorId", OracleDbType.Varchar2, data.OperatorId, ParameterDirection.Input));

                await cmd.ExecuteNonQueryAsync();

                // 插入测量数据
                foreach (var measurement in data.Measurements)
                {
                    using var cmdMeasure = new OracleCommand(
                        @"INSERT INTO MES_MEASUREMENTS
                          (DATA_ID, LOT_ID, PARAMETER_NAME, PARAMETER_VALUE, MEASUREMENT_TIME)
                          VALUES (SEQ_MES_DATA_ID.NEXTVAL, :lotId, :parameterName, :parameterValue, SYSTIMESTAMP)", conn);

                    cmdMeasure.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, data.LotId, ParameterDirection.Input));
                    cmdMeasure.Parameters.Add(new OracleParameter("parameterName", OracleDbType.Varchar2, measurement.Key, ParameterDirection.Input));
                    cmdMeasure.Parameters.Add(new OracleParameter("parameterValue", OracleDbType.Decimal, measurement.Value, ParameterDirection.Input));

                    await cmdMeasure.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertProductionDataAsync: {ex.Message}");
                return false;
            }
            finally
            {
                ReleaseConnection(conn);
            }
        }

        public async Task<bool> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId)
        {
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"UPDATE MES_EQUIPMENT
                      SET STATUS = :status, CURRENT_LOT_ID = :lotId, LAST_UPDATE_TIME = SYSTIMESTAMP
                      WHERE EQUIPMENT_ID = :equipmentId", conn);

                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, status, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, string.IsNullOrEmpty(lotId) ? "" : lotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, equipmentId, ParameterDirection.Input));

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateEquipmentStatusAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(string equipmentId)
        {
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT EQUIPMENT_ID, EQUIPMENT_NAME, EQUIPMENT_TYPE, STATUS, CURRENT_LOT_ID, LAST_UPDATE_TIME
                      FROM MES_EQUIPMENT WHERE EQUIPMENT_ID = :equipmentId", conn);

                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, equipmentId, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Equipment
                    {
                        EquipmentId = reader.GetString(0),
                        EquipmentName = reader.GetString(1),
                        EquipmentType = reader.GetString(2),
                        Status = reader.GetString(3),
                        CurrentLotId = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        LastUpdateTime = reader.GetDateTime(5)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEquipmentByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> InsertAlarmAsync(Alarm alarm)
        {
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"INSERT INTO MES_ALARMS
                      (ALARM_ID, EQUIPMENT_ID, LOT_ID, ALARM_CODE, ALARM_MESSAGE, SEVERITY, OCCUR_TIME, STATUS)
                      VALUES (:alarmId, :equipmentId, :lotId, :alarmCode, :alarmMessage, :severity, :occurTime, :status)", conn);

                cmd.Parameters.Add(new OracleParameter("alarmId", OracleDbType.Varchar2, alarm.AlarmId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, alarm.EquipmentId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, alarm.LotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("alarmCode", OracleDbType.Varchar2, alarm.AlarmCode, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("alarmMessage", OracleDbType.Varchar2, alarm.AlarmMessage, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("severity", OracleDbType.Varchar2, alarm.Severity, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("occurTime", OracleDbType.TimeStamp, alarm.OccurTime, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, alarm.Status, ParameterDirection.Input));

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertAlarmAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Alarm>> GetAlarmsByEquipmentAsync(string equipmentId, DateTime startTime, DateTime endTime)
        {
            var alarms = new List<Alarm>();
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT ALARM_ID, EQUIPMENT_ID, LOT_ID, ALARM_CODE, ALARM_MESSAGE, SEVERITY, OCCUR_TIME, STATUS
                      FROM MES_ALARMS
                      WHERE EQUIPMENT_ID = :equipmentId AND OCCUR_TIME BETWEEN :startTime AND :endTime
                      ORDER BY OCCUR_TIME DESC", conn);

                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, equipmentId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("startTime", OracleDbType.TimeStamp, startTime, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("endTime", OracleDbType.TimeStamp, endTime, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    alarms.Add(new Alarm
                    {
                        AlarmId = reader.GetString(0),
                        EquipmentId = reader.GetString(1),
                        LotId = reader.GetString(2),
                        AlarmCode = reader.GetString(3),
                        AlarmMessage = reader.GetString(4),
                        Severity = reader.GetString(5),
                        OccurTime = reader.GetDateTime(6),
                        Status = reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAlarmsByEquipmentAsync: {ex.Message}");
            }
            return alarms;
        }

        public async Task<bool> InsertLotTrackingAsync(LotTracking tracking)
        {
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"INSERT INTO MES_LOT_TRACKING
                      (LOT_ID, EQUIPMENT_ID, PROCESS_STEP_ID, IN_TIME, OUT_TIME, STATUS)
                      VALUES (:lotId, :equipmentId, :processStepId, :inTime, :outTime, :status)", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, tracking.LotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("equipmentId", OracleDbType.Varchar2, tracking.EquipmentId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("processStepId", OracleDbType.Varchar2, tracking.ProcessStepId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("inTime", OracleDbType.TimeStamp, tracking.InTime, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("outTime", OracleDbType.TimeStamp, tracking.OutTime ?? DateTime.Now, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Varchar2, tracking.Status, ParameterDirection.Input));

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertLotTrackingAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<LotTracking>> GetLotTrackingAsync(string lotId)
        {
            var tracking = new List<LotTracking>();
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT LOT_ID, EQUIPMENT_ID, PROCESS_STEP_ID, IN_TIME, OUT_TIME, STATUS
                      FROM MES_LOT_TRACKING
                      WHERE LOT_ID = :lotId
                      ORDER BY IN_TIME", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, lotId, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tracking.Add(new LotTracking
                    {
                        LotId = reader.GetString(0),
                        EquipmentId = reader.GetString(1),
                        ProcessStepId = reader.GetString(2),
                        InTime = reader.GetDateTime(3),
                        OutTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        Status = reader.GetString(5)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLotTrackingAsync: {ex.Message}");
            }
            return tracking;
        }

        public async Task<bool> InsertQualityDataAsync(QualityData qualityData)
        {
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"INSERT INTO MES_QUALITY_DATA
                      (LOT_ID, WAFER_ID, TEST_ID, YIELD, TEST_TIME, RESULT)
                      VALUES (:lotId, :waferId, :testId, :yield, :testTime, :result)", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, qualityData.LotId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("waferId", OracleDbType.Varchar2, qualityData.WaferId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("testId", OracleDbType.Varchar2, qualityData.TestId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("yield", OracleDbType.Decimal, qualityData.Yield, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("testTime", OracleDbType.TimeStamp, qualityData.TestTime, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("result", OracleDbType.Varchar2, qualityData.Result, ParameterDirection.Input));

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertQualityDataAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, double>> GetYieldReportAsync(string lotId)
        {
            var report = new Dictionary<string, double>();
            try
            {
                OracleConnection? conn = null;
                conn = await GetConnectionAsync();
                using var cmd = new OracleCommand(
                    @"SELECT PARAMETER_NAME, AVG(PARAMETER_VALUE) as AVG_VALUE
                      FROM MES_MEASUREMENTS
                      WHERE LOT_ID = :lotId
                      GROUP BY PARAMETER_NAME", conn);

                cmd.Parameters.Add(new OracleParameter("lotId", OracleDbType.Varchar2, lotId, ParameterDirection.Input));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    report[reader.GetString(0)] = reader.GetDouble(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetYieldReportAsync: {ex.Message}");
            }
            return report;
        }

        public void Dispose()
        {
            // 关闭缓存连接
            if (_cachedConnection != null)
            {
                try
                {
                    if (_cachedConnection.State == ConnectionState.Open)
                    {
                        _cachedConnection.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing cached connection: {ex.Message}");
                }
                finally
                {
                    _cachedConnection?.Dispose();
                    _cachedConnection = null;
                }
            }

            // 释放信号量
            _connectionSemaphore?.Dispose();
        }
    }
}
