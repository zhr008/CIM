using CIMMonitor.Forms;
using log4net;
using log4net.Config;
using System.Reflection;
using System.Windows.Forms;

namespace CIMMonitor;

public static class Program
{
    private static ILog? _logger;

    [STAThread]
    public static void Main()
    {
        // 配置log4net
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        if (File.Exists("log4net.config"))
        {
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

        _logger = LogManager.GetLogger("CIMInterface");

        try
        {
            _logger.Info("=== 工业自动化系统启动 ===");

            ApplicationConfiguration.Initialize();

            // 配置全局异常处理
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _logger.Info("系统初始化完成，启动主界面...");

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            _logger?.Error("系统启动失败，程序终止", ex);
            throw;
        }
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        _logger?.Error("未处理的UI线程异常", e.Exception);
        MessageBox.Show($"发生异常: {e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.Error("未处理的域异常", e.ExceptionObject as Exception);
    }

    public static ILog? GetLogger()
    {
        return _logger;
    }

    /// <summary>
    /// 根据日志类型获取对应的日志记录器
    /// </summary>
    /// <param name="logType">日志类型：SystemError、SystemOperation、DeviceCommunication</param>
    /// <returns>日志记录器</returns>
    public static ILog GetLogger(string logType)
    {
        return LogManager.GetLogger(logType);
    }
}
