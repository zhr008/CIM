using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TIBCO.Rendezvous;
using WCFServices.Models;

namespace WCFServices.Services
{
    public class TibcoMessageListener : ITibcoMessageListener
    {
        private readonly IMesBusinessService _businessService;
        private readonly ILogger<TibcoMessageListener> _logger;

        public TibcoMessageListener(IMesBusinessService businessService, ILogger<TibcoMessageListener> logger)
        {
            _businessService = businessService;
            _logger = logger;
        }

        public async Task HandleMessageAsync(MesMessage message)
        {
            try
            {
                _logger.LogInformation($"Processing TIBCO message: {message.MessageId}, Subject: {message.Subject}");

                // 调用业务服务处理消息
                var response = await _businessService.ProcessTibcoMessageAsync(message);

                _logger.LogInformation($"Message processed successfully: {message.MessageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing TIBCO message: {message.MessageId}");
                throw;
            }
        }
    }
    
    public interface IMesBusinessService
    {
        Task<MesResponse> ProcessTibcoMessageAsync(MesMessage message);
        Task<EquipmentInfo> GetEquipmentStatusAsync(string equipmentId);
        Task<LotInfo> GetLotInfoAsync(string lotId);
        Task<UpdateResult> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId);
        Task<List<AlarmInfo>> GetAlarmsAsync(string equipmentId, DateTime startTime, DateTime endTime);
        Task<List<LotTrackingInfo>> GetLotTrackingAsync(string lotId);
        Task<QualityDataInfo> GetQualityDataAsync(string lotId);
        Task<SendResult> SendResponseMessageAsync(ResponseMessage response);
    }

    public class MesBusinessService : IMesBusinessService
    {
        private readonly IOracleDataAccess _dataAccess;
        private readonly ILogger<MesBusinessService> _logger;

        public MesBusinessService(IOracleDataAccess dataAccess, ILogger<MesBusinessService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        public async Task<MesResponse> ProcessTibcoMessageAsync(MesMessage message)
        {
            try
            {
                _logger.LogInformation($"Processing TIBCO message type: {message.MessageType} for subject: {message.Subject}");

                switch (message.MessageType.ToUpper())
                {
                    case "EQUIPMENT_STATUS":
                        return await ProcessEquipmentStatusMessage(message);
                    case "LOT_INFO":
                        return await ProcessLotInfoMessage(message);
                    case "ALARM_EVENT":
                        return await ProcessAlarmEventMessage(message);
                    case "QUALITY_DATA":
                        return await ProcessQualityDataMessage(message);
                    default:
                        return new MesResponse
                        {
                            Success = true,
                            Message = $"Unknown message type: {message.MessageType}",
                            MessageId = message.MessageId,
                            Timestamp = DateTime.UtcNow
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing TIBCO message: {message.MessageId}");
                return new MesResponse
                {
                    Success = false,
                    Message = ex.Message,
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<MesResponse> ProcessEquipmentStatusMessage(MesMessage message)
        {
            try
            {
                var equipmentId = message.Fields.ContainsKey("EquipmentId") ? message.Fields["EquipmentId"].ToString() : "";
                var status = message.Fields.ContainsKey("Status") ? message.Fields["Status"].ToString() : "";
                var lotId = message.Fields.ContainsKey("LotId") ? message.Fields["LotId"].ToString() : "";

                if (!string.IsNullOrEmpty(equipmentId))
                {
                    var updateResult = await UpdateEquipmentStatusAsync(equipmentId, status, lotId);
                    
                    return new MesResponse
                    {
                        Success = updateResult.Success,
                        Message = updateResult.Message,
                        Data = updateResult.UpdatedEntity,
                        MessageId = message.MessageId,
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new MesResponse
                {
                    Success = false,
                    Message = "Missing EquipmentId in message",
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing equipment status message: {message.MessageId}");
                throw;
            }
        }

        private async Task<MesResponse> ProcessLotInfoMessage(MesMessage message)
        {
            try
            {
                var lotId = message.Fields.ContainsKey("LotId") ? message.Fields["LotId"].ToString() : "";

                if (!string.IsNullOrEmpty(lotId))
                {
                    var lotInfo = await GetLotInfoAsync(lotId);
                    
                    return new MesResponse
                    {
                        Success = lotInfo != null,
                        Message = lotInfo != null ? "Success" : "Lot not found",
                        Data = lotInfo,
                        MessageId = message.MessageId,
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new MesResponse
                {
                    Success = false,
                    Message = "Missing LotId in message",
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing lot info message: {message.MessageId}");
                throw;
            }
        }

        private async Task<MesResponse> ProcessAlarmEventMessage(MesMessage message)
        {
            try
            {
                var equipmentId = message.Fields.ContainsKey("EquipmentId") ? message.Fields["EquipmentId"].ToString() : "";
                var alarmCode = message.Fields.ContainsKey("AlarmCode") ? message.Fields["AlarmCode"].ToString() : "";
                var alarmMessage = message.Fields.ContainsKey("AlarmMessage") ? message.Fields["AlarmMessage"].ToString() : "";
                var severity = message.Fields.ContainsKey("Severity") ? message.Fields["Severity"].ToString() : "WARNING";

                if (!string.IsNullOrEmpty(equipmentId) && !string.IsNullOrEmpty(alarmCode))
                {
                    // 保存报警信息到数据库
                    var sql = @"
                        INSERT INTO MES_ALARMS (ALARM_ID, EQUIPMENT_ID, ALARM_CODE, ALARM_MESSAGE, SEVERITY, OCCUR_TIME, STATUS)
                        VALUES (:alarmId, :equipmentId, :alarmCode, :alarmMessage, :severity, :occurTime, 'ACTIVE')";
                    
                    var parameters = new
                    {
                        alarmId = $"ALM_{DateTime.UtcNow:yyyyMMddHHmmss}_{equipmentId}",
                        equipmentId = equipmentId,
                        alarmCode = alarmCode,
                        alarmMessage = alarmMessage,
                        severity = severity,
                        occurTime = DateTime.UtcNow
                    };

                    var result = await _dataAccess.ExecuteAsync(sql, parameters);

                    return new MesResponse
                    {
                        Success = result > 0,
                        Message = result > 0 ? "Alarm logged successfully" : "Failed to log alarm",
                        MessageId = message.MessageId,
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new MesResponse
                {
                    Success = false,
                    Message = "Missing required fields in alarm message",
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing alarm event message: {message.MessageId}");
                throw;
            }
        }

        private async Task<MesResponse> ProcessQualityDataMessage(MesMessage message)
        {
            try
            {
                var lotId = message.Fields.ContainsKey("LotId") ? message.Fields["LotId"].ToString() : "";
                var yield = message.Fields.ContainsKey("Yield") ? Convert.ToDouble(message.Fields["Yield"]) : 0.0;
                var testId = message.Fields.ContainsKey("TestId") ? message.Fields["TestId"].ToString() : "DEFAULT_TEST";
                var result = message.Fields.ContainsKey("Result") ? message.Fields["Result"].ToString() : "UNKNOWN";

                if (!string.IsNullOrEmpty(lotId))
                {
                    // 保存质量数据到数据库
                    var sql = @"
                        INSERT INTO MES_QUALITY_DATA (QUALITY_ID, LOT_ID, TEST_ID, YIELD, TEST_TIME, RESULT)
                        VALUES (SEQ_MES_QUALITY_ID.NEXTVAL, :lotId, :testId, :yield, :testTime, :result)";
                    
                    var parameters = new
                    {
                        lotId = lotId,
                        testId = testId,
                        yield = yield,
                        testTime = DateTime.UtcNow,
                        result = result
                    };

                    var dbResult = await _dataAccess.ExecuteAsync(sql, parameters);

                    return new MesResponse
                    {
                        Success = dbResult > 0,
                        Message = dbResult > 0 ? "Quality data saved successfully" : "Failed to save quality data",
                        MessageId = message.MessageId,
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new MesResponse
                {
                    Success = false,
                    Message = "Missing LotId in quality data message",
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing quality data message: {message.MessageId}");
                throw;
            }
        }

        public async Task<EquipmentInfo> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        EQUIPMENT_ID as EquipmentId,
                        EQUIPMENT_NAME as EquipmentName,
                        EQUIPMENT_TYPE as EquipmentType,
                        STATUS as Status,
                        CURRENT_LOT_ID as CurrentLotId,
                        LAST_UPDATE_TIME as LastUpdateTime
                    FROM MES_EQUIPMENT 
                    WHERE EQUIPMENT_ID = :equipmentId";
                
                var result = await _dataAccess.QueryFirstOrDefaultAsync<EquipmentInfo>(sql, new { equipmentId });
                
                return result ?? new EquipmentInfo
                {
                    EquipmentId = equipmentId,
                    EquipmentName = "Unknown",
                    EquipmentType = "Unknown",
                    Status = "NOT_FOUND",
                    CurrentLotId = "",
                    LastUpdateTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting equipment status for: {equipmentId}");
                throw;
            }
        }

        public async Task<LotInfo> GetLotInfoAsync(string lotId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        LOT_ID as LotId,
                        PRODUCT_ID as ProductId,
                        PRODUCT_NAME as ProductName,
                        WAFER_COUNT as WaferCount,
                        STATUS as Status,
                        CURRENT_EQUIPMENT as CurrentEquipment,
                        CREATED_TIME as CreatedTime
                    FROM MES_WAFER_LOTS 
                    WHERE LOT_ID = :lotId";
                
                var result = await _dataAccess.QueryFirstOrDefaultAsync<LotInfo>(sql, new { lotId });
                
                return result ?? new LotInfo
                {
                    LotId = lotId,
                    ProductId = "Unknown",
                    ProductName = "Unknown",
                    WaferCount = 0,
                    Status = "NOT_FOUND",
                    CurrentEquipment = "",
                    CreatedTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lot info for: {lotId}");
                throw;
            }
        }

        public async Task<UpdateResult> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId)
        {
            try
            {
                // 更新设备状态
                var updateSql = @"
                    UPDATE MES_EQUIPMENT 
                    SET STATUS = :status, 
                        CURRENT_LOT_ID = :lotId, 
                        LAST_UPDATE_TIME = :lastUpdateTime 
                    WHERE EQUIPMENT_ID = :equipmentId";
                
                var parameters = new
                {
                    status = status,
                    lotId = lotId,
                    lastUpdateTime = DateTime.UtcNow,
                    equipmentId = equipmentId
                };

                var result = await _dataAccess.ExecuteAsync(updateSql, parameters);

                if (result > 0)
                {
                    // 记录设备状态变更历史
                    var historySql = @"
                        INSERT INTO MES_EQUIPMENT_HISTORY (HISTORY_ID, EQUIPMENT_ID, STATUS, LOT_ID, CHANGE_TIME)
                        VALUES (SEQ_MES_EQUIPMENT_HISTORY_ID.NEXTVAL, :equipmentId, :status, :lotId, :changeTime)";
                    
                    var historyParams = new
                    {
                        equipmentId = equipmentId,
                        status = status,
                        lotId = lotId,
                        changeTime = DateTime.UtcNow
                    };

                    await _dataAccess.ExecuteAsync(historySql, historyParams);
                }

                return new UpdateResult
                {
                    Success = result > 0,
                    Message = result > 0 ? "Equipment status updated successfully" : "No equipment found to update",
                    UpdatedEntity = equipmentId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating equipment status for: {equipmentId}");
                throw;
            }
        }

        public async Task<List<AlarmInfo>> GetAlarmsAsync(string equipmentId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var sql = @"
                    SELECT 
                        ALARM_ID as AlarmId,
                        EQUIPMENT_ID as EquipmentId,
                        LOT_ID as LotId,
                        ALARM_CODE as AlarmCode,
                        ALARM_MESSAGE as AlarmMessage,
                        SEVERITY as Severity,
                        OCCUR_TIME as OccurTime,
                        STATUS as Status
                    FROM MES_ALARMS 
                    WHERE EQUIPMENT_ID = :equipmentId 
                      AND OCCUR_TIME BETWEEN :startTime AND :endTime
                    ORDER BY OCCUR_TIME DESC";
                
                var parameters = new
                {
                    equipmentId = equipmentId,
                    startTime = startTime,
                    endTime = endTime
                };

                var result = await _dataAccess.QueryAsync<AlarmInfo>(sql, parameters);
                
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting alarms for equipment: {equipmentId}");
                throw;
            }
        }

        public async Task<List<LotTrackingInfo>> GetLotTrackingAsync(string lotId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        LOT_ID as LotId,
                        EQUIPMENT_ID as EquipmentId,
                        PROCESS_STEP_ID as ProcessStepId,
                        IN_TIME as InTime,
                        OUT_TIME as OutTime,
                        STATUS as Status
                    FROM MES_LOT_TRACKING 
                    WHERE LOT_ID = :lotId
                    ORDER BY IN_TIME ASC";
                
                var result = await _dataAccess.QueryAsync<LotTrackingInfo>(sql, new { lotId });
                
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lot tracking for: {lotId}");
                throw;
            }
        }

        public async Task<QualityDataInfo> GetQualityDataAsync(string lotId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        LOT_ID as LotId,
                        YIELD as Yield,
                        TEST_TIME as TestTime,
                        RESULT as Result
                    FROM MES_QUALITY_DATA 
                    WHERE LOT_ID = :lotId
                    ORDER BY TEST_TIME DESC";
                
                var result = await _dataAccess.QueryFirstOrDefaultAsync<QualityDataInfo>(sql, new { lotId });
                
                if (result != null)
                {
                    // 获取测试结果详情
                    var testResultsSql = @"
                        SELECT 
                            PARAMETER_NAME as ParameterName,
                            PARAMETER_VALUE as ParameterValue
                        FROM MES_MEASUREMENTS 
                        WHERE LOT_ID = :lotId";
                    
                    var testResults = await _dataAccess.QueryAsync<dynamic>(testResultsSql, new { lotId });
                    
                    result.TestResults = new Dictionary<string, double>();
                    foreach (var testResult in testResults)
                    {
                        if (testResult.PARAMETER_NAME != null && testResult.PARAMETER_VALUE != null)
                        {
                            result.TestResults[testResult.PARAMETER_NAME.ToString()] = Convert.ToDouble(testResult.PARAMETER_VALUE);
                        }
                    }
                }
                
                return result ?? new QualityDataInfo
                {
                    LotId = lotId,
                    Yield = 0,
                    TestResults = new Dictionary<string, double>(),
                    TestTime = DateTime.UtcNow,
                    Result = "NOT_FOUND"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting quality data for: {lotId}");
                throw;
            }
        }

        public async Task<SendResult> SendResponseMessageAsync(ResponseMessage response)
        {
            // 实际的消息发送逻辑会在TibcoAdapter中处理
            return new SendResult
            {
                Success = true,
                Message = "Response message queued for sending",
                Subject = response.Subject
            };
        }
    }
}