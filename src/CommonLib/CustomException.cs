using System;

namespace Common
{
    /// <summary>
    /// 自定义业务异常基类
    /// </summary>
    public class CustomException : Exception
    {
        public string ErrorCode { get; }
        public object? AdditionalData { get; }

        public CustomException(string message) : base(message)
        {
            ErrorCode = "GENERIC_ERROR";
        }

        public CustomException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public CustomException(string errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public CustomException(string errorCode, string message, object? additionalData) : base(message)
        {
            ErrorCode = errorCode;
            AdditionalData = additionalData;
        }

        public CustomException(string errorCode, string message, Exception innerException, object? additionalData) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            AdditionalData = additionalData;
        }
    }

    /// <summary>
    /// 配置异常
    /// </summary>
    public class ConfigurationException : CustomException
    {
        public ConfigurationException(string message) : base("CONFIG_ERROR", message) { }
        public ConfigurationException(string message, Exception innerException) : base("CONFIG_ERROR", message, innerException) { }
    }

    /// <summary>
    /// 数据访问异常
    /// </summary>
    public class DataAccessException : CustomException
    {
        public DataAccessException(string message) : base("DATA_ACCESS_ERROR", message) { }
        public DataAccessException(string message, Exception innerException) : base("DATA_ACCESS_ERROR", message, innerException) { }
    }

    /// <summary>
    /// 通信异常
    /// </summary>
    public class CommunicationException : CustomException
    {
        public CommunicationException(string message) : base("COMM_ERROR", message) { }
        public CommunicationException(string message, Exception innerException) : base("COMM_ERROR", message, innerException) { }
    }

    /// <summary>
    /// 业务逻辑异常
    /// </summary>
    public class BusinessException : CustomException
    {
        public BusinessException(string message) : base("BUSINESS_ERROR", message) { }
        public BusinessException(string message, Exception innerException) : base("BUSINESS_ERROR", message, innerException) { }
    }
}