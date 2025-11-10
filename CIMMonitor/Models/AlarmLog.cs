namespace CIMMonitor.Models
{
    public class AlarmLog
    {
        public int AlarmId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string AlarmType { get; set; } = string.Empty;
        public string AlarmLevel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime OccurTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
