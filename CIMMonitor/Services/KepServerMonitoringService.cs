using CIMMonitor.Models.KepServer;
using ThreadingTimer = System.Threading.Timer;
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
        List<KepServer> GetServers();
        KepServer? GetServerById(string serverId);
        List<BitAddress> GetBitAddresses(string serverId, string projectId);
        List<WordAddress> GetWordAddresses(string serverId, string projectId);
        event EventHandler<DataChangedEvent>? DataChanged;
        event EventHandler<MappingTriggeredEvent>? MappingTriggered;
        MonitoringStatistics GetStatistics(string serverId);
    }

    /// <summary>
    /// KepServer监控服务实现
    /// </summary>
    public class KepServerMonitoringService : IKepServerMonitoringService, IDisposable
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(KepServerMonitoringService));
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private KepServerConfig? _config;
        private readonly Dictionary<string, MonitoringStatistics> _statistics = new();
        private readonly List<ThreadingTimer> _monitoringTimers = new();
        private bool _isRunning = false;

        public event EventHandler<DataChangedEvent>? DataChanged;
        public event EventHandler<MappingTriggeredEvent>? MappingTriggered;

        public KepServerMonitoringService()
        {
        }

        /// <summary>
        /// 初始化配置（支持XML格式）
        /// </summary>
        public async Task<bool> InitializeAsync(string configFilePath)
        {
            try
            {
                _logger.Info($"初始化KepServer监控服务，配置文件: {configFilePath}");

                // 检查XML配置文件是否存在
                var xmlPath = configFilePath.Replace(".json", ".xml");
                if (File.Exists(xmlPath))
                {
                    configFilePath = xmlPath;
                    _logger.Info("使用XML配置文件");
                }

                var xmlContent = await File.ReadAllTextAsync(configFilePath);
                _config = ParseXmlConfiguration(xmlContent);

                if (_config == null)
                {
                    _logger.Error("配置文件加载失败");
                    return false;
                }

                _logger.Info($"配置文件加载成功，共 { _config.Servers.Count} 个服务器");

                // 初始化统计信息
                foreach (var server in _config.Servers)
                {
                    _statistics[server.ServerId] = new MonitoringStatistics
                    {
                        ServerId = server.ServerId,
                        StartTime = DateTime.Now
                    };
                }

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
        /// 解析XML配置文件
        /// </summary>
        private KepServerConfig ParseXmlConfiguration(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var config = new KepServerConfig();

                // 解析KepServerSettings
                var settings = doc.Root?.Element("KepServerSettings");
                if (settings != null)
                {
                    config.KepServerSettings = new KepServerSettings
                    {
                        ConnectionTimeout = int.Parse(settings.Element("ConnectionTimeout")?.Value ?? "30000"),
                        ReconnectInterval = int.Parse(settings.Element("ReconnectInterval")?.Value ?? "10000"),
                        MaxRetries = int.Parse(settings.Element("MaxRetries")?.Value ?? "3"),
                        UpdateRate = int.Parse(settings.Element("UpdateRate")?.Value ?? "100"),
                        EnableLogging = bool.Parse(settings.Element("EnableLogging")?.Value ?? "true"),
                        LogLevel = settings.Element("LogLevel")?.Value ?? "Info"
                    };
                }

                // 解析Servers
                config.Servers = new List<KepServer>();
                var serversElement = doc.Root?.Element("Servers");
                if (serversElement != null)
                {
                    foreach (var serverElement in serversElement.Elements("Server"))
                    {
                        var server = new KepServer
                        {
                            ServerId = serverElement.Attribute("ServerId")?.Value ?? "",
                            ServerName = serverElement.Attribute("ServerName")?.Value ?? "",
                            Host = serverElement.Attribute("Host")?.Value ?? "",
                            Port = int.Parse(serverElement.Attribute("Port")?.Value ?? "49320"),
                            ProtocolType = serverElement.Attribute("ProtocolType")?.Value ?? "opc",
                            Description = serverElement.Attribute("Description")?.Value ?? "",
                            Enabled = bool.Parse(serverElement.Attribute("Enabled")?.Value ?? "true"),
                            ConnectionStatus = "Disconnected"
                        };

                        // 解析Projects
                        server.Projects = new List<Project>();
                        var projectsElement = serverElement.Element("Projects");
                        if (projectsElement != null)
                        {
                            foreach (var projectElement in projectsElement.Elements("Project"))
                            {
                                var project = new Project
                                {
                                    ProjectId = projectElement.Attribute("ProjectId")?.Value ?? "",
                                    ProjectName = projectElement.Attribute("ProjectName")?.Value ?? "",
                                    Description = projectElement.Attribute("Description")?.Value ?? "",
                                    Enabled = bool.Parse(projectElement.Attribute("Enabled")?.Value ?? "true")
                                };

                                // 解析DataGroups
                                project.DataGroups = new List<DataGroup>();
                                var dataGroupsElement = projectElement.Element("DataGroups");
                                if (dataGroupsElement != null)
                                {
                                    foreach (var groupElement in dataGroupsElement.Elements("DataGroup"))
                                    {
                                        var dataGroup = new DataGroup
                                        {
                                            GroupId = groupElement.Attribute("GroupId")?.Value ?? "",
                                            GroupName = groupElement.Attribute("GroupName")?.Value ?? "",
                                            Enabled = bool.Parse(groupElement.Attribute("Enabled")?.Value ?? "true"),
                                            UpdateRate = int.Parse(groupElement.Attribute("UpdateRate")?.Value ?? "500")
                                        };

                                        // 解析BitAddresses
                                        dataGroup.BitAddresses = new List<BitAddress>();
                                        var bitAddressesElement = groupElement.Element("BitAddresses");
                                        if (bitAddressesElement != null)
                                        {
                                            foreach (var bitElement in bitAddressesElement.Elements("BitAddress"))
                                            {
                                                dataGroup.BitAddresses.Add(new BitAddress
                                                {
                                                    AddressId = bitElement.Attribute("AddressId")?.Value ?? "",
                                                    Address = bitElement.Attribute("Address")?.Value ?? "",
                                                    Description = bitElement.Attribute("Description")?.Value ?? "",
                                                    DataType = bitElement.Attribute("DataType")?.Value ?? "Boolean",
                                                    CurrentValue = bool.Parse(bitElement.Attribute("InitialValue")?.Value ?? "false"),
                                                    PreviousValue = bool.Parse(bitElement.Attribute("InitialValue")?.Value ?? "false"),
                                                    Monitored = bool.Parse(bitElement.Attribute("Monitored")?.Value ?? "true")
                                                });
                                            }
                                        }

                                        // 解析WordAddresses
                                        dataGroup.WordAddresses = new List<WordAddress>();
                                        var wordAddressesElement = groupElement.Element("WordAddresses");
                                        if (wordAddressesElement != null)
                                        {
                                            foreach (var wordElement in wordAddressesElement.Elements("WordAddress"))
                                            {
                                                dataGroup.WordAddresses.Add(new WordAddress
                                                {
                                                    AddressId = wordElement.Attribute("AddressId")?.Value ?? "",
                                                    Address = wordElement.Attribute("Address")?.Value ?? "",
                                                    Description = wordElement.Attribute("Description")?.Value ?? "",
                                                    DataType = wordElement.Attribute("DataType")?.Value ?? "String",
                                                    CurrentValue = wordElement.Attribute("InitialValue")?.Value ?? "",
                                                    PreviousValue = wordElement.Attribute("InitialValue")?.Value ?? "",
                                                    MaxLength = int.Parse(wordElement.Attribute("MaxLength")?.Value ?? "100")
                                                });
                                            }
                                        }

                                        // 解析BitWordMappings
                                        dataGroup.BitWordMappings = new List<BitWordMapping>();
                                        var mappingsElement = groupElement.Element("BitWordMappings");
                                        if (mappingsElement != null)
                                        {
                                            foreach (var mappingElement in mappingsElement.Elements("BitWordMapping"))
                                            {
                                                dataGroup.BitWordMappings.Add(new BitWordMapping
                                                {
                                                    MappingId = mappingElement.Attribute("MappingId")?.Value ?? "",
                                                    BitAddressId = mappingElement.Attribute("BitAddressId")?.Value ?? "",
                                                    WordAddressId = mappingElement.Attribute("WordAddressId")?.Value ?? "",
                                                    TriggerCondition = mappingElement.Attribute("TriggerCondition")?.Value ?? "RisingEdge",
                                                    Action = mappingElement.Attribute("Action")?.Value ?? "ReadWord",
                                                    Description = mappingElement.Attribute("Description")?.Value ?? "",
                                                    Enabled = bool.Parse(mappingElement.Attribute("Enabled")?.Value ?? "true")
                                                });
                                            }
                                        }

                                        project.DataGroups.Add(dataGroup);
                                    }
                                }

                                server.Projects.Add(project);
                            }
                        }

                        config.Servers.Add(server);
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.Error("解析XML配置文件失败", ex);
                return null;
            }
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (_config == null)
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

                foreach (var server in _config.Servers.Where(s => s.Enabled))
                {
                    StartServerMonitoring(server);
                }

                _logger.Info("KepServer监控服务已启动");
            }
            finally
            {
                _semaphore.Release();
            }
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
                foreach (var timer in _monitoringTimers)
                {
                    timer?.Dispose();
                }
                _monitoringTimers.Clear();

                // 更新服务器状态
                foreach (var server in _config?.Servers ?? new())
                {
                    server.ConnectionStatus = "Disconnected";
                }

                _logger.Info("KepServer监控服务已停止");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 启动单个服务器监控
        /// </summary>
        private void StartServerMonitoring(KepServer server)
        {
            _logger.Info($"启动服务器监控: {server.ServerName} ({server.Host}:{server.Port})");

            // 模拟连接状态
            server.ConnectionStatus = "Connected";
            server.LastConnected = DateTime.Now;

            // 为每个项目创建监控定时器
            foreach (var project in server.Projects.Where(p => p.Enabled))
            {
                foreach (var group in project.DataGroups.Where(g => g.Enabled))
                {
                    var timer = new ThreadingTimer(async _ => await MonitorGroupAsync(server, project, group), null,
                        TimeSpan.Zero, TimeSpan.FromMilliseconds(group.UpdateRate));
                    _monitoringTimers.Add(timer);
                }
            }

            _logger.Info($"服务器 {server.ServerName} 监控已启动");
        }

        /// <summary>
        /// 监控数据组
        /// </summary>
        private async Task MonitorGroupAsync(KepServer server, Project project, DataGroup group)
        {
            try
            {
                // 模拟从KepServer读取数据
                await SimulateDataRead(server, project, group);

                // 检查Bit变化
                await CheckBitChangesAsync(server, project, group);

                // 检查Word变化
                await CheckWordChangesAsync(server, project, group);

                // 检查映射触发
                await CheckMappingTriggersAsync(server, project, group);
            }
            catch (Exception ex)
            {
                _logger.Error($"监控组 {group.GroupId} 时发生错误", ex);
//                 var stats1 = _statistics[server.ServerId]; Interlocked.Increment(ref stats1.TotalErrors); _statistics[server.ServerId] = stats1;
            }
        }

        /// <summary>
        /// 模拟数据读取（实际实现中应连接到真实的KepServer）
        /// </summary>
        private async Task SimulateDataRead(KepServer server, Project project, DataGroup group)
        {
            // 模拟网络延迟
            await Task.Delay(10);

            // 随机更新Bit值（5%概率）
            foreach (var bit in group.BitAddresses.Where(b => b.Monitored))
            {
                if (new Random().Next(0, 100) < 5)
                {
                    bit.PreviousValue = bit.CurrentValue;
                    bit.CurrentValue = !bit.CurrentValue;
                    bit.LastChanged = DateTime.Now;
                }
            }

            // 随机更新Word值（3%概率）
            foreach (var word in group.WordAddresses)
            {
                if (new Random().Next(0, 100) < 3)
                {
                    word.PreviousValue = word.CurrentValue;
                    word.CurrentValue = GenerateRandomData(word.Address);
                    word.LastChanged = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// 生成随机数据
        /// </summary>
        private string GenerateRandomData(string address)
        {
            var random = new Random();
            if (address.Contains("TEMPERATURE"))
            {
                return $"{random.Next(60, 90)}°C";
            }
            else if (address.Contains("PRESSURE"))
            {
                return $"{random.Next(1, 5)}Pa";
            }
            else if (address.Contains("BATCH"))
            {
                return $"BATCH{random.Next(1000, 9999)}";
            }
            else if (address.Contains("PRODUCT"))
            {
                var products = new[] { "产品A", "产品B", "产品C", "产品D" };
                return products[random.Next(products.Length)];
            }
            else
            {
                return $"DATA{random.Next(100, 999)}";
            }
        }

        /// <summary>
        /// 检查Bit变化
        /// </summary>
        private async Task CheckBitChangesAsync(KepServer server, Project project, DataGroup group)
        {
            foreach (var bit in group.BitAddresses.Where(b => b.Monitored))
            {
                if (bit.CurrentValue != bit.PreviousValue)
                {
                    var changeType = bit.CurrentValue ? "RisingEdge" : "FallingEdge";

                    _logger.Info($"Bit变化: {bit.Address} {changeType} ({bit.PreviousValue} -> {bit.CurrentValue})");

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEvent
                    {
                        ServerId = server.ServerId,
                        ProjectId = project.ProjectId,
                        GroupId = group.GroupId,
                        AddressId = bit.AddressId,
                        Address = bit.Address,
                        DataType = "Boolean",
                        OldValue = bit.PreviousValue,
                        NewValue = bit.CurrentValue,
                        Timestamp = DateTime.Now,
                        ChangeType = "BitChange"
                    });

                    // 更新统计
//                     var stats2 = _statistics[server.ServerId]; Interlocked.Increment(ref stats2.TotalBitChanges); _statistics[server.ServerId] = stats2;

                    bit.PreviousValue = bit.CurrentValue;
                }
            }
        }

        /// <summary>
        /// 检查Word变化
        /// </summary>
        private async Task CheckWordChangesAsync(KepServer server, Project project, DataGroup group)
        {
            foreach (var word in group.WordAddresses)
            {
                if (word.CurrentValue != word.PreviousValue)
                {
                    _logger.Info($"Word变化: {word.Address} ({word.PreviousValue} -> {word.CurrentValue})");

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEvent
                    {
                        ServerId = server.ServerId,
                        ProjectId = project.ProjectId,
                        GroupId = group.GroupId,
                        AddressId = word.AddressId,
                        Address = word.Address,
                        DataType = "String",
                        OldValue = word.PreviousValue,
                        NewValue = word.CurrentValue,
                        Timestamp = DateTime.Now,
                        ChangeType = "WordChange"
                    });

                    // 更新统计
//                     var stats3 = _statistics[server.ServerId]; Interlocked.Increment(ref stats3.TotalWordChanges); _statistics[server.ServerId] = stats3;

                    word.PreviousValue = word.CurrentValue;
                }
            }
        }

        /// <summary>
        /// 检查映射触发
        /// </summary>
        private async Task CheckMappingTriggersAsync(KepServer server, Project project, DataGroup group)
        {
            foreach (var mapping in group.BitWordMappings.Where(m => m.Enabled))
            {
                var bit = group.BitAddresses.FirstOrDefault(b => b.AddressId == mapping.BitAddressId);
                var word = group.WordAddresses.FirstOrDefault(w => w.AddressId == mapping.WordAddressId);

                if (bit == null || word == null) continue;

                bool shouldTrigger = false;

                switch (mapping.TriggerCondition)
                {
                    case "RisingEdge":
                        shouldTrigger = bit.CurrentValue && !bit.PreviousValue;
                        break;
                    case "FallingEdge":
                        shouldTrigger = !bit.CurrentValue && bit.PreviousValue;
                        break;
                    case "BothEdges":
                        shouldTrigger = bit.CurrentValue != bit.PreviousValue;
                        break;
                    case "LevelHigh":
                        shouldTrigger = bit.CurrentValue;
                        break;
                    case "LevelLow":
                        shouldTrigger = !bit.CurrentValue;
                        break;
                }

                if (shouldTrigger)
                {
                    await TriggerMappingAsync(server, project, group, mapping, bit, word);
                }
            }
        }

        /// <summary>
        /// 执行映射触发
        /// </summary>
        private async Task TriggerMappingAsync(KepServer server, Project project, DataGroup group,
            BitWordMapping mapping, BitAddress bit, WordAddress word)
        {
            try
            {
                _logger.Info($"映射触发: {mapping.MappingId} - Bit: {bit.Address} -> Word: {word.Address} = {word.CurrentValue}");

                var triggeredEvent = new MappingTriggeredEvent
                {
                    MappingId = mapping.MappingId,
                    BitAddressId = bit.AddressId,
                    WordAddressId = word.AddressId,
                    TriggerCondition = mapping.TriggerCondition,
                    BitOldValue = bit.PreviousValue,
                    BitNewValue = bit.CurrentValue,
                    WordValue = word.CurrentValue,
                    TriggeredTime = DateTime.Now,
                    ServerId = server.ServerId,
                    ProjectId = project.ProjectId,
                    GroupId = group.GroupId
                };

                // 触发映射事件
                MappingTriggered?.Invoke(this, triggeredEvent);

                // 更新统计
//                 var stats4 = _statistics[server.ServerId]; Interlocked.Increment(ref stats4.TotalMappingTriggers); _statistics[server.ServerId] = stats4;

                // 执行动作
                switch (mapping.Action)
                {
                    case "ReadWord":
                        _logger.Info($"读取Word值: {word.Address} = {word.CurrentValue}");
                        break;
                    case "WriteWord":
                        _logger.Info($"写入Word值: {word.Address}");
                        break;
                    case "Publish":
                        _logger.Info($"发布事件: {word.CurrentValue}");
                        break;
                    case "Log":
                        _logger.Info($"记录日志: Bit={bit.CurrentValue}, Word={word.CurrentValue}");
                        break;
                }

                mapping.LastTriggered = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.Error($"映射触发失败: {mapping.MappingId}", ex);
//                 var stats5 = _statistics[server.ServerId]; Interlocked.Increment(ref stats5.TotalErrors); _statistics[server.ServerId] = stats5;
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
        /// 获取Bit地址
        /// </summary>
        public List<BitAddress> GetBitAddresses(string serverId, string projectId)
        {
            var server = GetServerById(serverId);
            if (server == null) return new List<BitAddress>();

            var project = server.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return new List<BitAddress>();

            return project.DataGroups.SelectMany(g => g.BitAddresses).ToList();
        }

        /// <summary>
        /// 获取Word地址
        /// </summary>
        public List<WordAddress> GetWordAddresses(string serverId, string projectId)
        {
            var server = GetServerById(serverId);
            if (server == null) return new List<WordAddress>();

            var project = server.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return new List<WordAddress>();

            return project.DataGroups.SelectMany(g => g.WordAddresses).ToList();
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
