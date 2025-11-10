using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    /// <summary>
    /// 统一设备配置类
    /// 用于在设备列表中显示所有类型的设备
    /// </summary>
    [XmlInclude(typeof(HsmsDeviceXmlConfig))]
    [XmlInclude(typeof(OpcDeviceConfig))]
    [XmlInclude(typeof(OpcUaDeviceConfig))]
    public class UnifiedDeviceConfig
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 协议类型
        /// </summary>
        public string ProtocolType { get; set; } = string.Empty;

        /// <summary>
        /// 主机地址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 是否自动连接
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        public string Status { get; set; } = "Disconnected";

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 消息计数
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// 最后连接时间
        /// </summary>
        public DateTime? LastConnectionTime { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// HSMS特有属性
        /// </summary>
        public byte? DeviceIdValue { get; set; }
        public int? SessionIdValue { get; set; }

        /// <summary>
        /// OPC特有属性
        /// </summary>
        public string? OpcServerName { get; set; }
        public int? OpcUpdateRate { get; set; }

        /// <summary>
        /// OPC UA特有属性
        /// </summary>
        public string? OpcUaEndpointUrl { get; set; }
        public string? OpcUaSecurityPolicy { get; set; }

        /// <summary>
        /// 从DeviceConfig转换
        /// </summary>
        public static UnifiedDeviceConfig FromDeviceConfig(DeviceConfig deviceConfig)
        {
            var unified = new UnifiedDeviceConfig
            {
                DeviceId = deviceConfig.Id,
                DeviceName = deviceConfig.Name,
                ProtocolType = deviceConfig.Type,
                Host = deviceConfig.Connection.Host,
                Port = deviceConfig.Connection.Port,
                Timeout = deviceConfig.Connection.Timeout,
                AutoConnect = deviceConfig.Connection.AutoConnect,
                Description = deviceConfig.Description
            };

            // 根据设备类型设置特有属性
            if (deviceConfig is HsmsDeviceXmlConfig hsmsConfig)
            {
                unified.DeviceIdValue = hsmsConfig.SecsSettings.DeviceIdValue;
                unified.SessionIdValue = hsmsConfig.SecsSettings.SessionIdValue;
            }
            else if (deviceConfig is OpcDeviceConfig opcConfig)
            {
                unified.OpcServerName = opcConfig.OpcSettings.ServerName;
                unified.OpcUpdateRate = opcConfig.OpcSettings.UpdateRate;
            }
            else if (deviceConfig is OpcUaDeviceConfig opcUaConfig)
            {
                unified.OpcUaEndpointUrl = opcUaConfig.OpcUaSettings.EndpointUrl;
                unified.OpcUaSecurityPolicy = opcUaConfig.OpcUaSettings.SecurityPolicy;
            }

            return unified;
        }

        /// <summary>
        /// 获取显示字符串
        /// </summary>
        public override string ToString()
        {
            return $"{ProtocolType} - {DeviceName} ({DeviceId})";
        }
    }
}
