using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CIMMonitor.Models;
using log4net;
using HsmsSimulator.Models;

namespace CIMMonitor.Services
{
    /// <summary>
    /// HSMS设备管理器
    /// 负责管理所有HSMS设备的连接和状态
    /// </summary>
    public class HsmsDeviceManager : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(HsmsDeviceManager));

        private readonly Dictionary<string, HsmsConnectionService> _deviceConnections = new();
        private readonly Dictionary<string, HsmsDeviceConfig> _deviceConfigs = new();
        private readonly Dictionary<string, List<MessageLogEntry>> _messageLogs = new();

        /// <summary>
        /// 设备状态变化事件
        /// </summary>
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        /// <summary>
        /// 设备消息接收事件
        /// </summary>
        public event EventHandler<DeviceMessageEventArgs>? DeviceMessageReceived;

        /// <summary>
        /// 消息发送事件
        /// </summary>
        public event EventHandler<MessageLogEntry>? MessageSent;

        private void DebugWrite(string text)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CIMMonitor_DEBUG.txt");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}");
            }
            catch { }
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        public void AddDevice(HsmsDeviceConfig config)
        {
            if (!_deviceConfigs.ContainsKey(config.DeviceId))
            {
                _deviceConfigs[config.DeviceId] = config;
                _messageLogs[config.DeviceId] = new List<MessageLogEntry>();
                logger.Info($"添加HSMS设备: {config.DeviceId} - {config.DeviceName}");
                DebugWrite($"AddDevice: {config.DeviceId} Host={config.Host} Port={config.Port} Role={config.Role}");
            }
        }

        /// <summary>
        /// 移除设备
        /// </summary>
        public async Task RemoveDeviceAsync(string deviceId)
        {
            if (_deviceConnections.ContainsKey(deviceId))
            {
                await DisconnectDeviceAsync(deviceId);
                _deviceConnections.Remove(deviceId);
            }

            if (_deviceConfigs.ContainsKey(deviceId))
            {
                _deviceConfigs.Remove(deviceId);
                _messageLogs.Remove(deviceId);
                logger.Info($"移除HSMS设备: {deviceId}");
                DebugWrite($"RemoveDevice: {deviceId}");
            }
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        public async Task<bool> ConnectDeviceAsync(string deviceId)
        {
            try
            {
                if (!_deviceConfigs.ContainsKey(deviceId))
                {
                    logger.Warn($"设备不存在: {deviceId}");
                    DebugWrite($"ConnectDeviceAsync fail: device not found {deviceId}");
                    return false;
                }

                var config = _deviceConfigs[deviceId];

                // 如果已连接，先断开
                if (_deviceConnections.ContainsKey(deviceId))
                {
                    await DisconnectDeviceAsync(deviceId);
                }

                // 创建新的连接服务
                var connection = new HsmsConnectionService(
                    config.DeviceId,
                    config.DeviceIdValue,
                    config.SessionIdValue
                );

                // 绑定事件
                connection.ConnectionStatusChanged += OnConnectionStatusChanged;
                connection.MessageReceived += OnMessageReceived;

                // 根据Role决定是客户端模式还是服务端模式
                bool isServerMode = !string.IsNullOrEmpty(config.Role) && config.Role.Equals("Server", StringComparison.OrdinalIgnoreCase);
                string logMode = isServerMode ? "服务端" : "客户端";

                DebugWrite($"ConnectDeviceAsync: {deviceId} -> {config.Host}:{config.Port} Mode={logMode}");

                // 尝试连接
                var connected = await connection.ConnectAsync(config.Host, config.Port, isServerMode);

                DebugWrite($"ConnectAsync returned: {connected} for {deviceId}");

                if (connected)
                {
                    _deviceConnections[deviceId] = connection;
                    config.IsConnected = true;
                    config.LastConnectionTime = DateTime.Now;
                    config.Status = "已连接";
                    logger.Info($"设备{logMode}连接成功: {deviceId} ({config.Host}:{config.Port})");

                    // 触发状态变化事件
                    DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                    {
                        DeviceId = deviceId,
                        IsConnected = true,
                        Status = $"已连接({logMode})",
                        Timestamp = DateTime.Now
                    });

                    return true;
                }
                else
                {
                    logger.Error($"设备{logMode}连接失败: {deviceId}");
                    DebugWrite($"ConnectDeviceAsync: ConnectAsync returned false for {deviceId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"连接设备异常: {deviceId} - {ex.Message}", ex);
                DebugWrite($"ConnectDeviceAsync exception for {deviceId}: {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        public async Task DisconnectDeviceAsync(string deviceId)
        {
            try
            {
                if (_deviceConnections.TryGetValue(deviceId, out var connection))
                {
                    await connection.DisconnectAsync();
                    connection.Dispose();
                    _deviceConnections.Remove(deviceId);

                    if (_deviceConfigs.TryGetValue(deviceId, out var cfg))
                    {
                        cfg.IsConnected = false;
                        cfg.Status = "已断开";
                    }

                    // 触发状态变化事件
                    DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                    {
                        DeviceId = deviceId,
                        IsConnected = false,
                        Status = "已断开",
                        Timestamp = DateTime.Now
                    });

                    DebugWrite($"DisconnectDeviceAsync: {deviceId} disconnected");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"断开设备异常: {deviceId} - {ex.Message}", ex);
                DebugWrite($"DisconnectDeviceAsync exception for {deviceId}: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开所有设备
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            var tasks = _deviceConnections.Keys.Select(id => DisconnectDeviceAsync(id));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 发送消息到设备
        /// </summary>
        public async Task<bool> SendMessageAsync(string deviceId, string message)
        {
            // 尝试解析 SxFy:content 格式的消息
            ushort stream = 0; byte function = 0; string content = message;
            if (!string.IsNullOrEmpty(message))
            {
                var parts = message.Split(new[] { ':' }, 2);
                var head = parts[0];
                if (head.StartsWith("S", StringComparison.OrdinalIgnoreCase) && head.Contains("F"))
                {
                    var sf = head.Substring(1).Split('F');
                    if (sf.Length == 2) ushort.TryParse(sf[0], out stream);
                    if (sf.Length == 2) byte.TryParse(sf[1], out function);
                }
                if (parts.Length == 2) content = parts[1];
            }

            var hm = new HsmsMessage
            {
                Stream = stream,
                Function = function,
                Content = content,
                RequireResponse = false,
                DeviceId = _deviceConfigs.ContainsKey(deviceId) ? _deviceConfigs[deviceId].DeviceIdValue : (byte)1,
                SessionId = _deviceConfigs.ContainsKey(deviceId) ? _deviceConfigs[deviceId].SessionIdValue : 0x1234,
                Direction = MessageDirection.Outgoing,
                SenderId = deviceId,
                SenderRole = SenderRole.Client,
                Timestamp = DateTime.Now
            };

            return await SendMessageAsync(deviceId, hm);
        }

        /// <summary>
        /// 发送消息到设备（重载方法）
        /// </summary>
        public async Task<bool> SendMessageAsync(string deviceId, HsmsMessage message, int timeoutMs = 5000, int retry = 2)
        {
            var log = new MessageLogEntry
            {
                DeviceId = deviceId,
                Timestamp = DateTime.Now,
                Direction = MessageDirection.Outgoing,
                MessageType = message.MessageType ?? $"S{message.Stream}F{message.Function}",
                Content = message.Content ?? string.Empty,
                Success = false,
                Error = null,
                HsmsMessage = message
            };

            try
            {
                if (_deviceConnections.TryGetValue(deviceId, out var conn))
                {
                    var ok = await conn.SendMessageAsync(message, timeoutMs, retry);
                    log.Success = ok;
                    if (!ok) log.Error = "发送失败";

                    if (!_messageLogs.ContainsKey(deviceId)) _messageLogs[deviceId] = new List<MessageLogEntry>();
                    _messageLogs[deviceId].Insert(0, log);

                    MessageSent?.Invoke(this, log);

                    return ok;
                }

                log.Error = "设备未连接";
                logger.Warn($"设备未连接: {deviceId}");
            }
            catch (Exception ex)
            {
                log.Error = ex.Message;
                logger.Error($"发送异常: {deviceId} - {ex.Message}", ex);
            }
            finally
            {
                if (!_messageLogs.ContainsKey(deviceId)) _messageLogs[deviceId] = new List<MessageLogEntry>();
                _messageLogs[deviceId].Insert(0, log);
                MessageSent?.Invoke(this, log);
            }

            return false;
        }

        /// <summary>
        /// 获取设备状态
        /// </summary>
        public CIMMonitor.Models.DeviceStatus GetDeviceStatus(string deviceId)
        {
            if (_deviceConfigs.TryGetValue(deviceId, out var config))
            {
                return new CIMMonitor.Models.DeviceStatus
                {
                    DeviceId = config.DeviceId,
                    DeviceName = config.DeviceName,
                    IsConnected = config.IsConnected,
                    LastConnectionTime = config.LastConnectionTime,
                    Status = config.Status,
                    MessageCount = config.MessageCount,
                    Host = config.Host,
                    Port = config.Port
                };
            }
            return new CIMMonitor.Models.DeviceStatus { DeviceId = deviceId, Status = "未知设备" };
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        public List<CIMMonitor.Models.DeviceStatus> GetAllDeviceStatuses() => _deviceConfigs.Values.Select(c => GetDeviceStatus(c.DeviceId)).ToList();

        /// <summary>
        /// 获取设备消息日志
        /// </summary>
        public List<MessageLogEntry> GetMessageLogs(string deviceId, int max = 200)
        {
            if (_messageLogs.TryGetValue(deviceId, out var list)) return list.Take(max).ToList();
            return new List<MessageLogEntry>();
        }

        /// <summary>
        /// 连接状态变化事件处理
        /// </summary>
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            var connection = sender as HsmsConnectionService;
            if (connection == null) return;
            var deviceId = connection.DeviceId;
            if (_deviceConfigs.TryGetValue(deviceId, out var cfg))
            {
                cfg.IsConnected = isConnected;
                cfg.Status = isConnected ? "已连接" : "已断开";
                cfg.LastConnectionTime = isConnected ? DateTime.Now : cfg.LastConnectionTime;
            }
        }

        /// <summary>
        /// 消息接收事件处理
        /// </summary>
        private void OnMessageReceived(object? sender, HsmsMessage hsmsMessage)
        {
            var connection = sender as HsmsConnectionService;
            if (connection == null) return;
            var deviceId = connection.DeviceId;

            if (_deviceConfigs.TryGetValue(deviceId, out var cfg))
            {
                cfg.MessageCount++;
                cfg.LastAutoMessage = $"{hsmsMessage.MessageType}: {hsmsMessage.Content}";
                cfg.LastAutoMessageTime = hsmsMessage.Timestamp;
            }

            DeviceMessageReceived?.Invoke(this, new DeviceMessageEventArgs { DeviceId = deviceId, Message = hsmsMessage.Content, Timestamp = hsmsMessage.Timestamp, IsUserInteractive = hsmsMessage.IsUserInteractive, HsmsMessage = hsmsMessage });

            var recv = new MessageLogEntry
            {
                DeviceId = deviceId,
                Timestamp = hsmsMessage.Timestamp,
                Direction = MessageDirection.Incoming,
                MessageType = hsmsMessage.MessageType,
                Content = hsmsMessage.Content ?? string.Empty,
                Success = true,
                HsmsMessage = hsmsMessage
            };
            if (!_messageLogs.ContainsKey(deviceId)) _messageLogs[deviceId] = new List<MessageLogEntry>();
            _messageLogs[deviceId].Insert(0, recv);
            MessageSent?.Invoke(this, recv);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            DisconnectAllAsync().Wait(TimeSpan.FromSeconds(5));
            foreach (var kv in _deviceConnections) kv.Value.Dispose();
            _deviceConnections.Clear();
            _deviceConfigs.Clear();
            _messageLogs.Clear();
        }

        /// <summary>
        /// 设备状态变化事件参数
        /// </summary>
        public class DeviceStatusChangedEventArgs : EventArgs { public string DeviceId { get; set; } = string.Empty; public bool IsConnected { get; set; } public string Status { get; set; } = string.Empty; public DateTime Timestamp { get; set; } }
        /// <summary>
        /// 设备消息事件参数
        /// </summary>
        public class DeviceMessageEventArgs : EventArgs { public string DeviceId { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; public DateTime Timestamp { get; set; } public bool IsUserInteractive { get; set; } = false; public HsmsMessage? HsmsMessage { get; set; } }
    }
}
