using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using CIMMonitor.Models;
using CIMMonitor.Services;
using Common.Models;
using Common.Services;
using Common.Utilities;
using WCFServices.Database;

namespace CIMMonitor.Controllers
{
    /// <summary>
    /// CIMMonitor主控制器，负责协调PLC/KepServer、HSMS、TIBCO和WCF服务之间的数据流转
    /// </summary>
    public class CIMMonitorController : IDisposable
    {
        private readonly HsmsDeviceManager _hsmsDeviceManager;
        private readonly TibcoRVService _tibcoService;
        private readonly ConfigManager _configManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // 用于存储KepServer标签值的字典
        private readonly Dictionary<string, object> _tagValues = new Dictionary<string, object>();
        
        // 用于标识当前运行模式
        private bool _isRunning = false;

        public CIMMonitorController()
        {
            _hsmsDeviceManager = new HsmsDeviceManager();
            _configManager = new ConfigManager();
            
            // 初始化TIBCO服务
            var appConfig = _configManager.LoadAppConfig("Config/Config.xml");
            _tibcoService = new TibcoRVService(
                appConfig.TibrvService.Service,
                appConfig.TibrvService.NetworkInterface,
                appConfig.TibrvService.Daemon
            );
            
            _cancellationTokenSource = new CancellationTokenSource();

            // 订阅事件
            _hsmsDeviceManager.DeviceMessageReceived += OnHsmsMessageReceived;
            _hsmsDeviceManager.DeviceStatusChanged += OnDeviceStatusChanged;
        }

        /// <summary>
        /// 启动CIMMonitor控制器
        /// </summary>
        public async Task StartAsync()
        {
            _isRunning = true;

            // 初始化TIBCO服务
            _tibcoService.Initialize();

            // 加载并启动HSMS设备
            await LoadAndStartHsmsDevices();

            // 启动KepServer模拟数据读取任务
            _ = Task.Run(KepServerSimulationLoop);

            Console.WriteLine("CIMMonitor控制器已启动");
        }

        /// <summary>
        /// 停止CIMMonitor控制器
        /// </summary>
        public async Task StopAsync()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();

            // 停止HSMS设备连接
            await _hsmsDeviceManager.DisconnectAllAsync();

            // 停止TIBCO服务
            _tibcoService.Disconnect();

            Console.WriteLine("CIMMonitor控制器已停止");
        }

        /// <summary>
        /// 加载并启动HSMS设备
        /// </summary>
        private async Task LoadAndStartHsmsDevices()
        {
            try
            {
                // 从配置文件加载HSMS设备
                var hsmsConfig = _configManager.LoadHsmsConfig("Config/HsmsConfig.xml");

                foreach (var deviceInfo in hsmsConfig.Devices.DeviceList)
                {
                    var config = new HsmsDeviceConfig
                    {
                        DeviceId = deviceInfo.ID,
                        DeviceName = deviceInfo.Name,
                        Host = deviceInfo.IPAddress,
                        Port = deviceInfo.Port,
                        Role = deviceInfo.Role,
                        DeviceIdValue = 1, // 默认值，实际应从配置中读取
                        SessionIdValue = 0x1234, // 默认值
                        AutoConnect = true,
                        Enabled = true
                    };

                    _hsmsDeviceManager.AddDevice(config);
                    
                    if (config.Enabled && config.AutoConnect)
                    {
                        await _hsmsDeviceManager.ConnectDeviceAsync(config.DeviceId);
                    }
                }

                Console.WriteLine($"加载了 {hsmsConfig.Devices.DeviceList.Count} 个HSMS设备");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载HSMS设备配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// KepServer模拟数据读取循环
        /// </summary>
        private async Task KepServerSimulationLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    // 模拟从KepServer读取PLC数据
                    var plcData = SimulatePlcDataRead();
                    
                    // 处理PLC数据并发送到TIBCO
                    await ProcessPlcData(plcData);

                    // 等待下次读取
                    await Task.Delay(1000, _cancellationTokenSource.Token); // 每秒读取一次
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"KepServer模拟读取循环错误: {ex.Message}");
                    await Task.Delay(1000); // 出错后延迟一下再继续
                }
            }
        }

        /// <summary>
        /// 模拟从PLC通过KepServer读取数据
        /// </summary>
        private Dictionary<string, object> SimulatePlcDataRead()
        {
            var data = new Dictionary<string, object>();

            // 模拟一些常见的PLC标签值
            data["System_Run"] = true;
            data["Motor_Current"] = 15.2f;
            data["Motor_Frequency"] = 45.0f;
            data["Temperature_Zone1"] = 42.5f;
            data["Temperature_Zone2"] = 38.7f;
            data["Temperature_Zone3"] = 40.1f;
            data["Motor_Runtime"] = 12450; // 累计运行时间
            data["Batch_Count"] = 256;

            // 更新内部标签值字典
            foreach (var item in data)
            {
                _tagValues[item.Key] = item.Value;
            }

            return data;
        }

        /// <summary>
        /// 处理PLC数据并发送到TIBCO
        /// </summary>
        private async Task ProcessPlcData(Dictionary<string, object> plcData)
        {
            try
            {
                // 将PLC数据转换为XML格式
                var xmlMessage = CreateXmlMessageFromPlcData(plcData, "PLC_DATA");
                
                // 通过TIBCO发送到WCF服务
                _tibcoService.SendMessageToWcf(xmlMessage);

                Console.WriteLine($"已发送PLC数据到TIBCO: {xmlMessage.Substring(0, Math.Min(100, xmlMessage.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理PLC数据时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从PLC数据创建XML消息
        /// </summary>
        private string CreateXmlMessageFromPlcData(Dictionary<string, object> data, string messageType)
        {
            var message = new XmlMessage
            {
                Header = new MessageHeader
                {
                    MessageId = Guid.NewGuid().ToString(),
                    MessageType = messageType,
                    DeviceId = "PLC_SIMULATOR",
                    Timestamp = DateTime.Now,
                    SessionId = 1
                },
                Body = new MessageBody
                {
                    Data = data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
                }
            };

            return message.ToXml();
        }

        /// <summary>
        /// HSMS消息接收事件处理
        /// </summary>
        private async void OnHsmsMessageReceived(object sender, HsmsDeviceManager.DeviceMessageEventArgs e)
        {
            try
            {
                Console.WriteLine($"收到HSMS消息 - 设备: {e.DeviceId}, 内容: {e.Message}");

                // 将HSMS消息转换为XML格式
                var xmlMessage = CreateXmlMessageFromHsmsData(e.DeviceId, e.Message, e.Timestamp);
                
                // 通过TIBCO发送到WCF服务
                _tibcoService.SendMessageToWcf(xmlMessage);

                Console.WriteLine($"已发送HSMS数据到TIBCO: {xmlMessage.Substring(0, Math.Min(100, xmlMessage.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理HSMS消息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从HSMS数据创建XML消息
        /// </summary>
        private string CreateXmlMessageFromHsmsData(string deviceId, string message, DateTime timestamp)
        {
            var data = new Dictionary<string, string> { { "HSMS_Message", message } };
            
            var xmlMessage = new XmlMessage
            {
                Header = new MessageHeader
                {
                    MessageId = Guid.NewGuid().ToString(),
                    MessageType = "HSMS_MESSAGE",
                    DeviceId = deviceId,
                    Timestamp = timestamp,
                    SessionId = 1
                },
                Body = new MessageBody
                {
                    Data = data
                }
            };

            return xmlMessage.ToXml();
        }

        /// <summary>
        /// 设备状态变化事件处理
        /// </summary>
        private void OnDeviceStatusChanged(object sender, HsmsDeviceManager.DeviceStatusChangedEventArgs e)
        {
            Console.WriteLine($"设备状态变化 - 设备: {e.DeviceId}, 状态: {e.Status}");
        }

        /// <summary>
        /// 通过TIBCO接收来自WCF服务的响应
        /// </summary>
        public void ProcessTibcoResponse(string response)
        {
            try
            {
                Console.WriteLine($"收到TIBCO响应: {response}");

                // 解析响应XML
                var xmlMessage = XmlMessage.FromXml(response);
                if (xmlMessage != null)
                {
                    // 根据消息类型处理响应
                    switch (xmlMessage.Header.MessageType)
                    {
                        case "COMMAND_RESPONSE":
                            HandleCommandResponse(xmlMessage);
                            break;
                        case "DATABASE_RESULT":
                            HandleDatabaseResult(xmlMessage);
                            break;
                        default:
                            Console.WriteLine($"未知消息类型: {xmlMessage.Header.MessageType}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理TIBCO响应时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理命令响应
        /// </summary>
        private void HandleCommandResponse(XmlMessage message)
        {
            Console.WriteLine($"处理命令响应: {message.Header.MessageId}");
            
            // 如果有需要，可以将响应转发给相应的设备
            // 例如，如果是对PLC的写操作响应
        }

        /// <summary>
        /// 处理数据库结果
        /// </summary>
        private void HandleDatabaseResult(XmlMessage message)
        {
            Console.WriteLine($"处理数据库结果: {message.Header.MessageId}");
            
            // 可以更新本地缓存或其他处理
        }

        /// <summary>
        /// 发送命令到PLC通过KepServer
        /// </summary>
        public async Task<bool> SendCommandToPlc(string command, Dictionary<string, object> parameters = null)
        {
            try
            {
                // 创建命令XML消息
                var xmlMessage = new XmlMessage
                {
                    Header = new MessageHeader
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        MessageType = "PLC_COMMAND",
                        DeviceId = "CIM_MONITOR",
                        Timestamp = DateTime.Now,
                        SessionId = 1,
                        Command = command
                    },
                    Body = new MessageBody
                    {
                        Parameters = parameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "") ?? new Dictionary<string, string>()
                    }
                };

                // 通过TIBCO发送到WCF服务，最终到达PLC
                _tibcoService.SendMessageToWcf(xmlMessage.ToXml());

                Console.WriteLine($"已发送PLC命令: {command}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送PLC命令失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送HSMS消息到设备
        /// </summary>
        public async Task<bool> SendHsmsMessageToDevice(string deviceId, string message)
        {
            try
            {
                return await _hsmsDeviceManager.SendMessageAsync(deviceId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送HSMS消息失败: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _hsmsDeviceManager?.Dispose();
            _tibcoService?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}