using System.ComponentModel.DataAnnotations;

namespace WCFServices.Database
{
    public class MessageModel
    {
        public string Id { get; set; } = string.Empty;
        
        public string Content { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public string MessageType { get; set; } = string.Empty;
        
        public string? Source { get; set; }
    }
}