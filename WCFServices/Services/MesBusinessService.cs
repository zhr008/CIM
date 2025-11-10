using WCFServices.Models;
using WCFServices.DataAccess;
using Microsoft.Extensions.Logging;

namespace WCFServices.Services
{
    /// <summary>
    /// MES业务处理服务
    /// 负责处理TIBCO消息、数据库操作和响应发送
    /// </summary>
    public interface IMesBusinessService
    {
        Task<bool> ProcessMessageAsync(TibcoMessage message);
        Task<TibcoMessage> ProcessLotInAsync(TibcoMessage message);
        Task<TibcoMessage> ProcessLotOutAsync(TibcoMessage message);
        Task<TibcoMessage> ProcessEquipmentStatusAsync(TibcoMessage message);
        Task<TibcoMessage> ProcessProcessDataAsync(TibcoMessage message);
        Task<TibcoMessage> ProcessAlarmAsync(TibcoMessage message);
    }

    public class MesBusinessService : IMesBusinessService
    {
        private readonly IOracleDataAccess _dataAccess;
        private readonly ITibcoMessageSender _messageSender;
        private readonly ILogger<MesBusinessService> _logger;

        public MesBusinessService(
            IOracleDataAccess dataAccess,
            ITibcoMessageSender messageSender,
            ILogger<MesBusinessService> logger)
        {
            _dataAccess = dataAccess;
            _messageSender = messageSender;
            _logger = logger;
        }

        /// <summary>
        /// 处理TIBCO消息
        /// </summary>
        public async Task<bool> ProcessMessageAsync(TibcoMessage message)
        {
            try
            {
                _logger.LogInformation($"收到消息: {message.MessageType}, Subject: {message.Subject}");

                TibcoMessage? response = message.MessageType.ToUpper() switch
                {
                    "LOT_IN" => await ProcessLotInAsync(message),
                    "LOT_OUT" => await ProcessLotOutAsync(message),
                    "EQUIPMENT_STATUS" => await ProcessEquipmentStatusAsync(message),
                    "PROCESS_DATA" => await ProcessProcessDataAsync(message),
                    "ALARM" => await ProcessAlarmAsync(message),
                    _ => null
                };

                if (response != null && !string.IsNullOrEmpty(response.Subject))
                {
                    _logger.LogInformation($"发送响应: {response.MessageType}");
                    await _messageSender.SendMessageAsync(response);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理消息失败: {message.MessageId}");
                return false;
            }
        }

        /// <summary>
        /// 处理批次进入
        /// </summary>
        public async Task<TibcoMessage> ProcessLotInAsync(TibcoMessage message)
        {
            try
            {
                var lotId = message.Fields.GetValueOrDefault("LOT_ID", "").ToString();
                var equipmentId = message.Fields.GetValueOrDefault("EQUIPMENT_ID", "").ToString();
                var processStepId = message.Fields.GetValueOrDefault("PROCESS_STEP_ID", "").ToString();

                _logger.LogInformation($"批次进入: LotId={lotId}, Equipment={equipmentId}, Step={processStepId}");

                // 更新批次状态
                var updateSuccess = await _dataAccess.UpdateLotStatusAsync(lotId, "IN_PROCESS", equipmentId);

                // 插入批次追踪记录
                var tracking = new LotTracking
                {
                    LotId = lotId,
                    EquipmentId = equipmentId,
                    ProcessStepId = processStepId,
                    InTime = DateTime.Now,
                    Status = "IN"
                };
                await _dataAccess.InsertLotTrackingAsync(tracking);

                // 发送响应
                var response = TibcoMessageFactory.CreateResponseMessage(
                    message,
                    new { LotId = lotId, EquipmentId = equipmentId, Status = "IN_PROCESS" },
                    updateSuccess,
                    updateSuccess ? "" : "更新批次状态失败");

                _logger.LogInformation($"批次进入处理完成: {lotId}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批次进入失败");
                return TibcoMessageFactory.CreateResponseMessage(message, null, false, ex.Message);
            }
        }

        /// <summary>
        /// 处理批次离开
        /// </summary>
        public async Task<TibcoMessage> ProcessLotOutAsync(TibcoMessage message)
        {
            try
            {
                var lotId = message.Fields.GetValueOrDefault("LOT_ID", "").ToString();
                var equipmentId = message.Fields.GetValueOrDefault("EQUIPMENT_ID", "").ToString();
                var yield = Convert.ToDouble(message.Fields.GetValueOrDefault("YIELD", 0));

                _logger.LogInformation($"批次离开: LotId={lotId}, Equipment={equipmentId}, Yield={yield}%");

                // 更新批次状态为完成
                var updateSuccess = await _dataAccess.UpdateLotStatusAsync(lotId, "COMPLETED", "");

                // 更新批次追踪
                var tracking = new LotTracking
                {
                    LotId = lotId,
                    EquipmentId = equipmentId,
                    ProcessStepId = "",
                    InTime = DateTime.Now,
                    OutTime = DateTime.Now,
                    Status = "OUT"
                };
                await _dataAccess.InsertLotTrackingAsync(tracking);

                // 获取良率报告
                var yieldReport = await _dataAccess.GetYieldReportAsync(lotId);

                var response = TibcoMessageFactory.CreateResponseMessage(
                    message,
                    new
                    {
                        LotId = lotId,
                        Status = "COMPLETED",
                        Yield = yield,
                        YieldReport = yieldReport
                    },
                    updateSuccess,
                    updateSuccess ? "" : "更新批次状态失败");

                _logger.LogInformation($"批次离开处理完成: {lotId}, 良率: {yield}%");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批次离开失败");
                return TibcoMessageFactory.CreateResponseMessage(message, null, false, ex.Message);
            }
        }

        /// <summary>
        /// 处理设备状态更新
        /// </summary>
        public async Task<TibcoMessage> ProcessEquipmentStatusAsync(TibcoMessage message)
        {
            try
            {
                var equipmentId = message.Fields.GetValueOrDefault("EQUIPMENT_ID", "").ToString();
                var status = message.Fields.GetValueOrDefault("STATUS", "").ToString();
                var lotId = message.Fields.GetValueOrDefault("LOT_ID", "").ToString();

                _logger.LogInformation($"设备状态更新: Equipment={equipmentId}, Status={status}, Lot={lotId}");

                // 更新设备状态
                var updateSuccess = await _dataAccess.UpdateEquipmentStatusAsync(equipmentId, status, lotId);

                var response = TibcoMessageFactory.CreateResponseMessage(
                    message,
                    new { EquipmentId = equipmentId, Status = status },
                    updateSuccess,
                    updateSuccess ? "" : "更新设备状态失败");

                _logger.LogInformation($"设备状态更新完成: {equipmentId}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理设备状态更新失败");
                return TibcoMessageFactory.CreateResponseMessage(message, null, false, ex.Message);
            }
        }

        /// <summary>
        /// 处理工艺数据
        /// </summary>
        public async Task<TibcoMessage> ProcessProcessDataAsync(TibcoMessage message)
        {
            try
            {
                var lotId = message.Fields.GetValueOrDefault("LOT_ID", "").ToString();
                var equipmentId = message.Fields.GetValueOrDefault("EQUIPMENT_ID", "").ToString();
                var measurementsJson = message.Fields.GetValueOrDefault("MEASUREMENTS", "{}").ToString();

                _logger.LogInformation($"工艺数据: Lot={lotId}, Equipment={equipmentId}");

                // 解析测量数据
                var measurements = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(measurementsJson)
                    ?? new Dictionary<string, double>();

                // 创建生产数据记录
                var productionData = new ProductionData
                {
                    LotId = lotId,
                    EquipmentId = equipmentId,
                    ProcessStepId = "",
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    Measurements = measurements,
                    Status = "COMPLETED",
                    Result = "PASS",
                    OperatorId = "SYSTEM"
                };

                var insertSuccess = await _dataAccess.InsertProductionDataAsync(productionData);

                // 检查是否需要报警
                var alarms = await CheckAlarmConditionsAsync(equipmentId, measurements);

                var response = TibcoMessageFactory.CreateResponseMessage(
                    message,
                    new
                    {
                        LotId = lotId,
                        EquipmentId = equipmentId,
                        Measurements = measurements,
                        Alarms = alarms
                    },
                    insertSuccess,
                    insertSuccess ? "" : "插入工艺数据失败");

                _logger.LogInformation($"工艺数据处理完成: {lotId}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理工艺数据失败");
                return TibcoMessageFactory.CreateResponseMessage(message, null, false, ex.Message);
            }
        }

        /// <summary>
        /// 处理报警
        /// </summary>
        public async Task<TibcoMessage> ProcessAlarmAsync(TibcoMessage message)
        {
            try
            {
                var equipmentId = message.Fields.GetValueOrDefault("EQUIPMENT_ID", "").ToString();
                var lotId = message.Fields.GetValueOrDefault("LOT_ID", "").ToString();
                var alarmCode = message.Fields.GetValueOrDefault("ALARM_CODE", "").ToString();
                var alarmMessage = message.Fields.GetValueOrDefault("ALARM_MESSAGE", "").ToString();
                var severity = message.Fields.GetValueOrDefault("SEVERITY", "").ToString();

                _logger.LogWarning($"设备报警: Equipment={equipmentId}, Code={alarmCode}, Severity={severity}");

                // 创建报警记录
                var alarm = new Alarm
                {
                    AlarmId = Guid.NewGuid().ToString(),
                    EquipmentId = equipmentId,
                    LotId = lotId,
                    AlarmCode = alarmCode,
                    AlarmMessage = alarmMessage,
                    Severity = severity,
                    OccurTime = DateTime.Now,
                    Status = "ACTIVE"
                };

                var insertSuccess = await _dataAccess.InsertAlarmAsync(alarm);

                // 如果是严重报警，更新设备状态
                if (severity == "CRITICAL")
                {
                    await _dataAccess.UpdateEquipmentStatusAsync(equipmentId, "DOWN", "");
                }

                var response = TibcoMessageFactory.CreateResponseMessage(
                    message,
                    new
                    {
                        AlarmId = alarm.AlarmId,
                        EquipmentId = equipmentId,
                        Severity = severity,
                        ActionTaken = severity == "CRITICAL" ? "设备已停机" : "已记录报警"
                    },
                    insertSuccess,
                    insertSuccess ? "" : "插入报警失败");

                _logger.LogWarning($"报警处理完成: {alarm.AlarmId}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理报警失败");
                return TibcoMessageFactory.CreateResponseMessage(message, null, false, ex.Message);
            }
        }

        /// <summary>
        /// 检查报警条件
        /// </summary>
        private async Task<List<Alarm>> CheckAlarmConditionsAsync(string equipmentId, Dictionary<string, double> measurements)
        {
            var alarms = new List<Alarm>();

            // 示例：检查温度是否超限
            if (measurements.TryGetValue("Temperature", out var temp) && temp > 100.0)
            {
                var alarm = new Alarm
                {
                    AlarmId = Guid.NewGuid().ToString(),
                    EquipmentId = equipmentId,
                    LotId = "",
                    AlarmCode = "TEMP_OVER_LIMIT",
                    AlarmMessage = $"温度超限: {temp}°C",
                    Severity = "WARNING",
                    OccurTime = DateTime.Now,
                    Status = "ACTIVE"
                };
                await _dataAccess.InsertAlarmAsync(alarm);
                alarms.Add(alarm);
            }

            // 示例：检查压力是否超限
            if (measurements.TryGetValue("Pressure", out var pressure) && pressure > 10.0)
            {
                var alarm = new Alarm
                {
                    AlarmId = Guid.NewGuid().ToString(),
                    EquipmentId = equipmentId,
                    LotId = "",
                    AlarmCode = "PRESSURE_OVER_LIMIT",
                    AlarmMessage = $"压力超限: {pressure}Pa",
                    Severity = "WARNING",
                    OccurTime = DateTime.Now,
                    Status = "ACTIVE"
                };
                await _dataAccess.InsertAlarmAsync(alarm);
                alarms.Add(alarm);
            }

            return alarms;
        }
    }
}
