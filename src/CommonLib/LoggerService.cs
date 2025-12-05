using System;
using log4net;
using log4net.Core;

namespace Common
{
    /// <summary>
    /// 统一日志服务实现
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private readonly ILog _logger;

        public LoggerService(ILog logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public void LogInfo(string message)
        {
            _logger.Info(message);
        }

        public void LogWarning(string message)
        {
            _logger.Warn(message);
        }

        public void LogError(string message)
        {
            _logger.Error(message);
        }

        public void LogError(string message, Exception exception)
        {
            _logger.Error(message, exception);
        }

        public void LogCritical(string message)
        {
            _logger.Fatal(message);
        }

        public void LogCritical(string message, Exception exception)
        {
            _logger.Fatal(message, exception);
        }
    }
}