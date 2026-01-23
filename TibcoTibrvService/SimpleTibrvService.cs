using System;
using System.Threading.Tasks;
using TibcoTibrvService.Services;
using log4net;
using log4net.Config;
using System.Reflection;

namespace TibcoTibrvService;

/// <summary>
/// TIBCO集成服务 - 完整实现CIMMonitor到WCF的数据流转
/// </summary>
public class SimpleTibrvService : IDisposable
{
    private static readonly ILog log = LogManager.GetLogger(typeof(SimpleTibrvService));
    private TibcoIntegrationService? _integrationService;

    public async Task StartAsync()
    {
        // 配置log4net
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        log.Info("=== TIBCO集成服务启动 ===");
        Console.WriteLine("=== TIBCO集成服务启动 ===");

        try
        {
            // 初始化集成服务
            _integrationService = new TibcoIntegrationService();
            
            log.Info("TIBCO集成服务已启动，准备接收来自CIMMonitor的消息");
            Console.WriteLine("TIBCO集成服务已启动，监听来自CIMMonitor的消息");

            // 模拟持续运行
            await RunServiceAsync();
        }
        catch (Exception ex)
        {
            log.Error("TIBCO集成服务启动失败", ex);
            Console.WriteLine($"错误: {ex.Message}");
            throw;
        }
    }

    private async Task RunServiceAsync()
    {
        // 模拟服务持续运行
        while (true)
        {
            // 这里可以监听特定的消息队列或主题
            await Task.Delay(5000); // 每5秒检查一次
            
            // 输出心跳信息
            log.Debug("TIBCO集成服务运行中...");
        }
    }

    public void Dispose()
    {
        _integrationService?.Dispose();
        log.Info("TIBCO集成服务已停止");
    }
}
