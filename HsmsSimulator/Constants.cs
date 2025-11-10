namespace HsmsSimulator
{
    /// <summary>
    /// 系统常量定义
    /// 用于消除硬编码的魔法数字
    /// </summary>
    public static class Constants
    {
        #region 网络配置

        /// <summary>
        /// 默认缓冲区大小 (8KB)
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = 8192;

        /// <summary>
        /// 默认端口号
        /// </summary>
        public const int DEFAULT_PORT = 5000;

        /// <summary>
        /// 最大消息大小 (10KB)
        /// </summary>
        public const int MAX_MESSAGE_SIZE = 10240;

        #endregion

        #region 超时设置

        /// <summary>
        /// 默认连接超时时间 (毫秒)
        /// </summary>
        public const int DEFAULT_TIMEOUT = 5000;

        /// <summary>
        /// T3超时 - Reply Timeout (秒)
        /// </summary>
        public const int T3_TIMEOUT = 45;

        /// <summary>
        /// T5超时 - Separation Timeout (秒)
        /// </summary>
        public const int T5_TIMEOUT = 10;

        /// <summary>
        /// T6超时 - Control Timeout (秒)
        /// </summary>
        public const int T6_TIMEOUT = 5;

        /// <summary>
        /// T7超时 - Not Selected Timeout (秒)
        /// </summary>
        public const int T7_TIMEOUT = 10;

        /// <summary>
        /// T8超时 - Network Timeout (秒)
        /// </summary>
        public const int T8_TIMEOUT = 5;

        #endregion

        #region UI配置

        /// <summary>
        /// 消息列表最大显示数量
        /// </summary>
        public const int MAX_MESSAGE_COUNT = 1000;

        /// <summary>
        /// 心跳检测间隔 (毫秒)
        /// </summary>
        public const int HEARTBEAT_INTERVAL = 5000;

        /// <summary>
        /// 心跳动画间隔 (毫秒)
        /// </summary>
        public const int HEARTBEAT_ANIMATION_INTERVAL = 1000;

        /// <summary>
        /// 时间戳格式
        /// </summary>
        public const string TIMESTAMP_FORMAT = "HH:mm:ss.fff";

        /// <summary>
        /// 消息预览最大长度
        /// </summary>
        public const int MESSAGE_PREVIEW_LENGTH = 50;

        #endregion

        #region 默认配置

        /// <summary>
        /// 默认设备ID
        /// </summary>
        public const string DEFAULT_DEVICE_ID = "DEVICE001";

        /// <summary>
        /// 默认设备名称
        /// </summary>
        public const string DEFAULT_DEVICE_NAME = "模拟设备1";

        /// <summary>
        /// 默认设备状态
        /// </summary>
        public const string DEFAULT_DEVICE_STATUS = "Offline";

        /// <summary>
        /// 默认设备描述
        /// </summary>
        public const string DEFAULT_DEVICE_DESCRIPTION = "HSMS模拟设备";

        /// <summary>
        /// 默认连接状态
        /// </summary>
        public const string DEFAULT_CONNECTION_STATUS = "N/A";

        #endregion

        #region SECS配置

        /// <summary>
        /// 默认设备ID值
        /// </summary>
        public const byte DEFAULT_DEVICE_ID_VALUE = 1;

        /// <summary>
        /// 默认会话ID值
        /// </summary>
        public const int DEFAULT_SESSION_ID_VALUE = 4660;

        /// <summary>
        /// 默认主机地址
        /// </summary>
        public const string DEFAULT_HOST = "127.0.0.1";

        /// <summary>
        /// 监听所有接口的主机地址
        /// </summary>
        public const string ALL_INTERFACES_HOST = "0.0.0.0";

        #endregion

        #region 错误消息

        /// <summary>
        /// 错误消息：连接失败
        /// </summary>
        public const string ERROR_CONNECTION_FAILED = "连接失败";

        /// <summary>
        /// 错误消息：发送失败
        /// </summary>
        public const string ERROR_SEND_FAILED = "发送失败";

        /// <summary>
        /// 错误消息：服务器未运行
        /// </summary>
        public const string ERROR_SERVER_NOT_RUNNING = "服务器未运行";

        /// <summary>
        /// 错误消息：未连接到服务器
        /// </summary>
        public const string ERROR_NOT_CONNECTED = "未连接到服务器";

        #endregion

        #region 状态消息

        /// <summary>
        /// 状态消息：就绪
        /// </summary>
        public const string STATUS_READY = "就绪";

        /// <summary>
        /// 状态消息：服务器运行中
        /// </summary>
        public const string STATUS_SERVER_RUNNING = "服务器运行中";

        /// <summary>
        /// 状态消息：服务器已停止
        /// </summary>
        public const string STATUS_SERVER_STOPPED = "服务器已停止";

        /// <summary>
        /// 状态消息：已连接
        /// </summary>
        public const string STATUS_CONNECTED = "已连接";

        /// <summary>
        /// 状态消息：已断开连接
        /// </summary>
        public const string STATUS_DISCONNECTED = "已断开连接";

        #endregion
    }
}
