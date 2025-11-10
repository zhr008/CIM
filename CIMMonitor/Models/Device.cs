namespace CIMMonitor.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
