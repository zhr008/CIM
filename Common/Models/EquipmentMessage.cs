using System;

namespace Common.Models
{
    /// <summary>
    /// 设备消息模型
    /// </summary>
    public class EquipmentMessage
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string EquipmentID { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string MessageContent { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息主题
        /// </summary>
        public string Subject { get; set; }
    }
}