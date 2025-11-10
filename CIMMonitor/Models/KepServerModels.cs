namespace CIMMonitor.Models.KepServer
{
    /// <summary>
    /// KepServer配置根对象
    /// </summary>
    public class KepServerConfig
    {
        public string Version { get; set; } = "1.0";
        public string LastUpdated { get; set; } = string.Empty;
        public KepServerSettings KepServerSettings { get; set; } = new();
        public List<KepServer> Servers { get; set; } = new();
        public DataInteraction DataInteraction { get; set; } = new();
        public AlertSettings Alerts { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
    }

    /// <summary>
    /// KepServer设置
    /// </summary>
    public class KepServerSettings
    {
        public int ConnectionTimeout { get; set; } = 5000;
        public int ReconnectInterval { get; set; } = 10000;
        public int MaxRetries { get; set; } = 3;
        public int UpdateRate { get; set; } = 100;
        public bool EnableLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Info";
    }

    /// <summary>
    /// KepServer实例
    /// </summary>
    public class KepServer
    {
        public string ServerId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 49320;
        public string ProtocolType { get; set; } = "opc"; // 协议类型：opc/hsms
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public List<Project> Projects { get; set; } = new();
        public string ConnectionStatus { get; set; } = "Disconnected";
        public DateTime? LastConnected { get; set; }
        public int ErrorCount { get; set; } = 0;
    }

    /// <summary>
    /// 项目
    /// </summary>
    public class Project
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public List<DataGroup> DataGroups { get; set; } = new();
    }

    /// <summary>
    /// 数据组
    /// </summary>
    public class DataGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int UpdateRate { get; set; } = 500;
        public List<BitAddress> BitAddresses { get; set; } = new();
        public List<WordAddress> WordAddresses { get; set; } = new();
        public List<BitWordMapping> BitWordMappings { get; set; } = new();
    }

    /// <summary>
    /// Bit地址（布尔触发器）
    /// </summary>
    public class BitAddress
    {
        public string AddressId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "Boolean";
        public bool InitialValue { get; set; } = false;
        public bool Monitored { get; set; } = true;
        public bool CurrentValue { get; set; } = false;
        public DateTime? LastChanged { get; set; }
        public bool PreviousValue { get; set; } = false;
        public string Status { get; set; } = "Normal";
    }

    /// <summary>
    /// Word地址（字符串数据）
    /// </summary>
    public class WordAddress
    {
        public string AddressId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "String";
        public string InitialValue { get; set; } = string.Empty;
        public int MaxLength { get; set; } = 100;
        public string CurrentValue { get; set; } = string.Empty;
        public DateTime? LastChanged { get; set; }
        public string PreviousValue { get; set; } = string.Empty;
        public string Status { get; set; } = "Normal";
    }

    /// <summary>
    /// Bit-Word映射关系
    /// </summary>
    public class BitWordMapping
    {
        public string MappingId { get; set; } = string.Empty;
        public string BitAddressId { get; set; } = string.Empty;
        public string WordAddressId { get; set; } = string.Empty;
        public string TriggerCondition { get; set; } = "RisingEdge";
        public string Action { get; set; } = "ReadWord";
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string Status { get; set; } = "Active";
        public DateTime? LastTriggered { get; set; }
    }

    /// <summary>
    /// 数据交互配置
    /// </summary>
    public class DataInteraction
    {
        public bool Enabled { get; set; } = true;
        public string Mode { get; set; } = "Async";
        public int BatchSize { get; set; } = 10;
        public int BufferSize { get; set; } = 1000;
        public List<PublishEvent> PublishEvents { get; set; } = new();
        public HistoryLog HistoryLog { get; set; } = new();
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public class PublishEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PublishTopic { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 历史日志
    /// </summary>
    public class HistoryLog
    {
        public bool Enabled { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
        public string FilePath { get; set; } = "logs/kepserver_history.log";
    }

    /// <summary>
    /// 报警设置
    /// </summary>
    public class AlertSettings
    {
        public bool Enabled { get; set; } = true;
        public AlertRule ConnectionLost { get; set; } = new();
        public AlertRule DataTimeout { get; set; } = new();
        public AlertRule MappingError { get; set; } = new();
    }

    /// <summary>
    /// 报警规则
    /// </summary>
    public class AlertRule
    {
        public bool Enabled { get; set; } = true;
        public int Threshold { get; set; } = 3;
        public string Action { get; set; } = "Log";
    }

    /// <summary>
    /// 安全设置
    /// </summary>
    public class SecuritySettings
    {
        public EncryptionSettings Encryption { get; set; } = new();
        public AuthenticationSettings Authentication { get; set; } = new();
        public AccessControlSettings AccessControl { get; set; } = new();
    }

    /// <summary>
    /// 加密设置
    /// </summary>
    public class EncryptionSettings
    {
        public bool Enabled { get; set; } = false;
        public string Algorithm { get; set; } = "AES256";
    }

    /// <summary>
    /// 认证设置
    /// </summary>
    public class AuthenticationSettings
    {
        public bool Enabled { get; set; } = false;
        public string Method { get; set; } = "None";
    }

    /// <summary>
    /// 访问控制设置
    /// </summary>
    public class AccessControlSettings
    {
        public bool Enabled { get; set; } = false;
        public List<object> Users { get; set; } = new();
    }

    /// <summary>
    /// 触发条件类型
    /// </summary>
    public enum TriggerCondition
    {
        RisingEdge,    // 上升沿：false -> true
        FallingEdge,   // 下降沿：true -> false
        BothEdges,     // 任意变化
        LevelHigh,     // 高电平
        LevelLow       // 低电平
    }

    /// <summary>
    /// 操作类型
    /// </summary>
    public enum ActionType
    {
        ReadWord,      // 读取Word值
        WriteWord,     // 写入Word值
        Publish,       // 发布消息
        Log            // 记录日志
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Reconnecting
    }

    /// <summary>
    /// 地址状态
    /// </summary>
    public enum AddressStatus
    {
        Normal,
        Error,
        Timeout,
        Disabled
    }

    /// <summary>
    /// 数据变化事件
    /// </summary>
    public class DataChangedEvent
    {
        public string ServerId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string AddressId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ChangeType { get; set; } = string.Empty; // BitChange, WordChange, MappingTrigger
    }

    /// <summary>
    /// 映射触发事件
    /// </summary>
    public class MappingTriggeredEvent
    {
        public string MappingId { get; set; } = string.Empty;
        public string BitAddressId { get; set; } = string.Empty;
        public string WordAddressId { get; set; } = string.Empty;
        public string TriggerCondition { get; set; } = string.Empty;
        public bool BitOldValue { get; set; }
        public bool BitNewValue { get; set; }
        public string WordValue { get; set; } = string.Empty;
        public DateTime TriggeredTime { get; set; } = DateTime.Now;
        public string ServerId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 监控统计信息
    /// </summary>
    public class MonitoringStatistics
    {
        public string ServerId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public int TotalBitChanges { get; set; } = 0;
        public int TotalWordChanges { get; set; } = 0;
        public int TotalMappingTriggers { get; set; } = 0;
        public int TotalErrors { get; set; } = 0;
        public int ConnectionLossCount { get; set; } = 0;
        public TimeSpan Uptime { get; set; } = TimeSpan.Zero;
    }
}
