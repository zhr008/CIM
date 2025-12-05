using HsmsSimulator.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HsmsSimulator.Device
{
    /// <summary>
    /// 模拟设备类 - 增强版
    /// 参考HslCommunicationDemo的设备模拟实现
    /// </summary>
    public class SimulatedDevice
    {
        private readonly ConcurrentDictionary<string, object> _dataItems = new();
        private readonly ConcurrentDictionary<string, DeviceParameter> _parameters = new();
        private readonly ConcurrentQueue<HsmsMessage> _messageQueue = new();
        private System.Threading.Timer? _heartbeatTimer;
        private System.Threading.Timer? _eventTimer;
        private System.Threading.Timer? _statusMonitorTimer;

        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = "DEVICE001";

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = "模拟设备";

        /// <summary>
        /// 设备状态
        /// </summary>
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;

        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description { get; set; } = "HSMS模拟设备";

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息计数
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// 错误计数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 设备参数列表
        /// </summary>
        public IReadOnlyDictionary<string, DeviceParameter> Parameters => _parameters.AsReadOnly();

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan RunningTime => Status == DeviceStatus.Running ? DateTime.Now - _startTime : TimeSpan.Zero;

        private DateTime _startTime = DateTime.Now;

        /// <summary>
        /// 报警列表
        /// </summary>
        public List<string> Alarms { get; set; } = new List<string>();

        /// <summary>
        /// 当前批次ID
        /// </summary>
        public string CurrentBatchId { get; set; } = string.Empty;

        /// <summary>
        /// 当前Lot ID
        /// </summary>
        public string CurrentLotId { get; set; } = string.Empty;

        /// <summary>
        /// 当前配方
        /// </summary>
        public string CurrentRecipe { get; set; } = "DEFAULT";

        /// <summary>
        /// 构造函数
        /// </summary>
        public SimulatedDevice(string deviceId, string deviceName)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            InitializeDataItems();
        }

        /// <summary>
        /// 初始化数据项和参数
        /// </summary>
        private void InitializeDataItems()
        {
            _dataItems["STATUS"] = "OFFLINE";
            _dataItems["TEMPERATURE"] = "25.0";
            _dataItems["PRESSURE"] = "1.0";
            _dataItems["LOAD"] = "0";
            _dataItems["PROGRESS"] = "0";
            _dataItems["RECIPE"] = "DEFAULT";
            _dataItems["BATCH_ID"] = "";
            _dataItems["LOT_ID"] = "";
            _dataItems["STATE_CODE"] = "0";
            _dataItems["ALARM_ID"] = "";
            _dataItems["EQUIPMENT_ID"] = DeviceId;

            // 初始化设备参数
            InitializeParameters();
        }

        /// <summary>
        /// 初始化设备参数
        /// </summary>
        private void InitializeParameters()
        {
            // 基本参数
            AddParameter("Temperature", "25.0", "°C", "设备温度", 15.0, 35.0, 10.0, 40.0);
            AddParameter("Pressure", "1.0", "bar", "设备压力", 0.5, 2.0, 0.2, 3.0);
            AddParameter("Load", "0", "%", "设备负载", 0, 100, 0, 100);
            AddParameter("Progress", "0", "%", "当前进度", 0, 100, 0, 100);
            AddParameter("Speed", "0", "rpm", "运行速度", 0, 5000, 0, 6000);
            AddParameter("Power", "0", "kW", "功率消耗", 0, 100, 0, 150);
            AddParameter("Voltage", "220", "V", "电压", 180, 250, 160, 280);
            AddParameter("Current", "0", "A", "电流", 0, 50, 0, 60);
            AddParameter("Humidity", "45", "%", "湿度", 30, 70, 20, 80);
            AddParameter("Vibration", "0", "mm/s", "振动", 0, 5, 0, 8);
        }

        /// <summary>
        /// 添加设备参数
        /// </summary>
        public void AddParameter(string name, string value, string unit, string description,
            double? normalMin = null, double? normalMax = null,
            double? warningMin = null, double? warningMax = null)
        {
            var parameter = new DeviceParameter(name, value, unit, description)
            {
                NormalMin = normalMin,
                NormalMax = normalMax,
                WarningMin = warningMin,
                WarningMax = warningMax
            };

            _parameters[name] = parameter;
        }

        /// <summary>
        /// 获取设备参数
        /// </summary>
        public DeviceParameter? GetParameter(string name)
        {
            _parameters.TryGetValue(name, out var parameter);
            return parameter;
        }

        /// <summary>
        /// 更新设备参数
        /// </summary>
        public void UpdateParameter(string name, string value)
        {
            if (_parameters.TryGetValue(name, out var parameter))
            {
                parameter.UpdateValue(value);
            }
        }

        /// <summary>
        /// 获取所有报警状态的参数
        /// </summary>
        public IEnumerable<DeviceParameter> GetAlarmParameters()
        {
            return _parameters.Values.Where(p => p.Status == ParameterStatus.Alarm);
        }

        /// <summary>
        /// 获取所有警告状态的参数
        /// </summary>
        public IEnumerable<DeviceParameter> GetWarningParameters()
        {
            return _parameters.Values.Where(p => p.Status == ParameterStatus.Warning);
        }

        /// <summary>
        /// 启动设备
        /// </summary>
        public void Start()
        {
            Status = DeviceStatus.Online;
            ConnectionStatus = ConnectionStatus.Connected;
            _dataItems["STATUS"] = "ONLINE";
            LastActivity = DateTime.Now;

            // 启动心跳定时器
            _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            // 启动事件定时器
            _eventTimer = new System.Threading.Timer(GenerateEvents, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 停止设备
        /// </summary>
        public void Stop()
        {
            Status = DeviceStatus.Offline;
            ConnectionStatus = ConnectionStatus.Disconnected;
            _dataItems["STATUS"] = "OFFLINE";
            LastActivity = DateTime.Now;

            _heartbeatTimer?.Dispose();
            _eventTimer?.Dispose();
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        private void SendHeartbeat(object? state)
        {
            if (Status == DeviceStatus.Online)
            {
                LastActivity = DateTime.Now;
            }
        }

        /// <summary>
        /// 生成随机事件
        /// </summary>
        private void GenerateEvents(object? state)
        {
            if (Status != DeviceStatus.Online)
                return;

            var random = new Random();
            var events = new[]
            {
                () => UpdateProgress(),
                () => CheckAlarms(),
                () => UpdateTemperature(),
                () => UpdatePressure()
            };

            var eventIndex = random.Next(events.Length);
            events[eventIndex]();
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        private void UpdateProgress()
        {
            if (int.TryParse(_dataItems["PROGRESS"].ToString(), out var progress))
            {
                progress = Math.Min(progress + 5, 100);
                _dataItems["PROGRESS"] = progress.ToString();

                if (progress >= 100)
                {
                    _dataItems["STATUS"] = "COMPLETED";
                    Status = DeviceStatus.Busy;
                }
            }
        }

        /// <summary>
        /// 检查报警
        /// </summary>
        private void CheckAlarms()
        {
            var random = new Random();
            if (random.Next(0, 100) < 10) // 10%概率触发报警
            {
                _dataItems["ALARM_ID"] = $"ALM{random.Next(1000, 9999)}";
                _dataItems["STATUS"] = "ALARM";
            }
            else
            {
                _dataItems["ALARM_ID"] = "";
                if (_dataItems["STATUS"].ToString() == "ALARM")
                {
                    _dataItems["STATUS"] = "ONLINE";
                }
            }
        }

        /// <summary>
        /// 更新温度
        /// </summary>
        private void UpdateTemperature()
        {
            var random = new Random();
            var temperature = 25 + random.NextDouble() * 10; // 25-35度
            _dataItems["TEMPERATURE"] = temperature.ToString("F1");
        }

        /// <summary>
        /// 更新压力
        /// </summary>
        private void UpdatePressure()
        {
            var random = new Random();
            var pressure = 1.0 + random.NextDouble() * 0.5; // 1.0-1.5
            _dataItems["PRESSURE"] = pressure.ToString("F2");
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public HsmsMessage? ProcessMessage(HsmsMessage message)
        {
            LastActivity = DateTime.Now;
            MessageCount++;

            try
            {
                switch (message.Stream)
                {
                    case 1 when message.Function == 13:
                        // S1F13 - Are You There
                        return CreateResponse(message, "ONLINE");

                    case 2 when message.Function == 33:
                        // S2F33 - Equipment Status Request
                        return HandleStatusRequest(message);

                    case 6 when message.Function == 11:
                        // S6F11 - Event Report
                        return HandleEventReport(message);

                    case 7 when message.Function == 21:
                        // S7F21 - Process Program Request
                        return HandleProcessProgramRequest(message);

                    default:
                        // 未知消息
                        return CreateResponse(message, "UNKNOWN_COMMAND");
                }
            }
            catch (Exception ex)
            {
                ErrorCount++;
                return CreateErrorResponse(message, ex.Message);
            }
        }

        /// <summary>
        /// 创建响应消息
        /// </summary>
        private HsmsMessage CreateResponse(HsmsMessage request, string status)
        {
            var response = request.CreateResponse(status);
            response.SenderId = DeviceId;
            return response;
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        private HsmsMessage CreateErrorResponse(HsmsMessage request, string error)
        {
            var response = new HsmsMessage
            {
                Stream = request.Stream,
                Function = 99, // 错误响应
                Content = $"ERROR: {error}",
                DeviceId = request.DeviceId,
                SessionId = request.SessionId,
                RequireResponse = false,
                RelatedMessageId = request.MessageId,
                SenderId = DeviceId,
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now
            };

            response.MessageType = "Error Response";
            return response;
        }

        /// <summary>
        /// 处理状态请求
        /// </summary>
        private HsmsMessage HandleStatusRequest(HsmsMessage request)
        {
            var statusData = new StringBuilder();
            statusData.Append($"EQUIPMENT_ID={DeviceId};");
            statusData.Append($"STATUS={_dataItems["STATUS"]};");
            statusData.Append($"TEMPERATURE={_dataItems["TEMPERATURE"]};");
            statusData.Append($"PRESSURE={_dataItems["PRESSURE"]};");
            statusData.Append($"PROGRESS={_dataItems["PROGRESS"]};");

            return CreateResponse(request, statusData.ToString());
        }

        /// <summary>
        /// 处理事件报告
        /// </summary>
        private HsmsMessage HandleEventReport(HsmsMessage request)
        {
            // 处理事件报告，例如记录批次信息
            if (request.Content.Contains("BATCH_ID="))
            {
                _dataItems["BATCH_ID"] = ExtractValue(request.Content, "BATCH_ID");
            }

            if (request.Content.Contains("LOT_ID="))
            {
                _dataItems["LOT_ID"] = ExtractValue(request.Content, "LOT_ID");
            }

            if (request.Content.Contains("RECIPE="))
            {
                _dataItems["RECIPE"] = ExtractValue(request.Content, "RECIPE");
            }

            return CreateResponse(request, "ACK");
        }

        /// <summary>
        /// 处理工艺程序请求
        /// </summary>
        private HsmsMessage HandleProcessProgramRequest(HsmsMessage request)
        {
            var programData = new StringBuilder();
            programData.Append($"RECIPE_ID={_dataItems["RECIPE"]};");
            programData.Append($"STEP_COUNT=10;");
            programData.Append($"PROCESS_TIME=3600;"); // 秒

            return CreateResponse(request, programData.ToString());
        }

        /// <summary>
        /// 从内容中提取值
        /// </summary>
        private string ExtractValue(string content, string key)
        {
            var pairs = content.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    return parts[1].Trim();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取设备状态数据
        /// </summary>
        public string GetStatusData()
        {
            var data = new StringBuilder();
            foreach (var item in _dataItems)
            {
                data.AppendLine($"{item.Key}: {item.Value}");
            }
            return data.ToString();
        }

        /// <summary>
        /// 获取数据项
        /// </summary>
        public object? GetDataItem(string key)
        {
            return _dataItems.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 设置数据项
        /// </summary>
        public void SetDataItem(string key, object value)
        {
            _dataItems[key] = value;
        }

        /// <summary>
        /// 启动批量
        /// </summary>
        public void StartBatch(string batchId, string lotId, string recipe)
        {
            _dataItems["BATCH_ID"] = batchId;
            _dataItems["LOT_ID"] = lotId;
            _dataItems["RECIPE"] = recipe;
            _dataItems["STATUS"] = "RUNNING";
            _dataItems["PROGRESS"] = "0";
            _dataItems["ALARM_ID"] = "";
            Status = DeviceStatus.Busy;
        }

        /// <summary>
        /// 完成批量
        /// </summary>
        public void CompleteBatch()
        {
            _dataItems["STATUS"] = "COMPLETED";
            _dataItems["PROGRESS"] = "100";
            Status = DeviceStatus.Online;
        }

        /// <summary>
        /// 获取数据项值
        /// </summary>
        public string GetDataItemValue(string itemName)
        {
            if (_dataItems.TryGetValue(itemName, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// 设置数据项值
        /// </summary>
        public void SetDataItem(string itemName, string value)
        {
            _dataItems[itemName] = value;
            LastActivity = DateTime.Now;
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public string GetStatistics()
        {
            var uptime = DateTime.Now - LastActivity;
            return $"设备: {DeviceName}\n" +
                   $"状态: {Status}\n" +
                   $"连接: {ConnectionStatus}\n" +
                   $"消息: {MessageCount}\n" +
                   $"错误: {ErrorCount}\n" +
                   $"运行时间: {uptime:hh\\:mm\\:ss}";
        }
    }
}
