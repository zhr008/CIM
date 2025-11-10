using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using log4net;
using HsmsSimulator.Models;

namespace CIMMonitor.Services
{
    /// <summary>
    /// HSMS客户端/服务端连接服务
    /// 支持：客户端连接到单一服务器、服务端监听并管理多个客户端
    /// 提供二进制 HsmsMessage 发送接口
    /// </summary>
    public class HsmsConnectionService : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(HsmsConnectionService));

        private const int DEFAULT_BUFFER_SIZE = 8192;

        private TcpClient? _client;
        private TcpListener? _server;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isServerMode = false;

        private readonly Dictionary<string, ClientInfo> _clients = new();
        private readonly object _clientsLock = new();

        private class ClientInfo
        {
            public TcpClient Client { get; set; } = null!;
            public NetworkStream Stream { get; set; } = null!;
            public DateTime ConnectedTime { get; set; }
            public int MessageCount { get; set; }
        }

        public string DeviceId { get; private set; }
        public byte DeviceIdValue { get; private set; }
        public int SessionIdValue { get; private set; }

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<HsmsMessage>? MessageReceived;

        public int MessageCount { get; private set; }
        public DateTime? LastConnectionTime { get; private set; }

        public int ClientCount
        {
            get
            {
                if (!_isServerMode) return 0;
                lock (_clientsLock) return _clients.Count;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (_isServerMode)
                {
                    lock (_clientsLock) return _clients.Count > 0;
                }
                return _client?.Connected == true;
            }
        }

        public HsmsConnectionService(string deviceId, byte deviceIdValue, int sessionIdValue)
        {
            DeviceId = deviceId;
            DeviceIdValue = deviceIdValue;
            SessionIdValue = sessionIdValue;
        }

        private void DebugWrite(string text)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CIMMonitor_DEBUG.txt");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}");
            }
            catch { }
        }

        public async Task<bool> ConnectAsync(string host, int port, bool isServerMode = false)
        {
            try
            {
                _isServerMode = isServerMode;
                DebugWrite($"ConnectAsync called. Host={host}, Port={port}, IsServerMode={isServerMode}");

                if (isServerMode)
                {
                    try
                    {
                        DebugWrite($"Starting TcpListener on port {port}");
                        _server = new TcpListener(IPAddress.Any, port);
                        _server.Start();

                        // mark as listening
                        LastConnectionTime = DateTime.Now;

                        // Notify listeners that the server is now listening
                        ConnectionStatusChanged?.Invoke(this, true);

                        DebugWrite($"TcpListener started on port {port}");

                        _cancellationTokenSource = new CancellationTokenSource();
                        _ = Task.Run(() => AcceptClientAsync(_cancellationTokenSource.Token));
                        logger.Info($"HSMS server started on port {port}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Start server error: {ex.Message}", ex);
                        DebugWrite($"Start server error: {ex.Message} - {ex.StackTrace}");
                        // ensure server cleaned up
                        try { _server?.Stop(); } catch { }
                        _server = null;
                        return false;
                    }
                }
                else
                {
                    DebugWrite($"Attempting connect to server {host}:{port}");
                    _client = new TcpClient();
                    await _client.ConnectAsync(host, port);
                    _stream = _client.GetStream();
                    _cancellationTokenSource = new CancellationTokenSource();
                    LastConnectionTime = DateTime.Now;
                    ConnectionStatusChanged?.Invoke(this, true);
                    _ = Task.Run(() => StartListeningAsync(_cancellationTokenSource.Token));
                    logger.Info($"Connected to HSMS server {host}:{port}");
                    DebugWrite($"Connected to server {host}:{port}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"ConnectAsync error: {ex.Message}", ex);
                DebugWrite($"ConnectAsync error: {ex.Message} - Stack: {ex.StackTrace}");
                return false;
            }
        }

        private async Task AcceptClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _server != null)
                {
                    var client = await _server.AcceptTcpClientAsync();
                    var clientId = Guid.NewGuid().ToString();
                    var stream = client.GetStream();
                    lock (_clientsLock)
                    {
                        _clients[clientId] = new ClientInfo { Client = client, Stream = stream, ConnectedTime = DateTime.Now, MessageCount = 0 };
                    }
                    ConnectionStatusChanged?.Invoke(this, true);
                    _ = Task.Run(() => StartListeningForClientAsync(clientId, stream, cancellationToken));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { logger.Error($"AcceptClientAsync error: {ex.Message}", ex); DebugWrite($"AcceptClientAsync error: {ex.Message}"); }
        }

        private async Task StartListeningForClientAsync(string clientId, NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[DEFAULT_BUFFER_SIZE];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) { RemoveClient(clientId); break; }
                    var messageBytes = new byte[bytesRead];
                    Array.Copy(buffer, 0, messageBytes, 0, bytesRead);
                    try
                    {
                        var msg = HsmsMessage.Parse(messageBytes, clientId, SenderRole.Client);
                        MessageCount++;
                        lock (_clientsLock)
                        {
                            if (_clients.TryGetValue(clientId, out var ci)) ci.MessageCount++;
                        }
                        MessageReceived?.Invoke(this, msg);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Parse client message error: {ex.Message}", ex);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.Error($"StartListeningForClientAsync error: {ex.Message}", ex);
                RemoveClient(clientId);
            }
        }

        private void RemoveClient(string clientId)
        {
            lock (_clientsLock)
            {
                if (_clients.TryGetValue(clientId, out var ci))
                {
                    try { ci.Stream?.Close(); ci.Client?.Close(); } catch { }
                    _clients.Remove(clientId);
                }
            }
            if (ClientCount == 0) ConnectionStatusChanged?.Invoke(this, false);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                if (_isServerMode)
                {
                    lock (_clientsLock)
                    {
                        foreach (var kv in _clients.Values)
                        {
                            try { kv.Stream?.Close(); kv.Client?.Close(); } catch { }
                        }
                        _clients.Clear();
                    }
                    _server?.Stop();
                    _server = null;
                }
                else
                {
                    _stream?.Close(); _client?.Close(); _stream = null; _client = null;
                }

                ConnectionStatusChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                logger.Error($"DisconnectAsync error: {ex.Message}", ex);
                DebugWrite($"DisconnectAsync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Send string - compatibility wrapper (builds simple HsmsMessage)
        /// </summary>
        public Task<bool> SendMessageAsync(string message) => SendMessageAsync(new HsmsMessage
        {
            Stream = 0,
            Function = 0,
            Content = message ?? string.Empty,
            RequireResponse = false,
            DeviceId = DeviceIdValue,
            SessionId = SessionIdValue,
            Direction = MessageDirection.Outgoing,
            SenderRole = _isServerMode ? SenderRole.Server : SenderRole.Client,
            SenderId = DeviceId,
            Timestamp = DateTime.Now
        });

        /// <summary>
        /// Send HsmsMessage (binary). If server mode -> broadcast; client mode -> send to server.
        /// Supports simple timeout and retry.
        /// </summary>
        public async Task<bool> SendMessageAsync(HsmsMessage message, int timeoutMs = 5000, int retryCount = 2)
        {
            if (message == null) return false;
            var data = message.ToBytes();

            if (_isServerMode)
            {
                List<ClientInfo> snapshot;
                lock (_clientsLock) snapshot = _clients.Values.ToList();
                if (snapshot.Count == 0) { logger.Warn("No clients to broadcast"); return false; }
                int success = 0;
                foreach (var client in snapshot)
                {
                    bool sent = false;
                    for (int attempt = 0; attempt <= retryCount && !sent; attempt++)
                    {
                        try
                        {
                            var writeTask = client.Stream.WriteAsync(data, 0, data.Length);
                            var completed = await Task.WhenAny(writeTask, Task.Delay(timeoutMs));
                            if (completed == writeTask)
                            {
                                await client.Stream.FlushAsync();
                                client.MessageCount++; sent = true; success++;
                            }
                            else
                            {
                                logger.Warn($"Broadcast write timeout attempt={attempt}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Broadcast send error attempt={attempt}: {ex.Message}", ex);
                            await Task.Delay(100 * (attempt + 1));
                        }
                    }
                }
                MessageCount += success;
                logger.Info($"Broadcast result success={success}");
                return success > 0;
            }
            else
            {
                if (!IsConnected || _stream == null) { logger.Warn("Not connected"); return false; }
                for (int attempt = 0; attempt <= retryCount; attempt++)
                {
                    try
                    {
                        var writeTask = _stream.WriteAsync(data, 0, data.Length);
                        var completed = await Task.WhenAny(writeTask, Task.Delay(timeoutMs));
                        if (completed == writeTask)
                        {
                            await _stream.FlushAsync(); MessageCount++; return true;
                        }
                        else
                        {
                            logger.Warn($"Send write timeout attempt={attempt}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Send error attempt={attempt}: {ex.Message}", ex);
                    }
                    await Task.Delay(100 * (attempt + 1));
                }
                logger.Error("Send failed after retries");
                return false;
            }
        }

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[DEFAULT_BUFFER_SIZE];
            try
            {
                while (!cancellationToken.IsCancellationRequested && _stream != null)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) { ConnectionStatusChanged?.Invoke(this, false); break; }
                    var messageBytes = new byte[bytesRead]; Array.Copy(buffer, 0, messageBytes, 0, bytesRead);
                    try
                    {
                        var msg = HsmsMessage.Parse(messageBytes, DeviceId, SenderRole.Server);
                        MessageCount++; MessageReceived?.Invoke(this, msg);
                    }
                    catch (Exception ex) { logger.Error($"Parse server message error: {ex.Message}", ex); }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { logger.Error($"StartListeningAsync error: {ex.Message}", ex); ConnectionStatusChanged?.Invoke(this, false); }
        }

        public void Dispose()
        {
            try { DisconnectAsync().Wait(1000); } catch { }
        }
    }
}
