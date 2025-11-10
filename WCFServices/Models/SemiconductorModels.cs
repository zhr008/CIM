namespace WCFServices.Models
{
    /// <summary>
    /// 晶圆批次信息
    /// </summary>
    public class WaferLot
    {
        public string LotId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int WaferCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public string CurrentEquipment { get; set; } = string.Empty;
        public string CurrentProcess { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class Equipment
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CurrentLotId { get; set; } = string.Empty;
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, object> ProcessData { get; set; } = new();
    }

    /// <summary>
    /// 工艺流程
    /// </summary>
    public class ProcessStep
    {
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public Dictionary<string, string> Recipe { get; set; } = new();
        public double TargetYield { get; set; }
    }

    /// <summary>
    /// 生产数据
    /// </summary>
    public class ProductionData
    {
        public string LotId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string ProcessStepId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, double> Measurements { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 报警信息
    /// </summary>
    public class Alarm
    {
        public string AlarmId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string LotId { get; set; } = string.Empty;
        public string AlarmCode { get; set; } = string.Empty;
        public string AlarmMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime OccurTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批次追踪
    /// </summary>
    public class LotTracking
    {
        public string LotId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string ProcessStepId { get; set; } = string.Empty;
        public DateTime InTime { get; set; }
        public DateTime? OutTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 质量数据
    /// </summary>
    public class QualityData
    {
        public string LotId { get; set; } = string.Empty;
        public string WaferId { get; set; } = string.Empty;
        public string TestId { get; set; } = string.Empty;
        public double Yield { get; set; }
        public Dictionary<string, double> TestResults { get; set; } = new();
        public DateTime TestTime { get; set; }
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// 操作员信息
    /// </summary>
    public class Operator
    {
        public string OperatorId { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    /// <summary>
    /// MES消息类型
    /// </summary>
    public enum MesMessageType
    {
        LotIn,              // 批次进入
        LotOut,             // 批次离开
        ProcessStart,       // 工艺开始
        ProcessEnd,         // 工艺结束
        Alarm,              // 报警
        QualityData,        // 质量数据
        EquipmentStatus,    // 设备状态
        LotTracking         // 批次追踪
    }

    /// <summary>
    /// 设备状态
    /// </summary>
    public enum EquipmentStatus
    {
        Idle,               // 空闲
        Running,            // 运行中
        Down,               // 故障
        Maintenance,        // 维护
        Setup               // 设置中
    }

    /// <summary>
    /// 批次状态
    /// </summary>
    public enum LotStatus
    {
        Created,            // 已创建
        InQueue,            // 排队中
        InProcess,          // 处理中
        Completed,          // 已完成
        Rework,             // 返工
        Hold,               // 暂停
        Scrapped            // 报废
    }
}
