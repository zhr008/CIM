using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    [XmlRoot("HSMSMessage")]
    public class HSMSMessageXml
    {
        [XmlElement("Header")]
        public HSMSHeader Header { get; set; } = new();

        [XmlElement("Data")]
        public byte[]? Data { get; set; }

        [XmlElement("HexData")]
        public string? HexData { get; set; }

        [XmlElement("SessionId")]
        public int SessionId { get; set; }

        [XmlElement("Stream")]
        public int Stream { get; set; }

        [XmlElement("Function")]
        public int Function { get; set; }

        [XmlElement("IsReply")]
        public bool IsReply { get; set; }

        [XmlElement("ReplyTo")]
        public int ReplyTo { get; set; }

        [XmlElement("Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [XmlElement("IsValid")]
        public bool IsValid { get; set; }

        public string ToXml()
        {
            if (Data != null)
            {
                HexData = BitConverter.ToString(Data).Replace("-", "");
            }

            var serializer = new XmlSerializer(typeof(HSMSMessageXml));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using var writer = new StringWriter();
            serializer.Serialize(writer, this, namespaces);
            return writer.ToString();
        }

        public static HSMSMessageXml? FromXml(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(HSMSMessageXml));
                using var reader = new StringReader(xml);
                var msg = serializer.Deserialize(reader) as HSMSMessageXml;

                if (msg?.HexData != null)
                {
                    var hexBytes = msg.HexData.Split('-');
                    msg.Data = hexBytes.Select(b => Convert.ToByte(b, 16)).ToArray();
                }

                return msg;
            }
            catch
            {
                return null;
            }
        }

        public byte[] ToBytes()
        {
            var list = new List<byte>();

            // Header: 10 bytes
            list.AddRange(BitConverter.GetBytes((ushort)SessionId));
            list.Add((byte)Stream);
            list.Add((byte)Function);
            list.Add(IsReply ? (byte)1 : (byte)0);
            list.AddRange(BitConverter.GetBytes((ushort)ReplyTo));

            // Data
            if (Data != null)
            {
                list.AddRange(Data);
            }

            return list.ToArray();
        }

        public static HSMSMessageXml FromBytes(byte[] bytes)
        {
            var msg = new HSMSMessageXml();
            var reader = new BinaryReader(new MemoryStream(bytes));

            msg.SessionId = reader.ReadUInt16();
            msg.Stream = reader.ReadByte();
            msg.Function = reader.ReadByte();
            msg.IsReply = reader.ReadByte() == 1;
            msg.ReplyTo = reader.ReadUInt16();

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                msg.Data = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }

            msg.Timestamp = DateTime.Now;
            msg.IsValid = true;

            return msg;
        }
    }

    public class HSMSHeader
    {
        [XmlElement("MessageType")]
        public string MessageType { get; set; } = string.Empty;

        [XmlElement("DeviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [XmlElement("TransactionId")]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();

        [XmlElement("SystemBytes")]
        public byte[]? SystemBytes { get; set; }
    }
}
