using System;
using System.Collections.Generic;
using System.Text;

namespace HsmsSimulator.Models
{
    /// <summary>
    /// SECS-II 数据类型编码解码器
    /// 支持基础数据类型：LIST, ASCII
    /// </summary>
    public static class SecsData
    {
        #region SECS-II 数据类型常量

        /// <summary>
        /// LIST 类型 (0x01)
        /// </summary>
        public const byte TYPE_LIST = 0x01;

        /// <summary>
        /// BINARY 类型 (0x02)
        /// </summary>
        public const byte TYPE_BINARY = 0x02;

        /// <summary>
        /// BOOLEAN 类型 (0x03)
        /// </summary>
        public const byte TYPE_BOOLEAN = 0x03;

        /// <summary>
        /// ASCII 类型 (0x04)
        /// </summary>
        public const byte TYPE_ASCII = 0x04;

        /// <summary>
        /// JIS8 类型 (0x05)
        /// </summary>
        public const byte TYPE_JIS8 = 0x05;

        /// <summary>
        /// I1 类型 (0x11) - 1字节有符号整数
        /// </summary>
        public const byte TYPE_I1 = 0x11;

        /// <summary>
        /// I2 类型 (0x12) - 2字节有符号整数
        /// </summary>
        public const byte TYPE_I2 = 0x12;

        /// <summary>
        /// I4 类型 (0x14) - 4字节有符号整数
        /// </summary>
        public const byte TYPE_I4 = 0x14;

        /// <summary>
        /// I8 类型 (0x18) - 8字节有符号整数
        /// </summary>
        public const byte TYPE_I8 = 0x18;

        /// <summary>
        /// U1 类型 (0x21) - 1字节无符号整数
        /// </summary>
        public const byte TYPE_U1 = 0x21;

        /// <summary>
        /// U2 类型 (0x22) - 2字节无符号整数
        /// </summary>
        public const byte TYPE_U2 = 0x22;

        /// <summary>
        /// U4 类型 (0x24) - 4字节无符号整数
        /// </summary>
        public const byte TYPE_U4 = 0x24;

        /// <summary>
        /// U8 类型 (0x28) - 8字节无符号整数
        /// </summary>
        public const byte TYPE_U8 = 0x28;

        /// <summary>
        /// F4 类型 (0x34) - 4字节浮点数
        /// </summary>
        public const byte TYPE_F4 = 0x34;

        /// <summary>
        /// F8 类型 (0x38) - 8字节浮点数
        /// </summary>
        public const byte TYPE_F8 = 0x38;

        #endregion

        #region 基础编码方法

        /// <summary>
        /// 编码LIST类型
        /// </summary>
        /// <param name="items">数据项列表</param>
        /// <returns>编码后的字节数组</returns>
        public static byte[] EncodeList(List<object> items)
        {
            var result = new List<byte>();

            // 编码每个数据项
            foreach (var item in items)
            {
                result.AddRange(EncodeItem(item));
            }

            // 如果项目数为0，使用特殊格式
            if (items.Count == 0)
            {
                return new byte[] { TYPE_LIST, 0x00 };
            }

            // 正常格式：类型 + 长度 + 数据
            var length = result.Count;
            var header = EncodeLength(length);

            var buffer = new List<byte>();
            buffer.Add(TYPE_LIST);
            buffer.AddRange(header);
            buffer.AddRange(result);

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码ASCII字符串
        /// </summary>
        /// <param name="text">ASCII字符串</param>
        /// <returns>编码后的字节数组</returns>
        public static byte[] EncodeAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new byte[] { TYPE_ASCII, 0x00 };
            }

            // 使用ASCII编码（不是UTF-8，符合SECS标准）
            var asciiBytes = Encoding.ASCII.GetBytes(text);
            var length = asciiBytes.Length;

            var buffer = new List<byte>();
            buffer.Add(TYPE_ASCII);

            // 编码长度
            if (length < 256)
            {
                buffer.Add((byte)length);
            }
            else if (length < 65536)
            {
                // 2字节长度格式
                buffer.Add(0xFF);
                buffer.Add((byte)(length >> 8));
                buffer.Add((byte)(length & 0xFF));
            }
            else
            {
                throw new ArgumentException("ASCII text too long for SECS encoding");
            }

            buffer.AddRange(asciiBytes);

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码单个数据项
        /// </summary>
        /// <param name="item">数据项</param>
        /// <returns>编码后的字节数组</returns>
        public static byte[] EncodeItem(object item)
        {
            if (item == null)
            {
                return EncodeList(new List<object>()); // 空LIST
            }

            if (item is List<object> list)
            {
                return EncodeList(list);
            }

            if (item is string str)
            {
                return EncodeAscii(str);
            }

            if (item is int intVal)
            {
                return EncodeI4(intVal);
            }

            if (item is long longVal)
            {
                return EncodeI8(longVal);
            }

            if (item is bool boolVal)
            {
                return EncodeBoolean(boolVal);
            }

            if (item is byte byteVal)
            {
                return EncodeU1(byteVal);
            }

            if (item is double doubleVal)
            {
                return EncodeF8(doubleVal);
            }

            // 默认按字符串处理
            return EncodeAscii(item.ToString() ?? "");
        }

        /// <summary>
        /// 编码整数类型 (I4)
        /// </summary>
        public static byte[] EncodeI4(int value)
        {
            var buffer = new List<byte>();
            buffer.Add(TYPE_I4);

            // 编码长度
            buffer.Add(0x04); // 4字节长度

            // 使用Big-Endian编码
            buffer.Add((byte)((value >> 24) & 0xFF));
            buffer.Add((byte)((value >> 16) & 0xFF));
            buffer.Add((byte)((value >> 8) & 0xFF));
            buffer.Add((byte)(value & 0xFF));

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码长整数类型 (I8)
        /// </summary>
        public static byte[] EncodeI8(long value)
        {
            var buffer = new List<byte>();
            buffer.Add(TYPE_I8);

            // 编码长度
            buffer.Add(0x08); // 8字节长度

            // 使用Big-Endian编码
            buffer.Add((byte)((value >> 56) & 0xFF));
            buffer.Add((byte)((value >> 48) & 0xFF));
            buffer.Add((byte)((value >> 40) & 0xFF));
            buffer.Add((byte)((value >> 32) & 0xFF));
            buffer.Add((byte)((value >> 24) & 0xFF));
            buffer.Add((byte)((value >> 16) & 0xFF));
            buffer.Add((byte)((value >> 8) & 0xFF));
            buffer.Add((byte)(value & 0xFF));

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码无符号字节类型 (U1)
        /// </summary>
        public static byte[] EncodeU1(byte value)
        {
            var buffer = new List<byte>();
            buffer.Add(TYPE_U1);
            buffer.Add(0x01); // 1字节长度
            buffer.Add(value);

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码布尔类型
        /// </summary>
        public static byte[] EncodeBoolean(bool value)
        {
            var buffer = new List<byte>();
            buffer.Add(TYPE_BOOLEAN);
            buffer.Add(0x01); // 1字节长度
            buffer.Add(value ? (byte)0x01 : (byte)0x00);

            return buffer.ToArray();
        }

        /// <summary>
        /// 编码双精度浮点类型 (F8)
        /// </summary>
        public static byte[] EncodeF8(double value)
        {
            var buffer = new List<byte>();
            buffer.Add(TYPE_F8);
            buffer.Add(0x08); // 8字节长度

            var bytes = BitConverter.GetBytes(value);
            // 转换为Big-Endian
            Array.Reverse(bytes);
            buffer.AddRange(bytes);

            return buffer.ToArray();
        }

        #endregion

        #region 编码辅助方法

        /// <summary>
        /// 编码长度字段
        /// </summary>
        private static byte[] EncodeLength(int length)
        {
            if (length < 256)
            {
                return new byte[] { (byte)length };
            }
            else if (length < 65536)
            {
                return new byte[]
                {
                    0xFF,
                    (byte)(length >> 8),
                    (byte)(length & 0xFF)
                };
            }
            else
            {
                return new byte[]
                {
                    0xFF,
                    0xFF,
                    (byte)(length >> 16),
                    (byte)((length >> 8) & 0xFF),
                    (byte)(length & 0xFF)
                };
            }
        }

        #endregion

        #region 解码方法

        /// <summary>
        /// 解码数据项
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">解码长度</param>
        /// <returns>解码后的对象</returns>
        public static object DecodeItem(byte[] data, ref int offset, out int length)
        {
            if (offset >= data.Length)
            {
                throw new ArgumentException("Invalid offset");
            }

            byte type = data[offset++];
            length = 0;

            // 首先读取长度字段
            int itemLength = 0;
            if (type == TYPE_LIST)
            {
                itemLength = DecodeLength(data, ref offset);
            }
            else if (type == TYPE_ASCII)
            {
                itemLength = DecodeLength(data, ref offset);
            }
            else
            {
                // 固定长度类型
                itemLength = 1; // 默认1字节，后续根据类型调整
            }

            switch (type)
            {
                case TYPE_LIST:
                    return DecodeList(data, ref offset, itemLength, out length);

                case TYPE_ASCII:
                    return DecodeAscii(data, ref offset, itemLength, out length);

                case TYPE_I4:
                    length = itemLength + 2; // 类型 + 长度
                    return DecodeI4(data, ref offset);

                case TYPE_I8:
                    length = itemLength + 2;
                    return DecodeI8(data, ref offset);

                case TYPE_U1:
                    length = itemLength + 2;
                    return DecodeU1(data, ref offset);

                case TYPE_BOOLEAN:
                    length = itemLength + 2;
                    return DecodeBoolean(data, ref offset);

                case TYPE_F8:
                    length = itemLength + 2;
                    return DecodeF8(data, ref offset);

                default:
                    throw new NotSupportedException($"SECS type 0x{type:X2} not supported");
            }
        }

        /// <summary>
        /// 解码LIST类型
        /// </summary>
        private static List<object> DecodeList(byte[] data, ref int offset, int itemCount, out int totalLength)
        {
            var result = new List<object>();
            var startOffset = offset - 2; // 减去类型和长度字段

            for (int i = 0; i < itemCount; i++)
            {
                var itemLength = 0;
                var item = DecodeItem(data, ref offset, out itemLength);
                result.Add(item);
            }

            totalLength = offset - startOffset;
            return result;
        }

        /// <summary>
        /// 解码ASCII类型
        /// </summary>
        private static string DecodeAscii(byte[] data, ref int offset, int length, out int totalLength)
        {
            var startOffset = offset - 2;
            var text = Encoding.ASCII.GetString(data, offset, length);
            offset += length;
            totalLength = offset - startOffset;
            return text;
        }

        /// <summary>
        /// 解码I4类型
        /// </summary>
        private static int DecodeI4(byte[] data, ref int offset)
        {
            var length = data[offset++]; // 读取长度
            int value = (data[offset++] << 24) |
                       (data[offset++] << 16) |
                       (data[offset++] << 8) |
                       data[offset++];
            return value;
        }

        /// <summary>
        /// 解码I8类型
        /// </summary>
        private static long DecodeI8(byte[] data, ref int offset)
        {
            offset++; // 跳过长度
            long value = ((long)data[offset++] << 56) |
                        ((long)data[offset++] << 48) |
                        ((long)data[offset++] << 40) |
                        ((long)data[offset++] << 32) |
                        ((long)data[offset++] << 24) |
                        ((long)data[offset++] << 16) |
                        ((long)data[offset++] << 8) |
                        data[offset++];
            return value;
        }

        /// <summary>
        /// 解码U1类型
        /// </summary>
        private static byte DecodeU1(byte[] data, ref int offset)
        {
            offset++; // 跳过长度
            return data[offset++];
        }

        /// <summary>
        /// 解码布尔类型
        /// </summary>
        private static bool DecodeBoolean(byte[] data, ref int offset)
        {
            offset++; // 跳过长度
            return data[offset++] != 0;
        }

        /// <summary>
        /// 解码F8类型
        /// </summary>
        private static double DecodeF8(byte[] data, ref int offset)
        {
            offset++; // 跳过长度
            var bytes = new byte[8];
            Array.Copy(data, offset, bytes, 0, 8);
            Array.Reverse(bytes); // 转换为Little-Endian
            offset += 8;
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// 解码长度字段
        /// </summary>
        private static int DecodeLength(byte[] data, ref int offset)
        {
            byte first = data[offset++];

            if (first != 0xFF)
            {
                return first;
            }

            byte second = data[offset++];

            if (second != 0xFF)
            {
                return (second << 8) | data[offset++];
            }

            // 3字节长度格式
            return (data[offset++] << 16) |
                   (data[offset++] << 8) |
                   data[offset++];
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 检查是否为SECS-II数据类型
        /// </summary>
        public static bool IsSecsType(byte type)
        {
            return type == TYPE_LIST ||
                   type == TYPE_BINARY ||
                   type == TYPE_BOOLEAN ||
                   type == TYPE_ASCII ||
                   type == TYPE_JIS8 ||
                   type == TYPE_I1 ||
                   type == TYPE_I2 ||
                   type == TYPE_I4 ||
                   type == TYPE_I8 ||
                   type == TYPE_U1 ||
                   type == TYPE_U2 ||
                   type == TYPE_U4 ||
                   type == TYPE_U8 ||
                   type == TYPE_F4 ||
                   type == TYPE_F8;
        }

        /// <summary>
        /// 获取SECS-II数据类型的描述
        /// </summary>
        public static string GetTypeDescription(byte type)
        {
            switch (type)
            {
                case TYPE_LIST: return "LIST";
                case TYPE_BINARY: return "BINARY";
                case TYPE_BOOLEAN: return "BOOLEAN";
                case TYPE_ASCII: return "ASCII";
                case TYPE_JIS8: return "JIS8";
                case TYPE_I1: return "I1";
                case TYPE_I2: return "I2";
                case TYPE_I4: return "I4";
                case TYPE_I8: return "I8";
                case TYPE_U1: return "U1";
                case TYPE_U2: return "U2";
                case TYPE_U4: return "U4";
                case TYPE_U8: return "U8";
                case TYPE_F4: return "F4";
                case TYPE_F8: return "F8";
                default: return $"UNKNOWN(0x{type:X2})";
            }
        }

        #endregion
    }

    #region SECS-II 会话管理

    /// <summary>
    /// SECS会话状态枚举
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// 未连接状态
        /// </summary>
        NotConnected,

        /// <summary>
        /// 物理连接已建立，等待Select
        /// </summary>
        Connected,

        /// <summary>
        /// Select成功，会话已建立
        /// </summary>
        Selected,

        /// <summary>
        /// 正在处理Select请求
        /// </summary>
        Selecting,

        /// <summary>
        /// 正在处理Separate请求
        /// </summary>
        Separating,

        /// <summary>
        /// 通信中断
        /// </summary>
        CommunicationFailure,

        /// <summary>
        /// 会话已结束
        /// </summary>
        Separated,

        /// <summary>
        /// 链路测试状态
        /// </summary>
        LinkTest
    }

    /// <summary>
    /// SECS会话管理类
    /// </summary>
    public class SecsSession
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public byte DeviceId { get; set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        public SessionState State { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 客户端ID（服务器端使用）
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// 会话描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否已选择（Selected状态）
        /// </summary>
        public bool IsSelected => State == SessionState.Selected;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => State != SessionState.NotConnected && State != SessionState.Separated;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SecsSession()
        {
            SessionId = 0;
            DeviceId = 1;
            State = SessionState.NotConnected;
            LastActivity = DateTime.Now;
            CreatedTime = DateTime.Now;
            ClientId = null;
            Description = null;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="clientId">客户端ID</param>
        public SecsSession(int sessionId, byte deviceId, string? clientId = null)
        {
            SessionId = sessionId;
            DeviceId = deviceId;
            State = SessionState.Connected;
            LastActivity = DateTime.Now;
            CreatedTime = DateTime.Now;
            ClientId = clientId;
            Description = null;
        }

        /// <summary>
        /// 更新活动时间
        /// </summary>
        public void UpdateActivity()
        {
            LastActivity = DateTime.Now;
        }

        /// <summary>
        /// 设置为Selected状态
        /// </summary>
        public void SetSelected()
        {
            State = SessionState.Selected;
            UpdateActivity();
        }

        /// <summary>
        /// 设置为Separated状态
        /// </summary>
        public void SetSeparated()
        {
            State = SessionState.Separated;
            UpdateActivity();
        }

        /// <summary>
        /// 设置为Selecting状态
        /// </summary>
        public void SetSelecting()
        {
            State = SessionState.Selecting;
            UpdateActivity();
        }

        /// <summary>
        /// 设置为Separating状态
        /// </summary>
        public void SetSeparating()
        {
            State = SessionState.Separating;
            UpdateActivity();
        }

        /// <summary>
        /// 设置为通信失败状态
        /// </summary>
        public void SetCommunicationFailure()
        {
            State = SessionState.CommunicationFailure;
            UpdateActivity();
        }

        /// <summary>
        /// 执行链路测试
        /// </summary>
        public void PerformLinkTest()
        {
            State = SessionState.LinkTest;
            UpdateActivity();
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        public string GetStateDescription()
        {
            return State switch
            {
                SessionState.NotConnected => "未连接",
                SessionState.Connected => "已连接",
                SessionState.Selected => "已选择",
                SessionState.Selecting => "选择中",
                SessionState.Separating => "分离中",
                SessionState.CommunicationFailure => "通信失败",
                SessionState.Separated => "已分离",
                SessionState.LinkTest => "链路测试",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 检查会话是否超时
        /// </summary>
        /// <param name="timeoutMinutes">超时分钟数</param>
        public bool IsTimeout(int timeoutMinutes = 30)
        {
            return (DateTime.Now - LastActivity).TotalMinutes > timeoutMinutes;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"Session({SessionId}) - Device({DeviceId}) - {GetStateDescription()}";
        }
    }

    #endregion
}
