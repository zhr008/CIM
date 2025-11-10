namespace CIMMonitor.Services
{
    public static class DeviceService
    {
        private static readonly List<Models.Device> _devices = new();
        private static readonly List<Models.DeviceStatus> _deviceStatuses = new();
        private static readonly Random _random = new();
        private static System.Threading.Timer? _heartbeatTimer;
        private static readonly object _lock = new object();

        static DeviceService()
        {
            LoggingService.Info("=== 设备服务初始化 ===");
            InitializeDevices();
            InitializeDeviceStatuses();
            StartHeartbeatMonitoring();
            LoggingService.Info("设备服务初始化完成");
        }

        private static void InitializeDevices()
        {
            _devices.Clear();
            _devices.AddRange(new[]
            {
                new Models.Device { DeviceId = 1, DeviceName = "生产线控制器1", DeviceType = "PLC", Status = "运行中", Temperature = 65.5, Pressure = 2.3, LastUpdateTime = DateTime.Now },
                new Models.Device { DeviceId = 2, DeviceName = "生产线控制器2", DeviceType = "PLC", Status = "运行中", Temperature = 62.1, Pressure = 2.1, LastUpdateTime = DateTime.Now },
                new Models.Device { DeviceId = 3, DeviceName = "包装机器人", DeviceType = "Robot", Status = "待机", Temperature = 45.0, Pressure = 0, LastUpdateTime = DateTime.Now },
                new Models.Device { DeviceId = 4, DeviceName = "电机1", DeviceType = "Motor", Status = "运行中", Temperature = 55.8, Pressure = 0, LastUpdateTime = DateTime.Now },
                new Models.Device { DeviceId = 5, DeviceName = "传感器组1", DeviceType = "Sensor", Status = "运行中", Temperature = 38.2, Pressure = 1.8, LastUpdateTime = DateTime.Now }
            });
            LoggingService.Info($"已初始化 {_devices.Count} 个设备");
        }

        private static void InitializeDeviceStatuses()
        {
            _deviceStatuses.Clear();
            foreach (var device in _devices)
            {
                _deviceStatuses.Add(new Models.DeviceStatus
                {
                    IntDeviceId = device.DeviceId,
                    IsOnline = true,
                    LastHeartbeat = DateTime.Now,
                    HeartbeatCount = 0,
                    ConnectionQuality = "良好",
                    ResponseTimeMs = _random.Next(10, 100)
                });
            }
            LoggingService.Info("已初始化设备状态监控");
        }

        private static void StartHeartbeatMonitoring()
        {
            _heartbeatTimer = new System.Threading.Timer(HeartbeatTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
            LoggingService.Info("已启动心跳监控 (每2秒)");
        }

        private static void HeartbeatTimerCallback(object? state)
        {
            lock (_lock)
            {
                try
                {
                    foreach (var status in _deviceStatuses)
                    {
                        // 模拟心跳检测
                        var isOnline = _random.Next(0, 100) < 95; // 95%概率在线

                        if (isOnline)
                        {
                            status.IsOnline = true;
                            status.LastHeartbeat = DateTime.Now;
                            status.HeartbeatCount++;
                            status.ResponseTimeMs = _random.Next(10, 150);
                            status.ConnectionQuality = status.ResponseTimeMs < 50 ? "优秀" :
                                                       status.ResponseTimeMs < 100 ? "良好" : "一般";

                            // 更新设备数据
                            var device = _devices.FirstOrDefault(d => d.DeviceId == status.IntDeviceId);
                            if (device?.Status == "运行中")
                            {
                                device.Temperature = 50 + _random.NextDouble() * 30;
                                device.Pressure = _random.NextDouble() * 3;
                                device.LastUpdateTime = DateTime.Now;
                            }
                        }
                        else
                        {
                            status.IsOnline = false;
                            status.ConnectionQuality = "离线";

                            // 模拟设备离线
                            var device = _devices.FirstOrDefault(d => d.DeviceId == status.IntDeviceId);
                            if (device != null)
                            {
                                device.Status = "离线";
                                LoggingService.LogDeviceOperation($"设备 {device.DeviceName} (ID:{device.DeviceId}) 已离线");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogException("心跳监控发生异常", ex);
                }
            }
        }

        public static List<Models.Device> GetAllDevices()
        {
            lock (_lock)
            {
                return _devices.ToList();
            }
        }

        public static Models.Device? GetDeviceById(int deviceId)
        {
            lock (_lock)
            {
                return _devices.FirstOrDefault(d => d.DeviceId == deviceId);
            }
        }

        public static Models.DeviceStatus? GetDeviceStatus(int deviceId)
        {
            lock (_lock)
            {
                return _deviceStatuses.FirstOrDefault(s => s.IntDeviceId == deviceId);
            }
        }

        public static List<Models.DeviceStatus> GetAllDeviceStatuses()
        {
            lock (_lock)
            {
                return _deviceStatuses.ToList();
            }
        }

        public static bool UpdateDeviceStatus(int deviceId, string status)
        {
            lock (_lock)
            {
                try
                {
                    var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
                    if (device != null)
                    {
                        var oldStatus = device.Status;
                        device.Status = status;
                        device.LastUpdateTime = DateTime.Now;

                        LoggingService.LogDeviceOperation($"设备状态变更: {device.DeviceName} (ID:{deviceId}) {oldStatus} -> {status}");

                        // 发送XML命令到设备
                        var xmlMessage = CreateCommandXml(deviceId, status);
                        LoggingService.LogXmlMessage($"发送设备控制命令 XML:\n{xmlMessage}");

                        return true;
                    }
                    else
                    {
                        LoggingService.Error($"未找到设备 ID:{deviceId}");
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    LoggingService.LogException($"更新设备状态失败 ID:{deviceId}", ex);
                    return false;
                }
            }
        }

        private static string CreateCommandXml(int deviceId, string command)
        {
            try
            {
                var device = GetDeviceById(deviceId);
                var message = new Models.XmlMessage
                {
                    Header = new Models.MessageHeader
                    {
                        MessageType = "Command",
                        DeviceId = device?.DeviceName ?? "",
                        Command = command,
                        Timestamp = DateTime.Now
                    },
                    Body = new Models.MessageBody
                    {
                        Parameters = new Dictionary<string, string>
                        {
                            { "DeviceId", deviceId.ToString() },
                            { "Command", command },
                            { "Operator", "System" }
                        }
                    }
                };
                return message.ToXml();
            }
            catch (Exception ex)
            {
                LoggingService.LogException("创建XML命令失败", ex);
                return string.Empty;
            }
        }

        public static void Dispose()
        {
            LoggingService.Info("正在停止设备服务...");
            _heartbeatTimer?.Dispose();
            LoggingService.Info("设备服务已停止");
        }
    }
}
