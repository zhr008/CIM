using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Common.Models
{
    [Serializable]
    public class DeviceInfo
    {
        [XmlAttribute]
        public string ID { get; set; }
        
        [XmlAttribute]
        public string Name { get; set; }
        
        [XmlAttribute]
        public string IPAddress { get; set; }
        
        [XmlAttribute]
        public int Port { get; set; }
        
        [XmlAttribute]
        public string Role { get; set; } // "Server" or "Client"
        
        public DeviceInfo()
        {
            ID = string.Empty;
            Name = string.Empty;
            IPAddress = string.Empty;
            Role = "Client";
        }
    }

    [Serializable]
    public class HsmsConfig
    {
        [XmlElement("Devices")]
        public DevicesSection Devices { get; set; }
        
        [XmlElement("Settings")]
        public HsmsSettings Settings { get; set; }
        
        public HsmsConfig()
        {
            Devices = new DevicesSection();
            Settings = new HsmsSettings();
        }
    }

    [Serializable]
    public class DevicesSection
    {
        [XmlElement("Device")]
        public List<DeviceInfo> DeviceList { get; set; }
        
        public DevicesSection()
        {
            DeviceList = new List<DeviceInfo>();
        }
    }

    [Serializable]
    public class HsmsSettings
    {
        [XmlElement("Timeout")]
        public int Timeout { get; set; }
        
        [XmlElement("RetryCount")]
        public int RetryCount { get; set; }
        
        [XmlElement("ConnectionInterval")]
        public int ConnectionInterval { get; set; }
        
        public HsmsSettings()
        {
            Timeout = 30000;
            RetryCount = 3;
            ConnectionInterval = 5000;
        }
    }
    
    [Serializable]
    public class KepServerConfig
    {
        [XmlElement("Servers")]
        public ServersSection Servers { get; set; }
        
        [XmlElement("Settings")]
        public KepServerSettings Settings { get; set; }
        
        public KepServerConfig()
        {
            Servers = new ServersSection();
            Settings = new KepServerSettings();
        }
    }

    [Serializable]
    public class ServerInfo
    {
        [XmlAttribute]
        public string ID { get; set; }
        
        [XmlAttribute]
        public string Name { get; set; }
        
        [XmlAttribute]
        public string HostName { get; set; }
        
        [XmlAttribute]
        public int Port { get; set; }
        
        [XmlElement("Tags")]
        public TagsSection Tags { get; set; }
        
        public ServerInfo()
        {
            ID = string.Empty;
            Name = string.Empty;
            HostName = string.Empty;
            Tags = new TagsSection();
        }
    }

    [Serializable]
    public class ServersSection
    {
        [XmlElement("Server")]
        public List<ServerInfo> ServerList { get; set; }
        
        public ServersSection()
        {
            ServerList = new List<ServerInfo>();
        }
    }

    [Serializable]
    public class TagInfo
    {
        [XmlAttribute]
        public string Name { get; set; }
        
        [XmlAttribute]
        public string Path { get; set; }
        
        [XmlAttribute]
        public string DataType { get; set; }
        
        [XmlAttribute]
        public int ScanRate { get; set; }
        
        public TagInfo()
        {
            Name = string.Empty;
            Path = string.Empty;
            DataType = "String";
        }
    }

    [Serializable]
    public class TagsSection
    {
        [XmlElement("Tag")]
        public List<TagInfo> TagList { get; set; }
        
        public TagsSection()
        {
            TagList = new List<TagInfo>();
        }
    }

    [Serializable]
    public class KepServerSettings
    {
        [XmlElement("ConnectionTimeout")]
        public int ConnectionTimeout { get; set; }
        
        [XmlElement("ReconnectInterval")]
        public int ReconnectInterval { get; set; }
        
        [XmlElement("MaxRetries")]
        public int MaxRetries { get; set; }
        
        public KepServerSettings()
        {
            ConnectionTimeout = 10000;
            ReconnectInterval = 5000;
            MaxRetries = 5;
        }
    }
    
    // Message models for communication
    [Serializable]
    public class EquipmentMessage
    {
        public string EquipmentID { get; set; }
        public string MessageType { get; set; }
        public string MessageContent { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        public EquipmentMessage()
        {
            EquipmentID = string.Empty;
            MessageType = string.Empty;
            MessageContent = string.Empty;
            Timestamp = DateTime.Now;
            Properties = new Dictionary<string, object>();
        }
    }
    
    // Business data models
    [Serializable]
    public class EquipmentStatus
    {
        public string EquipmentID { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdate { get; set; }
        public Dictionary<string, object> DataPoints { get; set; }
        
        public EquipmentStatus()
        {
            EquipmentID = string.Empty;
            Status = "Unknown";
            LastUpdate = DateTime.Now;
            DataPoints = new Dictionary<string, object>();
        }
    }
    
    [Serializable]
    public class ProductionData
    {
        public string BatchID { get; set; }
        public string LotID { get; set; }
        public string RecipeID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> ProcessData { get; set; }
        
        public ProductionData()
        {
            BatchID = string.Empty;
            LotID = string.Empty;
            RecipeID = string.Empty;
            ProcessData = new Dictionary<string, object>();
        }
    }

    [Serializable]
    [XmlRoot("Configuration")]
    public class AppConfig
    {
        [XmlElement("TibrvService")]
        public TibrvServiceConfig TibrvService { get; set; }

        [XmlElement("Logging")]
        public LoggingConfig Logging { get; set; }

        public AppConfig()
        {
            TibrvService = new TibrvServiceConfig();
            Logging = new LoggingConfig();
        }
    }

    [Serializable]
    public class TibrvServiceConfig
    {
        [XmlElement("NetworkInterface")]
        public string NetworkInterface { get; set; }

        [XmlElement("Service")]
        public string Service { get; set; }

        [XmlElement("Daemon")]
        public string Daemon { get; set; }

        [XmlElement("WCFEndpoint")]
        public string WCFEndpoint { get; set; }

        [XmlElement("ConnectionTimeout")]
        public int ConnectionTimeout { get; set; }

        [XmlElement("RetryAttempts")]
        public int RetryAttempts { get; set; }

        public TibrvServiceConfig()
        {
            NetworkInterface = "127.0.0.1";
            Service = "7500";
            Daemon = "tcp:7500";
            WCFEndpoint = "http://localhost:8080/MesService";
            ConnectionTimeout = 30000;
            RetryAttempts = 3;
        }
    }

    [Serializable]
    public class LoggingConfig
    {
        [XmlElement("Level")]
        public string Level { get; set; }

        [XmlElement("FilePath")]
        public string FilePath { get; set; }

        public LoggingConfig()
        {
            Level = "INFO";
            FilePath = "logs/tibrv_service.log";
        }
    }
}