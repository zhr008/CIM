using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CIMMonitor.Models.KepServer;

namespace CIMMonitor.Services
{
    /// <summary>
    /// KEPServer读写器，专门处理基于事件驱动的Bit/Word读写逻辑
    /// </summary>
    public class KepServerReadWriter
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(KepServerReadWriter));
        
        // 存储Bit标签及其触发关系
        private readonly Dictionary<string, List<TriggeredTag>> _bitTriggers = new();
        // 存储所有标签的当前值
        private readonly Dictionary<string, object> _tagValues = new();
        // 存储上一次的Bit值，用于边沿检测
        private readonly Dictionary<string, bool> _previousBitValues = new();

        public KepServerReadWriter()
        {
        }

        /// <summary>
        /// 从KEPServer配置文件加载标签结构
        /// </summary>
        public async Task<bool> LoadConfigurationAsync(string configPath)
        {
            try
            {
                _logger.Info($"开始加载KEPServer配置: {configPath}");
                
                var xmlContent = await File.ReadAllTextAsync(configPath);
                var doc = XDocument.Parse(xmlContent);

                // 解析所有通道和设备中的标签
                var channels = doc.Root?.Element("Channels");
                if (channels != null)
                {
                    foreach (var channel in channels.Elements("Channel"))
                    {
                        var devices = channel.Element("Devices");
                        if (devices != null)
                        {
                            foreach (var device in devices.Elements("Device"))
                            {
                                await ProcessDeviceTags(device);
                            }
                        }
                    }
                }

                _logger.Info($"KEPServer配置加载完成，共处理了 {_tagValues.Count} 个标签");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("加载KEPServer配置失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 处理单个设备的标签
        /// </summary>
        private async Task ProcessDeviceTags(XElement deviceElement)
        {
            var deviceName = deviceElement.Attribute("Name")?.Value;
            var tagGroups = deviceElement.Element("TagGroups");
            if (tagGroups != null)
            {
                foreach (var tagGroup in tagGroups.Elements("TagGroup"))
                {
                    var groupName = tagGroup.Attribute("Name")?.Value;
                    var tags = tagGroup.Element("Tags");
                    
                    if (tags != null)
                    {
                        foreach (var tag in tags.Elements("Tag"))
                        {
                            await ProcessSingleTag(tag, groupName, deviceName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理单个标签，建立触发关系
        /// </summary>
        private async Task ProcessSingleTag(XElement tagElement, string groupName, string deviceName = "")
        {
            var tagName = tagElement.Attribute("Name")?.Value;
            var address = tagElement.Attribute("Address")?.Value;
            var dataType = tagElement.Attribute("DataType")?.Value;
            var accessRights = tagElement.Attribute("AccessRights")?.Value;
            
            if (string.IsNullOrEmpty(tagName)) return;

            // 解析扫描率
            var scanRateStr = tagElement.Element("Properties")?.Elements("Property")
                .FirstOrDefault(p => p.Attribute("Name")?.Value == "ScanRate")?.Attribute("Value")?.Value;
            int scanRate = 0;
            if (int.TryParse(scanRateStr, out var rate))
            {
                scanRate = rate;
            }

            // 解析触发的标签
            var triggeredTagsStr = tagElement.Element("Properties")?.Elements("Property")
                .FirstOrDefault(p => p.Attribute("Name")?.Value == "TriggeredTags")?.Attribute("Value")?.Value;

            // 根据组名判断是Bit还是Word标签
            bool isBitTag = groupName?.Equals("Bit", StringComparison.OrdinalIgnoreCase) == true;
            
            // 初始化标签值
            if (isBitTag && dataType?.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase) == true)
            {
                _tagValues[tagName] = false; // 默认布尔值为false
                _previousBitValues[tagName] = false;
                
                // 如果此Bit标签有触发其他标签的配置
                if (!string.IsNullOrEmpty(triggeredTagsStr))
                {
                    var triggeredTags = triggeredTagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var triggeredTag in triggeredTags)
                    {
                        var triggerKey = $"{deviceName}_{tagName}";
                        if (!_bitTriggers.ContainsKey(triggerKey))
                        {
                            _bitTriggers[triggerKey] = new List<TriggeredTag>();
                        }
                        
                        _bitTriggers[triggerKey].Add(new TriggeredTag
                        {
                            TargetTag = triggeredTag.Trim(),
                            TriggerCondition = DetermineTriggerCondition(tagName, triggeredTag.Trim()),
                            Action = "Read"
                        });
                    }
                }
            }
            else
            {
                // Word标签，初始值为空
                _tagValues[tagName] = string.Empty;
            }
        }

        /// <summary>
        /// 确定触发条件（基于标签名称）
        /// </summary>
        private TriggerCondition DetermineTriggerCondition(string sourceTag, string targetTag)
        {
            // 根据标签名称和上下文确定触发条件
            if (sourceTag.Contains("Start") || sourceTag.Contains("CMD"))
            {
                return TriggerCondition.RisingEdge; // 启动命令通常是上升沿触发
            }
            else if (sourceTag.Contains("Status") || sourceTag.Contains("Run"))
            {
                return TriggerCondition.RisingEdge; // 状态变化通常是上升沿触发
            }
            else if (sourceTag.Contains("Alarm") || sourceTag.Contains("Error"))
            {
                return TriggerCondition.BothEdges; // 报警信号可能是任意边沿
            }
            else
            {
                return TriggerCondition.RisingEdge; // 默认上升沿触发
            }
        }

        /// <summary>
        /// 读取单个标签的值
        /// </summary>
        public async Task<object> ReadTagAsync(string tagName)
        {
            // 模拟从KEPServer读取标签值
            // 实际实现中这里应该调用KEPServer的API
            
            // 模拟一些合理的值
            if (_tagValues.ContainsKey(tagName))
            {
                var currentValue = _tagValues[tagName];
                
                // 对于布尔类型，模拟一些随机变化（仅用于演示）
                if (currentValue is bool boolVal)
                {
                    // 模拟值的变化（仅用于演示目的）
                    var newValue = boolVal;
                    if (new Random().NextDouble() < 0.01) // 1% 概率改变
                    {
                        newValue = !boolVal;
                        _tagValues[tagName] = newValue;
                        _previousBitValues[tagName] = boolVal;
                    }
                    return newValue;
                }
                
                return currentValue;
            }
            
            return null;
        }

        /// <summary>
        /// 批量读取多个标签的值
        /// </summary>
        public async Task<Dictionary<string, object>> ReadTagsAsync(IEnumerable<string> tagNames)
        {
            var results = new Dictionary<string, object>();
            
            foreach (var tagName in tagNames)
            {
                var value = await ReadTagAsync(tagName);
                results[tagName] = value;
            }
            
            return results;
        }

        /// <summary>
        /// 写入标签值
        /// </summary>
        public async Task<bool> WriteTagAsync(string tagName, object value)
        {
            // 模拟向KEPServer写入标签值
            // 实际实现中这里应该调用KEPServer的API
            
            if (_tagValues.ContainsKey(tagName))
            {
                _tagValues[tagName] = value;
                
                // 如果是Bit标签，记录之前的值用于边沿检测
                if (_previousBitValues.ContainsKey(tagName))
                {
                    _previousBitValues[tagName] = Convert.ToBoolean(_tagValues[tagName]);
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 读取Bit组标签（高频轮询）
        /// </summary>
        public async Task<Dictionary<string, object>> ReadBitGroupAsync()
        {
            var bitTags = _tagValues.Keys.Where(k => _previousBitValues.ContainsKey(k)).ToList();
            return await ReadTagsAsync(bitTags);
        }

        /// <summary>
        /// 根据Bit变化触发Word标签读取
        /// </summary>
        public async Task<Dictionary<string, object>> ProcessBitTriggersAsync(Dictionary<string, object> bitValues, string deviceId = "")
        {
            var triggeredResults = new Dictionary<string, object>();
            
            foreach (var bitEntry in bitValues)
            {
                var tagName = bitEntry.Key;
                var currentValue = Convert.ToBoolean(bitEntry.Value);
                
                if (_previousBitValues.ContainsKey(tagName))
                {
                    var previousValue = _previousBitValues[tagName];
                    var triggerKey = $"{deviceId}_{tagName}";
                    
                    // 检查是否满足触发条件
                    bool shouldTrigger = false;
                    var triggerCondition = TriggerCondition.RisingEdge; // 默认条件
                    
                    if (currentValue && !previousValue)
                    {
                        // 上升沿
                        shouldTrigger = true;
                        triggerCondition = TriggerCondition.RisingEdge;
                    }
                    else if (!currentValue && previousValue)
                    {
                        // 下降沿
                        shouldTrigger = true;
                        triggerCondition = TriggerCondition.FallingEdge;
                    }
                    else if (currentValue != previousValue)
                    {
                        // 任意边沿
                        shouldTrigger = true;
                        triggerCondition = TriggerCondition.BothEdges;
                    }
                    
                    if (shouldTrigger && _bitTriggers.ContainsKey(triggerKey))
                    {
                        var triggeredTags = _bitTriggers[triggerKey];
                        
                        foreach (var triggeredTag in triggeredTags)
                        {
                            if (ShouldExecuteTrigger(triggerCondition, triggeredTag.TriggerCondition))
                            {
                                // 读取被触发的Word标签
                                var wordValue = await ReadTagAsync(triggeredTag.TargetTag);
                                triggeredResults[triggeredTag.TargetTag] = wordValue;
                                
                                _logger.Info($"触发读取: {tagName} -> {triggeredTag.TargetTag} = {wordValue}");
                            }
                        }
                    }
                    
                    // 更新前一次的值
                    _previousBitValues[tagName] = currentValue;
                }
            }
            
            return triggeredResults;
        }

        /// <summary>
        /// 判断是否应该执行触发
        /// </summary>
        private bool ShouldExecuteTrigger(TriggerCondition actualCondition, TriggerCondition configuredCondition)
        {
            switch (configuredCondition)
            {
                case TriggerCondition.RisingEdge:
                    return actualCondition == TriggerCondition.RisingEdge;
                case TriggerCondition.FallingEdge:
                    return actualCondition == TriggerCondition.FallingEdge;
                case TriggerCondition.BothEdges:
                    return actualCondition == TriggerCondition.RisingEdge || actualCondition == TriggerCondition.FallingEdge;
                case TriggerCondition.LevelHigh:
                    return true; // 级别触发通常由其他逻辑处理
                case TriggerCondition.LevelLow:
                    return true; // 级别触发通常由其他逻辑处理
                default:
                    return actualCondition == TriggerCondition.RisingEdge;
            }
        }

        /// <summary>
        /// 获取所有标签名称
        /// </summary>
        public IEnumerable<string> GetAllTagNames()
        {
            return _tagValues.Keys;
        }

        /// <summary>
        /// 获取Bit标签名称
        /// </summary>
        public IEnumerable<string> GetBitTagNames()
        {
            return _tagValues.Keys.Where(k => _previousBitValues.ContainsKey(k));
        }

        /// <summary>
        /// 获取Word标签名称
        /// </summary>
        public IEnumerable<string> GetWordTagNames()
        {
            return _tagValues.Keys.Where(k => !_previousBitValues.ContainsKey(k));
        }
    }

    /// <summary>
    /// 触发标签信息
    /// </summary>
    internal class TriggeredTag
    {
        public string TargetTag { get; set; } = string.Empty;
        public TriggerCondition TriggerCondition { get; set; } = TriggerCondition.RisingEdge;
        public string Action { get; set; } = "Read";
    }

    /// <summary>
    /// 触发条件枚举
    /// </summary>
    internal enum TriggerCondition
    {
        RisingEdge,    // 上升沿：false -> true
        FallingEdge,   // 下降沿：true -> false
        BothEdges,     // 任意变化
        LevelHigh,     // 高电平
        LevelLow       // 低电平
    }
}