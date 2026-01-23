using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using WCFServices.Contracts;
using Common.Models;
using log4net;
using log4net.Config;
using System.Reflection;
using System.IO;

namespace WCFServices.Services
{
    /// <summary>
    /// TIBCO订阅者服务 - 接收来自CIMMonitor的XML消息
    /// 并根据不同的主题调用相应的业务方法处理
    /// </summary>
    public class TibcoSubscriberService : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TibcoSubscriberService));
        private TibcoIntegrationService? _integrationService;
        private bool _isRunning;
        private Task? _listeningTask;

        public TibcoSubscriberService()
        {
            // 配置log4net
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            if (File.Exists("log4net.config"))
            {
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            }
        }

        public async Task StartAsync()
        {
            log.Info("=== TIBCO订阅者服务启动 ===");
            Console.WriteLine("=== TIBCO订阅者服务启动 ===");

            try
            {
                // 初始化集成服务
                _integrationService = new TibcoIntegrationService();
                
                log.Info("TIBCO订阅者服务已启动，准备接收来自CIMMonitor的消息");
                Console.WriteLine("TIBCO订阅者服务已启动，监听来自CIMMonitor的消息");

                _isRunning = true;
                _listeningTask = Task.Run(ListenForMessagesAsync);
            }
            catch (Exception ex)
            {
                log.Error("TIBCO订阅者服务启动失败", ex);
                Console.WriteLine($"错误: {ex.Message}");
                throw;
            }
        }

        private async Task ListenForMessagesAsync()
        {
            while (_isRunning)
            {
                try
                {
                    // 在实际实现中，这里会监听TIBCO主题
                    // 目前我们使用模拟方式展示流程
                    await Task.Delay(5000); // 每5秒检查一次
                    
                    // 输出心跳信息
                    log.Debug("TIBCO订阅者服务运行中...");
                }
                catch (Exception ex)
                {
                    log.Error("监听消息时发生错误", ex);
                }
            }
        }

        /// <summary>
        /// 处理来自CIMMonitor的XML消息
        /// 根据主题路由到相应的业务处理方法
        /// </summary>
        public async Task<string> ProcessMessageByTopicAsync(string topic, string xmlContent)
        {
            try
            {
                log.Info($"处理主题'{topic}'的消息，内容长度: {xmlContent.Length} 字符");

                // 根据主题类型路由到不同的处理方法
                switch (topic.ToUpper())
                {
                    case "EQUIPMENT.STATUS":
                        return await ProcessEquipmentStatusMessage(xmlContent);
                    case "PRODUCTION.DATA":
                        return await ProcessProductionDataMessage(xmlContent);
                    case "ALARM.EVENTS":
                        return await ProcessAlarmEventMessage(xmlContent);
                    case "CONFIG.CHANGES":
                        return await ProcessConfigChangeMessage(xmlContent);
                    case "MAINTENANCE.REPORTS":
                        return await ProcessMaintenanceReportMessage(xmlContent);
                    default:
                        return await ProcessGenericMessage(topic, xmlContent);
                }
            }
            catch (Exception ex)
            {
                log.Error($"处理主题'{topic}'的消息时发生错误: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理设备状态消息
        /// </summary>
        private async Task<string> ProcessEquipmentStatusMessage(string xmlContent)
        {
            log.Info("处理设备状态消息");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "UNKNOWN";
                var status = doc.Root?.Element("Status")?.Value ?? "UNKNOWN";
                
                // 调用集成服务更新设备状态
                var result = await _integrationService?.SendEquipmentStatusAsync(equipmentId, status);
                
                log.Info($"设备状态更新结果: {result}");
                return result ? "SUCCESS" : "FAILED";
            }
            catch (Exception ex)
            {
                log.Error($"处理设备状态消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理生产数据消息
        /// </summary>
        private async Task<string> ProcessProductionDataMessage(string xmlContent)
        {
            log.Info("处理生产数据消息");
            
            try
            {
                // 使用集成服务处理生产数据
                var result = await _integrationService?.ProcessXmlMessageAsync(xmlContent);
                
                log.Info($"生产数据处理结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"处理生产数据消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理报警事件消息
        /// </summary>
        private async Task<string> ProcessAlarmEventMessage(string xmlContent)
        {
            log.Info("处理报警事件消息");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "UNKNOWN";
                var alarmCode = doc.Root?.Element("AlarmCode")?.Value ?? "UNKNOWN";
                var description = doc.Root?.Element("Description")?.Value ?? "No Description";
                
                // 调用集成服务发送报警
                var result = await _integrationService?.SendAlarmAsync(equipmentId, alarmCode, description);
                
                log.Info($"报警事件处理结果: {result}");
                return result ? "SUCCESS" : "FAILED";
            }
            catch (Exception ex)
            {
                log.Error($"处理报警事件消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理配置变更消息
        /// </summary>
        private async Task<string> ProcessConfigChangeMessage(string xmlContent)
        {
            log.Info("处理配置变更消息");
            
            try
            {
                // 配置变更处理逻辑
                var result = await _integrationService?.ProcessXmlMessageAsync(xmlContent);
                
                log.Info($"配置变更处理结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"处理配置变更消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理维护报告消息
        /// </summary>
        private async Task<string> ProcessMaintenanceReportMessage(string xmlContent)
        {
            log.Info("处理维护报告消息");
            
            try
            {
                // 维护报告处理逻辑
                var result = await _integrationService?.ProcessXmlMessageAsync(xmlContent);
                
                log.Info($"维护报告处理结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"处理维护报告消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理通用消息
        /// </summary>
        private async Task<string> ProcessGenericMessage(string topic, string xmlContent)
        {
            log.Info($"处理通用消息，主题: {topic}");
            
            try
            {
                // 使用集成服务处理通用消息
                var result = await _integrationService?.ProcessXmlMessageAsync(xmlContent);
                
                log.Info($"通用消息处理结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"处理通用消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 通过设备ID获取设备状态
        /// </summary>
        public async Task<EquipmentStatus> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                log.Info($"获取设备状态: {equipmentId}");
                
                var result = await _integrationService?.GetEquipmentStatusAsync(equipmentId);
                
                log.Info($"设备状态查询结果: {result?.Status}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"获取设备状态失败: {ex.Message}", ex);
                return new EquipmentStatus
                {
                    EquipmentID = equipmentId,
                    Status = "Unknown"
                };
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            log.Info("停止TIBCO订阅者服务");
            _isRunning = false;
            
            _listeningTask?.Wait(1000); // 等待最多1秒让任务完成
        }

        public void Dispose()
        {
            Stop();
            _integrationService?.Dispose();
            log.Info("TIBCO订阅者服务已停止");
        }
    }
}