using CoreWCF;
using Microsoft.Extensions.Logging;
using WCFServices.Models;
using WCFServices.DataAccess;

namespace WCFServices.Services
{
    /// <summary>
    /// MES系统WCF服务实现
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class MesService : IMesService
    {
        private readonly IMesBusinessService _businessService;
        private readonly IOracleDataAccess _dataAccess;
        private readonly ITibcoMessageSender _messageSender;
        private readonly ILogger<MesService> _logger;

        public MesService(
            IMesBusinessService businessService,
            IOracleDataAccess dataAccess,
            ITibcoMessageSender messageSender,
            ILogger<MesService> logger)
        {
            _businessService = businessService;
            _dataAccess = dataAccess;
            _messageSender = messageSender;
            _logger = logger;
        }

        /// <summary>
        /// 接收TIBCO消息并处理
        /// </summary>
        public async Task<MesResponse> ProcessMessageAsync(MesMessage message)
        {
            try
            {
                _logger.LogInformation($"WCF收到消息: {message.MessageType}, Subject: {message.Subject}");

                // 转换为内部消息模型
                var tibcoMessage = new TibcoMessage
                {
                    MessageId = message.MessageId,
                    Subject = message.Subject,
                    MessageType = message.MessageType,
                    Timestamp = message.Timestamp,
                    Fields = message.Fields,
                    CorrelationId = message.CorrelationId,
                    ReplySubject = message.ReplySubject
                };

                // 调用业务服务处理
                var success = await _businessService.ProcessMessageAsync(tibcoMessage);

                return new MesResponse
                {
                    Success = success,
                    Message = success ? "消息处理成功" : "消息处理失败",
                    Data = message,
                    MessageId = message.MessageId,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理消息失败");
                return new MesResponse
                {
                    Success = false,
                    Message = $"处理失败: {ex.Message}",
                    MessageId = message.MessageId,
                    Timestamp = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 获取设备状态
        /// </summary>
        public async Task<EquipmentInfo> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                _logger.LogInformation($"获取设备状态: {equipmentId}");

                var equipment = await _dataAccess.GetEquipmentByIdAsync(equipmentId);

                if (equipment != null)
                {
                    return new EquipmentInfo
                    {
                        EquipmentId = equipment.EquipmentId,
                        EquipmentName = equipment.EquipmentName,
                        EquipmentType = equipment.EquipmentType,
                        Status = equipment.Status,
                        CurrentLotId = equipment.CurrentLotId,
                        LastUpdateTime = equipment.LastUpdateTime
                    };
                }

                return new EquipmentInfo { EquipmentId = equipmentId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备状态失败: {equipmentId}");
                throw;
            }
        }

        /// <summary>
        /// 获取批次信息
        /// </summary>
        public async Task<LotInfo> GetLotInfoAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"获取批次信息: {lotId}");

                var lot = await _dataAccess.GetLotByIdAsync(lotId);

                if (lot != null)
                {
                    return new LotInfo
                    {
                        LotId = lot.LotId,
                        ProductId = lot.ProductId,
                        ProductName = lot.ProductName,
                        WaferCount = lot.WaferCount,
                        Status = lot.Status,
                        CurrentEquipment = lot.CurrentEquipment,
                        CreatedTime = lot.CreatedTime
                    };
                }

                return new LotInfo { LotId = lotId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取批次信息失败: {lotId}");
                throw;
            }
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        public async Task<UpdateResult> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId)
        {
            try
            {
                _logger.LogInformation($"更新设备状态: {equipmentId}, Status: {status}, Lot: {lotId}");

                var success = await _dataAccess.UpdateEquipmentStatusAsync(equipmentId, status, lotId);

                return new UpdateResult
                {
                    Success = success,
                    Message = success ? "更新成功" : "更新失败",
                    UpdatedEntity = $"Equipment: {equipmentId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备状态失败: {equipmentId}");
                return new UpdateResult
                {
                    Success = false,
                    Message = ex.Message,
                    UpdatedEntity = $"Equipment: {equipmentId}"
                };
            }
        }

        /// <summary>
        /// 获取报警列表
        /// </summary>
        public async Task<List<AlarmInfo>> GetAlarmsAsync(string equipmentId, DateTime startTime, DateTime endTime)
        {
            try
            {
                _logger.LogInformation($"获取报警: Equipment={equipmentId}, From={startTime}, To={endTime}");

                var alarms = await _dataAccess.GetAlarmsByEquipmentAsync(equipmentId, startTime, endTime);

                return alarms.Select(a => new AlarmInfo
                {
                    AlarmId = a.AlarmId,
                    EquipmentId = a.EquipmentId,
                    LotId = a.LotId,
                    AlarmCode = a.AlarmCode,
                    AlarmMessage = a.AlarmMessage,
                    Severity = a.Severity,
                    OccurTime = a.OccurTime,
                    Status = a.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取报警列表失败: {equipmentId}");
                throw;
            }
        }

        /// <summary>
        /// 获取批次追踪信息
        /// </summary>
        public async Task<List<LotTrackingInfo>> GetLotTrackingAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"获取批次追踪: {lotId}");

                var tracking = await _dataAccess.GetLotTrackingAsync(lotId);

                return tracking.Select(t => new LotTrackingInfo
                {
                    LotId = t.LotId,
                    EquipmentId = t.EquipmentId,
                    ProcessStepId = t.ProcessStepId,
                    InTime = t.InTime,
                    OutTime = t.OutTime,
                    Status = t.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取批次追踪失败: {lotId}");
                throw;
            }
        }

        /// <summary>
        /// 获取质量数据
        /// </summary>
        public async Task<QualityDataInfo> GetQualityDataAsync(string lotId)
        {
            try
            {
                _logger.LogInformation($"获取质量数据: {lotId}");

                var yieldReport = await _dataAccess.GetYieldReportAsync(lotId);

                return new QualityDataInfo
                {
                    LotId = lotId,
                    Yield = yieldReport.ContainsKey("YIELD") ? yieldReport["YIELD"] : 0,
                    TestResults = yieldReport,
                    TestTime = DateTime.Now,
                    Result = "OK"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取质量数据失败: {lotId}");
                throw;
            }
        }

        /// <summary>
        /// 发送响应消息
        /// </summary>
        public async Task<SendResult> SendResponseMessageAsync(ResponseMessage response)
        {
            try
            {
                _logger.LogInformation($"发送响应消息: Subject={response.Subject}, Type={response.MessageType}");

                var tibcoMessage = new TibcoMessage
                {
                    Subject = response.Subject,
                    MessageType = response.MessageType,
                    Fields = response.Fields,
                    CorrelationId = response.CorrelationId,
                    Timestamp = response.Timestamp
                };

                var success = await _messageSender.SendMessageAsync(tibcoMessage);

                return new SendResult
                {
                    Success = success,
                    Message = success ? "发送成功" : "发送失败",
                    Subject = response.Subject
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送响应消息失败");
                return new SendResult
                {
                    Success = false,
                    Message = ex.Message,
                    Subject = response.Subject
                };
            }
        }
    }
}
