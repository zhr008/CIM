using System;
using HsmsSimulator.Models;

namespace CIMMonitor.Models
{
    /// <summary>
    /// 设备消息发送/接收日志条目
    /// </summary>
    public class MessageLogEntry
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public MessageDirection Direction { get; set; } = MessageDirection.Outgoing;
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public HsmsMessage? HsmsMessage { get; set; }
    }
}
