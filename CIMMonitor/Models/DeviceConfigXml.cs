using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    /// <summary>
    /// 设备配置XML根元素
    /// </summary>
    [XmlRoot("DeviceConfiguration")]
    public class DeviceConfiguration
    {
        [XmlElement("GlobalSettings")]
        public GlobalSettings GlobalSettings { get; set; } = new();

        [XmlArray("Devices")]
        [XmlArrayItem("Device")]
        public List<DeviceConfig> Devices { get; set; } = new();
    }

    /// <summary>
    /// 全局设置
    /// </summary>
    public class GlobalSettings
    {
        [XmlElement("DefaultTimeout")]
        public int DefaultTimeout { get; set; } = 5000;

        [XmlElement("RetryAttempts")]
        public int RetryAttempts { get; set; } = 3;

        [XmlElement("HeartbeatInterval")]
        public int HeartbeatInterval { get; set; } = 30000;

        [XmlElement("MaxMessageSize")]
        public int MaxMessageSize { get; set; } = 10240;
    }

    /// <summary>
    /// 设备配置基类
    /// </summary>
    [XmlInclude(typeof(HsmsDeviceXmlConfig))]
    [XmlInclude(typeof(OpcDeviceConfig))]
    [XmlInclude(typeof(OpcUaDeviceConfig))]
    public class DeviceConfig
    {
        [XmlAttribute("Type")]
        public string Type { get; set; } = string.Empty;

        [XmlAttribute("Id")]
        public string Id { get; set; } = string.Empty;

        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("Connection")]
        public ConnectionSettings Connection { get; set; } = new();

        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;

        [XmlElement("LastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 连接设置
    /// </summary>
    public class ConnectionSettings
    {
        [XmlElement("Host")]
        public string Host { get; set; } = "127.0.0.1";

        [XmlElement("Port")]
        public int Port { get; set; } = 5000;

        [XmlElement("Timeout")]
        public int Timeout { get; set; } = 5000;

        [XmlElement("AutoConnect")]
        public bool AutoConnect { get; set; } = false;
    }

    /// <summary>
    /// HSMS设备配置 - 使用HsmsDeviceConfig.cs中的HsmsDeviceXmlConfig
    /// </summary>
    public class HsmsDeviceXmlConfig : DeviceConfig
    {
        /// <summary>
        /// 连接角色：Server(服务端监听) 或 Client(客户端连接)
        /// </summary>
        [XmlElement("Role")]
        public string? Role { get; set; }

        [XmlElement("SecsSettings")]
        public SecsSettings SecsSettings { get; set; } = new();

        public override string ToString()
        {
            return $"HSMS - {Name} ({Id})";
        }
    }

    /// <summary>
    /// SECS设置
    /// </summary>
    public class SecsSettings
    {
        [XmlElement("DeviceIdValue")]
        public byte DeviceIdValue { get; set; } = 1;

        [XmlElement("SessionIdValue")]
        public int SessionIdValue { get; set; } = 0x1234;
    }

    /// <summary>
    /// OPC设备配置
    /// </summary>
    public class OpcDeviceConfig : DeviceConfig
    {
        [XmlElement("OpcSettings")]
        public OpcSettings OpcSettings { get; set; } = new();

        public override string ToString()
        {
            return $"OPC - {Name} ({Id})";
        }
    }

    /// <summary>
    /// OPC设置
    /// </summary>
    public class OpcSettings
    {
        [XmlElement("ServerName")]
        public string ServerName { get; set; } = string.Empty;

        [XmlElement("UpdateRate")]
        public int UpdateRate { get; set; } = 1000;

        [XmlElement("DeadBand")]
        public double DeadBand { get; set; } = 0.0;

        [XmlElement("GroupName")]
        public string GroupName { get; set; } = "Group1";

        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public List<OpcTag> Tags { get; set; } = new();
    }

    /// <summary>
    /// OPC标签
    /// </summary>
    public class OpcTag
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("ItemId")]
        public string ItemId { get; set; } = string.Empty;

        [XmlElement("DataType")]
        public string DataType { get; set; } = "Float";

        [XmlElement("UpdateRate")]
        public int UpdateRate { get; set; } = 1000;
    }

    /// <summary>
    /// OPC UA设备配置
    /// </summary>
    public class OpcUaDeviceConfig : DeviceConfig
    {
        [XmlElement("OpcUaSettings")]
        public OpcUaSettings OpcUaSettings { get; set; } = new();

        public override string ToString()
        {
            return $"OPC_UA - {Name} ({Id})";
        }
    }

    /// <summary>
    /// OPC UA设置
    /// </summary>
    public class OpcUaSettings
    {
        [XmlElement("EndpointUrl")]
        public string EndpointUrl { get; set; } = string.Empty;

        [XmlElement("SecurityPolicy")]
        public string SecurityPolicy { get; set; } = "None";

        [XmlElement("SecurityMode")]
        public string SecurityMode { get; set; } = "None";

        [XmlElement("UserName")]
        public string UserName { get; set; } = string.Empty;

        [XmlElement("Password")]
        public string Password { get; set; } = string.Empty;
    }
}
