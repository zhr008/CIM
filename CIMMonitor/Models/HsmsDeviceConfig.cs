using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    /// <summary>
    /// HSMS设备配置模型
    /// 支持JSON和XML两种格式
    /// </summary>
    [XmlType("HsmsDeviceConfig")]
    public class HsmsDeviceConfig
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [JsonPropertyName("DeviceId")]
        [XmlAttribute("DeviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        [JsonPropertyName("DeviceName")]
        [XmlElement("DeviceName")]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 协议类型
        /// </summary>
        [JsonPropertyName("ProtocolType")]
        [XmlIgnore]
        public string ProtocolType { get; set; } = "HSMS";

        /// <summary>
        /// 连接角色：Server(服务端监听) 或 Client(客户端连接)
        /// </summary>
        [JsonPropertyName("Role")]
        [XmlElement("Role")]
        public string Role { get; set; } = "Client";

        /// <summary>
        /// 主机地址（客户端模式使用）
        /// </summary>
        [JsonPropertyName("Host")]
        [XmlElement("Host")]
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口（服务端监听端口或客户端连接端口）
        /// </summary>
        [JsonPropertyName("Port")]
        [XmlElement("Port")]
        public int Port { get; set; } = 5000;

        /// <summary>
        /// 设备ID值
        /// </summary>
        [JsonPropertyName("DeviceIdValue")]
        [XmlElement("DeviceIdValue")]
        public byte DeviceIdValue { get; set; } = 1;

        /// <summary>
        /// 会话ID值
        /// </summary>
        [JsonPropertyName("SessionIdValue")]
        [XmlElement("SessionIdValue")]
        public int SessionIdValue { get; set; } = 0x1234;

        /// <summary>
        /// 描述
        /// </summary>
        [JsonPropertyName("Description")]
        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 自动连接
        /// </summary>
        [JsonPropertyName("AutoConnect")]
        [XmlElement("AutoConnect")]
        public bool AutoConnect { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        [JsonPropertyName("Enabled")]
        [XmlElement("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 连接超时
        /// </summary>
        [JsonPropertyName("ConnectionTimeout")]
        [XmlElement("ConnectionTimeout")]
        public int ConnectionTimeout { get; set; } = 5000;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [JsonPropertyName("LastUpdated")]
        [XmlElement("LastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否已连接
        /// </summary>
        [JsonPropertyName("IsConnected")]
        [XmlIgnore]
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// 最后连接时间
        /// </summary>
        [JsonPropertyName("LastConnectionTime")]
        [XmlIgnore]
        public DateTime? LastConnectionTime { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [JsonPropertyName("Status")]
        [XmlIgnore]
        public string Status { get; set; } = "未连接";

        /// <summary>
        /// 消息计数
        /// </summary>
        [JsonPropertyName("MessageCount")]
        [XmlIgnore]
        public int MessageCount { get; set; } = 0;

        /// <summary>
        /// 错误消息
        /// </summary>
        [JsonPropertyName("ErrorMessage")]
        [XmlIgnore]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 最近自动消息（用于在设备列表中显示设备活动状态）
        /// </summary>
        [JsonPropertyName("LastAutoMessage")]
        [XmlIgnore]
        public string? LastAutoMessage { get; set; }

        /// <summary>
        /// 最近自动消息时间
        /// </summary>
        [JsonPropertyName("LastAutoMessageTime")]
        [XmlIgnore]
        public DateTime? LastAutoMessageTime { get; set; }

        /// <summary>
        /// 从DeviceConfig转换
        /// </summary>
        public static HsmsDeviceConfig FromDeviceConfig(DeviceConfig config)
        {

            if (config is HsmsDeviceXmlConfig xmlConfig)
            {
                return new HsmsDeviceConfig
                {
                    DeviceId = xmlConfig.Id,
                    DeviceName = xmlConfig.Name,
                    ProtocolType = "HSMS",
                    Role = xmlConfig.Role ?? "Client",
                    Host = xmlConfig.Connection.Host,
                    Port = xmlConfig.Connection.Port,
                    DeviceIdValue = xmlConfig.SecsSettings.DeviceIdValue,
                    SessionIdValue = xmlConfig.SecsSettings.SessionIdValue,
                    Description = xmlConfig.Description,
                    AutoConnect = xmlConfig.Connection.AutoConnect,
                    ConnectionTimeout = xmlConfig.Connection.Timeout,
                    LastUpdated = xmlConfig.LastUpdated
                };
            }

            throw new ArgumentException($"不支持的设备配置类型: {config.GetType().Name}");
        }

        /// <summary>
        /// 转换为DeviceConfig
        /// </summary>
        public HsmsDeviceXmlConfig ToDeviceConfig()
        {
            return new HsmsDeviceXmlConfig
            {
                Type = "HSMS",
                Id = this.DeviceId,
                Name = this.DeviceName,
                Role = this.Role,
                Connection = new ConnectionSettings
                {
                    Host = this.Host,
                    Port = this.Port,
                    Timeout = this.ConnectionTimeout,
                    AutoConnect = this.AutoConnect
                },
                SecsSettings = new SecsSettings
                {
                    DeviceIdValue = this.DeviceIdValue,
                    SessionIdValue = this.SessionIdValue
                },
                Description = this.Description,
                LastUpdated = this.LastUpdated
            };
        }
    }
}
