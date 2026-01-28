namespace WCFServices.Models
{
    public class OracleConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableLogging { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 30;
    }
}