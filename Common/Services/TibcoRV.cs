using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace Common.Services
{
    public class TibcoRV
    {
        #region 字段、属性
        private string _service;                //服务
        private string _network;                //网络
        private string _daemon;                 //守护进程
        private string _messageField;           //消息字段
        private string _listenSubject;          //侦听主题
        private string _targetSubject;          //目标主题
        private bool _isOpen = false;           //是否打开环境
        private bool _isConnected = false;      //是否创建连接
        private bool _isListen = false;         //是否创建侦听
        private string _cmName;                 //My Name
        private Task task = null;

        private TIBCO.Rendezvous.NetTransport _transport;       //传输对象
        private TIBCO.Rendezvous.Listener _listener;            //侦听器对象
        private TIBCO.Rendezvous.Queue _queue;                  //消息队列

        public bool IsListened { get { return _isListen; } set { _isListen = value; } }
        public bool IsOpen { get { return _isConnected; } set { _isConnected = value; } }
        public bool IsConnected { get { return _isOpen; } set { _isOpen = value; } }
        public string Service { get { return _service; } set { _service = value; } }
        public string Network { get { return _network; } set { _network = value; } }
        public string Daemon { get { return _daemon; } set { _daemon = value; } }
        public string MessageField { get => _messageField; set => _messageField = value; }
        public string ListenSubject { get { return _listenSubject; } set { _listenSubject = value; } }
        public string TargetSubject { get { return _targetSubject; } set { _targetSubject = value; } }
        public string CmName { get => _cmName; set => _cmName = value; }
        public TIBCO.Rendezvous.NetTransport Transport { get => _transport; set => _transport = value; }
        public TIBCO.Rendezvous.Listener Listener { get => _listener; set => _listener = value; }
        public TIBCO.Rendezvous.Queue Queue { get => _queue; set => _queue = value; }

        #endregion

        #region 构造函数
        public TibcoRV() { }
        public TibcoRV(string server, string network, string daemon)
        {
            this.Service = server;
            this.Network = network;
            this.Daemon = daemon;
        }
        public TibcoRV(string server, string network, string daemon, string listenSubject, string targetSubject)
        {
            this.Service = server;
            this.Network = network;
            this.Daemon = daemon;
            this.ListenSubject = listenSubject;
            this.TargetSubject = targetSubject;
            this.ConnectedStatusHandler += OnConnectCallBack;
        }
        #endregion


        #region 连接
        /// <summary>
        /// 打开环境
        /// </summary>
        public bool Open()
        {
            try
            {
                TIBCO.Rendezvous.Environment.Open();
                IsOpen = true;
                string msg = $"打开环境成功！";
                ErrorMessageHandler.Invoke(this, msg);
                return IsOpen;
            }
            catch (Exception ex)
            {
                IsOpen = false;
                return IsOpen;
            }
        }
        /// <summary>
        /// 外部连接
        /// </summary>
        public void StartConnect()
        {
            Connected();
        }
        /// <summary>
        /// 内部连接
        /// </summary>
        private bool Connected()
        {
            if (IsOpen)
            {
                TryCreateConnect();
            }
            else
            {
                Open();
                TryCreateConnect();
            }
            return IsConnected;
        }
        /// <summary>
        /// 尝试创建连接，超时时间3s
        /// </summary>
        private async void TryCreateConnect()
        {
            string msg = string.Empty;
            try
            {
                Task createTask = Task.Run(() => CreateConnect());

                Task timeoutTask = Task.Delay(3000);

                Task completedTask = await Task.WhenAny(createTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    msg = $"\r\n连接超时...\r\nDaemon = {Daemon}，Network =  {Network} ，Service = {Service}";
                    ErrorMessageHandler.Invoke(this, msg);
                    IsConnected = false;
                    ConnectedStatusHandler.Invoke(this, false);
                    return;
                }
                IsConnected = true;
                ConnectedStatusHandler.Invoke(this, true);
            }
            catch (Exception ex)
            {
                msg = $"\r\n连接异常...\r\nDaemon = {Daemon}，Network =  {Network} ，Service = {Service}";
                IsConnected = false;
                ErrorMessageHandler.Invoke(this, msg);
                ConnectedStatusHandler.Invoke(this, false);
            }
        }
        /// <summary>
        /// 创建连接的方法
        /// </summary>
        private void CreateConnect()
        {
            string msg = string.Empty;
            try
            {
                msg = $"正在连接...: Daemon = {Daemon}，Network = {Network}，Service = {Service}";
                ErrorMessageHandler.Invoke(this, msg);
                Transport = new NetTransport(Service, Network, Daemon);
                IsConnected = true;
                msg = $"连接成功...";
                ErrorMessageHandler.Invoke(this, msg);
                ConnectedStatusHandler.Invoke(this, true);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                msg = $"连接失败...";
                ErrorMessageHandler.Invoke(this, msg);
                ConnectedStatusHandler.Invoke(this, false);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisConnected()
        {
            try
            {
                // 停止任务
                if (task != null)
                {
                    task = null;
                }
                
                // 移除消息接收事件处理器
                if (this.Listener != null)
                {
                    this.Listener.MessageReceived -= OnMessageReceivedCallBack;
                }
                
                // 销毁监听器
                if (Listener != null) 
                {
                    Listener.Destroy();
                    Listener = null;
                }
                
                // 销毁传输对象
                if (Transport != null) 
                {
                    Transport.Destroy();
                    Transport = null;
                }
                
                // 清理队列
                if (Queue != null)
                {
                    Queue = null;
                }
                
                string msg = $"断开连接... : Daemon = {Daemon} ，Network = {Network}，Service = {Service}";
                
                // 关闭TIBCO环境
                try
                {
                    TIBCO.Rendezvous.Environment.Close();
                }
                catch
                {
                    // 如果环境已经关闭，则忽略异常
                }

                IsListened = false;
                IsConnected = false;
                IsOpen = false;
                
                ErrorMessageHandler?.Invoke(this, msg);
                ConnectedStatusHandler?.Invoke(this, false);
                ListenedStatusHandler?.Invoke(this, false);
                
            }
            catch (Exception ex)
            {
                string msg = $"断开连接时发生异常: {ex.Message}";
                ErrorMessageHandler?.Invoke(this, msg);
            }
        }

        #endregion

        #region 侦听
        /// <summary>
        /// 尝试侦听
        /// </summary>
        private void TryCreateListen()
        {
            try
            {
                if (IsConnected && !IsListened)
                {
                    if (Transport == null)
                    {
                        string msg = $"transport 为空，未连接，请先连接！！！";
                        ErrorMessageHandler.Invoke(this, msg);
                        ListenedStatusHandler.Invoke(this, false);
                        IsListened = false;
                        return;
                    }
                    Queue = new TIBCO.Rendezvous.Queue();
                    Listener = new Listener(Queue, Transport, ListenSubject, null);
                    this.Listener.MessageReceived += OnMessageReceivedCallBack;
                    IsListened = true;
                    ListenedStatusHandler.Invoke(this, true);
                }
            }
            catch (Exception ex)
            {
                IsListened = false;
                string msg = $"侦听异常:{ex.Message}";
                ErrorMessageHandler.Invoke(this, msg);
                ListenedStatusHandler.Invoke(this, false);
            }
        }
        /// <summary>
        /// 内部侦听
        /// </summary>
        private void Listen()
        {
            try
            {
                string msg = $"开始侦听...";
                ErrorMessageHandler.Invoke(this, msg);
                if (!this.IsListened)
                {
                    TryCreateListen();
                    if (this.IsListened)
                    {
                        task = new Task(() =>
                        {
                            while (this.IsListened)
                            {
                                Queue.Dispatch();
                            }
                        });
                        task.Start();
                        msg = $"侦听成功！！！";
                        ErrorMessageHandler.Invoke(this, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                this.IsListened = false;
                string msg = $"侦听异常:{ex.Message}";
                ErrorMessageHandler.Invoke(this, msg);
            }
        }
        #endregion

        #region 发送
        public void Send(string data)
        {
            if (Transport == null || !IsConnected)
            {
                throw new InvalidOperationException("TIBCO transport is not initialized or not connected.");
            }
            
            TIBCO.Rendezvous.Message message = new TIBCO.Rendezvous.Message();
            message.SendSubject = TargetSubject;
            message.AddField(MessageField, data);
            Transport.Send(message);
        }
        
        public void Send(string field, string data)
        {
            if (Transport == null || !IsConnected)
            {
                throw new InvalidOperationException("TIBCO transport is not initialized or not connected.");
            }
            
            TIBCO.Rendezvous.Message message = new TIBCO.Rendezvous.Message();
            message.SendSubject = TargetSubject;
            message.AddField(field, data);
            Transport.Send(message);
        }
        
        /// <summary>
        /// 异步发送XML消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <param name="xmlContent">XML内容</param>
        /// <returns>发送结果</returns>
        public async Task<bool> SendXmlMessageAsync(string subject, string xmlContent)
        {
            try
            {
                if (Transport == null || !IsConnected)
                {
                    string msg = "TIBCO transport is not initialized or not connected.";
                    ErrorMessageHandler?.Invoke(this, msg);
                    return false;
                }
                
                await Task.Run(() =>
                {
                    TIBCO.Rendezvous.Message message = new TIBCO.Rendezvous.Message();
                    message.SendSubject = subject;
                    message.AddField("XMLContent", xmlContent);  // 使用标准字段名
                    Transport.Send(message);
                });
                
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"发送XML消息失败: {ex.Message}";
                ErrorMessageHandler?.Invoke(this, msg);
                return false;
            }
        }
        
        /// <summary>
        /// 发送设备消息
        /// </summary>
        /// <param name="equipmentId">设备ID</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="data">消息数据</param>
        public void SendEquipmentMessage(string equipmentId, string messageType, string data)
        {
            if (Transport == null || !IsConnected)
            {
                throw new InvalidOperationException("TIBCO transport is not initialized or not connected.");
            }
            
            TIBCO.Rendezvous.Message message = new TIBCO.Rendezvous.Message();
            string subject = $"EQUIPMENT.{messageType}.{equipmentId}";
            message.SendSubject = subject;
            message.AddField("Data", data);
            message.AddField("EquipmentId", equipmentId);
            message.AddField("MessageType", messageType);
            message.AddField("Timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            
            Transport.Send(message);
        }


        #endregion

        public void OnListenCallBack(object sender, bool listenStatu)
        {
            IsListened = listenStatu;
        }
        public void OnConnectCallBack(object sender, bool connectStatu)
        {
            IsConnected = connectStatu;
            if (IsConnected)
            {
                Listen();
            }
        }

        /// <summary>
        /// 消息接收
        /// </summary>
        public void OnMessageReceivedCallBack(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            messageReceivedHandler?.Invoke(sender, messageReceivedEventArgs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageReceivedEventArgs"></param>
        public delegate void MessageReceivedHandler(object sender, MessageReceivedEventArgs messageReceivedEventArgs);
        public MessageReceivedHandler messageReceivedHandler;
        public event EventHandler<string> ErrorMessageHandler;
        public event EventHandler<bool> ConnectedStatusHandler;
        public event EventHandler<bool> ListenedStatusHandler;

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    DisConnected();
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