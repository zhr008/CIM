using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CIMMonitor.Models;
using CIMMonitor.Services;
using log4net;
using log4net.Config;
using System.Reflection;
using System.Xml.Linq;

namespace HsmsConnectionTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 配置log4net
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            if (File.Exists("log4net.config"))
            {
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            }

            var logger = LogManager.GetLogger("TestProgram");
            logger.Info("=== HSMS连接测试程序启动 ===");

            try
            {
                await TestHsmsConnection();
            }
            catch (Exception ex)
            {
                logger.Error("测试异常", ex);
                throw;
            }

            logger.Info("=== 测试完成 ===");
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static async Task TestHsmsConnection()
        {
            var logger = LogManager.GetLogger("TestProgram");
            var deviceManager = new HsmsDeviceManager();

            // 读取配置
            var configPath = "Config/HsmsConfig.xml";
            if (!File.Exists(configPath))
            {
                logger.Error($"配置文件不存在: {configPath}");
                return;
            }

            logger.Info($"读取配置文件: {configPath}");
            var doc = XDocument.Load(configPath);
            var devices = doc.Root?.Element("Devices")?.Elements("Device")
                .Where(d => d.Attribute("Type")?.Value == "HSMS" && d.Attribute("Enabled")?.Value == "true")
                .ToList();

            if (devices == null || devices.Count == 0)
            {
                logger.Warn("没有找到启用的HSMS设备");
                return;
            }

            logger.Info($"找到 {devices.Count} 个启用的HSMS设备");

            // 连接每个设备
            foreach (var device in devices)
            {
                try
                {
                    var deviceId = device.Attribute("Id")?.Value;
                    var deviceName = device.Attribute("Name")?.Value;
                    var host = device.Element("Connection")?.Element("Host")?.Value;
                    var portStr = device.Element("Connection")?.Element("Port")?.Value;
                    var role = device.Element("SecsSettings")?.Element("Role")?.Value;
                    var deviceIdValueStr = device.Element("SecsSettings")?.Element("DeviceIdValue")?.Value;
                    var sessionIdValueStr = device.Element("SecsSettings")?.Element("SessionIdValue")?.Value;

                    if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(host) ||
                        string.IsNullOrEmpty(portStr) || string.IsNullOrEmpty(deviceIdValueStr) ||
                        string.IsNullOrEmpty(sessionIdValueStr))
                    {
                        logger.Warn($"设备 {deviceId} 配置不完整，跳过");
                        continue;
                    }

                    var port = int.Parse(portStr);
                    var deviceIdValue = byte.Parse(deviceIdValueStr);
                    var sessionIdValue = int.Parse(sessionIdValueStr, System.Globalization.NumberStyles.HexNumber);

                    var isServerMode = !string.IsNullOrEmpty(role) && role.Equals("Server", StringComparison.OrdinalIgnoreCase);
                    var modeStr = isServerMode ? "服务端" : "客户端";

                    logger.Info($"开始连接设备: {deviceId} ({deviceName}) - {modeStr}模式 - {host}:{port}");

                    // 创建设备配置
                    var config = new HsmsDeviceConfig
                    {
                        DeviceId = deviceId,
                        DeviceName = deviceName ?? deviceId,
                        Host = host,
                        Port = port,
                        DeviceIdValue = deviceIdValue,
                        SessionIdValue = sessionIdValue,
                        Role = role,
                        IsConnected = false
                    };

                    deviceManager.AddDevice(config);

                    // 尝试连接
                    var connected = await deviceManager.ConnectDeviceAsync(deviceId);

                    if (connected)
                    {
                        logger.Info($"✅ 设备连接成功: {deviceId}");

                        // 等待一段时间
                        await Task.Delay(5000);

                        // 检查连接状态
                        var connection = GetConnection(deviceManager, deviceId);
                        if (connection != null)
                        {
                            logger.Info($"连接状态: {connection.IsConnected}, 客户端数量: {connection.ClientCount}");
                        }
                    }
                    else
                    {
                        logger.Error($"❌ 设备连接失败: {deviceId}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"连接设备时异常: {ex.Message}", ex);
                }
            }
        }

        static HsmsConnectionService? GetConnection(HsmsDeviceManager manager, string deviceId)
        {
            // 使用反射获取私有字段
            var managerType = typeof(HsmsDeviceManager);
            var connectionsField = managerType.GetField("_deviceConnections",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var connections = connectionsField?.GetValue(manager) as Dictionary<string, HsmsConnectionService>;

            if (connections != null && connections.TryGetValue(deviceId, out var connection))
            {
                return connection;
            }

            return null;
        }
    }
}
