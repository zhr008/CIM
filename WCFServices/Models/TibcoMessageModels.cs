using System.Text.Json;

namespace WCFServices.Models
{
    /// <summary>
    /// TIBCO Rendezvous消息模型
    /// </summary>
    public class TibcoMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string Subject { get; set; } = string.Empty;
        public string ReplySubject { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string MessageType { get; set; } = string.Empty;
        public Dictionary<string, object> Fields { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public int RetryCount { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public static TibcoMessage FromJson(string json)
        {
            return JsonSerializer.Deserialize<TibcoMessage>(json) ?? new TibcoMessage();
        }
    }

    /// <summary>
    /// TIBCO主题常量 (半导体行业标准)
    /// </summary>
    public static class TibcoSubjects
    {
        // 工厂主题
        public const string Fab = "FAB";

        // 设备主题
        public const string Equipment = "EQUIPMENT";
        public const string EquipmentStatus = "EQUIPMENT.STATUS";
        public const string EquipmentAlarm = "EQUIPMENT.ALARM";
        public const string EquipmentCommand = "EQUIPMENT.COMMAND";

        // 批次主题
        public const string Lot = "LOT";
        public const string LotTracking = "LOT.TRACKING";
        public const string LotIn = "LOT.IN";
        public const string LotOut = "LOT.OUT";

        // 工艺主题
        public const string Process = "PROCESS";
        public const string ProcessStart = "PROCESS.START";
        public const string ProcessEnd = "PROCESS.END";
        public const string ProcessData = "PROCESS.DATA";

        // 质量主题
        public const string Quality = "QUALITY";
        public const string QualityData = "QUALITY.DATA";
        public const string YieldReport = "QUALITY.YIELD";

        // 报警主题
        public const string Alarm = "ALARM";
        public const string AlarmNotify = "ALARM.NOTIFY";
        public const string AlarmClear = "ALARM.CLEAR";

        // 通用主题
        public const string Command = "COMMAND";
        public const string Response = "RESPONSE";
        public const string Status = "STATUS";
    }

    /// <summary>
    /// TIBCO消息工厂
    /// </summary>
    public static class TibcoMessageFactory
    {
        /// <summary>
        /// 创建设备状态消息
        /// </summary>
        public static TibcoMessage CreateEquipmentStatusMessage(string equipmentId, string status, string lotId = "")
        {
            return new TibcoMessage
            {
                Subject = $"{TibcoSubjects.EquipmentStatus}.{equipmentId}",
                MessageType = "EQUIPMENT_STATUS",
                Timestamp = DateTime.Now,
                Fields = new Dictionary<string, object>
                {
                    { "EQUIPMENT_ID", equipmentId },
                    { "STATUS", status },
                    { "LOT_ID", lotId },
                    { "TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <summary>
        /// 创建批次追踪消息
        /// </summary>
        public static TibcoMessage CreateLotTrackingMessage(string lotId, string equipmentId, string action, string processStepId = "")
        {
            return new TibcoMessage
            {
                Subject = $"{TibcoSubjects.LotTracking}.{lotId}",
                MessageType = "LOT_TRACKING",
                Timestamp = DateTime.Now,
                Fields = new Dictionary<string, object>
                {
                    { "LOT_ID", lotId },
                    { "EQUIPMENT_ID", equipmentId },
                    { "ACTION", action },
                    { "PROCESS_STEP_ID", processStepId },
                    { "TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <summary>
        /// 创建工艺数据消息
        /// </summary>
        public static TibcoMessage CreateProcessDataMessage(string lotId, string equipmentId, Dictionary<string, double> measurements)
        {
            return new TibcoMessage
            {
                Subject = $"{TibcoSubjects.ProcessData}.{equipmentId}",
                MessageType = "PROCESS_DATA",
                Timestamp = DateTime.Now,
                Fields = new Dictionary<string, object>
                {
                    { "LOT_ID", lotId },
                    { "EQUIPMENT_ID", equipmentId },
                    { "MEASUREMENTS", measurements },
                    { "TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <summary>
        /// 创建报警消息
        /// </summary>
        public static TibcoMessage CreateAlarmMessage(string equipmentId, string lotId, string alarmCode, string alarmMessage, string severity)
        {
            return new TibcoMessage
            {
                Subject = $"{TibcoSubjects.AlarmNotify}.{equipmentId}",
                MessageType = "ALARM",
                Timestamp = DateTime.Now,
                Fields = new Dictionary<string, object>
                {
                    { "EQUIPMENT_ID", equipmentId },
                    { "LOT_ID", lotId },
                    { "ALARM_CODE", alarmCode },
                    { "ALARM_MESSAGE", alarmMessage },
                    { "SEVERITY", severity },
                    { "TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <summary>
        /// 创建响应消息
        /// </summary>
        public static TibcoMessage CreateResponseMessage(TibcoMessage request, object result, bool success, string errorMessage = "")
        {
            return new TibcoMessage
            {
                Subject = request.ReplySubject,
                MessageType = "RESPONSE",
                Timestamp = DateTime.Now,
                CorrelationId = request.MessageId,
                Fields = new Dictionary<string, object>
                {
                    { "SUCCESS", success },
                    { "RESULT", result },
                    { "ERROR_MESSAGE", errorMessage },
                    { "REQUEST_MESSAGE_ID", request.MessageId },
                    { "TIMESTAMP", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }
    }

    /// <summary>
    /// TIBCO消息监听器接口
    /// </summary>
    public interface ITibcoMessageListener
    {
        void OnMessageReceived(TibcoMessage message);
        void OnError(Exception ex);
    }

    /// <summary>
    /// TIBCO消息发送器接口
    /// </summary>
    public interface ITibcoMessageSender
    {
        bool SendMessage(TibcoMessage message);
        Task<bool> SendMessageAsync(TibcoMessage message);
    }

    /// <summary>
    /// TIBCO连接状态
    /// </summary>
    public enum TibcoConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// TIBCO连接信息
    /// </summary>
    public class TibcoConnectionInfo
    {
        public string Service { get; set; } = "7500";
        public string Network { get; set; } = ";";
        public string Daemon { get; set; } = "tcp:7500";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
    }
}
