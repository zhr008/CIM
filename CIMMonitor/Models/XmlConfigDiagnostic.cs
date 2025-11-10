using System;
using System.IO;
using System.Xml.Serialization;
using log4net;

namespace CIMMonitor.Models
{
    /// <summary>
    /// XML配置诊断工具
    /// 用于诊断和验证Devices.xml配置文件
    /// </summary>
    public static class XmlConfigDiagnostic
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(XmlConfigDiagnostic));

        /// <summary>
        /// 诊断XML配置文件
        /// </summary>
        /// <returns>诊断结果</returns>
        public static DiagnosticResult DiagnoseConfig()
        {
            var result = new DiagnosticResult();

            try
            {
                result.Step1_CheckFileExists();
                if (!result.FileExists)
                {
                    return result;
                }

                result.Step2_CheckFileReadable();
                if (!result.FileReadable)
                {
                    return result;
                }

                result.Step3_ValidateXmlStructure();
                if (!result.XmlValid)
                {
                    return result;
                }

                result.Step4_LoadConfiguration();
                if (!result.ConfigurationLoaded)
                {
                    return result;
                }

                result.Step5_ValidateDevices();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"诊断过程发生异常: {ex.Message}";
                result.Exception = ex;
                logger.Error("XML配置诊断失败", ex);
            }

            return result;
        }

        /// <summary>
        /// 尝试修复配置文件
        /// </summary>
        /// <returns>是否修复成功</returns>
        public static bool TryFixConfig()
        {
            try
            {
                var configPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Config",
                    "Devices.xml");

                // 如果文件不存在，创建默认配置
                if (!File.Exists(configPath))
                {
                    var defaultConfig = DeviceConfigManager.LoadConfiguration();
                    DeviceConfigManager.SaveConfiguration(defaultConfig);
                    logger.Info($"已创建默认配置文件: {configPath}");
                    return true;
                }

                // 如果文件损坏，尝试重新创建
                try
                {
                    var serializer = new XmlSerializer(typeof(DeviceConfiguration));
                    using (var reader = new StreamReader(configPath))
                    {
                        serializer.Deserialize(reader);
                    }
                    // 如果能正常反序列化，说明文件没问题
                    return true;
                }
                catch
                {
                    // 文件损坏，创建备份
                    var backupPath = $"{configPath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
                    File.Move(configPath, backupPath);
                    logger.Warn($"配置文件损坏，已备份到: {backupPath}");

                    // 创建默认配置
                    var defaultConfig = DeviceConfigManager.LoadConfiguration();
                    DeviceConfigManager.SaveConfiguration(defaultConfig);
                    logger.Info($"已重新创建配置文件: {configPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error("修复配置文件失败", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 诊断结果
    /// </summary>
    public class DiagnosticResult
    {
        public bool Success { get; set; }
        public bool FileExists { get; set; }
        public bool FileReadable { get; set; }
        public bool XmlValid { get; set; }
        public bool ConfigurationLoaded { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }

        public int DeviceCount { get; set; }
        public int HsmsDeviceCount { get; set; }
        public int OpcDeviceCount { get; set; }
        public int OpcUaDeviceCount { get; set; }

        public string GetReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== XML配置文件诊断报告 ===");
            report.AppendLine($"文件存在: {(FileExists ? "✅ 是" : "❌ 否")}");
            report.AppendLine($"文件可读: {(FileReadable ? "✅ 是" : "❌ 否")}");
            report.AppendLine($"XML有效: {(XmlValid ? "✅ 是" : "❌ 否")}");
            report.AppendLine($"配置加载: {(ConfigurationLoaded ? "✅ 是" : "❌ 否")}");
            report.AppendLine();

            if (DeviceCount > 0)
            {
                report.AppendLine("设备统计:");
                report.AppendLine($"  总设备数: {DeviceCount}");
                report.AppendLine($"  HSMS设备: {HsmsDeviceCount}");
                report.AppendLine($"  OPC设备: {OpcDeviceCount}");
                report.AppendLine($"  OPC-UA设备: {OpcUaDeviceCount}");
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                report.AppendLine();
                report.AppendLine($"错误信息: {ErrorMessage}");
            }

            if (Success)
            {
                report.AppendLine();
                report.AppendLine("✅ 配置文件状态正常");
            }

            return report.ToString();
        }

        // 步骤方法
        public void Step1_CheckFileExists()
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config",
                "Devices.xml");

            FileExists = File.Exists(configPath);
            if (!FileExists)
            {
                ErrorMessage = "配置文件不存在";
            }
        }

        public void Step2_CheckFileReadable()
        {
            if (!FileExists) return;

            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config",
                "Devices.xml");

            try
            {
                using (var stream = File.OpenRead(configPath))
                {
                    FileReadable = stream.Length > 0;
                }
            }
            catch (Exception ex)
            {
                FileReadable = false;
                ErrorMessage = $"文件无法读取: {ex.Message}";
            }
        }

        public void Step3_ValidateXmlStructure()
        {
            if (!FileReadable) return;

            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config",
                "Devices.xml");

            try
            {
                var serializer = new XmlSerializer(typeof(DeviceConfiguration));
                using (var reader = new StreamReader(configPath))
                {
                    var obj = serializer.Deserialize(reader);
                    XmlValid = obj != null;
                }
            }
            catch (Exception ex)
            {
                XmlValid = false;
                ErrorMessage = $"XML结构无效: {ex.Message}";
            }
        }

        public void Step4_LoadConfiguration()
        {
            if (!XmlValid) return;

            try
            {
                var config = DeviceConfigManager.LoadConfiguration();
                ConfigurationLoaded = true;
                DeviceCount = config.Devices.Count;
            }
            catch (Exception ex)
            {
                ConfigurationLoaded = false;
                ErrorMessage = $"配置加载失败: {ex.Message}";
            }
        }

        public void Step5_ValidateDevices()
        {
            if (!ConfigurationLoaded) return;

            try
            {
                var config = DeviceConfigManager.LoadConfiguration();
                HsmsDeviceCount = config.Devices.Count(d => d.Type == "HSMS");
                OpcDeviceCount = config.Devices.Count(d => d.Type == "OPC");
                OpcUaDeviceCount = config.Devices.Count(d => d.Type == "OPC_UA");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"设备验证失败: {ex.Message}";
            }
        }
    }
}
