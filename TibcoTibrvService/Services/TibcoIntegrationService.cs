using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using WCFServices.Contracts;
using Common.Models;
using log4net;

namespace TibcoTibrvService.Services
{
    /// <summary>
    /// TIBCO集成服务 - 处理从CIMMonitor到WCF服务的数据流转
    /// 实现完整的数据流: CIMMonitor → TibcoTibrvService → WCFServices → ORACLE
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TibcoIntegrationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TibcoIntegrationService));
        
        private readonly ChannelFactory<IMesService> _wcfChannelFactory;
        private readonly IMesService _mesService;
        
        public TibcoIntegrationService()
        {
            // 初始化WCF服务连接
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress("http://localhost:8080/MesService");
            _wcfChannelFactory = new ChannelFactory<IMesService>(binding, endpoint);
            _mesService = _wcfChannelFactory.CreateChannel();
        }
        
        /// <summary>
        /// 处理来自CIMMonitor的设备消息
        /// </summary>
        public async Task<string> ProcessEquipmentMessageAsync(EquipmentMessage message)
        {
            try
            {
                log.Info($"处理设备消息: {message.EquipmentID} - {message.MessageType}");
                
                // 调用WCF服务处理消息
                var result = await Task.Run(() => _mesService.ProcessEquipmentMessage(message));
                
                log.Info($"设备消息处理结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"处理设备消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理XML消息
        /// </summary>
        public async Task<string> ProcessXmlMessageAsync(string xmlContent)
        {
            try
            {
                log.Info($"处理XML消息，长度: {xmlContent.Length} 字符");
                
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;
                
                if (root == null)
                {
                    return "错误: XML格式无效";
                }
                
                // 根据XML根元素类型处理不同类型的消息
                var messageType = root.Name.LocalName;
                
                switch (messageType)
                {
                    case "EquipmentMessage":
                        return await ProcessEquipmentXmlMessage(doc);
                    case "ProductionData":
                        return await ProcessProductionXmlMessage(doc);
                    case "AlarmMessage":
                        return await ProcessAlarmXmlMessage(doc);
                    default:
                        return await ProcessGenericXmlMessage(doc);
                }
            }
            catch (Exception ex)
            {
                log.Error($"处理XML消息失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理设备XML消息
        /// </summary>
        private async Task<string> ProcessEquipmentXmlMessage(XDocument doc)
        {
            var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "Unknown";
            var messageType = doc.Root?.Element("MessageType")?.Value ?? "Unknown";
            var messageContent = doc.Root?.Element("MessageContent")?.Value ?? "";
            
            var equipmentMessage = new EquipmentMessage
            {
                EquipmentID = equipmentId,
                MessageType = messageType,
                MessageContent = messageContent,
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>()
            };
            
            // 添加额外属性
            var propertiesElement = doc.Root?.Element("Properties");
            if (propertiesElement != null)
            {
                foreach (var prop in propertiesElement.Elements())
                {
                    equipmentMessage.Properties[prop.Name.LocalName] = prop.Value;
                }
            }
            
            return await ProcessEquipmentMessageAsync(equipmentMessage);
        }
        
        /// <summary>
        /// 处理生产数据XML消息
        /// </summary>
        private async Task<string> ProcessProductionXmlMessage(XDocument doc)
        {
            var batchId = doc.Root?.Element("BatchId")?.Value ?? "Unknown";
            var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "Unknown";
            var processStepId = doc.Root?.Element("ProcessStepId")?.Value ?? "Unknown";
            
            var productionData = new ProductionData
            {
                LotId = batchId,
                EquipmentId = equipmentId,
                ProcessStepId = processStepId,
                StartTime = DateTime.Now,
                Status = "Completed",
                Result = "Success",
                OperatorId = "System",
                Measurements = new Dictionary<string, double>()
            };
            
            // 添加测量数据
            var measurementsElement = doc.Root?.Element("Measurements");
            if (measurementsElement != null)
            {
                foreach (var measurement in measurementsElement.Elements())
                {
                    if (double.TryParse(measurement.Value, out double value))
                    {
                        productionData.Measurements[measurement.Name.LocalName] = value;
                    }
                }
            }
            
            var equipmentMessage = new EquipmentMessage
            {
                EquipmentID = equipmentId,
                MessageType = "PRODUCTION_DATA",
                MessageContent = $"Production data for batch {batchId}",
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>()
                {
                    ["ProductionData"] = productionData
                }
            };
            
            return await ProcessEquipmentMessageAsync(equipmentMessage);
        }
        
        /// <summary>
        /// 处理报警XML消息
        /// </summary>
        private async Task<string> ProcessAlarmXmlMessage(XDocument doc)
        {
            var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "Unknown";
            var alarmCode = doc.Root?.Element("AlarmCode")?.Value ?? "Unknown";
            var description = doc.Root?.Element("Description")?.Value ?? "";
            
            var equipmentMessage = new EquipmentMessage
            {
                EquipmentID = equipmentId,
                MessageType = "ALARM",
                MessageContent = $"Alarm {alarmCode}: {description}",
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>()
                {
                    ["AlarmCode"] = alarmCode,
                    ["Description"] = description
                }
            };
            
            return await ProcessEquipmentMessageAsync(equipmentMessage);
        }
        
        /// <summary>
        /// 处理通用XML消息
        /// </summary>
        private async Task<string> ProcessGenericXmlMessage(XDocument doc)
        {
            var rootName = doc.Root?.Name?.LocalName ?? "Unknown";
            var equipmentId = doc.Root?.Attribute("EquipmentId")?.Value ?? "Unknown";
            
            var equipmentMessage = new EquipmentMessage
            {
                EquipmentID = equipmentId,
                MessageType = rootName,
                MessageContent = doc.ToString(),
                Timestamp = DateTime.Now,
                Properties = new Dictionary<string, object>()
            };
            
            return await ProcessEquipmentMessageAsync(equipmentMessage);
        }
        
        /// <summary>
        /// 发送设备状态到MES服务
        /// </summary>
        public async Task<bool> SendEquipmentStatusAsync(string equipmentId, string status)
        {
            try
            {
                log.Info($"发送设备状态: {equipmentId} = {status}");
                
                var result = await Task.Run(() => _mesService.SetEquipmentStatus(equipmentId, status));
                
                log.Info($"设备状态发送结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"发送设备状态失败: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 获取设备状态
        /// </summary>
        public async Task<EquipmentStatus> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                log.Info($"获取设备状态: {equipmentId}");
                
                var result = await Task.Run(() => _mesService.GetEquipmentStatus(equipmentId));
                
                log.Info($"获取设备状态结果: {result.Status}");
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
        /// 发送报警信息
        /// </summary>
        public async Task<bool> SendAlarmAsync(string equipmentId, string alarmCode, string description)
        {
            try
            {
                log.Info($"发送报警: {equipmentId} - {alarmCode}: {description}");
                
                var result = await Task.Run(() => _mesService.SendAlarm(equipmentId, alarmCode, description));
                
                log.Info($"报警发送结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                log.Error($"发送报警失败: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                (_mesService as IDisposable)?.Dispose();
                _wcfChannelFactory?.Close();
            }
            catch (Exception ex)
            {
                log.Error("释放TibcoIntegrationService资源时出错", ex);
            }
        }
    }
}