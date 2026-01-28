using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Common.Models;
using log4net;

namespace Common.Services
{
    /// <summary>
    /// Tibrv服务 - 作为CIMMonitor和WCFServices之间的桥梁
    /// 数据流向: PLC/KepServerEX/HSMS → CIMMonitor → TibcoTibrvService → WCFServices → ORACLE
    /// </summary>
    public class TibrvService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TibrvService));
        
        private readonly TibrvRendezvousService _tibrvService;
        
        public TibrvService()
        {
            _tibrvService = new TibrvRendezvousService();
        }
        
        /// <summary>
        /// 处理来自CIMMonitor的设备消息
        /// 将消息转换为XML并通过TIBCO发送到WCFServices
        /// </summary>
        public async Task<string> ProcessEquipmentMessageAsync(EquipmentMessage message)
        {
            try
            {
                log.Info($"处理设备消息: {message.EquipmentID} - {message.MessageType}");
                
                // 将设备消息转换为XML格式
                var xmlContent = ConvertEquipmentMessageToXml(message);
                
                // 确定目标主题，根据消息类型发送到相应的WCFServices端点
                var subject = DetermineSubjectFromMessageType(message.MessageType, message.EquipmentID);
                
                // 通过TIBCO发送XML消息到WCFServices
                var sendResult = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
                
                if (sendResult)
                {
                    var result = $"消息已发送到主题 '{subject}'";
                    log.Info(result);
                    return result;
                }
                else
                {
                    var result = $"发送消息到主题 '{subject}' 失败";
                    log.Error(result);
                    return result;
                }
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
        /// 直接通过TIBCO发送XML消息到WCFServices
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
            
            // 直接发送原始XML内容到WCFServices
            var xmlContent = doc.ToString();
            var subject = DetermineSubjectFromMessageType(messageType, equipmentId);
            
            var sendResult = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
            
            if (sendResult)
            {
                var result = $"设备XML消息已发送到主题 '{subject}'";
                log.Info(result);
                return result;
            }
            else
            {
                var result = $"发送设备XML消息到主题 '{subject}' 失败";
                log.Error(result);
                return result;
            }
        }
        
        /// <summary>
        /// 处理生产数据XML消息
        /// 直接通过TIBCO发送XML消息到WCFServices
        /// </summary>
        private async Task<string> ProcessProductionXmlMessage(XDocument doc)
        {
            var batchId = doc.Root?.Element("BatchId")?.Value ?? "Unknown";
            var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "Unknown";
            var processStepId = doc.Root?.Element("ProcessStepId")?.Value ?? "Unknown";
            
            // 直接发送原始XML内容到WCFServices
            var xmlContent = doc.ToString();
            var subject = DetermineSubjectFromMessageType("PRODUCTION_DATA", equipmentId);
            
            var sendResult = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
            
            if (sendResult)
            {
                var result = $"生产数据XML消息已发送到主题 '{subject}'";
                log.Info(result);
                return result;
            }
            else
            {
                var result = $"发送生产数据XML消息到主题 '{subject}' 失败";
                log.Error(result);
                return result;
            }
        }
        
        /// <summary>
        /// 处理报警XML消息
        /// 直接通过TIBCO发送XML消息到WCFServices
        /// </summary>
        private async Task<string> ProcessAlarmXmlMessage(XDocument doc)
        {
            var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "Unknown";
            var alarmCode = doc.Root?.Element("AlarmCode")?.Value ?? "Unknown";
            var description = doc.Root?.Element("Description")?.Value ?? "";
            
            // 直接发送原始XML内容到WCFServices
            var xmlContent = doc.ToString();
            var subject = DetermineSubjectFromMessageType("ALARM", equipmentId);
            
            var sendResult = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
            
            if (sendResult)
            {
                var result = $"报警XML消息已发送到主题 '{subject}'";
                log.Info(result);
                return result;
            }
            else
            {
                var result = $"发送报警XML消息到主题 '{subject}' 失败";
                log.Error(result);
                return result;
            }
        }
        
        /// <summary>
        /// 处理通用XML消息
        /// 直接通过TIBCO发送XML消息到WCFServices
        /// </summary>
        private async Task<string> ProcessGenericXmlMessage(XDocument doc)
        {
            var rootName = doc.Root?.Name?.LocalName ?? "Unknown";
            var equipmentId = doc.Root?.Attribute("EquipmentId")?.Value ?? "Unknown";
            
            // 直接发送原始XML内容到WCFServices
            var xmlContent = doc.ToString();
            var subject = DetermineSubjectFromMessageType(rootName, equipmentId);
            
            var sendResult = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
            
            if (sendResult)
            {
                var result = $"通用XML消息已发送到主题 '{subject}'";
                log.Info(result);
                return result;
            }
            else
            {
                var result = $"发送通用XML消息到主题 '{subject}' 失败";
                log.Error(result);
                return result;
            }
        }
        
        /// <summary>
        /// 发送设备状态到WCFServices
        /// 通过TIBCO发送状态消息
        /// </summary>
        public async Task<bool> SendEquipmentStatusAsync(string equipmentId, string status)
        {
            try
            {
                log.Info($"发送设备状态: {equipmentId} = {status}");
                
                // 创建包含设备状态的XML消息
                var xmlContent = $@"
                <EquipmentStatusUpdate>
                    <EquipmentId>{equipmentId}</EquipmentId>
                    <Status>{status}</Status>
                    <Timestamp>{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}</Timestamp>
                </EquipmentStatusUpdate>";
                
                var subject = $"EQUIPMENT.STATUS.{equipmentId}";
                
                var result = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
                
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
        /// 由于TIBCO是发布/订阅模式，这里返回一个默认状态或从缓存中获取
        /// 实际实现中可能需要查询数据库或其他持久化存储
        /// </summary>
        public async Task<EquipmentStatus> GetEquipmentStatusAsync(string equipmentId)
        {
            try
            {
                log.Info($"获取设备状态: {equipmentId}");
                
                // 在TIBCO模式下，我们通常不能直接"请求"状态，只能订阅更新
                // 这里返回一个默认状态，实际实现中应从缓存或数据库获取最新状态
                var status = new EquipmentStatus
                {
                    EquipmentID = equipmentId,
                    Status = "Unknown",
                    LastUpdate = DateTime.Now
                };
                
                log.Info($"获取设备状态结果: {status.Status}");
                return status;
            }
            catch (Exception ex)
            {
                log.Error($"获取设备状态失败: {ex.Message}", ex);
                return new EquipmentStatus
                {
                    EquipmentID = equipmentId,
                    Status = "Error"
                };
            }
        }
        
        /// <summary>
        /// 发送报警信息到WCFServices
        /// 通过TIBCO发送报警消息
        /// </summary>
        public async Task<bool> SendAlarmAsync(string equipmentId, string alarmCode, string description)
        {
            try
            {
                log.Info($"发送报警: {equipmentId} - {alarmCode}: {description}");
                
                // 创建包含报警信息的XML消息
                var xmlContent = $@"
                <AlarmMessage>
                    <EquipmentId>{equipmentId}</EquipmentId>
                    <AlarmCode>{alarmCode}</AlarmCode>
                    <Description>{description}</Description>
                    <Timestamp>{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}</Timestamp>
                </AlarmMessage>";
                
                var subject = $"ALARM.{equipmentId}";
                
                var result = await _tibrvService.SendXmlMessageAsync(subject, xmlContent);
                
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
                // 释放TIBCO服务资源
                _tibrvService?.Dispose();
            }
            catch (Exception ex)
            {
                log.Error("释放TibrvService资源时出错", ex);
            }
        }
        
        /// <summary>
        /// 将设备消息转换为XML格式
        /// </summary>
        private string ConvertEquipmentMessageToXml(EquipmentMessage message)
        {
            var xml = $@"
            <EquipmentMessage>
                <EquipmentId>{message.EquipmentID}</EquipmentId>
                <MessageType>{message.MessageType}</MessageType>
                <MessageContent>{message.MessageContent}</MessageContent>
                <Timestamp>{message.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}</Timestamp>
                <Properties>
                    {string.Join("", message.Properties.Select(p => $"<{p.Key}>{p.Value}</{p.Key}>"))}
                </Properties>
            </EquipmentMessage>";
            
            return xml.Trim();
        }
        
        /// <summary>
        /// 根据消息类型确定TIBCO主题
        /// </summary>
        private string DetermineSubjectFromMessageType(string messageType, string equipmentId)
        {
            // 根据消息类型映射到不同的TIBCO主题
            switch (messageType.ToUpper())
            {
                case "ALARM":
                case "ALARM_MESSAGE":
                    return $"ALARM.{equipmentId}";
                case "PRODUCTION_DATA":
                    return $"PRODUCTION.DATA.{equipmentId}";
                case "STATUS_UPDATE":
                    return $"EQUIPMENT.STATUS.{equipmentId}";
                case "CONFIG_CHANGE":
                    return $"CONFIG.CHANGE.{equipmentId}";
                case "HEARTBEAT":
                    return $"HEARTBEAT.{equipmentId}";
                default:
                    return $"MESSAGES.{messageType.ToUpper()}.{equipmentId}";
            }
        }
        
        /// <summary>
        /// 执行业务逻辑处理
        /// 将业务逻辑从WCFServices迁移到此处
        /// </summary>
        public async Task<string> ProcessBusinessLogicAsync(string messageType, string xmlContent)
        {
            try
            {
                log.Info($"执行业务逻辑处理: {messageType}");
                
                // 根据消息类型执行不同的业务逻辑
                switch (messageType.ToUpper())
                {
                    case "PRODUCTION_DATA":
                        return await ProcessProductionDataLogicAsync(xmlContent);
                    case "ALARM_MESSAGE":
                        return await ProcessAlarmLogicAsync(xmlContent);
                    case "STATUS_UPDATE":
                        return await ProcessStatusUpdateLogicAsync(xmlContent);
                    case "CONFIG_CHANGE":
                        return await ProcessConfigChangeLogicAsync(xmlContent);
                    default:
                        return await ProcessGenericBusinessLogicAsync(xmlContent);
                }
            }
            catch (Exception ex)
            {
                log.Error($"执行业务逻辑失败: {ex.Message}", ex);
                return $"错误: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理生产数据业务逻辑
        /// </summary>
        private async Task<string> ProcessProductionDataLogicAsync(string xmlContent)
        {
            log.Info("处理生产数据业务逻辑");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var lotId = doc.Root?.Element("LotId")?.Value ?? "";
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "";
                var processStepId = doc.Root?.Element("ProcessStepId")?.Value ?? "";
                
                // 这里可以添加具体的业务逻辑，比如：
                // 1. 验证数据完整性
                // 2. 更新生产状态
                // 3. 记录生产历史
                // 4. 触发后续工艺步骤
                
                log.Info($"生产数据处理完成 - Lot: {lotId}, 设备: {equipmentId}, 工艺: {processStepId}");
                return "生产数据处理成功";
            }
            catch (Exception ex)
            {
                log.Error($"处理生产数据业务逻辑失败: {ex.Message}", ex);
                return $"生产数据处理失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理报警业务逻辑
        /// </summary>
        private async Task<string> ProcessAlarmLogicAsync(string xmlContent)
        {
            log.Info("处理报警业务逻辑");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "";
                var alarmCode = doc.Root?.Element("AlarmCode")?.Value ?? "";
                var description = doc.Root?.Element("Description")?.Value ?? "";
                
                // 这里可以添加具体的报警处理逻辑，比如：
                // 1. 记录报警日志
                // 2. 通知相关人员
                // 3. 触发应急响应
                // 4. 更新设备状态
                
                log.Info($"报警处理完成 - 设备: {equipmentId}, 报警码: {alarmCode}, 描述: {description}");
                return "报警处理成功";
            }
            catch (Exception ex)
            {
                log.Error($"处理报警业务逻辑失败: {ex.Message}", ex);
                return $"报警处理失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理状态更新业务逻辑
        /// </summary>
        private async Task<string> ProcessStatusUpdateLogicAsync(string xmlContent)
        {
            log.Info("处理状态更新业务逻辑");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "";
                var status = doc.Root?.Element("Status")?.Value ?? "";
                
                // 这里可以添加具体的状态更新逻辑，比如：
                // 1. 更新设备状态表
                // 2. 检查状态变更合法性
                // 3. 触发相关业务流程
                
                log.Info($"状态更新处理完成 - 设备: {equipmentId}, 状态: {status}");
                return "状态更新处理成功";
            }
            catch (Exception ex)
            {
                log.Error($"处理状态更新业务逻辑失败: {ex.Message}", ex);
                return $"状态更新处理失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理配置变更业务逻辑
        /// </summary>
        private async Task<string> ProcessConfigChangeLogicAsync(string xmlContent)
        {
            log.Info("处理配置变更业务逻辑");
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var equipmentId = doc.Root?.Element("EquipmentId")?.Value ?? "";
                var configKey = doc.Root?.Element("ConfigKey")?.Value ?? "";
                
                // 这里可以添加具体的配置变更逻辑，比如：
                // 1. 验证配置参数
                // 2. 更新配置数据库
                // 3. 通知相关组件
                // 4. 记录变更历史
                
                log.Info($"配置变更处理完成 - 设备: {equipmentId}, 配置项: {configKey}");
                return "配置变更处理成功";
            }
            catch (Exception ex)
            {
                log.Error($"处理配置变更业务逻辑失败: {ex.Message}", ex);
                return $"配置变更处理失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 处理通用业务逻辑
        /// </summary>
        private async Task<string> ProcessGenericBusinessLogicAsync(string xmlContent)
        {
            log.Info("处理通用业务逻辑");
            
            try
            {
                // 对于未知类型的消息，进行通用处理
                log.Info("执行通用业务处理流程");
                return "通用业务处理成功";
            }
            catch (Exception ex)
            {
                log.Error($"处理通用业务逻辑失败: {ex.Message}", ex);
                return $"通用业务处理失败: {ex.Message}";
            }
        }
    }
}