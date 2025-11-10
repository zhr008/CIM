using HsmsSimulator.Device;
using HsmsSimulator.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HsmsSimulator.Server
{
    /// <summary>
    /// HSMS服务器
    /// 支持SECS-II数据编码和会话管理
    /// </summary>
    public class HsmsServer
    {
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
        private readonly List<SimulatedDevice> _devices = new();
        private bool _isRunning = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new();
        private readonly SecsSessionManager _sessionManager;

        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections { get; set; } = 100;

        /// <summary>
        /// 是否运行中
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 当前连接数
        /// </summary>
        public int ConnectionCount => _clients.Count;

        /// <summary>
        /// 设备列表
        /// </summary>
        public IReadOnlyList<SimulatedDevice> Devices => _devices.AsReadOnly();

        /// <summary>
        /// SECS会话管理器
        /// </summary>
        public SecsSessionManager SessionManager => _sessionManager;

        /// <summary>
        /// 客户端已连接事件
        /// </summary>
        public event EventHandler<string>? ClientConnected;

        /// <summary>
        /// 客户端已断开事件
        /// </summary>
        public event EventHandler<string>? ClientDisconnected;

        /// <summary>
        /// 消息已接收事件
        /// </summary>
        public event EventHandler<HsmsMessage>? MessageReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<Exception>? Error;

        /// <summary>
        /// 构造函数
        /// </summary>
        public HsmsServer(int port = 5000)
        {
            Port = port;
            _sessionManager = new SecsSessionManager();

            // 订阅会话管理事件
            _sessionManager.SessionStateChanged += OnSessionStateChanged;
            _sessionManager.SelectRequestReceived += OnSelectRequestReceived;
            _sessionManager.SeparateRequestReceived += OnSeparateRequestReceived;
            _sessionManager.LinktestRequestReceived += OnLinktestRequestReceived;

            InitializeDevices();
        }

        /// <summary>
        /// 初始化设备
        /// </summary>
        private void InitializeDevices()
        {
            // 创建默认设备
            _devices.Add(new SimulatedDevice("DEVICE001", "模拟设备1"));
            _devices.Add(new SimulatedDevice("DEVICE002", "模拟设备2"));
            _devices.Add(new SimulatedDevice("DEVICE003", "模拟设备3"));

            // 启动所有设备
            foreach (var device in _devices)
            {
                device.Start();
            }
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start(MaxConnections);
                _isRunning = true;

                OnLog($"HSMS服务器已启动，监听端口: {Port}");
                OnLog($"已初始化 {_devices.Count} 个模拟设备");

                // 启动接受连接的Task
                _ = Task.Run(() => AcceptClientsAsync(_cancellationTokenSource!.Token));

                // 启动设备监控Task
                _ = Task.Run(() => MonitorDevicesAsync(_cancellationTokenSource!.Token));
            }
            catch (Exception ex)
            {
                OnError(ex);
                throw;
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public async Task StopAsync()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            // 停止所有设备
            foreach (var device in _devices)
            {
                device.Stop();
            }

            // 断开所有客户端
            var tasks = _clients.Values.Select(c => DisconnectClientAsync(c.Id));
            await Task.WhenAll(tasks);

            // 停止监听
            _listener?.Stop();

            OnLog("HSMS服务器已停止");
        }

        /// <summary>
        /// 接受客户端连接
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync();

                    var clientId = Guid.NewGuid().ToString();
                    var clientInfo = new ClientInfo
                    {
                        Id = clientId,
                        Client = client,
                        ConnectedAt = DateTime.Now,
                        LastActivity = DateTime.Now
                    };

                    if (_clients.TryAdd(clientId, clientInfo))
                    {
                        ClientConnected?.Invoke(this, clientId);
                        OnLog($"客户端已连接: {clientId}");

                        // 处理客户端
                        _ = Task.Run(() => HandleClientAsync(clientInfo, cancellationToken));
                    }
                    else
                    {
                        client.Close();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// 处理客户端
        /// </summary>
        private async Task HandleClientAsync(ClientInfo clientInfo, CancellationToken cancellationToken)
        {
            try
            {
                using var stream = clientInfo.Client.GetStream();
                var buffer = new byte[8192];

                while (!cancellationToken.IsCancellationRequested && clientInfo.Client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, bytesRead);

                    var message = HsmsMessage.Parse(data, clientInfo.Id);
                    message.Timestamp = DateTime.Now;

                    clientInfo.LastActivity = DateTime.Now;
                    clientInfo.MessageCount++;

                    OnLog($"[{clientInfo.Id}] 接收: {message}");

                    MessageReceived?.Invoke(this, message);

                    // 处理SECS-II会话消息
                    var response = await ProcessSecsMessage(message, clientInfo.Id);
                    if (response != null)
                    {
                        await SendToClientAsync(clientInfo.Id, response);
                    }
                    else
                    {
                        // 处理设备消息
                        var device = _devices.FirstOrDefault(d => d.Status == DeviceStatus.Online || d.Status == DeviceStatus.Busy);
                        if (device != null)
                        {
                            response = device.ProcessMessage(message);
                            if (response != null)
                            {
                                await SendToClientAsync(clientInfo.Id, response);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                await DisconnectClientAsync(clientInfo.Id);
            }
        }

        /// <summary>
        /// 监控设备
        /// </summary>
        private async Task MonitorDevicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    // 检查设备状态
                    foreach (var device in _devices)
                    {
                        if (device.Status == DeviceStatus.Online || device.Status == DeviceStatus.Busy)
                        {
                            // 广播设备状态变化
                            var statusMessage = new HsmsMessage
                            {
                                Stream = 6,
                                Function = 11, // Event Report
                                Content = $"EQUIPMENT_ID={device.DeviceId};STATUS={device.Status};PROGRESS={device.GetDataItem("PROGRESS")}",
                                DeviceId = 0,
                                SessionId = 0,
                                RequireResponse = false,
                                Direction = MessageDirection.Outgoing,
                                Timestamp = DateTime.Now
                            };

                            await BroadcastAsync(statusMessage);
                        }
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        /// <summary>
        /// 向客户端发送消息
        /// </summary>
        public async Task SendToClientAsync(string clientId, HsmsMessage message)
        {
            if (_clients.TryGetValue(clientId, out var clientInfo) && clientInfo.Client.Connected)
            {
                try
                {
                    var data = message.ToBytes();
                    var stream = clientInfo.Client.GetStream();

                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();

                    clientInfo.MessageCount++;
                    clientInfo.LastActivity = DateTime.Now;

                    OnLog($"[{clientId}] 发送: {message}");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    await DisconnectClientAsync(clientId);
                }
            }
        }

        /// <summary>
        /// 广播消息给所有客户端
        /// </summary>
        public async Task BroadcastAsync(HsmsMessage message)
        {
            var tasks = _clients.Values
                .Where(c => c.Client.Connected)
                .Select(c => SendToClientAsync(c.Id, message));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 断开客户端
        /// </summary>
        private async Task DisconnectClientAsync(string clientId)
        {
            if (_clients.TryRemove(clientId, out var clientInfo))
            {
                clientInfo.Client.Close();
                ClientDisconnected?.Invoke(this, clientId);
                OnLog($"客户端已断开: {clientId}");

                // 清理该客户端相关的会话
                lock (_lockObject)
                {
                    var sessionsToRemove = _sessionManager.GetAllSessions()
                        .Where(s => s.ClientId == clientId)
                        .ToList();

                    foreach (var session in sessionsToRemove)
                    {
                        _sessionManager.EndSession(session.SessionId, "Client disconnected");
                        OnLog($"清理会话: {session.SessionId:X4}");
                    }
                }

                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// 获取所有客户端信息
        /// </summary>
        public List<ClientInfo> GetClientInfos()
        {
            return _clients.Values.ToList();
        }

        /// <summary>
        /// 获取设备状态
        /// </summary>
        public string GetDevicesStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 设备状态 ===");
            foreach (var device in _devices)
            {
                sb.AppendLine($"{device.DeviceName} ({device.DeviceId}): {device.Status}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 启动设备
        /// </summary>
        public void StartDevice(string deviceId)
        {
            var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
            {
                device.Start();
                OnLog($"设备已启动: {deviceId}");
            }
        }

        /// <summary>
        /// 停止设备
        /// </summary>
        public void StopDevice(string deviceId)
        {
            var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
            {
                device.Stop();
                OnLog($"设备已停止: {deviceId}");
            }
        }

        /// <summary>
        /// 启动批量
        /// </summary>
        public void StartBatch(string deviceId, string batchId, string lotId, string recipe)
        {
            var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
            {
                device.StartBatch(batchId, lotId, recipe);
                OnLog($"设备 {deviceId} 启动批量: {batchId}");
            }
        }

        /// <summary>
        /// 完成批量
        /// </summary>
        public void CompleteBatch(string deviceId)
        {
            var device = _devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
            {
                device.CompleteBatch();
                OnLog($"设备 {deviceId} 批量完成");
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private void OnLog(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        /// <summary>
        /// 错误处理
        /// </summary>
        private void OnError(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 错误: {ex.Message}");
            Error?.Invoke(this, ex);
        }

        /// <summary>
        /// 处理SECS-II消息
        /// </summary>
        private async Task<HsmsMessage?> ProcessSecsMessage(HsmsMessage message, string clientId)
        {
            // 处理S1F13 - Select Request
            if (message.Stream == 1 && message.Function == 13)
            {
                OnLog($"[{clientId}] 收到Select请求: S1F13");
                return await _sessionManager.ProcessSelectRequest(message.SessionId, (byte)message.DeviceId, clientId);
            }

            // 处理S1F15 - Separate Request
            if (message.Stream == 1 && message.Function == 15)
            {
                OnLog($"[{clientId}] 收到Separate请求: S1F15");
                return await _sessionManager.ProcessSeparateRequest(message.SessionId);
            }

            // 处理S1F17 - Linktest Request
            if (message.Stream == 1 && message.Function == 17)
            {
                OnLog($"[{clientId}] 收到Linktest请求: S1F17");
                return _sessionManager.ProcessLinktestRequest(message.SessionId);
            }

            // 检查是否需要响应（奇数Function需要响应）
            if (SecsSessionManager.RequiresResponse(message.Stream, (byte)message.Function))
            {
                OnLog($"[{clientId}] 收到需要响应的消息: S{message.Stream}F{message.Function}");
            }

            return null;
        }

        #region 会话管理事件处理器

        /// <summary>
        /// 会话状态改变事件处理器
        /// </summary>
        private void OnSessionStateChanged(object? sender, (int SessionId, SessionState OldState, SessionState NewState) e)
        {
            OnLog($"会话状态改变: {e.SessionId:X4} - {e.OldState} -> {e.NewState}");
        }

        /// <summary>
        /// Select请求事件处理器
        /// </summary>
        private void OnSelectRequestReceived(object? sender, SecsSession session)
        {
            OnLog($"收到Select请求: 会话{session.SessionId:X4}, 设备ID={session.DeviceId}");
        }

        /// <summary>
        /// Separate请求事件处理器
        /// </summary>
        private void OnSeparateRequestReceived(object? sender, SecsSession session)
        {
            OnLog($"收到Separate请求: 会话{session.SessionId:X4}");
        }

        /// <summary>
        /// Linktest请求事件处理器
        /// </summary>
        private void OnLinktestRequestReceived(object? sender, SecsSession session)
        {
            OnLog($"收到Linktest请求: 会话{session.SessionId:X4}");
        }

        #endregion
    }

    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        public string Id { get; set; } = string.Empty;
        public TcpClient Client { get; set; } = null!;
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public int MessageCount { get; set; }
    }
}
