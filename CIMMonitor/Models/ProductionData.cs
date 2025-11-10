namespace CIMMonitor.Models
{
    public class ProductionData
    {
        public int Id { get; set; }
        public string LineId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double FlowRate { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
