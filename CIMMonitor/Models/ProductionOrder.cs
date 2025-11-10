namespace CIMMonitor.Models
{
    public class ProductionOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal CompletedQuantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LineId { get; set; } = string.Empty;
    }
}
