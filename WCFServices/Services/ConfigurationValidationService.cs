using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WCFServices.Services
{
    /// <summary>
    /// 配置验证服务
    /// 确保所有必需的配置项都已正确设置
    /// </summary>
    public interface IConfigurationValidationService
    {
        Task<bool> ValidateConfigurationAsync();
    }

    public class ConfigurationValidationService : IConfigurationValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationValidationService> _logger;

        public ConfigurationValidationService(
            IConfiguration configuration,
            ILogger<ConfigurationValidationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateConfigurationAsync()
        {
            _logger.LogInformation("开始验证系统配置...");

            var isValid = true;
            var errors = new List<string>();

            // 验证Oracle配置
            var oracleSection = _configuration.GetSection("OracleSettings");
            var connectionString = oracleSection["ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("Oracle连接字符串未配置");
                isValid = false;
            }
            else
            {
                // 检查是否包含环境变量占位符
                if (connectionString.Contains("${env:"))
                {
                    errors.Add("Oracle连接字符串包含未解析的环境变量");
                    isValid = false;
                }
                else
                {
                    _logger.LogInformation("✓ Oracle连接字符串已配置");
                }
            }

            // 验证必需的Oracle设置
            var dbUser = Environment.GetEnvironmentVariable("DB_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
            var dbService = Environment.GetEnvironmentVariable("DB_SERVICE");

            if (string.IsNullOrWhiteSpace(dbUser))
            {
                errors.Add("环境变量 DB_USER 未设置");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("✓ DB_USER 环境变量已设置");
            }

            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                errors.Add("环境变量 DB_PASSWORD 未设置");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("✓ DB_PASSWORD 环境变量已设置");
            }

            if (string.IsNullOrWhiteSpace(dbHost))
            {
                errors.Add("环境变量 DB_HOST 未设置");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(dbPort))
            {
                errors.Add("环境变量 DB_PORT 未设置");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(dbService))
            {
                errors.Add("环境变量 DB_SERVICE 未设置");
                isValid = false;
            }

            // 验证TIBCO配置
            var tibcoSection = _configuration.GetSection("TibcoSettings");
            var tibcoUsername = tibcoSection["Username"];
            var tibcoPassword = tibcoSection["Password"];

            if (string.IsNullOrWhiteSpace(tibcoUsername) || tibcoUsername == "${env:TIBCO_USERNAME}")
            {
                errors.Add("TIBCO用户名未配置或使用环境变量但未设置");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("✓ TIBCO用户名已配置");
            }

            if (string.IsNullOrWhiteSpace(tibcoPassword) || tibcoPassword == "${env:TIBCO_PASSWORD}")
            {
                errors.Add("TIBCO密码未配置或使用环境变量但未设置");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("✓ TIBCO密码已配置");
            }

            // 验证WCF设置
            var wcfSection = _configuration.GetSection("WcfSettings");
            var baseAddress = wcfSection["BaseAddress"];

            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                errors.Add("WCF BaseAddress 未配置");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("✓ WCF BaseAddress 已配置");
            }

            // 输出验证结果
            if (isValid)
            {
                _logger.LogInformation("✅ 所有配置验证通过！");
            }
            else
            {
                _logger.LogCritical("❌ 配置验证失败！");
                foreach (var error in errors)
                {
                    _logger.LogCritical($"  - {error}");
                }
                _logger.LogCritical("请设置以下环境变量：");
                _logger.LogCritical("  DB_USER - 数据库用户名");
                _logger.LogCritical("  DB_PASSWORD - 数据库密码");
                _logger.LogCritical("  DB_HOST - 数据库主机地址");
                _logger.LogCritical("  DB_PORT - 数据库端口");
                _logger.LogCritical("  DB_SERVICE - 数据库服务名");
                _logger.LogCritical("  TIBCO_USERNAME - TIBCO用户名");
                _logger.LogCritical("  TIBCO_PASSWORD - TIBCO密码");
            }

            return await Task.FromResult(isValid);
        }
    }
}
