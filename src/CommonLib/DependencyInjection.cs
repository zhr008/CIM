using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using log4net;

namespace Common
{
    /// <summary>
    /// 依赖注入扩展类
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// 添加通用服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            // 注册日志服务
            services.AddSingleton<ILoggerService>(provider =>
            {
                var logger = LogManager.GetLogger("CommonLogger");
                return new LoggerService(logger);
            });

            return services;
        }

        /// <summary>
        /// 添加日志服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="loggerName">日志器名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLoggerService(this IServiceCollection services, string loggerName = "DefaultLogger")
        {
            services.AddSingleton<ILoggerService>(provider =>
            {
                var logger = LogManager.GetLogger(loggerName);
                return new LoggerService(logger);
            });

            return services;
        }
    }
}