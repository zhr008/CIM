using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Models;
using CIMMonitor.Models.KepServer;
using HsmsSimulator.Models;
using log4net;

namespace CIMMonitor.Services
{
    /// <summary>
    /// 数据流向服务 - 统一处理从设备到数据库的数据流
    /// 支持两种数据流：
    /// 1. PLC → KepServerEX → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE
    /// 2. PCL → HSMS → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE
    /// </summary>
    public class DataFlowService : IDisposable
    {
        private static readonly ILog _logger = log4net.LogManager.GetLogger(typeof(DataFlowService));
        
        private readonly IKepServerMonitoringService _kepServerService;
        private readonly KepServerEventHandler _kepServerEventHandler;
        private readonly HsmsDeviceManager _hsmsDeviceManager;
        
        public DataFlowService(
            IKepServerMonitoringService kepServerService, 
            KepServerEventHandler kepServerEventHandler,
            HsmsDeviceManager hsmsDeviceManager)
        {
            _kepServerService = kepServerService;
            _kepServerEventHandler = kepServerEventHandler;
            _hsmsDeviceManager = hsmsDeviceManager;
            
            // 订阅事件
            _kepServerService.DataChanged += OnKepServerDataChanged;
            _kepServerService.MappingTriggered += OnKepServerMappingTriggered;
            _hsmsDeviceManager.DeviceMessageReceived += OnHsmsMessageReceived;
        }

        #region KepServerEX Data Flow Handlers
        
        /// <summary>
        /// 处理KepServerEX数据变化事件
        /// 数据流向: PLC → KepServerEX → CIMMonitor
        /// </summary>
        private async void OnKepServerDataChanged(object? sender, DataChangedEvent e)
        {
            try
            {
                _logger.Info($"处理KepServerEX数据变化: [{e.ServerId}] {e.Address}");
                
                // 构建设备消息对象
                var equipmentMessage = new EquipmentMessage
                {
                    EquipmentID = e.ServerId,
                    MessageType = "OPC_DATA",
                    MessageContent = $"KepServerEX data change: {e.Address} = {e.NewValue}",
                    Timestamp = e.Timestamp,
                    Properties = new Dictionary<string, object>()
                    {
                        ["TagName"] = e.Address,
                        ["Value"] = e.NewValue,
                        ["DataType"] = e.DataType,
                        ["ChangeType"] = e.ChangeType
                    }
                };

                // 发送到TibcoTibrvService → WCFServices → ORACLE
                await ForwardToMesService(equipmentMessage);
            }
            catch (Exception ex)
            {
                _logger.Error("处理KepServerEX数据变化失败", ex);
            }
        }

        /// <summary>
        /// 处理KepServerEX映射触发事件
        /// </summary>
        private async void OnKepServerMappingTriggered(object? sender, MappingTriggeredEvent e)
        {
            try
            {
                _logger.Info($"处理KepServerEX映射触发: [{e.ServerId}] {e.MappingId}");
                
                var equipmentMessage = new EquipmentMessage
                {
                    EquipmentID = e.ServerId,
                    MessageType = "OPC_MAPPING",
                    MessageContent = $"KepServerEX mapping triggered: {e.MappingId}",
                    Timestamp = e.TriggeredTime,
                    Properties = new Dictionary<string, object>()
                    {
                        ["MappingId"] = e.MappingId,
                        ["BitAddress"] = e.BitAddressId,
                        ["WordAddress"] = e.WordAddressId,
                        ["WordValue"] = e.WordValue,
                        ["TriggerCondition"] = e.TriggerCondition
                    }
                };

                // 发送到TibcoTibrvService → WCFServices → ORACLE
                await ForwardToMesService(equipmentMessage);
            }
            catch (Exception ex)
            {
                _logger.Error("处理KepServerEX映射触发失败", ex);
            }
        }

        #endregion

        #region HSMS Data Flow Handlers
        
        /// <summary>
        /// 处理HSMS消息接收事件
        /// 数据流向: PCL → HSMS → CIMMonitor
        /// </summary>
        private async void OnHsmsMessageReceived(object? sender, HsmsDeviceManager.DeviceMessageEventArgs e)
        {
            try
            {
                _logger.Info($"处理HSMS消息: [{e.DeviceId}] {e.Message}");
                
                // 解析HSMS消息内容
                var parsedMessage = ParseHsmsMessage(e.HsmsMessage);
                
                var equipmentMessage = new EquipmentMessage
                {
                    EquipmentID = e.DeviceId,
                    MessageType = "HSMS",
                    MessageContent = e.Message,
                    Timestamp = e.Timestamp,
                    Properties = parsedMessage
                };

                // 发送到TibcoTibrvService → WCFServices → ORACLE
                await ForwardToMesService(equipmentMessage);
            }
            catch (Exception ex)
            {
                _logger.Error("处理HSMS消息失败", ex);
            }
        }

        /// <summary>
        /// 解析HSMS消息内容
        /// </summary>
        private Dictionary<string, object> ParseHsmsMessage(HsmsMessage? hsmsMessage)
        {
            var properties = new Dictionary<string, object>();
            
            if (hsmsMessage != null)
            {
                properties["Stream"] = hsmsMessage.Stream;
                properties["Function"] = hsmsMessage.Function;
                properties["MessageType"] = hsmsMessage.MessageType;
                properties["DeviceId"] = hsmsMessage.DeviceId;
                properties["SessionId"] = hsmsMessage.SessionId;
                properties["Direction"] = hsmsMessage.Direction.ToString();
                properties["SenderId"] = hsmsMessage.SenderId;
                properties["SenderRole"] = hsmsMessage.SenderRole.ToString();
                properties["Content"] = hsmsMessage.Content;
                
                // 如果内容是结构化数据，则尝试解析
                if (!string.IsNullOrEmpty(hsmsMessage.Content))
                {
                    try
                    {
                        // 简单解析JSON或键值对格式
                        if (hsmsMessage.Content.StartsWith("{") && hsmsMessage.Content.EndsWith("}"))
                        {
                            // 这里可以使用JSON解析库来处理复杂结构
                            properties["Data"] = hsmsMessage.Content;
                        }
                        else if (hsmsMessage.Content.Contains("="))
                        {
                            // 解析简单的键值对格式
                            var pairs = hsmsMessage.Content.Split(',');
                            foreach (var pair in pairs)
                            {
                                var keyValue = pair.Split('=');
                                if (keyValue.Length == 2)
                                {
                                    properties[keyValue[0].Trim()] = keyValue[1].Trim();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"解析HSMS消息内容失败: {ex.Message}");
                    }
                }
            }
            
            return properties;
        }

        #endregion

        #region Data Forwarding to MES Service
        
        /// <summary>
        /// 将设备消息转发到MES服务
        /// 数据流向: CIMMonitor → TibcoTibrvService → WCFServices → ORACLE
        /// </summary>
        private async Task ForwardToMesService(EquipmentMessage equipmentMessage)
        {
            try
            {
                _logger.Info($"转发消息到TibcoRV: {equipmentMessage.EquipmentID} - {equipmentMessage.MessageType}");
                
                // 将EquipmentMessage序列化为XML
                var xmlContent = SerializeToXml(equipmentMessage);
                
                // 根据消息类型确定Tibco主题
                var topic = DetermineTibcoTopic(equipmentMessage.MessageType);
                
                // 通过TibcoService发送消息
                var success = await TibcoService.Instance.SendMessageAsync(topic, xmlContent, "CIMMonitor");
                
                if (success)
                {
                    _logger.Info($"消息已通过TibcoRV发送到主题: {topic}");
                }
                else
                {
                    _logger.Error($"通过TibcoRV发送消息失败: {topic}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"转发消息到TibcoRV失败: {ex.Message}", ex);
                
                // 可能需要实现重试机制或错误队列
                HandleMesServiceError(equipmentMessage, ex);
            }
        }

        /// <summary>
        /// 确定Tibco主题
        /// </summary>
        private string DetermineTibcoTopic(string messageType)
        {
            return messageType.ToUpper() switch
            {
                "OPC_DATA" => "PRODUCTION.DATA",
                "OPC_MAPPING" => "CONFIG.CHANGES",
                "HSMS" => "EQUIPMENT.STATUS",
                "ALARM" => "ALARM.EVENTS",
                _ => "GENERIC.MESSAGE"
            };
        }

        /// <summary>
        /// 将EquipmentMessage序列化为XML
        /// </summary>
        private string SerializeToXml(EquipmentMessage equipmentMessage)
        {
            var xmlDoc = new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement("EquipmentMessage",
                    new System.Xml.Linq.XElement("EquipmentId", equipmentMessage.EquipmentID),
                    new System.Xml.Linq.XElement("MessageType", equipmentMessage.MessageType),
                    new System.Xml.Linq.XElement("MessageContent", equipmentMessage.MessageContent),
                    new System.Xml.Linq.XElement("Timestamp", equipmentMessage.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    new System.Xml.Linq.XElement("Properties",
                        equipmentMessage.Properties.Select(kvp => 
                            new System.Xml.Linq.XElement(kvp.Key, kvp.Value?.ToString()))
                    )
                )
            );
            
            return xmlDoc.ToString();
        }

        /// <summary>
        /// 处理MES服务错误
        /// </summary>
        private void HandleMesServiceError(EquipmentMessage originalMessage, Exception error)
        {
            _logger.Error($"MES服务错误处理: {originalMessage.EquipmentID} - {error.Message}");
            
            // 在实际实现中，这里可能需要：
            // 1. 将消息放入错误队列进行重试
            // 2. 记录错误日志
            // 3. 发送告警通知
            // 4. 尝试备用服务端点
            
            // 临时实现：仅记录错误
            _logger.Error($"原始消息内容: {originalMessage.MessageContent}");
        }

        #endregion

        #region Public Methods for Data Flow Management
        
        /// <summary>
        /// 启动数据流向服务
        /// </summary>
        public async Task StartAsync()
        {
            _logger.Info("启动数据流向服务...");
            
            // 启动KepServer监控
            await _kepServerService.StartMonitoringAsync();
            
            // 启动HSMS设备连接
            foreach (var deviceStatus in _hsmsDeviceManager.GetAllDeviceStatuses())
            {
                await _hsmsDeviceManager.ConnectDeviceAsync(deviceStatus.DeviceId);
            }
            
            _logger.Info("数据流向服务已启动");
        }

        /// <summary>
        /// 停止数据流向服务
        /// </summary>
        public async Task StopAsync()
        {
            _logger.Info("停止数据流向服务...");
            
            // 停止KepServer监控
            await _kepServerService.StopMonitoringAsync();
            
            // 断开HSMS设备连接
            await _hsmsDeviceManager.DisconnectAllAsync();
            
            _logger.Info("数据流向服务已停止");
        }

        /// <summary>
        /// 获取当前统计信息
        /// </summary>
        public DataFlowStatistics GetStatistics()
        {
            var statistics = new DataFlowStatistics();
            
            // 获取KepServer统计
            foreach (var server in _kepServerService.GetServers().Where(s => s.Enabled))
            {
                var serverStats = _kepServerService.GetStatistics(server.ServerId);
                statistics.KepServerStats[server.ServerId] = serverStats;
            }
            
            // 获取HSMS设备统计
            foreach (var deviceStatus in _hsmsDeviceManager.GetAllDeviceStatuses())
            {
                statistics.HsmsDeviceStats[deviceStatus.DeviceId] = deviceStatus;
            }
            
            return statistics;
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _kepServerService.DataChanged -= OnKepServerDataChanged;
                _kepServerService.MappingTriggered -= OnKepServerMappingTriggered;
                _hsmsDeviceManager.DeviceMessageReceived -= OnHsmsMessageReceived;
            }
            catch (Exception ex)
            {
                _logger.Error("释放DataFlowService资源时出错", ex);
            }
        }
    }

    /// <summary>
    /// 数据流向统计信息
    /// </summary>
    public class DataFlowStatistics
    {
        public DataFlowStatistics()
        {
            KepServerStats = new Dictionary<string, MonitoringStatistics>();
            HsmsDeviceStats = new Dictionary<string, Models.DeviceStatus>();
        }
        
        public Dictionary<string, MonitoringStatistics> KepServerStats { get; set; }
        public Dictionary<string, Models.DeviceStatus> HsmsDeviceStats { get; set; }
        
        public int TotalProcessedMessages { get; set; }
        public int TotalFailedMessages { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public TimeSpan Uptime => DateTime.Now - StartTime;
    }
}