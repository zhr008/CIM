using System;

namespace HsmsSimulator.Models
{
    /// <summary>
    /// 设备参数类
    /// 用于监控和管理设备的实时参数
    /// </summary>
    public class DeviceParameter
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 参数单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        /// <summary>
        /// 参数状态
        /// </summary>
        public ParameterStatus Status { get; set; } = ParameterStatus.Normal;

        /// <summary>
        /// 正常范围下限
        /// </summary>
        public double? NormalMin { get; set; }

        /// <summary>
        /// 正常范围上限
        /// </summary>
        public double? NormalMax { get; set; }

        /// <summary>
        /// 警告范围下限
        /// </summary>
        public double? WarningMin { get; set; }

        /// <summary>
        /// 警告范围上限
        /// </summary>
        public double? WarningMax { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否可写
        /// </summary>
        public bool Writable { get; set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceParameter()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceParameter(string name, string value, string unit, string description = "")
        {
            Name = name;
            Value = value;
            Unit = unit;
            Description = description;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 更新参数值并自动判断状态
        /// </summary>
        public void UpdateValue(string newValue)
        {
            Value = newValue;
            LastUpdate = DateTime.Now;

            // 尝试解析数值并自动判断状态
            if (double.TryParse(newValue, out double numericValue))
            {
                if (Status == ParameterStatus.Alarm)
                {
                    // 如果当前是报警状态，保持不变直到手动清除
                    return;
                }

                if ((NormalMin.HasValue && numericValue < NormalMin.Value) ||
                    (NormalMax.HasValue && numericValue > NormalMax.Value))
                {
                    Status = ParameterStatus.Normal;
                }
                else if ((WarningMin.HasValue && numericValue < WarningMin.Value) ||
                         (WarningMax.HasValue && numericValue > WarningMax.Value))
                {
                    Status = ParameterStatus.Warning;
                }
                else
                {
                    Status = ParameterStatus.Normal;
                }
            }
        }

        /// <summary>
        /// 设置报警状态
        /// </summary>
        public void SetAlarm()
        {
            Status = ParameterStatus.Alarm;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 清除报警状态
        /// </summary>
        public void ClearAlarm()
        {
            Status = ParameterStatus.Normal;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"{Name}: {Value} {Unit} [{Status}]";
        }
    }

    /// <summary>
    /// 参数状态枚举
    /// </summary>
    public enum ParameterStatus
    {
        Normal,   // 正常
        Warning,  // 警告
        Alarm     // 报警
    }
}
