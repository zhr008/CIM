using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Common.Models;
using log4net;

namespace CIMMonitor.Services
{
    public class HsmsService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HsmsService));
        private List<DeviceInfo> devices;
        private Dictionary<string, TcpClient> connections;
        private CancellationTokenSource cancellationTokenSource;
        
        public event EventHandler<EquipmentMessage> OnMessageReceived;
        public event EventHandler<string> OnStatusChanged;
        
        public HsmsService()
        {
            devices = new List<DeviceInfo>();
            connections = new Dictionary<string, TcpClient>();
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void LoadConfiguration(string configPath)
        {
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(HsmsConfig));
                using var fileStream = new System.IO.FileStream(configPath, System.IO.FileMode.Open);
                var config = (HsmsConfig)serializer.Deserialize(fileStream);
                
                devices = config.Devices.DeviceList;
                
                log.Info($"Loaded {devices.Count} HSMS devices from configuration");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading HSMS configuration: {ex.Message}", ex);
                throw;
            }
        }
        
        public async Task StartAsync()
        {
            foreach (var device in devices)
            {
                if (device.Role == "Server")
                {
                    _ = Task.Run(() => StartServer(device));
                }
                else
                {
                    _ = Task.Run(() => ConnectToClient(device));
                }
            }
        }
        
        private async Task StartServer(DeviceInfo device)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Parse(device.IPAddress), device.Port);
                listener.Start();
                
                OnStatusChanged?.Invoke(this, $"HSMS Server started on {device.IPAddress}:{device.Port}");
                log.Info($"HSMS Server started on {device.IPAddress}:{device.Port}");
                
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    var clientId = device.ID;
                    
                    connections[clientId] = client;
                    
                    OnStatusChanged?.Invoke(this, $"New connection from {client.Client.RemoteEndPoint}");
                    log.Info($"New connection from {client.Client.RemoteEndPoint}");
                    
                    _ = Task.Run(() => HandleClient(client, clientId));
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in HSMS server: {ex.Message}", ex);
                OnStatusChanged?.Invoke(this, $"HSMS Server error: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
            }
        }
        
        private async Task ConnectToClient(DeviceInfo device)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = new TcpClient();
                    await client.ConnectAsync(device.IPAddress, device.Port);
                    
                    var clientId = device.ID;
                    connections[clientId] = client;
                    
                    OnStatusChanged?.Invoke(this, $"Connected to HSMS client {device.IPAddress}:{device.Port}");
                    log.Info($"Connected to HSMS client {device.IPAddress}:{device.Port}");
                    
                    _ = Task.Run(() => HandleClient(client, clientId));
                    
                    // Break out of loop after successful connection
                    break;
                }
                catch (Exception ex)
                {
                    log.Warn($"Failed to connect to HSMS client {device.IPAddress}:{device.Port}, retrying... {ex.Message}");
                    OnStatusChanged?.Invoke(this, $"Failed to connect to HSMS client {device.IPAddress}:{device.Port}, retrying...");
                    
                    await Task.Delay(5000); // Wait 5 seconds before retry
                }
            }
        }
        
        private async Task HandleClient(TcpClient client, string clientId)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                var buffer = new byte[1024];
                
                while (client.Connected && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        var equipmentMessage = new EquipmentMessage
                        {
                            EquipmentID = clientId,
                            MessageType = "HSMS",
                            MessageContent = message,
                            Timestamp = DateTime.Now
                        };
                        
                        OnMessageReceived?.Invoke(this, equipmentMessage);
                        log.Info($"Received HSMS message from {clientId}: {message}");
                        
                        // Process the message and potentially send a response
                        await ProcessHsmsMessage(message, stream);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error handling client {clientId}: {ex.Message}", ex);
                OnStatusChanged?.Invoke(this, $"Error handling client {clientId}: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                
                if (connections.ContainsKey(clientId))
                {
                    connections.Remove(clientId);
                }
                
                OnStatusChanged?.Invoke(this, $"Connection to {clientId} closed");
            }
        }
        
        private async Task ProcessHsmsMessage(string message, NetworkStream stream)
        {
            // Simple echo for demonstration - in real implementation, this would parse SECS/GEM messages
            try
            {
                var response = $"ACK:{message}";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                log.Error($"Error processing HSMS message: {ex.Message}", ex);
            }
        }
        
        public async Task SendMessageAsync(string deviceId, string message)
        {
            if (connections.ContainsKey(deviceId))
            {
                var client = connections[deviceId];
                if (client.Connected)
                {
                    var stream = client.GetStream();
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    
                    log.Info($"Sent HSMS message to {deviceId}: {message}");
                }
                else
                {
                    log.Warn($"Connection to {deviceId} is not active");
                }
            }
            else
            {
                log.Warn($"No connection found for device {deviceId}");
            }
        }
        
        public void Stop()
        {
            cancellationTokenSource.Cancel();
            
            foreach (var connection in connections.Values)
            {
                connection?.Close();
            }
            
            connections.Clear();
            log.Info("HSMS service stopped");
        }
        
        public List<DeviceInfo> GetConnectedDevices()
        {
            var connectedDevices = new List<DeviceInfo>();
            foreach (var device in devices)
            {
                if (connections.ContainsKey(device.ID))
                {
                    connectedDevices.Add(device);
                }
            }
            return connectedDevices;
        }
    }
}