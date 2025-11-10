using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    [XmlRoot("Message")]
    public class XmlMessage
    {
        [XmlElement("Header")]
        public MessageHeader Header { get; set; } = new();

        [XmlElement("Body")]
        public MessageBody Body { get; set; } = new();

        [XmlAttribute("Version")]
        public string Version { get; set; } = "1.0";

        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(XmlMessage));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using var writer = new StringWriter();
            serializer.Serialize(writer, this, namespaces);
            return writer.ToString();
        }

        public static XmlMessage? FromXml(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(XmlMessage));
                using var reader = new StringReader(xml);
                return serializer.Deserialize(reader) as XmlMessage;
            }
            catch
            {
                return null;
            }
        }
    }

    public class MessageHeader
    {
        [XmlElement("MessageId")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        [XmlElement("MessageType")]
        public string MessageType { get; set; } = string.Empty;

        [XmlElement("DeviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [XmlElement("Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [XmlElement("SessionId")]
        public int SessionId { get; set; }

        [XmlElement("Command")]
        public string Command { get; set; } = string.Empty;
    }

    public class MessageBody
    {
        [XmlElement("Data")]
        public Dictionary<string, string> Data { get; set; } = new();

        [XmlElement("Parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new();

        [XmlElement("Result")]
        public string Result { get; set; } = string.Empty;

        [XmlElement("ErrorCode")]
        public string ErrorCode { get; set; } = string.Empty;

        [XmlElement("ErrorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public enum MessageType
    {
        Heartbeat,     // 心跳
        ReadData,      // 读取数据
        WriteData,     // 写入数据
        Command,       // 命令
        Response,      // 响应
        Alarm,         // 报警
        Status         // 状态
    }

    public enum CommandType
    {
        Start,         // 启动
        Stop,          // 停止
        Restart,       // 重启
        Read,          // 读取
        Write,         // 写入
        Reset          // 复位
    }
}
