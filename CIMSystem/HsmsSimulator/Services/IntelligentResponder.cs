using HsmsSimulator.Models;
using HsmsSimulator.Device;
using System.Collections.Generic;
using System.Text;

namespace HsmsSimulator.Services
{
    /// <summary>
    /// 智能消息响应器
    /// 根据接收到的消息类型自动生成适当的响应
    /// </summary>
    public class IntelligentResponder
    {
        private readonly Dictionary<string, ResponseTemplate> _responseTemplates = new();
        private readonly SimulatedDevice? _device;

        /// <summary>
        /// 构造函数
        /// </summary>
        public IntelligentResponder(SimulatedDevice? device = null)
        {
            _device = device;
            InitializeResponseTemplates();
        }

        /// <summary>
        /// 初始化响应模板
        /// </summary>
        private void InitializeResponseTemplates()
        {
            // S1系列 - 握手消息
            AddTemplate("S1F13", "ARE_YOU_THERE", GenerateS1F14Response);
            AddTemplate("S1F15", "ARE_YOU_THERE_REQUEST", GenerateS1F16Response);
            AddTemplate("S1F17", "SELECT_REQUEST", GenerateS1F18Response);
            AddTemplate("S1F19", "SELECT_STATUS_REQUEST", GenerateS1F20Response);

            // S2系列 - 设备状态
            AddTemplate("S2F33", "EQUIPMENT_STATUS_REQUEST", GenerateS2F34Response);
            AddTemplate("S2F35", "EQUIPMENT_STATUS_REQUEST", GenerateS2F36Response);
            AddTemplate("S2F37", "EQUIPMENT_CONSTANT_REQUEST", GenerateS2F38Response);
            AddTemplate("S2F41", "UNSELECT_REQUEST", GenerateS2F42Response);

            // S5系列 - 报警
            AddTemplate("S5F17", "ALARM_REPORT_SEND", GenerateS5F18Response);

            // S6系列 - 事件报告
            AddTemplate("S6F13", "EVENT_REPORT_REQUEST", GenerateS6F14Response);

            // S7系列 - 工艺程序
            AddTemplate("S7F21", "PROCESS_PROGRAM_REQUEST", GenerateS7F22Response);
            AddTemplate("S7F23", "PROCESS_PROGRAM_INQUIRY", GenerateS7F24Response);

            // S9系列 - 系统错误
            AddTemplate("S9F1", "UNRECOGNIZED_MESSAGE_TYPE", GenerateS9F2Response);
            AddTemplate("S9F3", "ILLEGAL_DATA", GenerateS9F4Response);
            AddTemplate("S9F5", "FRAGMENT_REASSEMBLY_SEQUENCE_ERROR", GenerateS9F6Response);
        }

        /// <summary>
        /// 添加响应模板
        /// </summary>
        private void AddTemplate(string messageType, string description, ResponseGenerator generator)
        {
            _responseTemplates[messageType] = new ResponseTemplate
            {
                MessageType = messageType,
                Description = description,
                Generator = generator
            };
        }

        /// <summary>
        /// 生成响应消息
        /// </summary>
        public HsmsMessage? GenerateResponse(HsmsMessage request)
        {
            if (request == null)
                return null;

            string key = $"S{request.Stream}F{request.Function}";
            if (_responseTemplates.TryGetValue(key, out var template))
            {
                return template.Generator(request);
            }

            // 默认响应
            return GenerateDefaultResponse(request);
        }

        #region 响应生成方法

        /// <summary>
        /// 生成S1F14 - I Am Here响应
        /// </summary>
        private HsmsMessage GenerateS1F14Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 1,
                Function = 14,
                Content = "I_AM_HERE",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S1F16 - I Am Here Request响应
        /// </summary>
        private HsmsMessage GenerateS1F16Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 1,
                Function = 16,
                Content = "I_AM_HERE_REQUEST",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S1F18 - Selected响应
        /// </summary>
        private HsmsMessage GenerateS1F18Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 1,
                Function = 18,
                Content = "SELECTED",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S1F20 - Select Status响应
        /// </summary>
        private HsmsMessage GenerateS1F20Response(HsmsMessage request)
        {
            var status = GetDeviceStatusString();
            return new HsmsMessage
            {
                Stream = 1,
                Function = 20,
                Content = $"SELECT_STATUS={status}",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S2F34 - Equipment Status Data响应
        /// </summary>
        private HsmsMessage GenerateS2F34Response(HsmsMessage request)
        {
            var content = new StringBuilder();
            content.Append($"EQUIPMENT_ID={_device?.DeviceId ?? "DEVICE001"};");
            content.Append($"STATUS={GetDeviceStatusString()};");
            content.Append($"PROGRESS={_device?.GetDataItemValue("PROGRESS") ?? "0"};");
            content.Append($"LOAD={_device?.GetDataItemValue("LOAD") ?? "0"};");
            content.Append($"RECIPE={_device?.CurrentRecipe ?? "DEFAULT"};");

            return new HsmsMessage
            {
                Stream = 2,
                Function = 34,
                Content = content.ToString(),
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S2F36 - Equipment Status Data响应
        /// </summary>
        private HsmsMessage GenerateS2F36Response(HsmsMessage request)
        {
            return GenerateS2F34Response(request);
        }

        /// <summary>
        /// 生成S2F38 - Equipment Constant响应
        /// </summary>
        private HsmsMessage GenerateS2F38Response(HsmsMessage request)
        {
            var content = new StringBuilder();
            content.Append("CONSTANT1=100;");
            content.Append("CONSTANT2=200;");
            content.Append("CONSTANT3=300;");

            return new HsmsMessage
            {
                Stream = 2,
                Function = 38,
                Content = content.ToString(),
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S2F42 - Unselected响应
        /// </summary>
        private HsmsMessage GenerateS2F42Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 2,
                Function = 42,
                Content = "UNSELECTED",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S5F18 - Alarm Report Acknowledge响应
        /// </summary>
        private HsmsMessage GenerateS5F18Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 5,
                Function = 18,
                Content = "ALARM_REPORT_ACK",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S6F14 - Event Report Data响应
        /// </summary>
        private HsmsMessage GenerateS6F14Response(HsmsMessage request)
        {
            var content = new StringBuilder();
            content.Append($"EVENT_ID={request.Content.Split('=')[1]};");
            content.Append($"TIMESTAMP={DateTime.Now:yyyy-MM-dd HH:mm:ss};");
            content.Append($"STATUS={GetDeviceStatusString()};");

            return new HsmsMessage
            {
                Stream = 6,
                Function = 14,
                Content = content.ToString(),
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S7F22 - Process Program响应
        /// </summary>
        private HsmsMessage GenerateS7F22Response(HsmsMessage request)
        {
            var recipeName = request.Content.Contains("=") ?
                request.Content.Split('=')[1] : "DEFAULT";

            var content = new StringBuilder();
            content.Append($"RECIPE_ID={recipeName};");
            content.Append("RECIPE_DATA=<RECIPE_CONTENT>;");

            return new HsmsMessage
            {
                Stream = 7,
                Function = 22,
                Content = content.ToString(),
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S7F24 - Process Program Acknowledge响应
        /// </summary>
        private HsmsMessage GenerateS7F24Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 7,
                Function = 24,
                Content = "PROCESS_PROGRAM_ACK",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S9F2 - Unrecognized Message Type Acknowledge响应
        /// </summary>
        private HsmsMessage GenerateS9F2Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 9,
                Function = 2,
                Content = "UNRECOGNIZED_MESSAGE_TYPE_ACK",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S9F4 - Illegal Data Acknowledge响应
        /// </summary>
        private HsmsMessage GenerateS9F4Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 9,
                Function = 4,
                Content = "ILLEGAL_DATA_ACK",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成S9F6 - Fragment Reassembly Sequence Error Acknowledge响应
        /// </summary>
        private HsmsMessage GenerateS9F6Response(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = 9,
                Function = 6,
                Content = "FRAGMENT_REASSEMBLY_SEQUENCE_ERROR_ACK",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 生成默认响应
        /// </summary>
        private HsmsMessage GenerateDefaultResponse(HsmsMessage request)
        {
            return new HsmsMessage
            {
                Stream = request.Stream,
                Function = (byte)(request.Function + 1), // 通常响应Function = 请求Function + 1
                Content = $"ACK for S{request.Stream}F{request.Function}",
                DeviceId = 0,
                SessionId = 0,
                RequireResponse = false,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                SenderId = "SIMULATOR"
            };
        }

        /// <summary>
        /// 获取设备状态字符串
        /// </summary>
        private string GetDeviceStatusString()
        {
            return _device?.Status switch
            {
                DeviceStatus.Online => "ONLINE",
                DeviceStatus.Idle => "IDLE",
                DeviceStatus.Running => "RUNNING",
                DeviceStatus.Paused => "PAUSED",
                DeviceStatus.Alarm => "ALARM",
                DeviceStatus.Maintenance => "MAINTENANCE",
                DeviceStatus.Error => "ERROR",
                _ => "OFFLINE"
            };
        }

        #endregion
    }

    /// <summary>
    /// 响应模板
    /// </summary>
    internal class ResponseTemplate
    {
        public string MessageType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ResponseGenerator Generator { get; set; } = null!;
    }

    /// <summary>
    /// 响应生成委托
    /// </summary>
    internal delegate HsmsMessage ResponseGenerator(HsmsMessage request);
}
