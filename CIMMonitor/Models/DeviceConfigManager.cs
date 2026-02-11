using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using log4net;

namespace CIMMonitor.Models
{
    /// <summary>
    /// 协议类型枚举
    /// </summary>
    public enum ProtocolType
    {
        HSMS,
        OPC,
        OPC_DA,
        OPC_UA,
        Unknown
    }

    /// <summary>
    /// 统一设备配置管理器
    /// 支持XML配置文件，根据协议类型加载设备
    /// </summary>
    public static class DeviceConfigManager
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DeviceConfigManager));
        private static readonly string ConfigFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Config",
            "HsmsConfig.xml");

        private static DeviceConfiguration? _configuration;
        private static readonly object _lock = new();

        /// <summary>
        /// 加载设备配置
        /// </summary>
        /// <returns>设备配置对象</returns>
        public static DeviceConfiguration LoadConfiguration()
        {
            lock (_lock)
            {
                if (_configuration != null)
                {
                    return _configuration;
                }

                try
                {
                    if (!File.Exists(ConfigFilePath))
                    {
                        logger.Warn($"配置文件不存在: {ConfigFilePath}");
                        _configuration = CreateDefaultConfiguration();
                        SaveConfiguration(_configuration);
                        return _configuration;
                    }

                    var serializer = new XmlSerializer(typeof(DeviceConfiguration));
                    using (var reader = new StreamReader(ConfigFilePath))
                    {
                        _configuration = (DeviceConfiguration)serializer.Deserialize(reader)!;
                    }

                    logger.Info($"成功加载设备配置，共 {_configuration.Devices.Count} 个设备");
                    return _configuration;
                }
                catch (Exception ex)
                {
                    logger.Error("加载设备配置失败", ex);
                    throw new Exception($"加载设备配置失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        /// <returns>设备配置对象</returns>
        public static DeviceConfiguration ReloadConfiguration()
        {
            lock (_lock)
            {
                _configuration = null;
                return LoadConfiguration();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        public static void SaveConfiguration(DeviceConfiguration configuration)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var serializer = new XmlSerializer(typeof(DeviceConfiguration));
                using (var writer = new StreamWriter(ConfigFilePath))
                {
                    serializer.Serialize(writer, configuration);
                }

                lock (_lock)
                {
                    _configuration = configuration;
                }

                logger.Info("设备配置保存成功");
            }
            catch (Exception ex)
            {
                logger.Error("保存设备配置失败", ex);
                throw new Exception($"保存设备配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns>设备列表</returns>
        public static List<DeviceConfig> GetAllDevices()
        {
            return LoadConfiguration().Devices;
        }

        /// <summary>
        /// 获取指定协议类型的所有设备
        /// </summary>
        /// <param name="protocolType">协议类型</param>
        /// <returns>设备列表</returns>
        public static List<DeviceConfig> GetDevicesByProtocol(ProtocolType protocolType)
        {
            var devices = GetAllDevices();
            return devices.Where(d => GetProtocolType(d.Type) == protocolType).ToList();
        }

        /// <summary>
        /// 获取指定协议类型的所有设备（按类型返回）
        /// </summary>
        /// <typeparam name="T">设备配置类型</typeparam>
        /// <returns>设备列表</returns>
        public static List<T> GetDevicesByProtocol<T>() where T : DeviceConfig
        {
            var devices = GetAllDevices();
            return devices.OfType<T>().ToList();
        }

        /// <summary>
        /// 获取HSMS设备
        /// </summary>
        /// <returns>HSMS设备列表</returns>
        public static List<HsmsDeviceXmlConfig> GetHsmsDevices()
        {
            var devices = GetAllDevices();
            return devices
                .Where(d => d.Type == "HSMS")
                .Cast<HsmsDeviceXmlConfig>()
                .ToList();
        }

        /// <summary>
        /// 获取OPC设备
        /// </summary>
        /// <returns>OPC设备列表</returns>
        public static List<OpcDeviceConfig> GetOpcDevices()
        {
            var devices = GetAllDevices();
            return devices
                .Where(d => d.Type == "OPC")
                .Cast<OpcDeviceConfig>()
                .ToList();
        }

        /// <summary>
        /// 获取OPC UA设备
        /// </summary>
        /// <returns>OPC UA设备列表</returns>
        public static List<OpcUaDeviceConfig> GetOpcUaDevices()
        {
            var devices = GetAllDevices();
            return devices
                .Where(d => d.Type == "OPC_UA")
                .Cast<OpcUaDeviceConfig>()
                .ToList();
        }

        /// <summary>
        /// 根据设备ID获取设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备配置，如果不存在返回null</returns>
        public static DeviceConfig? GetDeviceById(string deviceId)
        {
            return GetAllDevices().FirstOrDefault(d => d.Id == deviceId);
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device">设备配置</param>
        public static void AddDevice(DeviceConfig device)
        {
            var configuration = LoadConfiguration();

            // 检查ID是否已存在
            if (configuration.Devices.Any(d => d.Id == device.Id))
            {
                throw new Exception($"设备ID '{device.Id}' 已存在");
            }

            device.LastUpdated = DateTime.Now;
            configuration.Devices.Add(device);
            SaveConfiguration(configuration);
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="updatedDevice">更新后的设备配置</param>
        public static void UpdateDevice(string deviceId, DeviceConfig updatedDevice)
        {
            var configuration = LoadConfiguration();
            var device = configuration.Devices.FirstOrDefault(d => d.Id == deviceId);

            if (device == null)
            {
                throw new Exception($"未找到设备ID '{deviceId}'");
            }

            updatedDevice.LastUpdated = DateTime.Now;
            int index = configuration.Devices.IndexOf(device);
            configuration.Devices[index] = updatedDevice;
            SaveConfiguration(configuration);
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public static void DeleteDevice(string deviceId)
        {
            var configuration = LoadConfiguration();
            var device = configuration.Devices.FirstOrDefault(d => d.Id == deviceId);

            if (device == null)
            {
                throw new Exception($"未找到设备ID '{deviceId}'");
            }

            configuration.Devices.Remove(device);
            SaveConfiguration(configuration);
        }

        /// <summary>
        /// 获取协议类型枚举
        /// </summary>
        /// <param name="typeString">协议类型字符串</param>
        /// <returns>协议类型枚举</returns>
        public static ProtocolType GetProtocolType(string typeString)
        {
            return typeString?.ToUpper() switch
            {
                "HSMS" => ProtocolType.HSMS,
                "OPC" => ProtocolType.OPC,
                "OPC_UA" or "OPCUA" => ProtocolType.OPC_UA,
                _ => ProtocolType.Unknown
            };
        }

        /// <summary>
        /// 获取协议类型的显示名称
        /// </summary>
        /// <param name="protocolType">协议类型</param>
        /// <returns>显示名称</returns>
        public static string GetProtocolDisplayName(ProtocolType protocolType)
        {
            return protocolType switch
            {
                ProtocolType.HSMS => "HSMS",
                ProtocolType.OPC => "OPC",
                ProtocolType.OPC_UA => "OPC-UA",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private static DeviceConfiguration CreateDefaultConfiguration()
        {
            return new DeviceConfiguration
            {
                GlobalSettings = new GlobalSettings
                {
                    DefaultTimeout = 5000,
                    RetryAttempts = 3,
                    HeartbeatInterval = 30000,
                    MaxMessageSize = 10240
                },
                Devices = new List<DeviceConfig>
                {
                    new HsmsDeviceXmlConfig
                    {
                        Type = "HSMS",
                        Id = "EQP001",
                        Name = "测试设备001",
                        Connection = new ConnectionSettings
                        {
                            Host = "127.0.0.1",
                            Port = 5000,
                            Timeout = 5000,
                            AutoConnect = true
                        },
                        SecsSettings = new SecsSettings
                        {
                            DeviceIdValue = 1,
                            SessionIdValue = 4660
                        },
                        Description = "默认HSMS设备配置",
                        LastUpdated = DateTime.Now
                    }
                }
            };
        }

        /// <summary>
        /// 获取设备统计信息
        /// </summary>
        /// <returns>设备统计信息</returns>
        public static DeviceStatistics GetStatistics()
        {
            var devices = GetAllDevices();
            return new DeviceStatistics
            {
                TotalDevices = devices.Count,
                HsmsDeviceCount = devices.Count(d => d.Type == "HSMS"),
                OpcDeviceCount = devices.Count(d => d.Type == "OPC"),
                OpcUaDeviceCount = devices.Count(d => d.Type == "OPC_UA"),
                AutoConnectCount = devices.Count(d => d.Connection.AutoConnect)
            };
        }
    }

    /// <summary>
    /// 设备统计信息
    /// </summary>
    public class DeviceStatistics
    {
        public int TotalDevices { get; set; }
        public int HsmsDeviceCount { get; set; }
        public int OpcDeviceCount { get; set; }
        public int OpcUaDeviceCount { get; set; }
        public int AutoConnectCount { get; set; }
    }
}
