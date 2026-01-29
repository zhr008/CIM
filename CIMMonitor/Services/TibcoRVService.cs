using System;
using System.Threading.Tasks;
using Common.Services;
using TIBCO.Rendezvous;

namespace CIMMonitor.Services
{
    /// <summary>
    /// CIM Monitor端的TIBCO Rendezvous服务
    /// 负责向WCFServices发送消息，并接收响应
    /// </summary>
    public class TibrvService : IDisposable
    {
        private readonly Common.Services.TibcoRV _tibcoRVService;
        
        // 主题配置
        private const string REQUEST_SUBJECT = "CIM.REQUEST";
        private const string REPLY_SUBJECT_PREFIX = "CIM.REPLY";
        private const string LISTEN_SUBJECT = "WCF.RESPONSE"; // 监听来自WCF服务的响应

        public TibrvService(string service, string network, string daemon)
        {
            _tibcoRVService = new Common.Services.TibcoRV(service, network, daemon, LISTEN_SUBJECT, REQUEST_SUBJECT);
            
            // 注册事件处理器
            _tibcoRVService.ErrorMessageHandler += OnErrorMessage;
            _tibcoRVService.ConnectedStatusHandler += OnConnectedStatusChanged;
            _tibcoRVService.ListenedStatusHandler += OnListenedStatusChanged;
            _tibcoRVService.messageReceivedHandler += OnMessageReceived;
        }

        /// <summary>
        /// 初始化并连接到TIBCO Rendezvous
        /// </summary>
        public void Initialize()
        {
            _tibcoRVService.Open();
            _tibcoRVService.StartConnect();
        }

        /// <summary>
        /// 发送简单消息到WCF服务
        /// </summary>
        /// <param name="messageData">消息数据</param>
        public void SendMessageToWcf(string messageData)
        {
            try
            {
                _tibcoRVService.Send(messageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息到WCF服务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 消息接收事件处理器
        /// </summary>
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                TIBCO.Rendezvous.Message message = e.Message;
                string receiveData = message.GetFieldByIndex(0);
                string fieldName = message.GetFieldByIndex(0).Name;
                Console.WriteLine($"send subject = {message.SendSubject}\r\n field name = {fieldName}\r\n{receiveData}");

                // 在这里处理收到的响应消息
                ProcessReceivedMessage(receiveData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理接收消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void ProcessReceivedMessage(string messageContent)
        {
            // 根据业务需求处理消息
            Console.WriteLine($"处理接收到的消息: {messageContent}");
        }

        /// <summary>
        /// 错误消息事件处理器
        /// </summary>
        private void OnErrorMessage(object sender, string errorMessage)
        {
            Console.WriteLine($"TIBCO服务错误: {errorMessage}");
        }

        /// <summary>
        /// 连接状态变化事件处理器
        /// </summary>
        private void OnConnectedStatusChanged(object sender, bool isConnected)
        {
            Console.WriteLine($"TIBCO连接状态变化: {(isConnected ? "已连接" : "已断开")}");
        }

        /// <summary>
        /// 侦听状态变化事件处理器
        /// </summary>
        private void OnListenedStatusChanged(object sender, bool isListened)
        {
            Console.WriteLine($"TIBCO侦听状态变化: {(isListened ? "正在侦听" : "停止侦听")}");
        }

        /// <summary>
        /// 断开连接并释放资源
        /// </summary>
        public void Disconnect()
        {
            _tibcoRVService.DisConnected();
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _tibcoRVService?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}