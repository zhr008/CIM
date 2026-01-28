using System;
using System.Threading.Tasks;
using Common.Services;

namespace WCFServices.Services
{
    /// <summary>
    /// WCF Services端的TIBCO Rendezvous服务
    /// 负责接收来自CIMMonitor的消息，调用WCF服务处理，并发送响应
    /// </summary>
    public class TibrvService : IDisposable
    {
        private readonly TibrvRendezvousService _tibrvService;
        
        // 主题配置
        private const string REQUEST_SUBJECT = "CIM.REQUEST";
        private const string REPLY_SUBJECT_PREFIX = "CIM.REPLY";
        private const string RESPONSE_SUBJECT = "WCF.RESPONSE";

        public TibrvService(string service, string network, string daemon)
        {
            _tibrvService = new TibrvRendezvousService(service, network, daemon, REQUEST_SUBJECT, RESPONSE_SUBJECT);
            
            // 注册事件处理器
            _tibrvService.ErrorMessageHandler += OnErrorMessage;
            _tibrvService.ConnectedStatusHandler += OnConnectedStatusChanged;
            _tibrvService.ListenedStatusHandler += OnListenedStatusChanged;
            _tibrvService.messageReceivedHandler += OnMessageReceived;
        }

        /// <summary>
        /// 初始化并连接到TIBCO Rendezvous
        /// </summary>
        public void Initialize()
        {
            _tibrvService.Open();
            _tibrvService.StartConnect();
        }

        /// <summary>
        /// 消息接收事件处理器
        /// </summary>
        private void OnMessageReceived(object sender, TIBCO.Rendezvous.MessageReceivedEventArgs e)
        {
            try
            {
                string messageContent = _tibrvService.ParseMessageContent(e.Message);
                Console.WriteLine($"收到请求消息: {messageContent}");
                
                // 处理接收到的消息并发送响应
                ProcessAndRespondToMessage(e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理接收消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理消息并发送响应
        /// </summary>
        private async void ProcessAndRespondToMessage(TIBCO.Rendezvous.Message receivedMessage)
        {
            try
            {
                // 提取请求数据和回复主题
                string requestData = ExtractFieldValue(receivedMessage, "Data");
                string replySubject = ExtractFieldValue(receivedMessage, "ReplySubject");
                string requestId = ExtractFieldValue(receivedMessage, "RequestId");

                if (string.IsNullOrEmpty(replySubject))
                {
                    Console.WriteLine("无法找到回复主题，无法发送响应");
                    return;
                }

                // 调用WCF服务处理业务逻辑
                string processedData = await ProcessWithWcfService(requestData);

                // 发送响应
                SendResponse(replySubject, requestId, processedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息并发送响应时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 提取消息字段值
        /// </summary>
        private string ExtractFieldValue(TIBCO.Rendezvous.Message message, string fieldName)
        {
            foreach (TIBCO.Rendezvous.Field field in message.Fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return field.Value?.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// 调用WCF服务处理业务逻辑
        /// </summary>
        private async Task<string> ProcessWithWcfService(string requestData)
        {
            try
            {
                // 这里应该是实际调用WCF服务的逻辑
                // 为了演示目的，我们创建一个服务实例来模拟
                var cimService = new CimServiceImpl();
                
                Console.WriteLine($"正在处理WCF服务请求: {requestData}");
                
                // 模拟一些处理时间
                await Task.Delay(100);
                
                // 调用WCF服务方法
                string responseData = cimService.ProcessCimRequest(requestData);
                
                Console.WriteLine($"WCF服务处理完成: {responseData}");
                
                return responseData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调用WCF服务时发生错误: {ex.Message}");
                
                // 返回错误响应
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// 发送响应消息
        /// </summary>
        private void SendResponse(string replySubject, string requestId, string responseData)
        {
            try
            {
                TIBCO.Rendezvous.Message responseMessage = new TIBCO.Rendezvous.Message();
                responseMessage.SendSubject = replySubject;
                responseMessage.AddField("RequestId", requestId);
                responseMessage.AddField("ResponseData", responseData);
                responseMessage.AddField("Timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                responseMessage.AddField("Status", "Success");

                _tibrvService.Transport.Send(responseMessage);
                
                Console.WriteLine($"已发送响应到主题 '{replySubject}'，请求ID: {requestId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送响应消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送简单响应消息到CIM Monitor
        /// </summary>
        /// <param name="messageData">响应消息数据</param>
        public void SendResponseToCim(string messageData)
        {
            try
            {
                _tibrvService.Send(messageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送响应到CIM Monitor失败: {ex.Message}");
                throw;
            }
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
            _tibrvService.DisConnected();
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
                    _tibrvService?.Dispose();
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