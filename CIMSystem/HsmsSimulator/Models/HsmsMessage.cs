using System;
using System.Collections.Generic;
using System.Text;

namespace HsmsSimulator.Models
{
    /// <summary>
    /// HSMS (High-Speed Message Specification) 消息类
    /// 参考HslCommunication的SECS GEM实现
    /// </summary>
    public class HsmsMessage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 会话ID - SECS/GEM兼容性：应使用有效值（非0）
        /// </summary>
        public int SessionId { get; set; } = 0x1234;

        /// <summary>
        /// 消息头
        /// </summary>
        public byte Header { get; set; }

        /// <summary>
        /// Stream (S)
        /// </summary>
        public ushort Stream { get; set; }

        /// <summary>
        /// Function (F)
        /// </summary>
        public byte Function { get; set; }

        /// <summary>
        /// 是否需要响应
        /// </summary>
        public bool RequireResponse { get; set; }

        /// <summary>
        /// 设备ID - SECS/GEM兼容性：应使用有效值（非0）
        /// </summary>
        public byte DeviceId { get; set; } = 1;

        /// <summary>
        /// 消息类型描述
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 发送者ID
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// 是否为用户交互发送的消息
        /// </summary>
        public bool IsUserInteractive { get; set; } = false;

        /// <summary>
        /// 发送者角色：区分服务端和客户端
        /// </summary>
        public SenderRole SenderRole { get; set; } = SenderRole.Unknown;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 是否为SECS-II格式数据
        /// </summary>
        public bool IsSecsFormat { get; set; } = false;

        /// <summary>
        /// 原始数据
        /// </summary>
        public byte[] RawData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 关联的消息ID（用于响应消息）
        /// </summary>
        public string? RelatedMessageId { get; set; }

        /// <summary>
        /// 消息方向
        /// </summary>
        public MessageDirection Direction { get; set; }

        /// <summary>
        /// 解析HSMS消息 - SECS/GEM兼容版本
        /// </summary>
        /// <param name="data">消息字节数据</param>
        /// <param name="senderId">发送者ID</param>
        /// <param name="senderRole">发送者角色</param>
        /// <returns>解析后的消息对象</returns>
        public static HsmsMessage Parse(byte[] data, string? senderId = null, SenderRole senderRole = SenderRole.Unknown)
        {
            var message = new HsmsMessage
            {
                RawData = data,
                SenderId = senderId ?? "Unknown",
                SenderRole = senderRole
            };

            if (data.Length >= 10)
            {
                // HSMS消息头解析（Big-Endian）
                message.Header = data[0];
                message.Stream = (ushort)(data[1] << 8 | data[2]);  // Big-Endian
                message.Function = data[3];
                message.RequireResponse = (data[4] & 0x80) != 0;
                message.DeviceId = data[5];
                message.SessionId = (data[6] << 8) | data[7];      // Big-Endian

                // 解析消息内容（支持SECS-II数据格式）
                if (data.Length > 10)
                {
                    var contentBytes = new byte[data.Length - 10];
                    Array.Copy(data, 10, contentBytes, 0, contentBytes.Length);

                    // 首先尝试解析为SECS-II数据
                    if (contentBytes.Length > 0)
                    {
                        var secsType = contentBytes[0];

                        if (SecsData.IsSecsType(secsType))
                        {
                            // 是SECS-II数据，尝试解码
                            try
                            {
                                int offset = 0;
                                var decoded = SecsData.DecodeItem(contentBytes, ref offset, out _);
                                message.Content = FormatDecodedContent(decoded);
                                message.IsSecsFormat = true;
                            }
                            catch (Exception)
                            {
                                // 解码失败，使用UTF-8解码作为后备
                                message.Content = Encoding.UTF8.GetString(contentBytes).Trim();
                                message.IsSecsFormat = false;
                            }
                        }
                        else
                        {
                            // 不是SECS-II数据，使用UTF-8解码
                            message.Content = Encoding.UTF8.GetString(contentBytes).Trim();
                            message.IsSecsFormat = false;
                        }
                    }
                }

                // 设置消息类型
                message.MessageType = GetMessageType(message.Stream, message.Function);
            }

            return message;
        }

        /// <summary>
        /// 解析HSMS消息（字符串内容版本，向后兼容）
        /// </summary>
        public static HsmsMessage ParseString(byte[] data, string? senderId = null)
        {
            var message = new HsmsMessage
            {
                RawData = data,
                SenderId = senderId ?? "Unknown"
            };

            if (data.Length >= 10)
            {
                // HSMS消息头解析
                message.Header = data[0];
                message.Stream = (ushort)(data[1] << 8 | data[2]);
                message.Function = data[3];
                message.RequireResponse = (data[4] & 0x80) != 0;
                message.DeviceId = data[5];
                message.SessionId = (data[6] << 8) | data[7];

                // 解析消息内容（使用UTF-8编码，支持中文）
                if (data.Length > 10)
                {
                    var contentBytes = new byte[data.Length - 10];
                    Array.Copy(data, 10, contentBytes, 0, contentBytes.Length);
                    message.Content = Encoding.UTF8.GetString(contentBytes).Trim();
                }

                // 设置消息类型
                message.MessageType = GetMessageType(message.Stream, message.Function);
            }

            return message;
        }

        /// <summary>
        /// 构建HSMS消息 - SECS/GEM兼容版本
        /// </summary>
        /// <param name="stream">Stream号码</param>
        /// <param name="function">Function号码</param>
        /// <param name="content">消息内容（字符串或SECS-II数据）</param>
        /// <param name="requireResponse">是否需要响应</param>
        /// <param name="deviceId">设备ID（默认1，符合SECS标准）</param>
        /// <param name="sessionId">会话ID（默认0x1234，符合SECS标准）</param>
        /// <returns>编码后的字节数组</returns>
        public static byte[] Build(ushort stream, byte function, object content,
            bool requireResponse = false, byte deviceId = 1, int sessionId = 0x1234)
        {
            // 编码消息内容（支持SECS-II数据格式）
            byte[] contentBytes;
            if (content is string strContent)
            {
                // 如果是字符串，使用UTF-8编码（支持中文）
                contentBytes = string.IsNullOrEmpty(strContent) ? Array.Empty<byte>()
                    : Encoding.UTF8.GetBytes(strContent);
            }
            else if (content is byte[] bytesContent)
            {
                // 如果是字节数组，直接使用
                contentBytes = bytesContent;
            }
            else if (content is List<object> secsList)
            {
                // SECS-II LIST格式
                contentBytes = SecsData.EncodeList(secsList);
            }
            else
            {
                // 其他类型，先转换为字符串，再转为UTF-8
                var text = content?.ToString() ?? "";
                contentBytes = Encoding.UTF8.GetBytes(text);
            }

            var data = new byte[10 + contentBytes.Length];

            // 消息头 - 使用Big-Endian编码
            data[0] = 0x00; // Header
            data[1] = (byte)(stream >> 8);       // Stream高位
            data[2] = (byte)(stream & 0xFF);     // Stream低位
            data[3] = function;
            data[4] = (byte)(requireResponse ? 0x80 : 0x00);
            data[5] = deviceId;                   // DeviceId
            data[6] = (byte)(sessionId >> 8);     // SessionId高位（Big-Endian）
            data[7] = (byte)(sessionId & 0xFF);   // SessionId低位（Big-Endian）
            data[8] = 0x00; // 系统保留
            data[9] = 0x00; // 系统保留

            // 消息内容
            if (contentBytes.Length > 0)
            {
                Array.Copy(contentBytes, 0, data, 10, contentBytes.Length);
            }

            return data;
        }

        /// <summary>
        /// 构建HSMS消息（字符串内容版本，向后兼容）
        /// </summary>
        public static byte[] Build(ushort stream, byte function, string content,
            bool requireResponse = false, byte deviceId = 1, int sessionId = 0x1234)
        {
            return Build(stream, function, (object)content, requireResponse, deviceId, sessionId);
        }

        /// <summary>
        /// 创建响应消息
        /// </summary>
        public HsmsMessage CreateResponse(string? content = null)
        {
            var responseStream = this.Stream;
            var responseFunction = (byte)(this.Function + 1);

            var response = new HsmsMessage
            {
                Stream = responseStream,
                Function = responseFunction,
                Content = content ?? this.Content,
                DeviceId = this.DeviceId,
                SessionId = this.SessionId,
                RequireResponse = false,
                RelatedMessageId = this.MessageId,
                SenderId = this.SenderId,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now
            };

            response.MessageType = GetMessageType(response.Stream, response.Function);
            return response;
        }

        /// <summary>
        /// 获取消息类型的描述
        /// </summary>
        private static string GetMessageType(ushort stream, byte function)
        {
            var messageTypes = new Dictionary<string, string>
            {
                { "S1F13", "Are You There" },
                { "S1F14", "I Am Here" },
                { "S1F15", "Are You There Request" },
                { "S1F16", "I Am Here Request" },
                { "S2F33", "Equipment Status Request" },
                { "S2F34", "Equipment Status Data" },
                { "S2F35", "Equipment Status Request" },
                { "S2F36", "Equipment Status Data" },
                { "S5F17", "Alarm Report Send" },
                { "S5F18", "Alarm Report Acknowledge" },
                { "S6F11", "Event Report Send" },
                { "S6F12", "Event Report Acknowledge" },
                { "S6F13", "Event Report Request" },
                { "S6F14", "Event Report Data" },
                { "S7F17", "Process Program Load" },
                { "S7F18", "Process Program Load Acknowledge" },
                { "S7F19", "Process Program" },
                { "S7F20", "Process Program Acknowledge" },
                { "S7F21", "Process Program Request" },
                { "S7F22", "Process Program" },
                { "S7F23", "Process Program" },
                { "S7F24", "Process Program Acknowledge" },
                { "S9F1", "Unrecognized Message Type" },
                { "S9F3", "Illegal Data" },
                { "S9F5", "Fragment Reassembly Sequence Error" },
                { "S9F7", "Fragment Length Error" }
            };

            var key = $"S{stream}F{function}";
            return messageTypes.ContainsKey(key) ? messageTypes[key] : key;
        }

        /// <summary>
        /// 格式化解码后的SECS-II数据为字符串
        /// </summary>
        /// <param name="decoded">解码后的数据对象</param>
        /// <returns>格式化的字符串</returns>
        private static string FormatDecodedContent(object decoded)
        {
            if (decoded == null)
                return "";

            if (decoded is List<object> list)
            {
                // 格式化LIST类型
                var result = new List<string>();
                foreach (var item in list)
                {
                    if (item is List<object> subList)
                    {
                        result.Add("[" + string.Join(", ", FormatDecodedContent(subList)) + "]");
                    }
                    else
                    {
                        result.Add(item.ToString() ?? "");
                    }
                }
                return "{" + string.Join("; ", result) + "}";
            }

            // 直接转换为字符串
            return decoded.ToString() ?? "";
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] S{Stream}F{Function} - {MessageType} - {Content}";
        }

        /// <summary>
        /// 获取HSMS消息的字节数据
        /// </summary>
        public byte[] ToBytes()
        {
            return Build(Stream, Function, Content, RequireResponse, DeviceId, SessionId);
        }
    }

    /// <summary>
    /// 消息方向
    /// </summary>
    public enum MessageDirection
    {
        Incoming,
        Outgoing
    }

    /// <summary>
    /// 发送者角色
    /// </summary>
    public enum SenderRole
    {
        Unknown,    // 未知
        Server,     // 服务端
        Client      // 客户端
    }

    /// <summary>
    /// 设备状态 - 扩展的设备状态枚举
    /// </summary>
    public enum DeviceStatus
    {
        Offline,     // 离线
        Online,      // 在线
        Idle,        // 空闲
        Running,     // 运行中
        Paused,      // 暂停
        Maintenance, // 维护
        Alarm,       // 报警
        Error,       // 错误
        Busy         // 忙碌（保留兼容）
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// HSMS消息扩展方法
    /// </summary>
    public static class HsmsMessageExtensions
    {
        /// <summary>
        /// 从消息内容中获取数据项值
        /// </summary>
        public static string GetDataItem(this HsmsMessage message, string itemName)
        {
            if (string.IsNullOrEmpty(message.Content))
                return string.Empty;

            var items = message.Content.Split(';');
            foreach (var item in items)
            {
                if (item.Contains('='))
                {
                    var parts = item.Split('=');
                    if (parts.Length == 2 && parts[0].Trim().Equals(itemName, StringComparison.OrdinalIgnoreCase))
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 解析消息内容中的所有数据项
        /// </summary>
        public static Dictionary<string, string> ParseDataItems(this HsmsMessage message)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(message.Content))
                return result;

            var items = message.Content.Split(';');
            foreach (var item in items)
            {
                if (item.Contains('='))
                {
                    var parts = item.Split('=');
                    if (parts.Length == 2)
                    {
                        result[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检查消息是否包含指定的数据项
        /// </summary>
        public static bool HasDataItem(this HsmsMessage message, string itemName)
        {
            return !string.IsNullOrEmpty(message.GetDataItem(itemName));
        }
    }
}
