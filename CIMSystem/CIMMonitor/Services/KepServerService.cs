using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Models;
using log4net;

namespace CIMMonitor.Services
{
    // Mock OPC/KepServer service since we can't include actual OPC libraries
    public class KepServerService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(KepServerService));
        private List<ServerInfo> servers;
        private Dictionary<string, bool> serverConnections;
        private CancellationTokenSource cancellationTokenSource;
        
        public event EventHandler<EquipmentMessage> OnDataReceived;
        public event EventHandler<string> OnStatusChanged;
        
        public KepServerService()
        {
            servers = new List<ServerInfo>();
            serverConnections = new Dictionary<string, bool>();
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void LoadConfiguration(string configPath)
        {
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(KepServerConfig));
                using var fileStream = new System.IO.FileStream(configPath, System.IO.FileMode.Open);
                var config = (KepServerConfig)serializer.Deserialize(fileStream);
                
                servers = config.Servers.ServerList;
                
                log.Info($"Loaded {servers.Count} KepServer instances from configuration");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading KepServer configuration: {ex.Message}", ex);
                throw;
            }
        }
        
        public async Task StartAsync()
        {
            foreach (var server in servers)
            {
                _ = Task.Run(() => ConnectToServer(server));
            }
        }
        
        private async Task ConnectToServer(ServerInfo server)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Simulate connection to KepServer
                    await Task.Delay(1000); // Simulate connection time
                    
                    serverConnections[server.ID] = true;
                    
                    OnStatusChanged?.Invoke(this, $"Connected to KepServer {server.Name} at {server.HostName}:{server.Port}");
                    log.Info($"Connected to KepServer {server.Name} at {server.HostName}:{server.Port}");
                    
                    // Start monitoring tags
                    _ = Task.Run(() => MonitorTags(server));
                    
                    break; // Break out of loop after successful connection
                }
                catch (Exception ex)
                {
                    log.Warn($"Failed to connect to KepServer {server.Name}, retrying... {ex.Message}");
                    OnStatusChanged?.Invoke(this, $"Failed to connect to KepServer {server.Name}, retrying...");
                    
                    await Task.Delay(5000); // Wait 5 seconds before retry
                }
            }
        }
        
        private async Task MonitorTags(ServerInfo server)
        {
            while (serverConnections.ContainsKey(server.ID) && 
                   serverConnections[server.ID] && 
                   !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Simulate reading tag values from KepServer
                    foreach (var tag in server.Tags.TagList)
                    {
                        var value = GenerateMockValue(tag.DataType);
                        
                        var equipmentMessage = new EquipmentMessage
                        {
                            EquipmentID = server.ID,
                            MessageType = "OPC_DATA",
                            MessageContent = $"{tag.Name}={value}",
                            Timestamp = DateTime.Now,
                            Properties = new Dictionary<string, object>
                            {
                                { "TagName", tag.Name },
                                { "Value", value },
                                { "DataType", tag.DataType }
                            }
                        };
                        
                        OnDataReceived?.Invoke(this, equipmentMessage);
                        log.Info($"Read tag {tag.Name} from {server.Name}: {value}");
                    }
                    
                    // Wait for the scan rate interval
                    await Task.Delay(server.Tags.TagList.Count > 0 ? 
                                    server.Tags.TagList[0].ScanRate : 1000);
                }
                catch (Exception ex)
                {
                    log.Error($"Error monitoring tags for {server.Name}: {ex.Message}", ex);
                    OnStatusChanged?.Invoke(this, $"Error monitoring tags for {server.Name}: {ex.Message}");
                    
                    // Reconnect after error
                    serverConnections[server.ID] = false;
                    await ConnectToServer(server);
                    break;
                }
            }
        }
        
        private object GenerateMockValue(string dataType)
        {
            var random = new Random();
            switch (dataType.ToLower())
            {
                case "float":
                case "double":
                    return random.NextDouble() * 100.0;
                case "int":
                case "integer":
                    return random.Next(0, 1000);
                case "bool":
                case "boolean":
                    return random.Next(0, 2) == 1;
                case "string":
                default:
                    return $"MockValue_{random.Next(1000, 9999)}";
            }
        }
        
        public void Stop()
        {
            cancellationTokenSource.Cancel();
            
            foreach (var serverId in serverConnections.Keys)
            {
                serverConnections[serverId] = false;
            }
            
            log.Info("KepServer service stopped");
        }
        
        public List<ServerInfo> GetConnectedServers()
        {
            var connectedServers = new List<ServerInfo>();
            foreach (var server in servers)
            {
                if (serverConnections.ContainsKey(server.ID) && serverConnections[server.ID])
                {
                    connectedServers.Add(server);
                }
            }
            return connectedServers;
        }
        
        public async Task<object> ReadTagAsync(string serverId, string tagName)
        {
            // In a real implementation, this would read the actual tag value from the OPC server
            // For now, we'll generate a mock value
            var server = servers.Find(s => s.ID == serverId);
            if (server != null)
            {
                var tag = server.Tags.TagList.Find(t => t.Name == tagName);
                if (tag != null)
                {
                    return GenerateMockValue(tag.DataType);
                }
            }
            return null;
        }
        
        public async Task<bool> WriteTagAsync(string serverId, string tagName, object value)
        {
            // In a real implementation, this would write the value to the OPC server
            // For now, we'll just return true to simulate success
            log.Info($"Simulated write to tag {tagName} on server {serverId} with value {value}");
            return true;
        }
    }
}