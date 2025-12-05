using System;

namespace Common
{
    /// <summary>
    /// 统一日志服务接口
    /// </summary>
    public interface ILoggerService
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
        void LogCritical(string message);
        void LogCritical(string message, Exception exception);
    }
}