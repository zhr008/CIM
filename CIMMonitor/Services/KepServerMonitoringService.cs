using System.Xml.Serialization;
using CIMMonitor.Models;
using System.Xml.Linq;

namespace CIMMonitor.Services
{
    /// <summary>
    /// KepServer监控服务接口
    /// </summary>
    public interface IKepServerMonitoringService
    {
        Task<bool> InitializeAsync(string configFilePath);
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
        List<Channel> GetChannels();
        List<Device> GetDevices(string channelName);
        List<TagGroup> GetTagGroups(string channelName, string deviceName);
        List<Tag> GetTags(string channelName, string deviceName, string groupName);
        event DataChangedEventHandler? DataChanged;
        MonitoringStatistics GetStatistics();
    }

    /// <summary>
    /// 监控统计信息
    /// </summary>
    public class MonitoringStatistics
    {
        public string ProjectId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public int TotalTagChanges { get; set; } = 0;
        public int TotalErrors { get; set; } = 0;
        public TimeSpan Uptime { get; set; } = TimeSpan.Zero;
    }

    /// <summary>
    /// KepServer监控服务实现
    /// </summary>
    public class KepServerMonitoringService : IKepServerMonitoringService, IDisposable
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(KepServerMonitoringService));
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private KepServerProject? _project;
        private MonitoringStatistics _statistics = new();
        private readonly Dictionary<string, Timer> _groupTimers = new();
        private readonly Dictionary<string, object> _tagValues = new();
        private bool _isRunning = false;

        public event DataChangedEventHandler? DataChanged;

        public KepServerMonitoringService()
        {
        }

        /// <summary>
        /// 初始化配置（支持标准KepServer EX XML格式）
        /// </summary>
        public async Task<bool> InitializeAsync(string configFilePath)
        {
            try
            {
                _logger.Info($"初始化KepServer监控服务，配置文件: {configFilePath}");

                var xmlContent = await File.ReadAllTextAsync(configFilePath);
                _project = ParseStandardXmlConfiguration(xmlContent);

                if (_project == null)
                {
                    _logger.Error("配置文件加载失败");
                    return false;
                }

                _logger.Info($"配置文件加载成功，项目名称: {_project.Properties.PropertyList.FirstOrDefault(p => p.Name == "ProjectName")?.Value}");
                _logger.Info($"共有 {_project.Channels.Count} 个通道");

                // 初始化统计信息
                _statistics = new MonitoringStatistics
                {
                    ProjectId = _project.ProjectId,
                    StartTime = DateTime.Now
                };

                _logger.Info("KepServer监控服务初始化完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("初始化KepServer监控服务失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 解析标准KepServer EX XML配置文件
        /// </summary>
        private KepServerProject ParseStandardXmlConfiguration(string xmlContent)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(KepServerProject));
                using var reader = new StringReader(xmlContent);
                return serializer.Deserialize(reader) as KepServerProject;
            }
            catch (Exception ex)
            {
                _logger.Error("解析标准XML配置文件失败", ex);
                return null;
            }
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (_project == null)
            {
                throw new InvalidOperationException("服务未初始化");
            }

            await _semaphore.WaitAsync();
            try
            {
                if (_isRunning)
                {
                    _logger.Warn("监控服务已在运行");
                    return;
                }

                _logger.Info("启动KepServer监控服务...");

                _isRunning = true;

                // 为每个通道、设备、标签组创建监控定时器
                foreach (var channel in _project.Channels)
                {
                    foreach (var device in channel.Devices)
                    {
                        foreach (var tagGroup in device.TagGroups)
                        {
                            StartGroupMonitoring(channel, device, tagGroup);
                        }
                    }
                }

                _logger.Info("KepServer监控服务已启动");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 启动特定组的监控
        /// </summary>
        private void StartGroupMonitoring(Channel channel, Device device, TagGroup tagGroup)
        {
            // 计算最小扫描周期作为该组的扫描周期
            var minScanRate = tagGroup.Tags.Any() ? 
                tagGroup.Tags.Min(t => t.ScanRate > 0 ? t.ScanRate : 1000) : 1000;

            var timerKey = $"{channel.Name}_{device.Name}_{tagGroup.Name}";
            
            // 创建定时器进行轮询
            var timer = new Timer(async (state) =>
            {
                await MonitorGroupAsync(channel, device, tagGroup);
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(minScanRate));

            _groupTimers[timerKey] = timer;

            _logger.Debug($"已为 {channel.Name}.{device.Name}.{tagGroup.Name} 创建监控定时器，扫描周期: {minScanRate}ms");
        }

        /// <summary>
        /// 监控特定组
        /// </summary>
        private async Task MonitorGroupAsync(Channel channel, Device device, TagGroup tagGroup)
        {
            try
            {
                foreach (var tag in tagGroup.Tags)
                {
                    // 模拟从PLC读取标签值
                    var currentValue = await SimulateReadTagValueAsync(channel, device, tag);

                    var tagKey = $"{channel.Name}_{device.Name}_{tagGroup.Name}_{tag.Name}";

                    // 检查值是否发生变化
                    if (!_tagValues.ContainsKey(tagKey) || !Equals(_tagValues[tagKey], currentValue))
                    {
                        // 存储新值
                        _tagValues[tagKey] = currentValue;

                        // 触发数据变更事件
                        OnDataChanged(new DataChangedEventArgs
                        {
                            TagName = tag.Name,
                            Value = currentValue,
                            Timestamp = DateTime.Now,
                            DeviceName = device.Name,
                            ChannelName = channel.Name,
                            GroupName = tagGroup.Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"监控组 {channel.Name}.{device.Name}.{tagGroup.Name} 时发生错误", ex);
                _statistics.TotalErrors++;
            }
        }

        /// <summary>
        /// 模拟读取标签值
        /// </summary>
        private async Task<object> SimulateReadTagValueAsync(Channel channel, Device device, Tag tag)
        {
            // 在实际实现中，这里会调用真实的OPC DA客户端来读取标签值
            // 目前我们返回模拟值
            
            await Task.Delay(1); // 模拟网络延迟

            // 根据数据类型返回合适的模拟值
            return tag.DataType?.ToUpper() switch
            {
                "BOOLEAN" or "BOOL" => Random.Shared.NextDouble() > 0.95, // 5% 概率改变状态
                "BYTE" or "UINT1" => (byte)Random.Shared.Next(0, 256),
                "WORD" or "UINT16" => (ushort)Random.Shared.Next(0, 65536),
                "DWORD" or "UINT32" => (uint)Random.Shared.Next(0, int.MaxValue),
                "SINT" or "INT8" => (sbyte)Random.Shared.Next(-128, 128),
                "INT" or "INT16" => (short)Random.Shared.Next(-32768, 32768),
                "DINT" or "INT32" => Random.Shared.Next(),
                "REAL" or "FLOAT" => Math.Round(Random.Shared.NextDouble() * 1000, 2),
                "LREAL" or "DOUBLE" => Random.Shared.NextDouble() * 1000000,
                "STRING" or "WSTRING" => $"SIM_{tag.Name}_{DateTime.Now:HHmmss}",
                _ => $"SIM_{tag.Name}_VALUE" // 默认字符串值
            };
        }

        /// <summary>
        /// 触发数据变更事件
        /// </summary>
        private void OnDataChanged(DataChangedEventArgs args)
        {
            DataChanged?.Invoke(this, args);
            _statistics.TotalTagChanges++;
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_isRunning)
                {
                    return;
                }

                _logger.Info("停止KepServer监控服务...");

                _isRunning = false;

                // 停止所有定时器
                foreach (var timer in _groupTimers.Values)
                {
                    timer?.Dispose();
                }
                _groupTimers.Clear();

                _logger.Info("KepServer监控服务已停止");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取所有通道
        /// </summary>
        public List<Channel> GetChannels()
        {
            return _project?.Channels ?? new List<Channel>();
        }

        /// <summary>
        /// 获取指定通道下的设备
        /// </summary>
        public List<Device> GetDevices(string channelName)
        {
            if (_project?.Channels == null) return new List<Device>();

            var channel = _project.Channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            return channel?.Devices ?? new List<Device>();
        }

        /// <summary>
        /// 获取指定设备下的标签组
        /// </summary>
        public List<TagGroup> GetTagGroups(string channelName, string deviceName)
        {
            var device = GetDevices(channelName)
                .FirstOrDefault(d => d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
            return device?.TagGroups ?? new List<TagGroup>();
        }

        /// <summary>
        /// 获取指定标签组下的标签
        /// </summary>
        public List<Tag> GetTags(string channelName, string deviceName, string groupName)
        {
            var tagGroup = GetTagGroups(channelName, deviceName)
                .FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            return tagGroup?.Tags ?? new List<Tag>();
        }

        /// <summary>
        /// 获取监控统计信息
        /// </summary>
        public MonitoringStatistics GetStatistics()
        {
            _statistics.Uptime = DateTime.Now - _statistics.StartTime;
            return _statistics;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopMonitoringAsync().Wait();
            
            foreach (var timer in _groupTimers.Values)
            {
                timer?.Dispose();
            }
            
            _semaphore?.Dispose();
        }
    }
}
                    return $"DATA{random.Next(100, 999)}";
                }
            }
        }

        /// <summary>
        /// 检查Tag变化
        /// </summary>
        private async Task CheckTagChangesAsync(KepServer server, Channel channel, Device device, Group group)
        {
            foreach (var tag in group.Tags.Where(t => t.Monitored))
            {
                if (tag.CurrentValue != tag.PreviousValue)
                {
                    _logger.Info($"Tag变化: {tag.Address} ({tag.PreviousValue} -> {tag.CurrentValue})");

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEvent
                    {
                        ServerId = server.ServerId,
                        ChannelId = channel.ChannelId,
                        DeviceId = device.DeviceId,
                        GroupId = group.GroupId,
                        AddressId = tag.TagId,
                        Address = tag.Address,
                        DataType = tag.DataType,
                        OldValue = tag.PreviousValue,
                        NewValue = tag.CurrentValue,
                        Timestamp = DateTime.Now,
                        ChangeType = "TagChange"
                    });

                    tag.PreviousValue = tag.CurrentValue;
                }
            }
        }

        /// <summary>
        /// 检查OPC DA事件触发
        /// </summary>
        private async Task CheckOpcDaEventTriggersAsync(KepServer server, Channel channel, Device device, Group group)
        {
            foreach (var opcEvent in device.OpcDaEvents.Where(e => e.Enabled))
            {
                // 查找触发标签
                var triggerTag = FindTagInDevice(device, opcEvent.TriggerTagId);
                if (triggerTag == null) continue;

                // 检查触发条件
                bool shouldTrigger = false;
                
                // 将字符串转换为布尔值进行比较
                bool currentValue = Convert.ToBoolean(triggerTag.CurrentValue);
                bool previousValue = Convert.ToBoolean(triggerTag.PreviousValue);

                switch (opcEvent.TriggerCondition)
                {
                    case "RisingEdge":
                        shouldTrigger = currentValue && !previousValue;
                        break;
                    case "FallingEdge":
                        shouldTrigger = !currentValue && previousValue;
                        break;
                    case "BothEdges":
                        shouldTrigger = currentValue != previousValue;
                        break;
                    case "LevelHigh":
                        shouldTrigger = currentValue;
                        break;
                    case "LevelLow":
                        shouldTrigger = !currentValue;
                        break;
                }

                if (shouldTrigger)
                {
                    await TriggerOpcDaEventAsync(server, channel, device, group, opcEvent, triggerTag);
                }
            }
        }

        /// <summary>
        /// 查找设备中的标签
        /// </summary>
        private Tag? FindTagInDevice(Device device, string tagId)
        {
            foreach (var group in device.Groups)
            {
                var tag = group.Tags.FirstOrDefault(t => t.TagId == tagId);
                if (tag != null) return tag;
            }
            return null;
        }

        /// <summary>
        /// 执行OPC DA事件触发
        /// </summary>
        private async Task TriggerOpcDaEventAsync(KepServer server, Channel channel, Device device, Group group, OpcDaEvent opcEvent, Tag triggerTag)
        {
            try
            {
                _logger.Info($"OPC DA事件触发: {opcEvent.EventId} - Type: {opcEvent.EventType}, TriggerTag: {triggerTag.Address}");

                var triggeredEvent = new OpcDaEventTriggeredEvent
                {
                    EventId = opcEvent.EventId,
                    EventType = opcEvent.EventType,
                    TriggerTagId = opcEvent.TriggerTagId,
                    TriggerCondition = opcEvent.TriggerCondition,
                    TargetGroupIds = opcEvent.TargetGroupIds.Split(','),
                    ServerId = server.ServerId,
                    ChannelId = channel.ChannelId,
                    DeviceId = device.DeviceId,
                    TriggeredTime = DateTime.Now
                };

                // 触发OPC DA事件
                OpcDaEventTriggered?.Invoke(this, triggeredEvent);

                // 根据事件类型执行相应操作
                if (opcEvent.EventType.Equals("Send", StringComparison.OrdinalIgnoreCase))
                {
                    // 发送目标组的数据集
                    await HandleSendEventAsync(server, channel, device, opcEvent);
                }
                else if (opcEvent.EventType.Equals("Receive", StringComparison.OrdinalIgnoreCase))
                {
                    // 接收报告并更新标签值
                    await HandleReceiveEventAsync(server, channel, device, opcEvent);
                }

                _logger.Info($"OPC DA事件 {opcEvent.EventId} 处理完成");
            }
            catch (Exception ex)
            {
                _logger.Error($"处理OPC DA事件失败: {opcEvent.EventId}", ex);
            }
        }

        /// <summary>
        /// 处理发送事件
        /// </summary>
        private async Task HandleSendEventAsync(KepServer server, Channel channel, Device device, OpcDaEvent opcEvent)
        {
            var targetGroupIds = opcEvent.TargetGroupIds.Split(',');
            
            foreach (var groupId in targetGroupIds)
            {
                var group = device.Groups.FirstOrDefault(g => g.GroupId == groupId.Trim());
                if (group != null)
                {
                    _logger.Info($"发送组 {groupId} 的数据集");
                    
                    // 在实际实现中，这里会将数据发送到指定的目的地
                    // 模拟发送操作
                    await Task.Delay(10);
                }
            }
        }

        /// <summary>
        /// 处理接收事件
        /// </summary>
        private async Task HandleReceiveEventAsync(KepServer server, Channel channel, Device device, OpcDaEvent opcEvent)
        {
            var targetGroupIds = opcEvent.TargetGroupIds.Split(',');
            
            foreach (var groupId in targetGroupIds)
            {
                var group = device.Groups.FirstOrDefault(g => g.GroupId == groupId.Trim());
                if (group != null)
                {
                    _logger.Info($"更新组 {groupId} 的标签值");
                    
                    // 在实际实现中，这里会从报告中接收数据并更新标签
                    // 模拟接收操作
                    foreach (var tag in group.Tags)
                    {
                        tag.PreviousValue = tag.CurrentValue;
                        tag.CurrentValue = GenerateRandomData(tag.DataType, tag.Address);
                        tag.LastChanged = DateTime.Now;
                        
                        // 触发数据变化事件
                        DataChanged?.Invoke(this, new DataChangedEvent
                        {
                            ServerId = server.ServerId,
                            ChannelId = channel.ChannelId,
                            DeviceId = device.DeviceId,
                            GroupId = group.GroupId,
                            AddressId = tag.TagId,
                            Address = tag.Address,
                            DataType = tag.DataType,
                            OldValue = tag.PreviousValue,
                            NewValue = tag.CurrentValue,
                            Timestamp = DateTime.Now,
                            ChangeType = "TagUpdateFromReport"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有服务器
        /// </summary>
        public List<KepServer> GetServers()
        {
            return _config?.Servers ?? new List<KepServer>();
        }

        /// <summary>
        /// 根据ID获取服务器
        /// </summary>
        public KepServer? GetServerById(string serverId)
        {
            return _config?.Servers.FirstOrDefault(s => s.ServerId == serverId);
        }

        /// <summary>
        /// 获取标签
        /// </summary>
        public List<Tag> GetTags(string serverId, string channelId, string deviceId)
        {
            var server = GetServerById(serverId);
            if (server == null) return new List<Tag>();

            var channel = server.Channels.FirstOrDefault(c => c.ChannelId == channelId);
            if (channel == null) return new List<Tag>();

            var device = channel.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device == null) return new List<Tag>();

            return device.Groups.SelectMany(g => g.Tags).ToList();
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public MonitoringStatistics GetStatistics(string serverId)
        {
            if (_statistics.TryGetValue(serverId, out var stats))
            {
                stats.Uptime = DateTime.Now - stats.StartTime;
                return stats;
            }
            return new MonitoringStatistics { ServerId = serverId };
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            foreach (var timer in _monitoringTimers)
            {
                timer?.Dispose();
            }
        }
    }
}