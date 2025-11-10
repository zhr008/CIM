namespace CIMMonitor.Models
{
    /// <summary>
    /// 设备状态模型
    /// </summary>
    public class DeviceStatus
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsOnline
        {
            get => IsConnected;
            set => IsConnected = value;
        }
        public DateTime? LastConnectionTime { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public int HeartbeatCount { get; set; }
        public int MessageCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ConnectionQuality { get; set; } = string.Empty;
        public int ResponseTimeMs { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }

        // 为向后兼容提供int类型的DeviceId属性
        public int IntDeviceId
        {
            get
            {
                if (int.TryParse(DeviceId, out int result))
                    return result;
                return 0;
            }
            set => DeviceId = value.ToString();
        }
    }
}
