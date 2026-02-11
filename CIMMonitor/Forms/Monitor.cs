using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CIMMonitor.Models;

namespace CIMMonitor.Forms
{
    public partial class Monitor : Form
    {
        private System.Windows.Forms.Timer? refreshTimer;
        private int selectedDeviceId = 0;
        private List<DeviceInfo> devices = new List<DeviceInfo>();

        /// <summary>
        /// 存储打开的设备详情窗体
        /// </summary>
        private Dictionary<string, MonitorDetail> _openDetailForms = new Dictionary<string, MonitorDetail>();

        /// <summary>
        /// HSMS设备管理器
        /// </summary>
        private Services.HsmsDeviceManager? _deviceManager;

        /// <summary>
        /// 已添加的设备ID集合（避免重复添加）
        /// </summary>
        private readonly HashSet<string> _addedDeviceIds = new();

        /// <summary>
        /// 是否已加载过设备配置（用于区分首次加载和刷新）
        /// </summary>
        private bool _isDevicesLoaded = false;

        public Monitor()
        {
            InitializeComponent();

            // 绑定CheckBox列的事件处理程序
            dgvDevices.CellValueChanged += DgvDevices_CellValueChanged;

            try
            {
                // 初始化设备管理器（如果HsmsSimulator引用可用）
                try
                {
                    _deviceManager = new Services.HsmsDeviceManager();
                    _deviceManager.DeviceStatusChanged += OnDeviceStatusChanged;
                    _deviceManager.DeviceMessageReceived += OnDeviceMessageReceived;

                    // 显示成功信息
                    txtInfo.Text = "设备监控已启动，等待HSMS/OPC消息...\n";
                    txtInfo.Text += "✅ 设备管理器初始化成功\n";
                    txtInfo.Text += "✅ 事件订阅已绑定\n";
                }
                catch (Exception ex)
                {
                    // 如果初始化失败，记录但不阻止界面启动
                    txtInfo.Text = $"警告: 设备管理器初始化失败，将以只读模式运行\n{ex.Message}\n";
                    System.Diagnostics.Debug.WriteLine($"[设备监控] 设备管理器初始化失败: {ex.Message}");
                }

                LoadDevices();

                // 自动连接已启用的设备
                AutoConnectEnabledDevices();

                StartAutoRefresh();

                // 5秒后显示调试提示
                Task.Delay(5000).ContinueWith(t =>
                {
                    this.Invoke(new Action(() =>
                    {
                        txtInfo.Text += "\n💡 调试提示: 在Visual Studio中打开'输出'窗口，选择'调试'查看详细日志\n";
                        txtInfo.Text += "💡 或使用DebugView工具查看所有调试消息\n";
                    }));
                });
            }
            catch (Exception ex)
            {
                // 显示错误但允许界面继续运行
                txtInfo.Text = $"设备监控初始化错误: {ex.Message}\n{ex.StackTrace}\n\n界面将以基本模式运行。";
            }
        }

        /// <summary>
        /// 设备信息模型
        /// </summary>
        public class DeviceInfo
        {
            public string ServerId { get; set; } = "";
            public string ServerName { get; set; } = "";
            public string ProtocolType { get; set; } = "";
            public string DeviceType { get; set; } = "";  // host/EQP
            public string Host { get; set; } = "";
            public int Port { get; set; }
            public bool Enabled { get; set; }
            public bool IsOnline { get; set; }
            public int HeartbeatCount { get; set; }
            public int ResponseTimeMs { get; set; }
            public string ConnectionQuality { get; set; } = "";
            public string LastUpdate { get; set; } = "";
            public string SourceFile { get; set; } = "";
        }

        // 新增：从配置解析 DeviceId / SessionId 的辅助方法，支持 0x 前缀的十六进制或十进制字符串
        private static byte ParseDeviceIdValue(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 1;
            var s = raw.Trim();
            try
            {
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(2);
                    if (byte.TryParse(s, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hv))
                        return hv;
                }

                if (byte.TryParse(s, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out var dv))
                    return dv;

                if (byte.TryParse(s, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hv2))
                    return hv2;
            }
            catch { }
            return 1;
        }

        private static int ParseSessionIdValue(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0x1234;
            var s = raw.Trim();
            try
            {
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
                if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex)) return hex;
                if (int.TryParse(s, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec)) return dec;
            }
            catch { }
            return 0x1234;
        }

        private void LoadDevices()
        {
            try
            {
                var configDir = Path.Combine(Application.StartupPath, "Config");
                if (!Directory.Exists(configDir))
                {
                    txtInfo.Text = $"错误: Config目录不存在 {configDir}";
                    return;
                }

                var xmlFiles = Directory.GetFiles(configDir, "*.xml");
                int totalDevices = 0;
                int hsmsDevices = 0;
                int opcDevices = 0;
                int opcUaDevices = 0;
                int kepServerDevices = 0;

                // 如果不是首次加载，则只更新已存在设备的配置，不清空列表
                if (!_isDevicesLoaded)
                {
                    // 首次加载：完全重新加载
                    AddInfoText("🔄 首次加载设备配置...");
                    // 清空并重新加载设备列表
                    dgvDevices!.Rows.Clear();
                    devices.Clear();
                    _addedDeviceIds.Clear(); // 清空已添加设备ID

                    foreach (var xmlFile in xmlFiles)
                    {
                        var fileName = Path.GetFileName(xmlFile);

                        try
                        {
                            if (fileName.Equals("HsmsConfig.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                var count = LoadHsmsDevices(xmlFile);
                                totalDevices += count;
                                hsmsDevices += count;
                            }
                            else if (fileName.Equals("KepServerConfig.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                var count = LoadKepServerDevices(xmlFile);
                                totalDevices += count;
                                kepServerDevices += count;
                            }
                            else
                            {
                                var count = LoadGenericDevices(xmlFile);
                                totalDevices += count;
                            }
                        }
                        catch (Exception ex)
                        {
                            AddInfoText($"  ❌ 加载失败: {ex.Message}");
                        }
                    }

                    _isDevicesLoaded = true;
                }
                else
                {
                    // 刷新加载：只更新配置，不影响已连接设备
                    AddInfoText("🔄 刷新设备配置（已连接设备保持在线）...");

                    // 保存已连接设备的状态
                    var connectedDevices = new Dictionary<string, bool>();
                    foreach (var device in devices)
                    {
                        connectedDevices[device.ServerId] = device.IsOnline;
                    }

                    // 创建新的设备列表，但不影响已连接的设备
                    var newDevices = new List<DeviceInfo>();

                    foreach (var xmlFile in xmlFiles)
                    {
                        var fileName = Path.GetFileName(xmlFile);

                        try
                        {
                            if (fileName.Equals("HsmsConfig.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                var count = LoadHsmsDevicesIncremental(xmlFile, connectedDevices, newDevices);
                                totalDevices += count;
                                hsmsDevices += count;
                            }
                            else if (fileName.Equals("KepServerConfig.xml", StringComparison.OrdinalIgnoreCase))
                            {
                                var count = LoadKepServerDevicesIncremental(xmlFile, connectedDevices, newDevices);
                                totalDevices += count;
                                kepServerDevices += count;
                            }
                            // 其他配置文件类型暂时跳过刷新
                        }
                        catch (Exception ex)
                        {
                            AddInfoText($"  ❌ 刷新失败: {ex.Message}");
                        }
                    }

                    // 恢复已连接设备的状态
                    foreach (var device in newDevices)
                    {
                        if (connectedDevices.TryGetValue(device.ServerId, out bool wasOnline))
                        {
                            device.IsOnline = wasOnline;
                            device.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }

                    // 更新设备列表
                    devices = newDevices;
                }

                RefreshDataGridView();

                // 使用设备管理器更新真实连接状态（如果可用）
                if (_deviceManager != null)
                {
                    UpdateDeviceConnectionStatus();
                }

                // 只在首次加载时显示设备统计信息
                if (!_isDevicesLoaded || !txtInfo.Text.Contains("设备加载完成"))
                {
                    AddInfoText($"\n✅ 设备加载完成!");
                    AddInfoText($"  总设备数: {totalDevices}");
                    AddInfoText($"  HSMS设备: {hsmsDevices}");
                    AddInfoText($"  OPC设备: {opcDevices}");
                    AddInfoText($"  OPC-UA设备: {opcUaDevices}");
                    AddInfoText($"  KepServer设备: {kepServerDevices}");
                }
                else
                {
                    AddInfoText($"✅ 配置已刷新，总设备数: {totalDevices}");
                }
            }
            catch (Exception ex)
            {
                AddInfoText($"加载设备信息失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[设备监控] LoadDevices异常: {ex.StackTrace}");
            }
        }

        private int LoadHsmsDevices(string configPath)
        {
            var xmlContent = File.ReadAllText(configPath);
            var doc = XDocument.Parse(xmlContent);
            var devicesElement = doc.Root?.Element("Devices");

            int count = 0;
            if (devicesElement != null)
            {
                foreach (var deviceElement in devicesElement.Elements("Device"))
                {
                    var deviceType = deviceElement.Attribute("Type")?.Value ?? "";
                    var deviceId = deviceElement.Attribute("Id")?.Value ?? "";
                    var deviceName = deviceElement.Attribute("Name")?.Value ?? "";

                    if (string.IsNullOrEmpty(deviceId))
                        continue;

                    var connectionElement = deviceElement.Element("Connection");
                    var host = connectionElement?.Element("Host")?.Value ?? "127.0.0.1";
                    var port = int.Parse(connectionElement?.Element("Port")?.Value ?? "5000");
                    var enabled = bool.Parse(deviceElement.Attribute("Enabled")?.Value ?? "true");

                    var deviceInfo = new DeviceInfo
                    {
                        ServerId = deviceId,
                        ServerName = deviceName,
                        ProtocolType = deviceType,
                        DeviceType = host.Contains(".") ? "host" : "EQP",
                        Host = host,
                        Port = port,
                        Enabled = enabled,
                        IsOnline = false,
                        HeartbeatCount = 0,
                        ResponseTimeMs = 0,
                        ConnectionQuality = "",
                        LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        SourceFile = Path.GetFileName(configPath)
                    };

                    if (deviceType.Equals("HSMS", StringComparison.OrdinalIgnoreCase))
                    {
                        var secsElement = deviceElement.Element("SecsSettings");
                        if (secsElement != null)
                        {
                            var deviceIdValue = secsElement.Element("DeviceIdValue")?.Value;
                            var sessionIdValue = secsElement.Element("SessionIdValue")?.Value;

                            // 优先从配置文件读取Role，如果没有则根据设备类型推断
                            // 配置文件中的<Role>节点：
                            // Client - CIMMonitor作为客户端，主动连接HsmsSimulator服务端（适用于Host设备）
                            // Server - CIMMonitor作为服务端，等待HsmsSimulator客户端连接（适用于EQP设备）
                            var roleValue = secsElement.Element("Role")?.Value;
                            string role = !string.IsNullOrEmpty(roleValue)
                                ? roleValue
                                : (deviceInfo.DeviceType.Equals("host", StringComparison.OrdinalIgnoreCase) ? "Client" : "Server");

                            deviceInfo.ServerName += !string.IsNullOrEmpty(deviceIdValue)
                                ? $" (设备ID:{deviceIdValue}, 会话ID:{sessionIdValue}, {role})"
                                : "";

                            // 添加设备到管理器（避免重复添加）

                            // 解析 DeviceIdValue（支持十进制或带0x前缀的16进制）
                            byte parsedDeviceId = 1;
                            if (!string.IsNullOrEmpty(deviceIdValue))
                            {
                                var dv = deviceIdValue.Trim();
                                if (dv.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                {
                                    dv = dv.Substring(2);
                                    if (!byte.TryParse(dv, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedDeviceId))
                                    {
                                        byte.TryParse(dv, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedDeviceId);
                                    }
                                }
                                else
                                {
                                    if (!byte.TryParse(dv, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedDeviceId))
                                    {
                                        // 尝试作为十六进制（不带0x前缀）
                                        byte.TryParse(dv, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedDeviceId);
                                    }
                                }
                            }

                            // 解析 SessionIdValue（支持十进制或带0x前缀的16进制）
                            int parsedSessionId = 0x1234;
                            if (!string.IsNullOrEmpty(sessionIdValue))
                            {
                                var s = sessionIdValue.Trim();
                                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
                                if (!int.TryParse(s, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedSessionId))
                                {
                                    int.TryParse(s, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedSessionId);
                                }
                            }

                            var hsmsConfig = new HsmsDeviceConfig
                            {
                                DeviceId = deviceId,
                                DeviceName = deviceName,
                                ProtocolType = "HSMS",
                                Role = role,  // 优先使用配置文件中的Role，回退到根据设备类型推断
                                Host = host,
                                Port = port,
                                DeviceIdValue = parsedDeviceId,
                                SessionIdValue = parsedSessionId,
                                Enabled = enabled
                            };

                            // 只有在设备管理器中不存在该设备时才添加
                            if (_deviceManager != null && !_addedDeviceIds.Contains(deviceId))
                            {
                                _deviceManager.AddDevice(hsmsConfig);
                                _addedDeviceIds.Add(deviceId);
                            }
                        }
                    }
                    else if (deviceType.Equals("OPC", StringComparison.OrdinalIgnoreCase))
                    {
                        var opcElement = deviceElement.Element("OpcSettings");
                        if (opcElement != null)
                        {
                            var serverName = opcElement.Element("ServerName")?.Value;
                            deviceInfo.ServerName += !string.IsNullOrEmpty(serverName) ? $" ({serverName})" : "";
                        }
                    }

                    devices.Add(deviceInfo);
                    count++;
                }
            }

            return count;
        }

        private int LoadKepServerDevices(string configPath)
        {
            var xmlContent = File.ReadAllText(configPath);
            var doc = XDocument.Parse(xmlContent);

            // 解析KEPServer原生配置结构
            var channelsElement = doc.Root?.Element("Channels");
            int count = 0;

            if (channelsElement != null)
            {
                foreach (var channelElement in channelsElement.Elements("Channel"))
                {
                    var devicesElement = channelElement.Element("Devices");
                    if (devicesElement != null)
                    {
                        foreach (var deviceElement in devicesElement.Elements("Device"))
                        {
                            var properties = deviceElement.Element("Properties");
                            var ipAddressProp = properties?.Elements("Property")
                                .FirstOrDefault(p => p.Attribute("Name")?.Value == "IPAddress");
                            
                            var deviceInfo = new DeviceInfo
                            {
                                ServerId = deviceElement.Attribute("Name")?.Value ?? "", // 使用设备名称作为ID
                                ServerName = $"KepServer - {channelElement.Attribute("Name")?.Value ?? "Unknown Channel"} - {deviceElement.Attribute("Name")?.Value ?? "Unknown Device"}",
                                ProtocolType = channelElement.Attribute("Driver")?.Value ?? "OPC",
                                DeviceType = "EQP", // KepServer通常作为设备端点
                                Host = ipAddressProp?.Attribute("Value")?.Value ?? "localhost",
                                Port = 49320, // KepServer默认端口
                                Enabled = true, // 从KEPServer配置中获取设备状态
                                IsOnline = false, // 默认离线，等待实际连接
                                HeartbeatCount = 0,
                                ResponseTimeMs = 0,
                                ConnectionQuality = "",
                                LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                SourceFile = Path.GetFileName(configPath)
                            };

                            if (!string.IsNullOrEmpty(deviceInfo.ServerId) && !_addedDeviceIds.Contains(deviceInfo.ServerId))
                            {
                                devices.Add(deviceInfo);
                                _addedDeviceIds.Add(deviceInfo.ServerId); // 添加到已添加设备ID集合
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private int LoadGenericDevices(string configPath)
        {
            return 0;
        }

        /// <summary>
        /// 增量加载HSMS设备（刷新时使用，不影响已连接设备）
        /// </summary>
        private int LoadHsmsDevicesIncremental(string configPath, Dictionary<string, bool> connectedDevices, List<DeviceInfo> newDevicesList)
        {
            var xmlContent = File.ReadAllText(configPath);
            var doc = XDocument.Parse(xmlContent);
            var devicesElement = doc.Root?.Element("Devices");

            int count = 0;
            if (devicesElement != null)
            {
                foreach (var deviceElement in devicesElement.Elements("Device"))
                {
                    var deviceType = deviceElement.Attribute("Type")?.Value ?? "";
                    var deviceId = deviceElement.Attribute("Id")?.Value ?? "";
                    var deviceName = deviceElement.Attribute("Name")?.Value ?? "";

                    if (string.IsNullOrEmpty(deviceId))
                        continue;

                    var connectionElement = deviceElement.Element("Connection");
                    var host = connectionElement?.Element("Host")?.Value ?? "127.0.0.1";
                    var port = int.Parse(connectionElement?.Element("Port")?.Value ?? "5000");
                    var enabled = bool.Parse(deviceElement.Attribute("Enabled")?.Value ?? "true");

                    var deviceInfo = new DeviceInfo
                    {
                        ServerId = deviceId,
                        ServerName = deviceName,
                        ProtocolType = deviceType,
                        DeviceType = host.Contains(".") ? "host" : "EQP",
                        Host = host,
                        Port = port,
                        Enabled = enabled,
                        IsOnline = false, // 稍后会从connectedDevices恢复
                        HeartbeatCount = 0,
                        ResponseTimeMs = 0,
                        ConnectionQuality = "",
                        LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        SourceFile = Path.GetFileName(configPath)
                    };

                    if (deviceType.Equals("HSMS", StringComparison.OrdinalIgnoreCase))
                    {
                        var secsElement = deviceElement.Element("SecsSettings");
                        if (secsElement != null)
                        {
                            var deviceIdValue = secsElement.Element("DeviceIdValue")?.Value;
                            var sessionIdValue = secsElement.Element("SessionIdValue")?.Value;

                            // 优先从配置文件读取Role，如果没有则根据设备类型推断
                            var roleValue = secsElement.Element("Role")?.Value;
                            string role = !string.IsNullOrEmpty(roleValue)
                                ? roleValue
                                : (deviceInfo.DeviceType.Equals("host", StringComparison.OrdinalIgnoreCase) ? "Client" : "Server");

                            deviceInfo.ServerName += !string.IsNullOrEmpty(deviceIdValue)
                                ? $" (设备ID:{deviceIdValue}, 会话ID:{sessionIdValue}, {role})"
                                : "";

                            // 添加设备到管理器（如果尚未添加）
                            if (_deviceManager != null && !_addedDeviceIds.Contains(deviceId))
                            {
                                try
                                {
                                    // 安全解析 DeviceIdValue 和 SessionIdValue，支持 0x 前缀的16进制或十进制
                                    byte parsedDeviceId = 1;
                                    if (!string.IsNullOrEmpty(deviceIdValue))
                                    {
                                        var dv = deviceIdValue.Trim();
                                        if (dv.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                        {
                                            dv = dv.Substring(2);
                                            if (!byte.TryParse(dv, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedDeviceId))
                                            {
                                                byte.TryParse(dv, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedDeviceId);
                                            }
                                        }
                                        else
                                        {
                                            if (!byte.TryParse(dv, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedDeviceId))
                                            {
                                                // 尝试作为十六进制（不带0x前缀）
                                                byte.TryParse(dv, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedDeviceId);
                                            }
                                        }
                                    }

                                    int parsedSessionId = 0x1234;
                                    if (!string.IsNullOrEmpty(sessionIdValue))
                                    {
                                        var s = sessionIdValue.Trim();
                                        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
                                        if (!int.TryParse(s, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedSessionId))
                                        {
                                            int.TryParse(s, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedSessionId);
                                        }
                                    }

                                    var hsmsConfig = new HsmsDeviceConfig
                                    {
                                        DeviceId = deviceId,
                                        DeviceName = deviceName,
                                        ProtocolType = "HSMS",
                                        Role = role,
                                        Host = host,
                                        Port = port,
                                        DeviceIdValue = parsedDeviceId,
                                        SessionIdValue = parsedSessionId,
                                        Enabled = enabled
                                    };
                                    _deviceManager.AddDevice(hsmsConfig);
                                    _addedDeviceIds.Add(deviceId);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[设备监控] 增量添加设备失败 {deviceId}: {ex.Message}");
                                }
                            }
                        }
                    }

                    newDevicesList.Add(deviceInfo);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 增量加载KepServer设备（刷新时使用）
        /// </summary>
        private int LoadKepServerDevicesIncremental(string configPath, Dictionary<string, bool> connectedDevices, List<DeviceInfo> newDevicesList)
        {
            var xmlContent = File.ReadAllText(configPath);
            var doc = XDocument.Parse(xmlContent);

            // 解析KEPServer原生配置结构
            var channelsElement = doc.Root?.Element("Channels");
            int count = 0;

            if (channelsElement != null)
            {
                foreach (var channelElement in channelsElement.Elements("Channel"))
                {
                    var devicesElement = channelElement.Element("Devices");
                    if (devicesElement != null)
                    {
                        foreach (var deviceElement in devicesElement.Elements("Device"))
                        {
                            var properties = deviceElement.Element("Properties");
                            var ipAddressProp = properties?.Elements("Property")
                                .FirstOrDefault(p => p.Attribute("Name")?.Value == "IPAddress");
                            
                            var deviceInfo = new DeviceInfo
                            {
                                ServerId = deviceElement.Attribute("Name")?.Value ?? "", // 使用设备名称作为ID
                                ServerName = $"KepServer - {channelElement.Attribute("Name")?.Value ?? "Unknown Channel"} - {deviceElement.Attribute("Name")?.Value ?? "Unknown Device"}",
                                ProtocolType = channelElement.Attribute("Driver")?.Value ?? "OPC",
                                DeviceType = "EQP", // KepServer通常作为设备端点
                                Host = ipAddressProp?.Attribute("Value")?.Value ?? "localhost",
                                Port = 49320, // KepServer默认端口
                                Enabled = true, // 从KEPServer配置中获取设备状态
                                IsOnline = false, // 稍后会从connectedDevices恢复
                                HeartbeatCount = 0,
                                ResponseTimeMs = 0,
                                ConnectionQuality = "",
                                LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                SourceFile = Path.GetFileName(configPath)
                            };

                            if (!string.IsNullOrEmpty(deviceInfo.ServerId))
                            {
                                // 检查是否已存在相同的设备ID，避免重复添加
                                if (!newDevicesList.Any(d => d.ServerId == deviceInfo.ServerId))
                                {
                                    newDevicesList.Add(deviceInfo);
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            return count;
        }

        private void RefreshDataGridView()
        {
            dgvDevices!.Rows.Clear();

            foreach (var deviceInfo in devices)
            {
                // 如果设备在线，计算响应时间和连接质量
                if (deviceInfo.IsOnline)
                {
                    deviceInfo.ResponseTimeMs = CalculateResponseTime(deviceInfo.ServerId);
                    deviceInfo.ConnectionQuality = GetConnectionQuality(deviceInfo.ResponseTimeMs);
                }
                else
                {
                    deviceInfo.ResponseTimeMs = 0;
                    deviceInfo.ConnectionQuality = "";
                }

                var rowIndex = dgvDevices.Rows.Add(
                    deviceInfo.ServerId,
                    deviceInfo.ServerName,
                    deviceInfo.ProtocolType.ToUpper(),
                    deviceInfo.DeviceType,
                    deviceInfo.Host,
                    deviceInfo.Port,
                    deviceInfo.Enabled,  // 直接使用bool值，显示为CheckBox
                    deviceInfo.IsOnline ? "在线" : "离线",
                    deviceInfo.HeartbeatCount,
                    deviceInfo.ResponseTimeMs > 0 ? deviceInfo.ResponseTimeMs + "ms" : "-",
                    deviceInfo.ConnectionQuality,
                    deviceInfo.SourceFile
                );
            }

            //DisplayXmlLog();
        }

        private string GetConnectionQuality(int responseTime)
        {
            if (responseTime < 50) return "优秀";
            if (responseTime < 100) return "良好";
            if (responseTime < 200) return "一般";
            if (responseTime < 300) return "较差";
            return "差";
        }

        private void DisplayXmlLog()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<DeviceMonitorLog>");
                sb.AppendLine($"  <Timestamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</Timestamp>");
                sb.AppendLine($"  <TotalDevices>{devices.Count}</TotalDevices>");
                sb.AppendLine($"  <OnlineDevices>{devices.Count(d => d.IsOnline)}</OnlineDevices>");
                sb.AppendLine($"  <EnabledDevices>{devices.Count(d => d.Enabled)}</EnabledDevices>");
                sb.AppendLine("  <Devices>");
                foreach (var device in devices)
                {
                    sb.AppendLine("    <Device>");
                    sb.AppendLine($"      <ID>{device.ServerId}</ID>");
                    sb.AppendLine($"      <Name>{device.ServerName}</Name>");
                    sb.AppendLine($"      <Protocol>{device.ProtocolType}</Protocol>");
                    sb.AppendLine($"      <IP>{device.Host}</IP>");
                    sb.AppendLine($"      <Port>{device.Port}</Port>");
                    sb.AppendLine($"      <Enabled>{device.Enabled}</Enabled>");
                    sb.AppendLine($"      <Status>{(device.IsOnline ? "Online" : "Offline")}</Status>");
                    sb.AppendLine($"      <Heartbeat>{device.HeartbeatCount}</Heartbeat>");
                    sb.AppendLine($"      <ResponseTime>{device.ResponseTimeMs}ms</ResponseTime>");
                    sb.AppendLine($"      <Quality>{device.ConnectionQuality}</Quality>");
                    sb.AppendLine($"      <SourceFile>{device.SourceFile}</SourceFile>");
                    sb.AppendLine($"      <LastUpdate>{device.LastUpdate}</LastUpdate>");
                    sb.AppendLine("    </Device>");
                }
                sb.AppendLine("  </Devices>");
                sb.AppendLine("</DeviceMonitorLog>");

                txtInfo.Text += $"\n\n=== XML格式设备信息 ===\n{sb.ToString()}";
            }
            catch (Exception ex)
            {
                txtInfo.Text += $"\n生成XML日志失败: {ex.Message}";
            }
        }

        private void DgvDevices_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvDevices!.SelectedRows.Count > 0)
            {
                selectedDeviceId = dgvDevices.SelectedRows[0].Index;
            }
        }

        private void DgvDevices_DoubleClick(object? sender, EventArgs e)
        {
            if (dgvDevices!.SelectedRows.Count > 0)
            {
                int rowIndex = dgvDevices.SelectedRows[0].Index;
                if (rowIndex >= 0 && rowIndex < devices.Count)
                {
                    var deviceInfo = devices[rowIndex];
                    
                    // 检查是否已经打开了该设备的详情窗口
                    string formKey = deviceInfo.ServerId;
                    if (_openDetailForms.ContainsKey(formKey))
                    {
                        // 如果窗口已存在，激活它
                        _openDetailForms[formKey].Activate();
                    }
                    else
                    {
                        // 创建新的详情窗口
                        var detailForm = new MonitorDetail(deviceInfo);
                        
                        // 保存窗口引用以便后续管理
                        _openDetailForms[formKey] = detailForm;
                        
                        // 当窗口关闭时，从字典中移除引用
                        detailForm.FormClosed += (s, args) =>
                        {
                            if (_openDetailForms.ContainsKey(formKey))
                            {
                                _openDetailForms.Remove(formKey);
                            }
                        };
                        
                        // 显示窗口
                        detailForm.Show();
                    }
                }
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadDevices();
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                // 记录系统操作日志
                var operationLogger = Program.GetLogger("SystemOperation");
                operationLogger.Info("用户点击'启动监控'按钮");

                txtInfo.Text += "\n启动设备监控...";

                // 如果有选中的设备，优先监控该设备
                if (selectedDeviceId >= 0 && selectedDeviceId < devices.Count)
                {
                    var device = devices[selectedDeviceId];
                    txtInfo.Text += $"\n正在启动选中设备的监控: {device.ServerId}";
                    operationLogger.Info($"启动选中设备监控: {device.ServerId}");

                    // 启用设备
                    device.Enabled = true;
                    txtInfo.Text += $"\n✅ 设备 {device.ServerId} 已启用";
                    operationLogger.Info($"设备已启用: {device.ServerId}");

                    // 自动连接设备
                    if (_deviceManager != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var connected = await _deviceManager.ConnectDeviceAsync(device.ServerId);
                                if (connected)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        txtInfo.Text += $"\n✅ 设备连接成功: {device.ServerId}";
                                        txtInfo.Text += $"\n🔄 正在监控设备状态...";

                                        operationLogger.Info($"设备连接成功: {device.ServerId}");

                                        // 启动设备状态监控定时器
                                        StartDeviceMonitoring(device.ServerId);
                                    }));
                                }
                                else
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        txtInfo.Text += $"\n❌ 设备连接失败: {device.ServerId}";
                                        operationLogger.Error($"设备连接失败: {device.ServerId}");
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    txtInfo.Text += $"\n❌ 启动监控失败: {device.ServerId} - {ex.Message}";
                                    operationLogger.Error($"启动监控失败: {device.ServerId} - {ex.Message}", ex);
                                }));
                            }
                        });
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            txtInfo.Text += $"\n⚠️ 设备管理器未初始化，无法连接设备";
                            operationLogger.Warn("设备管理器未初始化，无法连接设备");
                        }));
                    }
                }
                else
                {
                    txtInfo.Text += $"\nℹ️ 未选中设备，将对所有设备进行监控";
                    operationLogger.Info("未选中设备，将对所有设备进行监控");
                }

                // 启动自动刷新
                StartAutoRefresh();
                operationLogger.Info("启动自动刷新定时器");
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("启动监控过程中发生错误", ex);
                txtInfo.Text += $"\n❌ 启动监控时发生错误: {ex.Message}";
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                // 记录系统操作日志
                var operationLogger = Program.GetLogger("SystemOperation");
                operationLogger.Info("用户点击'停止监控'按钮");

                txtInfo.Text += "\n停止设备监控...";
                operationLogger.Info("停止设备监控");
                StopAutoRefresh();
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("停止监控时发生错误", ex);
                txtInfo.Text += $"\n❌ 停止监控时发生错误: {ex.Message}";
            }
        }

        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            try
            {
                // 记录系统操作日志
                var operationLogger = Program.GetLogger("SystemOperation");
                operationLogger.Info("用户点击'重启监控'按钮");

                txtInfo.Text += "\n重启设备监控...";
                operationLogger.Info("重启设备监控");
                StopAutoRefresh();
                LoadDevices();
                operationLogger.Info("重新加载设备配置");
                StartAutoRefresh();
                operationLogger.Info("重启完成，启动自动刷新");
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("重启监控时发生错误", ex);
                txtInfo.Text += $"\n❌ 重启监控时发生错误: {ex.Message}";
            }
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000; // 改为5秒刷新一次
            refreshTimer.Tick += (s, e) =>
            {
                LoadDevices();

                // 额外更新在线设备的响应时间和连接质量
                foreach (var device in devices)
                {
                    if (device.IsOnline && device.Enabled)
                    {
                        var status = _deviceManager?.GetDeviceStatus(device.ServerId);
                        if (status != null && status.IsConnected)
                        {
                            device.ResponseTimeMs = CalculateResponseTime(device.ServerId);
                            device.ConnectionQuality = GetConnectionQuality(device.ResponseTimeMs);
                            device.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }

                // 刷新DataGridView显示最新数据
                RefreshDataGridView();
            };
            refreshTimer.Start();
        }

        private void StopAutoRefresh()
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
        }

        private void BtnClearLog_Click(object? sender, EventArgs e)
        {
            txtInfo.Clear();
            txtInfo.Text = "日志已清理，等待新的消息...";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopAutoRefresh();
            _deviceManager?.Dispose();
            base.OnFormClosed(e);
        }

        #region HSMS设备连接管理

        /// <summary>
        /// 连接设备按钮点击事件
        /// </summary>
        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedDeviceId >= 0 && selectedDeviceId < devices.Count)
                {
                    var device = devices[selectedDeviceId];

                    // 记录系统操作日志
                    var operationLogger = Program.GetLogger("SystemOperation");
                    operationLogger.Info($"用户点击'连接设备'按钮 - 设备: {device.ServerId}");

                    txtInfo.Text += $"\n正在连接设备: {device.ServerId} ({device.Host}:{device.Port})...";
                    operationLogger.Info($"正在连接设备: {device.ServerId} ({device.Host}:{device.Port})");

                    if (_deviceManager != null)
                    {
                        bool connected = await _deviceManager.ConnectDeviceAsync(device.ServerId);

                        if (connected)
                        {
                            txtInfo.Text += $"\n✅ 设备连接成功: {device.ServerId}";
                            operationLogger.Info($"设备连接成功: {device.ServerId}");
                        }
                        else
                        {
                            txtInfo.Text += $"\n❌ 设备连接失败: {device.ServerId}";
                            operationLogger.Error($"设备连接失败: {device.ServerId}");
                        }
                    }
                    else
                    {
                        txtInfo.Text += $"\n⚠️ 设备管理器未初始化，无法连接设备";
                        operationLogger.Warn("设备管理器未初始化，无法连接设备");
                    }

                    RefreshDataGridView();
                }
                else
                {
                    MessageBox.Show("请先选择要连接的设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    var operationLogger = Program.GetLogger("SystemOperation");
                    operationLogger.Warn("用户尝试连接设备但未选择设备");
                }
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("连接设备时发生错误", ex);
                txtInfo.Text += $"\n❌ 连接设备时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 断开设备按钮点击事件
        /// </summary>
        private async void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedDeviceId >= 0 && selectedDeviceId < devices.Count)
                {
                    var device = devices[selectedDeviceId];

                    // 记录系统操作日志
                    var operationLogger = Program.GetLogger("SystemOperation");
                    operationLogger.Info($"用户点击'断开设备'按钮 - 设备: {device.ServerId}");

                    txtInfo.Text += $"\n正在断开设备: {device.ServerId}...";
                    operationLogger.Info($"正在断开设备: {device.ServerId}");

                    if (_deviceManager != null)
                    {
                        await _deviceManager.DisconnectDeviceAsync(device.ServerId);
                        txtInfo.Text += $"\n✅ 设备已断开: {device.ServerId}";
                        operationLogger.Info($"设备已断开: {device.ServerId}");
                    }
                    else
                    {
                        txtInfo.Text += $"\n⚠️ 设备管理器未初始化";
                        operationLogger.Warn("设备管理器未初始化");
                    }

                    RefreshDataGridView();
                }
                else
                {
                    MessageBox.Show("请先选择要断开的设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    var operationLogger = Program.GetLogger("SystemOperation");
                    operationLogger.Warn("用户尝试断开设备但未选择设备");
                }
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("断开设备时发生错误", ex);
                txtInfo.Text += $"\n❌ 断开设备时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 设备状态变化事件处理
        /// </summary>
        private void OnDeviceStatusChanged(object? sender, Services.HsmsDeviceManager.DeviceStatusChangedEventArgs e)
        {
            try
            {
                // 记录设备状态变化到系统操作日志
                var operationLogger = Program.GetLogger("SystemOperation");
                operationLogger.Info($"设备状态变化: {e.DeviceId} - {e.Status}");

                this.Invoke(new Action(() =>
                {
                    AddInfoText($"[{e.Timestamp:HH:mm:ss}] 设备 {e.DeviceId} 状态变化: {e.Status}");
                    RefreshDataGridView();
                }));
            }
            catch (Exception ex)
            {
                // 记录到系统错误日志
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("处理设备状态变化事件时发生错误", ex);
            }
        }

        /// <summary>
        /// 设备消息接收事件处理
        /// </summary>
        private void OnDeviceMessageReceived(object? sender, Services.HsmsDeviceManager.DeviceMessageEventArgs e)
        {
            // 记录方法调用（调试）
            System.Diagnostics.Debug.WriteLine($"[设备监控] OnDeviceMessageReceived 被调用！");
            System.Diagnostics.Debug.WriteLine($"[设备监控]   - DeviceId: {e?.DeviceId ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[设备监控]   - Message: {e?.Message ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[设备监控]   - Sender: {sender?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[设备监控]   - EventArgs: {e?.GetType().Name ?? "null"}");

            this.Invoke(new Action(() =>
            {
                try
                {
                    // 记录所有接收到的消息到调试日志
                    string messageType = e?.HsmsMessage?.MessageType ?? "Unknown";
                    bool isUserInteractive = e?.HsmsMessage?.IsUserInteractive ?? false;

                    System.Diagnostics.Debug.WriteLine($"[设备监控] 消息详情: Type={messageType}, IsUserInteractive={isUserInteractive}");

                    // 改进的过滤逻辑：
                    // 1. 所有消息都记录到"最近自动消息"列
                    // 2. 用户交互消息在主界面显示
                    // 3. 重要的自动消息（如报警）也在主界面显示

                    // 更新设备列表中的"最近自动消息"列
                    if (e?.HsmsMessage != null)
                    {
                        UpdateDeviceAutoMessageColumn(e.DeviceId, messageType, e.Timestamp);
                    }

                    // 所有消息都应该在主界面显示
                    // 根据消息类型调整显示详细程度
                    string displayMessage = "";
                    string displayReason = "";

                    if (isUserInteractive)
                    {
                        // 用户交互消息 - 详细显示
                        displayReason = "用户交互";
                        displayMessage = FormatMessageAsXml(e.DeviceId, e.Message, e.Timestamp);
                    }
                    else if (messageType.Contains("ALARM") || messageType.Contains("EVENT"))
                    {
                        // 报警和事件消息 - 详细显示
                        displayReason = "报警/事件";
                        displayMessage = FormatMessageAsXml(e.DeviceId, e.Message, e.Timestamp);
                    }
                    else
                    {
                        // 其他自动消息 - 简化显示
                        displayReason = "自动消息";
                        displayMessage = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<HSMSMessage>
  <Timestamp>{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}</Timestamp>
  <DeviceId>{e.DeviceId}</DeviceId>
  <Direction>Receive</Direction>
  <MessageType>{messageType}</MessageType>
  <Content>
    <Text>{e.Message}</Text>
  </Content>
  <Properties>
    <IsUserInteractive>{isUserInteractive}</IsUserInteractive>
    <Encoding>UTF-8</Encoding>
  </Properties>
</HSMSMessage>";
                    }

                    if (e != null)
                    {
                        // 构建完整的显示文本
                        var fullText = $"[{e.Timestamp:HH:mm:ss}] [显示原因: {displayReason}]{Environment.NewLine}{displayMessage}{Environment.NewLine}";

                        // 使用辅助方法添加文本，确保换行正确
                        AddInfoText(fullText);

                        // 同时记录到日志文件
                        LogHsmsMessage(e.DeviceId, displayReason, displayMessage);
                    }
                }
                catch (Exception ex)
                {
                    // 记录处理消息时的错误
                    System.Diagnostics.Debug.WriteLine($"[设备监控] 处理消息时出错: {ex.Message}");
                    AddInfoText($"❌ 处理消息时出错: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 更新设备列表中的最近自动消息列
        /// 注意：此列已被删除，该方法保留用于向后兼容
        /// </summary>
        private void UpdateDeviceAutoMessageColumn(string deviceId, string messageType, DateTime timestamp)
        {
            // 由于删除了"最近自动消息"列，此方法不再执行实际操作
            // 保留用于向后兼容和潜在的未来功能扩展
            System.Diagnostics.Debug.WriteLine($"[设备监控] 消息接收: {deviceId} - {messageType} ({timestamp:HH:mm:ss})");
        }

        /// <summary>
        /// 将消息格式化为XML格式
        /// </summary>
        private string FormatMessageAsXml(string deviceId, string message, DateTime timestamp)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<HSMSMessage>");
            sb.AppendLine($"  <Timestamp>{timestamp:yyyy-MM-dd HH:mm:ss.fff}</Timestamp>");
            sb.AppendLine($"  <DeviceId>{deviceId}</DeviceId>");
            sb.AppendLine($"  <Direction>Receive</Direction>");

            // 尝试解析消息类型
            string messageType = ParseMessageType(message);
            sb.AppendLine($"  <MessageType>{messageType}</MessageType>");

            // 添加消息内容
            sb.AppendLine("  <Content>");
            if (IsXmlContent(message))
            {
                // 如果消息本身是XML，格式化显示
                sb.AppendLine("    <![CDATA[");
                sb.AppendLine($"      {message}");
                sb.AppendLine("    ]]>");
            }
            else if (IsSimpleText(message))
            {
                // 简单文本消息
                sb.AppendLine($"    <Text>{message}</Text>");
            }
            else
            {
                // 其他类型内容
                sb.AppendLine($"    <Data>{message}</Data>");
            }
            sb.AppendLine("  </Content>");

            // 添加消息属性
            sb.AppendLine("  <Properties>");
            sb.AppendLine($"    <Length>{message.Length}</Length>");
            sb.AppendLine($"    <Encoding>UTF-8</Encoding>");
            sb.AppendLine("  </Properties>");
            sb.AppendLine("</HSMSMessage>");

            return sb.ToString();
        }

        /// <summary>
        /// 解析消息类型
        /// </summary>
        private string ParseMessageType(string message)
        {
            // 预定义的消息类型映射
            var messageTypeMap = new Dictionary<string, string>
            {
                { "ARE_YOU_THERE", "S1F13 - Are You There" },
                { "I_AM_HERE", "S1F14 - I Am Here" },
                { "ARE_YOU_THERE_REQUEST", "S1F15 - Are You There Request" },
                { "I_AM_HERE_REQUEST", "S1F16 - I Am Here Request" },
                { "ALARM_REPORT_SEND", "S5F17 - Alarm Report Send" },
                { "EVENT_REPORT_SEND", "S6F11 - Event Report Send" }
            };

            if (messageTypeMap.TryGetValue(message, out string? type))
            {
                return type;
            }

            // 如果是未知消息，尝试从内容中提取类型
            if (message.StartsWith("S") && message.Contains("F"))
            {
                return message;
            }

            return "Unknown";
        }

        /// <summary>
        /// 检查是否为XML内容
        /// </summary>
        private bool IsXmlContent(string content)
        {
            return content.TrimStart().StartsWith("<?xml") || content.TrimStart().StartsWith("<");
        }

        /// <summary>
        /// 检查是否为简单文本
        /// </summary>
        private bool IsSimpleText(string content)
        {
            // 如果是纯文本（没有特殊字符），认为是简单文本
            return !content.Contains("<") && !content.Contains(">") && !content.Contains("&");
        }

        /// <summary>
        /// 更新设备连接状态
        /// </summary>
        private void UpdateDeviceConnectionStatus()
        {
            if (_deviceManager == null) return;

            foreach (var device in devices)
            {
                var status = _deviceManager.GetDeviceStatus(device.ServerId);
                device.IsOnline = status.IsConnected;
                device.HeartbeatCount = status.HeartbeatCount;
                device.LastUpdate = status.LastConnectionTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? device.LastUpdate;

                // 如果设备在线，计算响应时间和连接质量
                if (device.IsOnline)
                {
                    device.ResponseTimeMs = CalculateResponseTime(device.ServerId);
                    device.ConnectionQuality = GetConnectionQuality(device.ResponseTimeMs);
                }
                else
                {
                    device.ResponseTimeMs = 0;
                    device.ConnectionQuality = "";
                }
            }
        }

        /// <summary>
        /// 启动设备状态监控
        /// </summary>
        private void StartDeviceMonitoring(string deviceId)
        {
            // 创建一个定时器来监控设备状态
            var monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 2000; // 每2秒更新一次
            monitorTimer.Tick += (s, e) =>
            {
                var device = devices.FirstOrDefault(d => d.ServerId == deviceId);
                if (device != null)
                {
                    var status = _deviceManager?.GetDeviceStatus(deviceId);
                    if (status != null && status.IsConnected)
                    {
                        // 更新设备状态
                        device.IsOnline = true;
                        device.HeartbeatCount++;
                        device.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // 计算响应时间
                        device.ResponseTimeMs = CalculateResponseTime(deviceId);
                        device.ConnectionQuality = GetConnectionQuality(device.ResponseTimeMs);

                        // 刷新DataGridView
                        RefreshDataGridView();

                        // 记录心跳日志
                        AddInfoText($"[{DateTime.Now:HH:mm:ss}] 设备 {deviceId} 心跳: {device.HeartbeatCount}, 响应时间: {device.ResponseTimeMs}ms, 连接质量: {device.ConnectionQuality}");
                    }
                }
            };
            monitorTimer.Start();

            // 5分钟后停止监控（避免内存泄漏）
            var stopTimer = new System.Windows.Forms.Timer();
            stopTimer.Interval = 300000; // 5分钟
            stopTimer.Tick += (s, e) =>
            {
                monitorTimer.Stop();
                monitorTimer.Dispose();
                stopTimer.Stop();
                stopTimer.Dispose();
            };
            stopTimer.Start();
        }

        /// <summary>
        /// 计算设备响应时间
        /// </summary>
        private int CalculateResponseTime(string deviceId)
        {
            // 这里可以实际测量TCP响应时间
            // 暂时返回模拟值
            var random = new Random(deviceId.GetHashCode());
            return random.Next(10, 100);
        }

        /// <summary>
        /// 测试消息按钮点击事件
        /// </summary>
        private void BtnTestMessage_Click(object? sender, EventArgs e)
        {
            // 手动触发一个测试消息来验证消息处理流程
            txtInfo.Text += $"\n[{DateTime.Now:HH:mm:ss}] [测试] 手动触发测试消息...";

            try
            {
                // 创建一个模拟的HSMS消息事件
                if (_deviceManager != null)
                {
                    // 模拟发送一个测试消息
                    var testDeviceId = "TEST_DEVICE_001";
                    var testMessage = "S1F13 - Are You There Request";
                    var timestamp = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine($"[设备监控] 手动测试消息已触发: {testDeviceId} - {testMessage}");

                    // 尝试通过设备管理器发送测试消息（如果支持）
                    // 这里只是模拟，不会实际发送
                    this.Invoke(new Action(() =>
                    {
                        AddInfoText($"✅ 测试消息已发送: {testDeviceId}");
                        AddInfoText($"💡 提示: 请检查设备是否能收到此消息");
                        AddInfoText($"📝 消息内容: {testMessage}");
                        AddInfoText($"⏰ 时间戳: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    }));

                    // 更新设备的"最近自动消息"列
                    UpdateDeviceAutoMessageColumn(testDeviceId, "TEST_MESSAGE", timestamp);
                }
                else
                {
                    AddInfoText($"❌ 测试失败: 设备管理器未初始化");
                }
            }
            catch (Exception ex)
            {
                AddInfoText($"❌ 测试消息时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[设备监控] 测试消息错误: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 自动连接所有已启用的设备
        /// </summary>
        private async void AutoConnectEnabledDevices()
        {
            if (_deviceManager == null)
            {
                txtInfo.Text += "\n⚠️ 设备管理器未初始化，无法自动连接设备";
                return;
            }

            try
            {
                var enabledDevices = devices.Where(d => d.Enabled).ToList();

                if (enabledDevices.Count == 0)
                {
                    txtInfo.Text += "\n📝 没有找到已启用的设备";
                    return;
                }

                txtInfo.Text += $"\n🔄 找到 {enabledDevices.Count} 个已启用设备，开始自动连接...";

                // 逐个连接已启用的设备
                foreach (var device in enabledDevices)
                {
                    try
                    {
                        txtInfo.Text += $"\n⏳ 正在连接: {device.ServerId} ({device.Host}:{device.Port})...";

                        var connected = await _deviceManager.ConnectDeviceAsync(device.ServerId);

                        if (connected)
                        {
                            device.IsOnline = true;
                            device.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            txtInfo.Text += $"\n✅ 设备连接成功: {device.ServerId}";
                        }
                        else
                        {
                            txtInfo.Text += $"\n❌ 设备连接失败: {device.ServerId}";
                        }
                    }
                    catch (Exception ex)
                    {
                        txtInfo.Text += $"\n❌ 连接设备 {device.ServerId} 时出错: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine($"[设备监控] 自动连接设备异常: {device.ServerId} - {ex.Message}");
                    }
                }

                // 刷新显示
                RefreshDataGridView();
                txtInfo.Text += $"\n✅ 自动连接完成！";
            }
            catch (Exception ex)
            {
                txtInfo.Text += $"\n❌ 自动连接过程中出错: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[设备监控] 自动连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 将HSMS消息记录到设备交互日志文件
        /// </summary>
        private void LogHsmsMessage(string deviceId, string displayReason, string xmlMessage)
        {
            try
            {
                // 获取设备交互日志记录器
                var logger = Program.GetLogger("DeviceCommunication");
                if (logger != null)
                {
                    // 构建日志消息
                    var logMessage = new System.Text.StringBuilder();
                    logMessage.AppendLine($"[HSMS消息] 设备: {deviceId}, 类型: {displayReason}");
                    logMessage.AppendLine(xmlMessage);
                    logMessage.AppendLine(new string('-', 80)); // 分隔线

                    // 记录到设备交互日志文件（INFO级别）
                    logger.Info(logMessage.ToString());
                }
            }
            catch (Exception ex)
            {
                // 记录日志记录失败，但不中断消息处理
                System.Diagnostics.Debug.WriteLine($"[设备监控] 记录日志失败: {ex.Message}");
                try
                {
                    // 尝试记录到系统错误日志
                    var errorLogger = Program.GetLogger("SystemError");
                    errorLogger.Error($"[设备监控] 记录设备交互日志失败: {ex.Message}", ex);
                }
                catch
                {
                    // 忽略嵌套异常
                }
            }
        }

        #endregion

        /// <summary>
        /// 向txtInfo添加文本，确保正确换行和显示
        /// </summary>
        private void AddInfoText(string text)
        {
            try
            {
                string lineBreak = Environment.NewLine;

                // 如果text中包含换行符，逐行添加
                if (text.Contains('\n'))
                {
                    var lines = text.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (i == 0 && txtInfo.Text.Length == 0)
                        {
                            // 第一行直接追加，不添加额外换行
                            txtInfo.AppendText(lines[i]);
                        }
                        else
                        {
                            txtInfo.AppendText($"{lineBreak}{lines[i]}");
                        }
                    }
                }
                else
                {
                    // 单行文本
                    txtInfo.AppendText($"{lineBreak}{text}");
                }

                // 自动滚动到底部
                txtInfo.SelectionStart = txtInfo.Text.Length;
                txtInfo.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[设备监控] 添加文本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// DataGridView单元格值变化事件处理（用于处理启用/禁用开关）
        /// </summary>
        private void DgvDevices_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 检查是否是CheckBox列（第7列，索引为6）
                if (e.ColumnIndex == 6 && e.RowIndex >= 0 && e.RowIndex < devices.Count)
                {
                    var deviceInfo = devices[e.RowIndex];
                    var cellValue = dgvDevices.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    // 获取新的启用状态
                    bool newEnabledState = cellValue is bool boolValue ? boolValue : false;

                    if (deviceInfo.Enabled != newEnabledState)
                    {
                        // 更新设备状态
                        deviceInfo.Enabled = newEnabledState;
                        deviceInfo.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // 回写配置文件
                        _ = Task.Run(() => UpdateDeviceEnabledInConfig(deviceInfo));

                        // 记录操作日志
                        var operationLogger = Program.GetLogger("SystemOperation");
                        var action = newEnabledState ? "启用" : "禁用";
                        operationLogger.Info($"用户通过CheckBox{action}设备: {deviceInfo.ServerId}");

                        AddInfoText($"[{DateTime.Now:HH:mm:ss}] 用户{action}设备: {deviceInfo.ServerId}");

                        // 如果启用设备，自动连接
                        if (newEnabledState && _deviceManager != null)
                        {
                            AddInfoText($"  → 尝试自动连接设备...");
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var connected = await _deviceManager.ConnectDeviceAsync(deviceInfo.ServerId);
                                    this.Invoke(new Action(() =>
                                    {
                                        if (connected)
                                        {
                                            AddInfoText($"  → ✅ 设备连接成功: {deviceInfo.ServerId}");
                                        }
                                        else
                                        {
                                            AddInfoText($"  → ❌ 设备连接失败: {deviceInfo.ServerId}");
                                        }
                                        RefreshDataGridView();
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        AddInfoText($"  → ❌ 连接异常: {ex.Message}");
                                    }));
                                }
                            });
                        }
                        // 如果禁用设备，断开连接
                        else if (!newEnabledState && _deviceManager != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _deviceManager.DisconnectDeviceAsync(deviceInfo.ServerId);
                                    this.Invoke(new Action(() =>
                                    {
                                        AddInfoText($"  → ✅ 设备已断开: {deviceInfo.ServerId}");
                                        RefreshDataGridView();
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        AddInfoText($"  → ❌ 断开异常: {ex.Message}");
                                    }));
                                }
                            });
                        }

                        // 刷新显示
                        RefreshDataGridView();
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"[设备监控] 处理CheckBox变化失败: {ex.Message}");
                var errorLogger = Program.GetLogger("SystemError");
                errorLogger.Error("处理设备启用状态变化时发生错误", ex);
            }
        }

        /// <summary>
        /// 更新配置文件中的设备启用状态
        /// 支持HsmsConfig.xml和KepServerConfig.xml两个配置文件
        /// </summary>
        private void UpdateDeviceEnabledInConfig(DeviceInfo deviceInfo)
        {
            try
            {
                var configDir = Path.Combine(Application.StartupPath, "Config");
                string configPath;
                string rootElementName;
                string childElementName;
                string idAttributeName;

                // 根据配置文件类型选择不同的XML结构和属性名
                if (deviceInfo.SourceFile.Equals("KepServerConfig.xml", StringComparison.OrdinalIgnoreCase))
                {
                    // KepServer配置文件
                    configPath = Path.Combine(configDir, "KepServerConfig.xml");
                    rootElementName = "Servers";
                    childElementName = "Server";
                    idAttributeName = "ServerId";
                }
                else
                {
                    // HSMS配置文件（默认）
                    configPath = Path.Combine(configDir, "HsmsConfig.xml");
                    rootElementName = "Devices";
                    childElementName = "Device";
                    idAttributeName = "Id";
                }

                if (!File.Exists(configPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[设备监控] 配置文件不存在: {configPath}");
                    this.Invoke(new Action(() =>
                    {
                        AddInfoText($"  → ❌ 配置文件不存在: {Path.GetFileName(configPath)}");
                    }));
                    return;
                }

                // 读取并修改XML
                var xmlDoc = XDocument.Load(configPath);

                // 查找对应设备（使用动态属性名）
                var deviceElement = xmlDoc.Root?
                    .Element(rootElementName)?
                    .Elements(childElementName)
                    .FirstOrDefault(d => d.Attribute(idAttributeName)?.Value == deviceInfo.ServerId);

                if (deviceElement != null)
                {
                    // 更新Enabled属性
                    deviceElement.SetAttributeValue("Enabled", deviceInfo.Enabled.ToString().ToLower());

                    // 保存文件
                    xmlDoc.Save(configPath);

                    this.Invoke(new Action(() =>
                    {
                        AddInfoText($"  → ✅ 配置已更新: {deviceInfo.ServerId} = {(deviceInfo.Enabled ? "启用" : "禁用")} ({Path.GetFileName(configPath)})");
                    }));

                    System.Diagnostics.Debug.WriteLine($"[设备监控] 配置文件已更新: {deviceInfo.ServerId} = {deviceInfo.Enabled} ({Path.GetFileName(configPath)})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[设备监控] 在配置文件{Path.GetFileName(configPath)}中未找到设备: {deviceInfo.ServerId}");
                    this.Invoke(new Action(() =>
                    {
                        AddInfoText($"  → ❌ 在{Path.GetFileName(configPath)}中未找到设备: {deviceInfo.ServerId}");
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[设备监控] 更新配置文件失败: {ex.Message}");
                this.Invoke(new Action(() =>
                {
                    AddInfoText($"  → ❌ 配置更新失败: {ex.Message}");
                }));
            }
        }
    }
}
