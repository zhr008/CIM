using CoreWCF;
using System.Runtime.Serialization;

namespace WCFServices.Services
{
    /// <summary>
    /// MES系统WCF服务接口
    /// 用于接收TIBCO Rendezvous服务端的消息，进行业务处理
    /// </summary>
    [ServiceContract]
    public interface IMesService
    {
        /// <summary>
        /// 接收TIBCO消息并处理
        /// </summary>
        /// <param name="message">TIBCO消息</param>
        /// <returns>处理结果</returns>
        [OperationContract]
        Task<MesResponse> ProcessMessageAsync(MesMessage message);

        /// <summary>
        /// 获取设备状态
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <returns>设备信息</returns>
        [OperationContract]
        Task<EquipmentInfo> GetEquipmentStatusAsync(string equipmentId);

        /// <summary>
        /// 获取批次信息
        /// </summary>
        /// <param name="lotId">批次ID</param>
        /// <returns>批次信息</returns>
        [OperationContract]
        Task<LotInfo> GetLotInfoAsync(string lotId);

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <param name="status">状态</param>
        /// <param name="lotId">批次ID</param>
        /// <returns>更新结果</returns>
        [OperationContract]
        Task<UpdateResult> UpdateEquipmentStatusAsync(string equipmentId, string status, string lotId);

        /// <summary>
        /// 获取报警列表
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>报警列表</returns>
        [OperationContract]
        Task<List<AlarmInfo>> GetAlarmsAsync(string equipmentId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// 获取批次追踪信息
        /// </summary>
        /// <param name="lotId">批次ID</param>
        /// <returns>追踪信息列表</returns>
        [OperationContract]
        Task<List<LotTrackingInfo>> GetLotTrackingAsync(string lotId);

        /// <summary>
        /// 获取质量数据
        /// </summary>
        /// <param name="lotId">批次ID</param>
        /// <returns>质量数据</returns>
        [OperationContract]
        Task<QualityDataInfo> GetQualityDataAsync(string lotId);

        /// <summary>
        /// 发送响应消息
        /// </summary>
        /// <param name="response">响应消息</param>
        /// <returns>发送结果</returns>
        [OperationContract]
        Task<SendResult> SendResponseMessageAsync(ResponseMessage response);
    }

    #region 数据契约

    /// <summary>
    /// MES消息
    /// </summary>
    [DataContract]
    public class MesMessage
    {
        [DataMember]
        public string MessageId { get; set; } = string.Empty;

        [DataMember]
        public string Subject { get; set; } = string.Empty;

        [DataMember]
        public string MessageType { get; set; } = string.Empty;

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public Dictionary<string, object> Fields { get; set; } = new();

        [DataMember]
        public string CorrelationId { get; set; } = string.Empty;

        [DataMember]
        public string ReplySubject { get; set; } = string.Empty;
    }

    /// <summary>
    /// MES响应
    /// </summary>
    [DataContract]
    public class MesResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; } = string.Empty;

        [DataMember]
        public object? Data { get; set; }

        [DataMember]
        public string MessageId { get; set; } = string.Empty;

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    [DataContract]
    public class EquipmentInfo
    {
        [DataMember]
        public string EquipmentId { get; set; } = string.Empty;

        [DataMember]
        public string EquipmentName { get; set; } = string.Empty;

        [DataMember]
        public string EquipmentType { get; set; } = string.Empty;

        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public string CurrentLotId { get; set; } = string.Empty;

        [DataMember]
        public DateTime LastUpdateTime { get; set; }
    }

    /// <summary>
    /// 批次信息
    /// </summary>
    [DataContract]
    public class LotInfo
    {
        [DataMember]
        public string LotId { get; set; } = string.Empty;

        [DataMember]
        public string ProductId { get; set; } = string.Empty;

        [DataMember]
        public string ProductName { get; set; } = string.Empty;

        [DataMember]
        public int WaferCount { get; set; }

        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public string CurrentEquipment { get; set; } = string.Empty;

        [DataMember]
        public DateTime CreatedTime { get; set; }
    }

    /// <summary>
    /// 更新结果
    /// </summary>
    [DataContract]
    public class UpdateResult
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; } = string.Empty;

        [DataMember]
        public string UpdatedEntity { get; set; } = string.Empty;
    }

    /// <summary>
    /// 报警信息
    /// </summary>
    [DataContract]
    public class AlarmInfo
    {
        [DataMember]
        public string AlarmId { get; set; } = string.Empty;

        [DataMember]
        public string EquipmentId { get; set; } = string.Empty;

        [DataMember]
        public string LotId { get; set; } = string.Empty;

        [DataMember]
        public string AlarmCode { get; set; } = string.Empty;

        [DataMember]
        public string AlarmMessage { get; set; } = string.Empty;

        [DataMember]
        public string Severity { get; set; } = string.Empty;

        [DataMember]
        public DateTime OccurTime { get; set; }

        [DataMember]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批次追踪信息
    /// </summary>
    [DataContract]
    public class LotTrackingInfo
    {
        [DataMember]
        public string LotId { get; set; } = string.Empty;

        [DataMember]
        public string EquipmentId { get; set; } = string.Empty;

        [DataMember]
        public string ProcessStepId { get; set; } = string.Empty;

        [DataMember]
        public DateTime InTime { get; set; }

        [DataMember]
        public DateTime? OutTime { get; set; }

        [DataMember]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 质量数据信息
    /// </summary>
    [DataContract]
    public class QualityDataInfo
    {
        [DataMember]
        public string LotId { get; set; } = string.Empty;

        [DataMember]
        public double Yield { get; set; }

        [DataMember]
        public Dictionary<string, double> TestResults { get; set; } = new();

        [DataMember]
        public DateTime TestTime { get; set; }

        [DataMember]
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// 响应消息
    /// </summary>
    [DataContract]
    public class ResponseMessage
    {
        [DataMember]
        public string Subject { get; set; } = string.Empty;

        [DataMember]
        public string MessageType { get; set; } = string.Empty;

        [DataMember]
        public Dictionary<string, object> Fields { get; set; } = new();

        [DataMember]
        public string CorrelationId { get; set; } = string.Empty;

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 发送结果
    /// </summary>
    [DataContract]
    public class SendResult
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; } = string.Empty;

        [DataMember]
        public string Subject { get; set; } = string.Empty;
    }

    #endregion
}
