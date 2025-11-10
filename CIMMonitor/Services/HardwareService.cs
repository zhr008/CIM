namespace CIMMonitor.Services
{
    /// <summary>
    /// 硬件服务 - 纯中间层
    /// 不做业务处理，仅提供KepServer监控接口
    /// </summary>
    public static class HardwareService
    {
        private static IKepServerMonitoringService? _monitoringService;
        private static IKepServerEventHandler? _eventHandler;

        /// <summary>
        /// 硬件数据模型
        /// </summary>
        public class PLCData
        {
            public string DeviceId { get; set; } = string.Empty;
            public Dictionary<string, object> TagValues { get; set; } = new();
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Status { get; set; } = string.Empty;
        }

        /// <summary>
        /// 初始化KepServer监控服务
        /// </summary>
        public static async Task<bool> InitializeAsync()
        {
            try
            {
                LoggingService.Info("初始化KepServer监控服务...");

                // 创建监控服务实例
                _monitoringService = new KepServerMonitoringService();

                // 加载配置文件
                var configPath = Path.Combine(Application.StartupPath, "Config", "KepServerConfig.json");
                var initialized = await _monitoringService.InitializeAsync(configPath);

                if (initialized)
                {
                    // 创建事件处理器
                    _eventHandler = new KepServerEventHandler();

                    await _eventHandler.InitializeAsync(_monitoringService);

                    LoggingService.Info("KepServer监控服务初始化成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogException("初始化KepServer监控服务失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 启动监控
        /// </summary>
        public static async Task StartMonitoringAsync()
        {
            if (_monitoringService != null)
            {
                await _monitoringService.StartMonitoringAsync();
                LoggingService.Info("KepServer监控已启动");
            }
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public static async Task StopMonitoringAsync()
        {
            if (_monitoringService != null)
            {
                await _monitoringService.StopMonitoringAsync();
                LoggingService.Info("KepServer监控已停止");
            }
        }

        /// <summary>
        /// 获取所有服务器
        /// </summary>
        public static List<CIMMonitor.Models.KepServer.KepServer> GetServers()
        {
            return _monitoringService?.GetServers() ?? new List<CIMMonitor.Models.KepServer.KepServer>();
        }

        /// <summary>
        /// 根据ID获取服务器
        /// </summary>
        public static CIMMonitor.Models.KepServer.KepServer? GetServerById(string serverId)
        {
            return _monitoringService?.GetServerById(serverId);
        }

        /// <summary>
        /// 获取Bit地址
        /// </summary>
        public static List<CIMMonitor.Models.KepServer.BitAddress> GetBitAddresses(string serverId, string projectId)
        {
            return _monitoringService?.GetBitAddresses(serverId, projectId) ?? new List<CIMMonitor.Models.KepServer.BitAddress>();
        }

        /// <summary>
        /// 获取Word地址
        /// </summary>
        public static List<CIMMonitor.Models.KepServer.WordAddress> GetWordAddresses(string serverId, string projectId)
        {
            return _monitoringService?.GetWordAddresses(serverId, projectId) ?? new List<CIMMonitor.Models.KepServer.WordAddress>();
        }

        /// <summary>
        /// 获取监控统计
        /// </summary>
        public static CIMMonitor.Models.KepServer.MonitoringStatistics? GetStatistics(string serverId)
        {
            return _monitoringService?.GetStatistics(serverId);
        }

        /// <summary>
        /// 获取数据变化历史
        /// </summary>
        public static List<CIMMonitor.Models.KepServer.DataChangedEvent>? GetDataChangeHistory(string serverId)
        {
            return _eventHandler?.GetDataChangeHistory(serverId);
        }

        /// <summary>
        /// 获取映射触发历史
        /// </summary>
        public static List<CIMMonitor.Models.KepServer.MappingTriggeredEvent>? GetMappingHistory(string serverId)
        {
            return _eventHandler?.GetMappingHistory(serverId);
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public static void ClearHistory(string serverId)
        {
            _eventHandler?.ClearHistory(serverId);
        }

        // 保留兼容接口（标记为废弃）
        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static bool ConnectKepserver()
        {
            LoggingService.Warn("ConnectKepserver已废弃，请使用StartMonitoringAsync");
            return true;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static bool DisconnectKepserver()
        {
            LoggingService.Warn("DisconnectKepserver已废弃，请使用StopMonitoringAsync");
            return true;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static PLCData? ReadPLCData(string deviceId, string[] tagNames)
        {
            LoggingService.Warn("ReadPLCData已废弃，请使用KepServer监控服务");
            return null;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static bool WritePLCData(string deviceId, Dictionary<string, object> tagValues)
        {
            LoggingService.Warn("WritePLCData已废弃，请使用KepServer监控服务");
            return false;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static bool SendHSMSCommand(string deviceId, string command)
        {
            LoggingService.Warn("SendHSMSCommand已废弃，请使用KepServer监控服务");
            return false;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static Models.HSMSMessageXml? ReceiveHSMSMessage(int timeoutMs = 5000)
        {
            LoggingService.Warn("ReceiveHSMSMessage已废弃，请使用KepServer监控服务");
            return null;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static string XmlToHsmsBytes(string xml)
        {
            LoggingService.Warn("XmlToHsmsBytes已废弃，请使用KepServer监控服务");
            return string.Empty;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static string HsmsBytesToXml(byte[] bytes)
        {
            LoggingService.Warn("HsmsBytesToXml已废弃，请使用KepServer监控服务");
            return string.Empty;
        }

        [Obsolete("此方法已废弃，请使用KepServer监控服务")]
        public static List<string> GetOPCTags()
        {
            LoggingService.Warn("GetOPCTags已废弃，请使用KepServer监控服务");
            return new List<string>();
        }
    }
}
