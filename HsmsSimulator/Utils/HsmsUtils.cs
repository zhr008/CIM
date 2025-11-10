using HsmsSimulator.Models;

namespace HsmsSimulator.Utils
{
    /// <summary>
    /// HSMS工具类
    /// </summary>
    public static class HsmsUtils
    {
        /// <summary>
        /// 解析HSMS消息
        /// </summary>
        public static HsmsMessage ParseMessage(byte[] data, string? senderId = null)
        {
            return HsmsMessage.Parse(data, senderId);
        }

        /// <summary>
        /// 构建HSMS消息
        /// </summary>
        public static byte[] BuildMessage(ushort stream, byte function, string content,
            bool requireResponse = false, byte deviceId = 0, int sessionId = 0)
        {
            return HsmsMessage.Build(stream, function, content, requireResponse, deviceId, sessionId);
        }

        /// <summary>
        /// 创建握手消息
        /// </summary>
        public static HsmsMessage CreateHandshakeMessage()
        {
            return new HsmsMessage
            {
                Stream = 1,
                Function = 13,
                Content = "ARE_YOU_THERE",
                RequireResponse = true,
                Direction = MessageDirection.Outgoing
            };
        }

        /// <summary>
        /// 创建设备状态请求消息
        /// </summary>
        public static HsmsMessage CreateStatusRequestMessage()
        {
            return new HsmsMessage
            {
                Stream = 2,
                Function = 33,
                Content = "EQUIPMENT_STATUS_REQUEST",
                RequireResponse = true,
                Direction = MessageDirection.Outgoing
            };
        }

        /// <summary>
        /// 创建事件报告消息
        /// </summary>
        public static HsmsMessage CreateEventReportMessage(string eventType, Dictionary<string, string> parameters)
        {
            var content = new System.Text.StringBuilder();
            content.Append($"EVENT_TYPE={eventType};");
            foreach (var param in parameters)
            {
                content.Append($"{param.Key}={param.Value};");
            }

            return new HsmsMessage
            {
                Stream = 6,
                Function = 11,
                Content = content.ToString(),
                RequireResponse = false,
                Direction = MessageDirection.Outgoing
            };
        }

        /// <summary>
        /// 创建工艺程序请求消息
        /// </summary>
        public static HsmsMessage CreateProcessProgramRequestMessage(string recipeId)
        {
            return new HsmsMessage
            {
                Stream = 7,
                Function = 21,
                Content = $"RECIPE_ID={recipeId}",
                RequireResponse = true,
                Direction = MessageDirection.Outgoing
            };
        }

        /// <summary>
        /// 验证消息格式
        /// </summary>
        public static bool ValidateMessage(HsmsMessage message)
        {
            if (message.Stream == 0 || message.Function == 0)
                return false;

            if (string.IsNullOrEmpty(message.MessageType))
                return false;

            return true;
        }

        /// <summary>
        /// 解析消息内容中的键值对
        /// </summary>
        public static Dictionary<string, string> ParseKeyValuePairs(string content)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(content))
                return result;

            var pairs = content.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return result;
        }

        /// <summary>
        /// 创建键值对内容
        /// </summary>
        public static string CreateKeyValueContent(Dictionary<string, string> parameters)
        {
            var content = new System.Text.StringBuilder();
            foreach (var param in parameters)
            {
                content.Append($"{param.Key}={param.Value};");
            }
            return content.ToString().TrimEnd(';');
        }

        /// <summary>
        /// 获取消息类型描述
        /// </summary>
        public static string GetMessageTypeDescription(ushort stream, byte function)
        {
            var key = $"S{stream}F{function}";

            return key switch
            {
                "S1F13" => "Are You There - 询问设备是否在线",
                "S1F14" => "I Am Here - 设备响应在线状态",
                "S2F33" => "Equipment Status Request - 设备状态请求",
                "S2F34" => "Equipment Status Data - 设备状态数据",
                "S6F11" => "Event Report Send - 事件报告发送",
                "S6F12" => "Event Report Acknowledge - 事件报告确认",
                "S7F21" => "Process Program Request - 工艺程序请求",
                "S7F22" => "Process Program - 工艺程序数据",
                "S9F1" => "Unrecognized Message Type - 无法识别的消息类型",
                "S9F3" => "Illegal Data - 非法数据",
                _ => $"未知消息类型 ({key})"
            };
        }

        /// <summary>
        /// 格式化时间戳
        /// </summary>
        public static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 计算运行时间
        /// </summary>
        public static string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
                return $"{(int)uptime.TotalDays}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
            else if (uptime.TotalHours >= 1)
                return $"{(int)uptime.TotalHours}小时 {uptime.Minutes}分钟";
            else if (uptime.TotalMinutes >= 1)
                return $"{(int)uptime.TotalMinutes}分钟 {uptime.Seconds}秒";
            else
                return $"{uptime.TotalSeconds:F2}秒";
        }
    }
}
