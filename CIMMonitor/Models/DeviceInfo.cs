namespace CIMMonitor.Models
{
    public class DeviceInfo
    {
        public string ServerId { get; set; } = "";
        public string ServerName { get; set; } = "";
        public string ProtocolType { get; set; } = "";
        public string DeviceType { get; set; } = "";  // host/EQP
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool Enabled { get; set; }
        public bool IsOnline { get; set; }
        public int HeartbeatCount { get; set; }
        public int ResponseTimeMs { get; set; }
        public string ConnectionQuality { get; set; } = "";
        public string LastUpdate { get; set; } = "";
        public string SourceFile { get; set; } = "";
    }

}
