using System;
using System.IO;
using System.Xml.Serialization;
using Common.Models;

namespace Common.Utilities
{
    public class ConfigManager
    {
        public static T LoadConfig<T>(string filePath) where T : class, new()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }

            try
            {
                using var reader = new StreamReader(filePath);
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {filePath}", ex);
            }
        }

        public static void SaveConfig<T>(T config, string filePath) where T : class
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = new StreamWriter(filePath);
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration to {filePath}", ex);
            }
        }

        public static HsmsConfig LoadHsmsConfig(string filePath = "Config/HsmsConfig.xml")
        {
            return LoadConfig<HsmsConfig>(filePath);
        }

        public static KepServerConfig LoadKepServerConfig(string filePath = "Config/KepServerConfig.xml")
        {
            return LoadConfig<KepServerConfig>(filePath);
        }

        public static AppConfig LoadAppConfig(string filePath = "Config/Config.xml")
        {
            return LoadConfig<AppConfig>(filePath);
        }
    }
}