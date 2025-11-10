using log4net;

namespace CIMMonitor.Services
{
    public static class LoggingService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LoggingService));

        public static void Info(string message)
        {
            _logger.Info(message);
        }

        public static void Warn(string message)
        {
            _logger.Warn(message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Error(message, ex);
            else
                _logger.Error(message);
        }

        public static void Debug(string message)
        {
            _logger.Debug(message);
        }

        public static void Fatal(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Fatal(message, ex);
            else
                _logger.Fatal(message);
        }

        public static void LogDataInteraction(string message)
        {
            _logger.Info($"[DATA] {message}");
        }

        public static void LogXmlMessage(string message)
        {
            _logger.Info($"[XML] {message}");
        }

        public static void LogDeviceOperation(string message)
        {
            _logger.Info($"[DEVICE] {message}");
        }

        public static void LogException(string message, Exception ex)
        {
            _logger.Error($"[ERROR] {message}", ex);
        }
    }
}
