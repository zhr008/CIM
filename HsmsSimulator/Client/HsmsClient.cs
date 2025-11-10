using HsmsSimulator.Models;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace HsmsSimulator.Client
{
    /// <summary>
    /// HSMS客户端
    /// 参考HslCommunicationDemo的SECS Client实现
    /// </summary>
    public class HsmsClient
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected = false;
        private readonly ConcurrentQueue<HsmsMessage> _messageQueue = new();
        private bool _autoReconnect = true; // 是否启用自动重连
        private int _reconnectDelay = 1000; // 重连延迟（毫秒）
        private int _maxReconnectDelay = 30000; // 最大重连延迟（毫秒）
        private int _reconnectAttempt = 0; // 重连尝试次数

        /// <summary>
        /// 客户端名称
        /// </summary>
        public string Name { get; set; } = "CLIENT";

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        public bool AutoReconnect
        {
            get => _autoReconnect;
            set => _autoReconnect = value;
        }

        /// <summary>
        /// 重连尝试次数
        /// </summary>
        public int ReconnectAttempt => _reconnectAttempt;

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息计数
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<bool>? ConnectionChanged;

        /// <summary>
        /// 消息已接收事件
        /// </summary>
        public event EventHandler<HsmsMessage>? MessageReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<Exception>? Error;

        /// <summary>
        /// 连接服务器
        /// </summary>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _tcpClient = new TcpClient();

                OnLog($"正在连接到 {Host}:{Port}...");

                await _tcpClient.ConnectAsync(Host, Port, cancellationToken);

                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                LastActivity = DateTime.Now;

                OnLog("HSMS客户端已连接");

                ConnectionChanged?.Invoke(this, true);

                // 启动接收消息的Task
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource!.Token));

                // 发送初始握手消息
                await SendHandshakeAsync();

                return true;
            }
            catch (Exception ex)
            {
                OnError(ex);
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync(bool manualDisconnect = false)
        {
            _isConnected = false;
            _cancellationTokenSource?.Cancel();
            _networkStream?.Close();
            _tcpClient?.Close();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();

            OnLog("HSMS客户端已断开连接");

            // 手动断开时，不启动自动重连
            if (manualDisconnect)
            {
                _autoReconnect = false;
                ConnectionChanged?.Invoke(this, false);
            }
            else
            {
                // 非手动断开，且启用自动重连时，启动重连逻辑
                if (_autoReconnect)
                {
                    _ = Task.Run(() => StartReconnectAsync());
                }
                else
                {
                    ConnectionChanged?.Invoke(this, false);
                }
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    if (_networkStream == null)
                        break;

                    var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        OnLog("连接已关闭");
                        break;
                    }

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, bytesRead);

                    var message = HsmsMessage.Parse(data, Name);
                    message.Timestamp = DateTime.Now;
                    message.Direction = MessageDirection.Incoming;

                    LastActivity = DateTime.Now;
                    MessageCount++;

                    OnLog($"接收: {message}");

                    // 触发事件
                    MessageReceived?.Invoke(this, message);

                    // 消息入队
                    _messageQueue.Enqueue(message);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                if (IsConnected)
                {
                    OnError(ex);
                }
            }
            finally
            {
                // 非手动断开，触发重连逻辑
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task SendMessageAsync(ushort stream, byte function, string content, bool requireResponse = false, bool isUserInteractive = true, SenderRole senderRole = SenderRole.Client)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("客户端未连接");
            }

            try
            {
                var message = new HsmsMessage
                {
                    Stream = stream,
                    Function = function,
                    Content = content,
                    RequireResponse = requireResponse,
                    DeviceId = 0,
                    SessionId = 0,
                    SenderId = Name,
                    Direction = MessageDirection.Outgoing,
                    Timestamp = DateTime.Now,
                    IsUserInteractive = isUserInteractive,
                    SenderRole = senderRole
                };

                var data = message.ToBytes();

                await _networkStream!.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();

                LastActivity = DateTime.Now;
                MessageCount++;

                OnLog($"发送: S{stream}F{function} - {content}");
            }
            catch (Exception ex)
            {
                OnError(ex);
                throw;
            }
        }

        /// <summary>
        /// 发送握手消息
        /// </summary>
        public async Task SendHandshakeAsync()
        {
            await SendMessageAsync(1, 13, "ARE_YOU_THERE", true, false);
        }

        /// <summary>
        /// 查询设备状态
        /// </summary>
        public async Task QueryEquipmentStatus()
        {
            await SendMessageAsync(2, 33, "EQUIPMENT_STATUS_REQUEST", true, false);
        }

        /// <summary>
        /// 发送事件报告
        /// </summary>
        public async Task SendEventReport(string eventType, Dictionary<string, string> parameters)
        {
            var content = new StringBuilder();
            content.Append($"EVENT_TYPE={eventType};");
            foreach (var param in parameters)
            {
                content.Append($"{param.Key}={param.Value};");
            }

            await SendMessageAsync(6, 11, content.ToString(), false, false);
        }

        /// <summary>
        /// 发送工艺程序请求
        /// </summary>
        public async Task RequestProcessProgram(string recipeId)
        {
            await SendMessageAsync(7, 21, $"RECIPE_ID={recipeId}", true, false);
        }

        /// <summary>
        /// 启动批量
        /// </summary>
        public async Task StartBatch(string batchId, string lotId, string recipe)
        {
            var parameters = new Dictionary<string, string>
            {
                ["BATCH_ID"] = batchId,
                ["LOT_ID"] = lotId,
                ["RECIPE"] = recipe
            };

            await SendEventReport("BATCH_START", parameters);
        }

        /// <summary>
        /// 完成批量
        /// </summary>
        public async Task CompleteBatch(string batchId)
        {
            var parameters = new Dictionary<string, string>
            {
                ["BATCH_ID"] = batchId,
                ["RESULT"] = "COMPLETED"
            };

            await SendEventReport("BATCH_COMPLETE", parameters);
        }

        /// <summary>
        /// 获取消息队列中的消息
        /// </summary>
        public bool TryDequeue(out HsmsMessage? message)
        {
            return _messageQueue.TryDequeue(out message);
        }

        /// <summary>
        /// 清空消息队列
        /// </summary>
        public void ClearQueue()
        {
            while (_messageQueue.TryDequeue(out _))
            {
                // 丢弃所有消息
            }
        }

        /// <summary>
        /// 获取客户端统计信息
        /// </summary>
        public string GetStatistics()
        {
            var uptime = DateTime.Now - LastActivity;
            return $"客户端: {Name}\n" +
                   $"连接: {(IsConnected ? "已连接" : "未连接")}\n" +
                   $"服务器: {Host}:{Port}\n" +
                   $"消息: {MessageCount}\n" +
                   $"队列: {_messageQueue.Count}\n" +
                   $"最后活动: {LastActivity:yyyy-MM-dd HH:mm:ss}\n" +
                   $"运行时间: {uptime:hh\\:mm\\:ss}";
        }

        /// <summary>
        /// 等待响应消息
        /// </summary>
        public async Task<HsmsMessage?> WaitForResponseAsync(string relatedMessageId, TimeSpan timeout)
        {
            var startTime = DateTime.Now;
            var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

            while (DateTime.Now - startTime < timeout)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    if (message.RelatedMessageId == relatedMessageId)
                    {
                        return message;
                    }
                    else
                    {
                        // 不是目标消息，重新入队
                        _messageQueue.Enqueue(message);
                    }
                }

                await Task.Delay(100, cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// 发送并等待响应
        /// </summary>
        public async Task<HsmsMessage?> SendAndWaitForResponseAsync(ushort stream, byte function, string content, TimeSpan timeout)
        {
            var messageId = Guid.NewGuid().ToString();

            await SendMessageAsync(stream, function, content, true, false);

            return await WaitForResponseAsync(messageId, timeout);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private void OnLog(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{Name}] {message}");
        }

        /// <summary>
        /// 错误处理
        /// </summary>
        private void OnError(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{Name}] 错误: {ex.Message}");
            Error?.Invoke(this, ex);
        }

        /// <summary>
        /// 启动自动重连
        /// </summary>
        private async Task StartReconnectAsync()
        {
            if (!_autoReconnect)
            {
                return;
            }

            _reconnectAttempt++;
            var delay = Math.Min(_reconnectDelay * Math.Pow(2, _reconnectAttempt - 1), _maxReconnectDelay);

            OnLog($"准备重连... (第{_reconnectAttempt}次尝试，{delay / 1000:F1}秒后)");

            await Task.Delay((int)delay);

            // 检查是否仍然启用自动重连
            if (!_autoReconnect)
            {
                OnLog("自动重连已禁用");
                return;
            }

            try
            {
                OnLog($"正在尝试重连到 {Host}:{Port}...");

                // 重新创建cancellation token
                _cancellationTokenSource = new CancellationTokenSource();

                // 尝试连接
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(Host, Port, _cancellationTokenSource.Token);

                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                LastActivity = DateTime.Now;

                OnLog("重连成功！");

                // 重置重连计数
                _reconnectAttempt = 0;

                // 触发连接事件
                ConnectionChanged?.Invoke(this, true);

                // 启动接收消息
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                // 发送握手消息
                await SendHandshakeAsync();
            }
            catch (Exception ex)
            {
                OnLog($"重连失败: {ex.Message}");

                // 重连失败，继续尝试
                if (_autoReconnect)
                {
                    _ = Task.Run(() => StartReconnectAsync());
                }
                else
                {
                    ConnectionChanged?.Invoke(this, false);
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
            _cancellationTokenSource?.Dispose();
        }
    }
}
