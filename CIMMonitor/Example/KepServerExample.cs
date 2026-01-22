using CIMMonitor.Services;
using CIMMonitor.Models;

namespace CIMMonitor.Example
{
    public class KepServerExample
    {
        public static async Task RunExample()
        {
            Console.WriteLine("=== KepServer EX 标准配置示例 ===\n");

            // 创建监控服务实例
            var monitoringService = new KepServerMonitoringService();

            // 订阅数据变更事件
            monitoringService.DataChanged += OnDataChanged;

            try
            {
                // 初始化服务 - 使用标准KepServer EX XML配置
                var configPath = "Config/KepServerConfig.xml";
                var initialized = await monitoringService.InitializeAsync(configPath);

                if (!initialized)
                {
                    Console.WriteLine("初始化失败！");
                    return;
                }

                Console.WriteLine("服务初始化成功！\n");

                // 显示项目信息
                DisplayProjectInfo(monitoringService);

                // 开始监控
                await monitoringService.StartMonitoringAsync();
                Console.WriteLine("\n开始监控...\n");

                // 显示一些统计信息
                var stats = monitoringService.GetStatistics();
                Console.WriteLine($"项目ID: {stats.ProjectId}");
                Console.WriteLine($"开始时间: {stats.StartTime}");
                Console.WriteLine($"标签变更数: {stats.TotalTagChanges}");
                Console.WriteLine($"错误数: {stats.TotalErrors}\n");

                // 等待一段时间让监控运行
                Console.WriteLine("按任意键停止监控...");
                Console.ReadKey();

                // 停止监控
                await monitoringService.StopMonitoringAsync();
                Console.WriteLine("\n监控已停止。");

                // 显示最终统计信息
                var finalStats = monitoringService.GetStatistics();
                Console.WriteLine($"\n最终统计:");
                Console.WriteLine($"总标签变更数: {finalStats.TotalTagChanges}");
                Console.WriteLine($"总错误数: {finalStats.TotalErrors}");
                Console.WriteLine($"运行时间: {finalStats.Uptime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }
            finally
            {
                monitoringService.Dispose();
            }
        }

        private static void DisplayProjectInfo(IKepServerMonitoringService service)
        {
            Console.WriteLine("--- 项目信息 ---");
            
            var channels = service.GetChannels();
            Console.WriteLine($"通道数量: {channels.Count}");

            foreach (var channel in channels)
            {
                Console.WriteLine($"\n通道: {channel.Name} (驱动: {channel.Driver})");
                
                var devices = service.GetDevices(channel.Name);
                Console.WriteLine($"  设备数量: {devices.Count}");

                foreach (var device in devices)
                {
                    Console.WriteLine($"    设备: {device.Name}");
                    
                    var tagGroups = service.GetTagGroups(channel.Name, device.Name);
                    Console.WriteLine($"      标签组数量: {tagGroups.Count}");

                    foreach (var tagGroup in tagGroups)
                    {
                        Console.WriteLine($"        标签组: {tagGroup.Name}");
                        
                        var tags = service.GetTags(channel.Name, device.Name, tagGroup.Name);
                        Console.WriteLine($"          标签数量: {tags.Count}");

                        foreach (var tag in tags.Take(3)) // 只显示前3个标签
                        {
                            Console.WriteLine($"            - {tag.Name}: {tag.Address} ({tag.DataType}) - {tag.Description}");
                        }

                        if (tags.Count > 3)
                        {
                            Console.WriteLine($"            ... 还有 {tags.Count - 3} 个标签");
                        }
                    }
                }
            }
            Console.WriteLine();
        }

        private static void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            Console.WriteLine($"[数据变更] {e.ChannelName}.{e.DeviceName}.{e.GroupName}.{e.TagName} = {e.Value} @ {e.Timestamp:HH:mm:ss.fff}");
        }
    }
}