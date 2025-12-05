using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HsmsSimulator.Server;
using HsmsSimulator.Client;
using HsmsSimulator.Models;

namespace HsmsSimulator
{
    public partial class MainForm : Form
    {
        private HsmsServer? _server;
        private HsmsClient? _client;
        private bool _isServerMode = true;
        private ListViewItem? _selectedMessage;
        private bool _filterAutoMessages = true; // 是否过滤自动生成的消息
        private System.Timers.Timer? _heartbeatTimer; // 心跳检测定时器
        private DateTime _lastHeartbeatTime = DateTime.Now; // 最后心跳时间
        private int _cycleStep = 0; // 心跳动画循环步数 (0-3)
        private System.Timers.Timer? _cycleTimer; // 心跳动画定时器

        // 超时设置 - 使用常量定义
        private int _t3Timeout = Constants.T3_TIMEOUT; // Reply Timeout (秒)
        private int _t5Timeout = Constants.T5_TIMEOUT; // Separation Timeout (秒)
        private int _t6Timeout = Constants.T6_TIMEOUT; // Control Timeout (秒)
        private int _t7Timeout = Constants.T7_TIMEOUT; // Not Selected Timeout (秒)
        private int _t8Timeout = Constants.T8_TIMEOUT; // Network Timeout (秒)

        // 心跳图标
        private Bitmap? _greenIndicator;
        private Bitmap? _redIndicator;
        private Bitmap? _grayIndicator; // 默认状态
        private ToolStripStatusLabel? _heartbeatIndicator;

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
        }

        /// <summary>
        /// 初始化窗体
        /// </summary>
        private void InitializeForm()
        {
            this.Text = "HSMS Simulator - 设备通信模拟器";
            this.Size = new Size(1150, 920);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;

            // 设置固定大小模式，防止调整窗体大小
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false; // 禁用最大化按钮
            this.MinimizeBox = true;  // 保留最小化按钮
            this.WindowState = FormWindowState.Normal;
            this.AutoScaleMode = AutoScaleMode.None; // 禁用DPI缩放

            // 创建红绿灰圆形状态图标
            CreateStatusIndicators();

            // 启动心跳检测定时器
            _heartbeatTimer = new System.Timers.Timer(Constants.HEARTBEAT_INTERVAL);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Start();

            // 启动心跳动画定时器
            _cycleTimer = new System.Timers.Timer(Constants.HEARTBEAT_ANIMATION_INTERVAL);
            _cycleTimer.Elapsed += CycleTimer_Elapsed;
            _cycleTimer.AutoReset = true;
            _cycleTimer.Start();

            // 绑定事件
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        /// <summary>
        /// 创建红绿灰圆形状态指示器
        /// </summary>
        private void CreateStatusIndicators()
        {
            const int size = 16;
            _greenIndicator = new Bitmap(size, size);
            _redIndicator = new Bitmap(size, size);
            _grayIndicator = new Bitmap(size, size);

            // 绘制绿色圆形 (在线)
            using (var g = Graphics.FromImage(_greenIndicator))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.Green))
                {
                    g.FillEllipse(brush, 2, 2, size - 4, size - 4);
                }
            }

            // 绘制红色圆形 (离线)
            using (var g = Graphics.FromImage(_redIndicator))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(brush, 2, 2, size - 4, size - 4);
                }
            }

            // 绘制灰色圆形 (默认/未初始化)
            using (var g = Graphics.FromImage(_grayIndicator))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.Gray))
                {
                    g.FillEllipse(brush, 2, 2, size - 4, size - 4);
                }
            }

            // 获取heartbeatIndicator引用
            _heartbeatIndicator = heartbeatIndicator;

            // 初始化显示灰色（默认状态）
            _heartbeatIndicator.Image = _grayIndicator;
        }

        /// <summary>
        /// 初始化设备列表
        /// </summary>
        private void InitializeDevices()
        {
            if (devicesListView.Items.Count == 0)
            {
                devicesListView.View = View.Details;
                devicesListView.BackColor = Color.White;
                devicesListView.ForeColor = Color.Black;
                devicesListView.FullRowSelect = true;
                devicesListView.GridLines = true;

                devicesListView.Columns.Add("设备ID", 100);
                devicesListView.Columns.Add("设备名称", 120);
                devicesListView.Columns.Add("状态", 80);
                devicesListView.Columns.Add("描述", 200);

                // 添加3个模拟设备 (仅用于显示，客户端模式下禁用)
                AddDevice("DEVICE001", "模拟设备1", Constants.DEFAULT_DEVICE_STATUS, "HSMS模拟设备");
                AddDevice("DEVICE002", "模拟设备2", Constants.DEFAULT_DEVICE_STATUS, "HSMS模拟设备");
                AddDevice("DEVICE003", "模拟设备3", Constants.DEFAULT_DEVICE_STATUS, "HSMS模拟设备");

                // 如果是客户端模式，显示禁用状态
                if (!_isServerMode)
                {
                    for (int i = 0; i < devicesListView.Items.Count; i++)
                    {
                        devicesListView.Items[i].ForeColor = Color.Gray;
                        devicesListView.Items[i].SubItems[2].Text = "N/A";
                        devicesListView.Items[i].SubItems[3].Text = "客户端模式不可用";
                    }
                }
            }
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        private void AddDevice(string deviceId, string name, string status, string description)
        {
            var item = new ListViewItem(deviceId);
            item.SubItems.Add(name);
            item.SubItems.Add(status);
            item.SubItems.Add(description);

            // 根据状态设置颜色
            if (status == "Online")
            {
                item.ForeColor = Color.DarkGreen;
            }
            else
            {
                item.ForeColor = Color.DarkGray;
            }

            devicesListView.Items.Add(item);
        }

        /// <summary>
        /// 初始化消息列表
        /// </summary>
        private void InitializeMessageList()
        {
            if (messagesListView.Items.Count == 0)
            {
                messagesListView.View = View.Details;
                messagesListView.BackColor = Color.White;
                messagesListView.ForeColor = Color.Black;
                messagesListView.FullRowSelect = true;
                messagesListView.GridLines = true;
                messagesListView.MultiSelect = false;

                messagesListView.Columns.Add("时间", 100);
                messagesListView.Columns.Add("方向", 80);
                messagesListView.Columns.Add("消息类型", 100);
                messagesListView.Columns.Add("内容", 400);

                messagesListView.SelectedIndexChanged += MessagesListView_SelectedIndexChanged;
            }
        }

        /// <summary>
        /// 添加消息到列表
        /// </summary>
        private void AddMessage(string timestamp, string direction, string messageType, string content)
        {
            var item = new ListViewItem(timestamp);
            item.SubItems.Add(direction);
            item.SubItems.Add(messageType);

            // 内容预览
            string preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            item.SubItems.Add(preview);

            // 根据方向设置颜色
            if (direction == "发送")
            {
                item.ForeColor = Color.LightBlue;
            }
            else
            {
                item.ForeColor = Color.DarkGreen;
            }

            messagesListView.Items.Insert(0, item);

            // 限制消息数量
            if (messagesListView.Items.Count > Constants.MAX_MESSAGE_COUNT)
            {
                messagesListView.Items.RemoveAt(messagesListView.Items.Count - 1);
            }

            // 更新统计
            UpdateStatistics();
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            statusLabel.Text = Constants.STATUS_READY;
            messageCountLabel.Text = messagesListView.Items.Count.ToString();

            // 获取实际连接数（根据模式不同）
            int connectionCount = _isServerMode
                ? (_server?.ConnectionCount ?? 0)
                : (_client?.IsConnected == true ? 1 : 0);
            connectionCountLabel.Text = connectionCount.ToString();

            deviceCountLabel.Text = devicesListView.Items.Count.ToString();
        }

        #region 事件处理

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 初始化工具栏
            InitializeToolStrip();

            // 初始化设备列表
            InitializeDevices();

            // 初始化消息列表
            InitializeMessageList();

            // 初始化快捷命令树
            InitializeQuickCommands();

            // 初始化TreeView右键菜单
            InitializeTreeViewContextMenu();

            // 初始化按钮状态
            UpdateConnectButtonState();

            UpdateStatistics();
        }

        /// <summary>
        /// 初始化工具栏
        /// </summary>
        private void InitializeToolStrip()
        {
            toolStrip.Items.Clear();

            // 添加工具栏项目
            toolStrip.Items.Add(modeLabel);
            toolStrip.Items.Add(modeComboBox);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(connectButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(portLabel);
            toolStrip.Items.Add(portTextBox);
            toolStrip.Items.Add(hostLabel);
            toolStrip.Items.Add(hostTextBox);
            toolStrip.Items.Add(nameLabel);
            toolStrip.Items.Add(nameTextBox);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(settingsButton);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 清理资源
            _heartbeatTimer?.Stop();
            _heartbeatTimer?.Dispose();
            _cycleTimer?.Stop();
            _cycleTimer?.Dispose();
            _greenIndicator?.Dispose();
            _redIndicator?.Dispose();
            _grayIndicator?.Dispose();
            _server?.StopAsync().Wait(TimeSpan.FromSeconds(2));
            _client?.DisconnectAsync().Wait(TimeSpan.FromSeconds(2));
        }

        private void MessagesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (messagesListView.SelectedItems.Count > 0)
            {
                _selectedMessage = messagesListView.SelectedItems[0];
                ShowMessageDetails();
            }
        }

        #endregion

        #region 连接管理

        private async void connectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_isServerMode)
                {
                    if (_server == null || !_server.IsRunning)
                    {
                        await StartServer();
                    }
                    else
                    {
                        await StopServer();
                    }
                }
                else
                {
                    if (_client == null || !_client.IsConnected)
                    {
                        await StartClient();
                    }
                    else
                    {
                        await StopClient();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StartServer()
        {
            var port = int.Parse(portTextBox.Text);
            _server = new HsmsServer(port);

            // 绑定事件
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.MessageReceived += OnMessageReceived;
            _server.Error += OnServerError;

            await _server.StartAsync();

            // 更新设备状态
            for (int i = 0; i < devicesListView.Items.Count; i++)
            {
                devicesListView.Items[i].SubItems[2].Text = "Online";
                devicesListView.Items[i].ForeColor = Color.DarkGreen;
            }

            statusLabel.Text = $"服务器运行在端口 {port}";
            UpdateConnectButtonState();
            UpdateStatistics();
            ResetHeartbeat();
        }

        private async Task StopServer()
        {
            if (_server != null)
            {
                await _server.StopAsync();

                // 更新设备状态
                for (int i = 0; i < devicesListView.Items.Count; i++)
                {
                    devicesListView.Items[i].SubItems[2].Text = "Offline";
                    devicesListView.Items[i].ForeColor = Color.DarkGray;
                }

                statusLabel.Text = "服务器已停止";
                UpdateConnectButtonState();
                UpdateStatistics();
                ResetHeartbeat();
            }
        }

        private async Task StartClient()
        {
            var host = hostTextBox.Text;
            var port = int.Parse(portTextBox.Text);
            var name = nameTextBox.Text;

            _client = new HsmsClient
            {
                Host = host,
                Port = port,
                Name = name
            };

            // 绑定事件
            _client.ConnectionChanged += OnConnectionChanged;
            _client.MessageReceived += OnClientMessageReceived;
            _client.Error += OnClientError;

            var connected = await _client.ConnectAsync();

            if (connected)
            {
                statusLabel.Text = $"已连接到 {host}:{port}";
                UpdateConnectButtonState();
                UpdateStatistics();
                ResetHeartbeat();
            }
            else
            {
                throw new Exception("连接服务器失败");
            }
        }

        private async Task StopClient()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync(true); // 手动断开，禁用自动重连

                statusLabel.Text = "已断开连接";
                UpdateConnectButtonState();
                UpdateStatistics();
                ResetHeartbeat();
            }
        }

        /// <summary>
        /// 更新连接按钮状态
        /// </summary>
        private void UpdateConnectButtonState()
        {
            if (_isServerMode)
            {
                if (_server != null && _server.IsRunning)
                {
                    connectButton.Text = "停止服务";
                }
                else
                {
                    connectButton.Text = "启动服务";
                }
            }
            else
            {
                if (_client != null && _client.IsConnected)
                {
                    connectButton.Text = "断开连接";
                }
                else
                {
                    connectButton.Text = "连接";
                }
            }
        }

        #endregion

        #region 事件回调

        private void OnClientConnected(object? sender, string clientId)
        {
            this.Invoke(new Action(() =>
            {
                statusLabel.Text = $"客户端已连接: {clientId}";
                UpdateStatistics();
            }));
        }

        private void OnClientDisconnected(object? sender, string clientId)
        {
            this.Invoke(new Action(() =>
            {
                statusLabel.Text = $"客户端已断开: {clientId}";
                UpdateStatistics();
            }));
        }

        private void OnMessageReceived(object? sender, HsmsMessage message)
        {
            this.Invoke(new Action(() =>
            {
                AddMessage(
                    message.Timestamp.ToString("HH:mm:ss.fff"),
                    "接收",
                    message.MessageType,
                    message.Content
                );

                // 更新心跳状态
                ResetHeartbeat();
            }));
        }

        private void OnServerError(object? sender, Exception ex)
        {
            this.Invoke(new Action(() =>
            {
                statusLabel.Text = $"错误: {ex.Message}";
            }));
        }

        private void OnConnectionChanged(object? sender, bool connected)
        {
            this.Invoke(new Action(() =>
            {
                if (connected)
                {
                    statusLabel.Text = "已连接";
                    ResetHeartbeat();
                }
                else
                {
                    statusLabel.Text = "已断开连接";
                }
                UpdateStatistics();
            }));
        }

        private void OnClientMessageReceived(object? sender, HsmsMessage message)
        {
            this.Invoke(new Action(() =>
            {
                // 过滤自动生成的EventReport消息（仅在客户端模式下）
                if (_filterAutoMessages && !_isServerMode)
                {
                    // 过滤S6F11 Event Report Send消息（通常是自动生成的）
                    // MessageType可能是"S6F11"或"Event Report Send"，两种情况都要过滤
                    if ((message.Stream == 6 && message.Function == 11) ||
                        message.MessageType == "S6F11" ||
                        message.MessageType == "Event Report Send")
                    {
                        return; // 不显示此消息
                    }
                }

                AddMessage(
                    message.Timestamp.ToString("HH:mm:ss.fff"),
                    "接收",
                    message.MessageType,
                    message.Content
                );

                // 更新心跳状态
                ResetHeartbeat();
            }));
        }

        private void OnClientError(object? sender, Exception ex)
        {
            this.Invoke(new Action(() =>
            {
                statusLabel.Text = $"错误: {ex.Message}";
            }));
        }

        #endregion

        #region 消息发送

        private async void SendMessage(ushort stream, byte function, string content, bool requireResponse)
        {
            try
            {
                if (_isServerMode)
                {
                    // 服务端模式：通过服务器发送消息给所有连接的客户端
                    if (_server != null && _server.IsRunning && _server.ConnectionCount > 0)
                    {
                        var message = new HsmsMessage
                        {
                            Stream = stream,
                            Function = function,
                            Content = content,
                            RequireResponse = requireResponse,
                            DeviceId = 0,
                            SessionId = 0,
                            SenderId = "SERVER",
                            Direction = MessageDirection.Outgoing,
                            Timestamp = DateTime.Now,
                            IsUserInteractive = true, // 标记为用户交互发送的消息
                            SenderRole = SenderRole.Server // 标记为服务端发送
                        };

                        await _server.BroadcastAsync(message);

                        AddMessage(
                            DateTime.Now.ToString("HH:mm:ss.fff"),
                            "发送",
                            $"S{stream}F{function}",
                            content
                        );
                    }
                    else
                    {
                        MessageBox.Show("服务器未运行或没有客户端连接", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // 客户端模式：通过客户端发送消息
                    if (_client != null && _client.IsConnected)
                    {
                        await _client.SendMessageAsync(stream, function, content, requireResponse, true, SenderRole.Client);

                        AddMessage(
                            DateTime.Now.ToString("HH:mm:ss.fff"),
                            "发送",
                            $"S{stream}F{function}",
                            content
                        );
                    }
                    else
                    {
                        MessageBox.Show("未连接到服务器", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送消息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 消息详情显示

        private void ShowMessageDetails()
        {
            if (_selectedMessage == null) return;

            try
            {
                // 清空详情面板
                rawDataTextBox.Clear();
                asciiTextBox.Clear();
                hexTextBox.Clear();
                xmlTextBox.Clear();

                // 从ListViewItem中提取消息信息
                string timestamp = _selectedMessage.SubItems[0].Text;
                string direction = _selectedMessage.SubItems[1].Text;
                string messageType = _selectedMessage.SubItems[2].Text;
                string content = _selectedMessage.SubItems[3].Text;

                // 解析消息类型 (例如 "S1F13")
                string stream = "0";
                string function = "0";
                if (messageType.StartsWith("S") && messageType.Contains("F"))
                {
                    var parts = messageType.Substring(1).Split('F');
                    if (parts.Length == 2)
                    {
                        stream = parts[0];
                        function = parts[1];
                    }
                }

                // 生成格式化的XML
                var xml = new StringBuilder();
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                xml.AppendLine("<HSMSMessage>");
                xml.AppendLine("  <Header>");
                xml.AppendLine($"    <Stream>S{stream}</Stream>");
                xml.AppendLine($"    <Function>F{function}</Function>");
                xml.AppendLine($"    <Timestamp>{timestamp}</Timestamp>");
                xml.AppendLine($"    <Direction>{direction}</Direction>");
                xml.AppendLine($"    <MessageType>{messageType}</MessageType>");
                xml.AppendLine("  </Header>");
                xml.AppendLine("  <Body>");
                xml.AppendLine($"    <Content><![CDATA[{content}]]></Content>");
                xml.AppendLine("  </Body>");
                xml.AppendLine("</HSMSMessage>");

                xmlTextBox.Text = xml.ToString();

                // 原始数据 (简化显示)
                rawDataTextBox.Text = $"Stream: S{stream}, Function: F{function}\n" +
                                      $"Timestamp: {timestamp}\n" +
                                      $"Direction: {direction}\n" +
                                      $"Content: {content}";

                // ASCII格式
                asciiTextBox.Text = content;

                // 十六进制格式 - 优化性能，使用StringBuilder
                var hexBytes = Encoding.UTF8.GetBytes(content);
                var hexBuilder = new StringBuilder();
                hexBuilder.Append("十六进制数据: ");

                for (int i = 0; i < hexBytes.Length; i++)
                {
                    if (i > 0)
                        hexBuilder.Append(' ');
                    hexBuilder.Append(hexBytes[i].ToString("X2"));
                }

                hexTextBox.Text = hexBuilder.ToString();

                // 生成信息结构树
                BuildMessageStructureTree(stream, function, timestamp, direction, messageType, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示消息详情失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 构建消息结构树
        /// </summary>
        private void BuildMessageStructureTree(string stream, string function, string timestamp, string direction, string messageType, string content)
        {
            structureTreeView.Nodes.Clear();

            // 创建Header节点
            var headerNode = new TreeNode("Header (消息头)");
            headerNode.Nodes.Add($"Stream: S{stream}");
            headerNode.Nodes.Add($"Function: F{function}");
            headerNode.Nodes.Add($"Timestamp: {timestamp}");
            headerNode.Nodes.Add($"Direction: {direction}");
            headerNode.Nodes.Add($"MessageType: {messageType}");

            // 创建Body节点
            var bodyNode = new TreeNode("Body (消息体)");
            bodyNode.Nodes.Add($"Content: {content}");

            // 如果内容包含键值对，解析它们
            if (content.Contains("="))
            {
                var dataItemsNode = new TreeNode("Data Items (数据项)");
                var items = content.Split(';');
                foreach (var item in items)
                {
                    if (item.Contains("="))
                    {
                        var parts = item.Split('=');
                        if (parts.Length == 2)
                        {
                            dataItemsNode.Nodes.Add($"{parts[0].Trim()}: {parts[1].Trim()}");
                        }
                    }
                }
                if (dataItemsNode.Nodes.Count > 0)
                {
                    bodyNode.Nodes.Add(dataItemsNode);
                }
            }

            // 添加根节点
            structureTreeView.Nodes.Add("HSMSMessage");
            structureTreeView.Nodes[0].Nodes.Add(headerNode);
            structureTreeView.Nodes[0].Nodes.Add(bodyNode);

            // 展开所有节点
            structureTreeView.ExpandAll();
        }

        #endregion

        #region TreeView快捷命令

        private void quickCommandTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag == null) return;

            var command = e.Node.Tag as string[];
            if (command != null && command.Length == 4)
            {
                ushort stream = ushort.Parse(command[0]);
                byte function = byte.Parse(command[1]);
                string content = command[2];
                bool requireResponse = bool.Parse(command[3]);

                SendMessage(stream, function, content, requireResponse);
            }
        }

        #endregion

        #region 工具方法

        private void modeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _isServerMode = modeComboBox.SelectedIndex == 0;

            if (_isServerMode)
            {
                hostTextBox.Enabled = false;
                nameTextBox.Enabled = false;
                connectButton.Text = "启动服务";
                statusLabel.Text = $"{Constants.STATUS_READY} - 服务端模式";
            }
            else
            {
                hostTextBox.Enabled = true;
                nameTextBox.Enabled = true;
                connectButton.Text = "连接";
                statusLabel.Text = $"{Constants.STATUS_READY} - 客户端模式";
            }
        }

        #endregion

        #region 快捷命令初始化

        /// <summary>
        /// 初始化快捷命令树
        /// </summary>
        private void InitializeQuickCommands()
        {
            quickCommandTreeView.Nodes.Clear();

            // S1系列 - 握手消息
            var s1Node = new TreeNode("S1 - 握手消息");
            s1Node.Nodes.Add(CreateCommandNode("S1F13 - Are You There", "1", "13", "ARE_YOU_THERE", "True"));
            s1Node.Nodes.Add(CreateCommandNode("S1F14 - I Am Here", "1", "14", "I_AM_HERE", "False"));
            s1Node.Nodes.Add(CreateCommandNode("S1F15 - Are You There Request", "1", "15", "ARE_YOU_THERE_REQUEST", "True"));
            s1Node.Nodes.Add(CreateCommandNode("S1F16 - I Am Here Request", "1", "16", "I_AM_HERE_REQUEST", "False"));
            quickCommandTreeView.Nodes.Add(s1Node);

            // S2系列 - 设备状态
            var s2Node = new TreeNode("S2 - 设备状态");
            s2Node.Nodes.Add(CreateCommandNode("S2F33 - Equipment Status Request", "2", "33", "EQUIPMENT_STATUS_REQUEST", "True"));
            s2Node.Nodes.Add(CreateCommandNode("S2F34 - Equipment Status Data", "2", "34", "EQUIPMENT_STATUS_DATA", "False"));
            s2Node.Nodes.Add(CreateCommandNode("S2F35 - Equipment Status Request", "2", "35", "EQUIPMENT_STATUS_REQUEST", "True"));
            s2Node.Nodes.Add(CreateCommandNode("S2F36 - Equipment Status Data", "2", "36", "EQUIPMENT_STATUS_DATA", "False"));
            quickCommandTreeView.Nodes.Add(s2Node);

            // S5系列 - 报警
            var s5Node = new TreeNode("S5 - 报警消息");
            s5Node.Nodes.Add(CreateCommandNode("S5F17 - Alarm Report Send", "5", "17", "ALARM_REPORT_SEND", "False"));
            s5Node.Nodes.Add(CreateCommandNode("S5F18 - Alarm Report Acknowledge", "5", "18", "ALARM_REPORT_ACK", "False"));
            quickCommandTreeView.Nodes.Add(s5Node);

            // S6系列 - 事件报告
            var s6Node = new TreeNode("S6 - 事件报告");
            s6Node.Nodes.Add(CreateCommandNode("S6F11 - Event Report Send", "6", "11", "EVENT_REPORT_SEND", "False"));
            s6Node.Nodes.Add(CreateCommandNode("S6F12 - Event Report Acknowledge", "6", "12", "EVENT_REPORT_ACK", "False"));
            s6Node.Nodes.Add(CreateCommandNode("S6F13 - Event Report Request", "6", "13", "EVENT_REPORT_REQUEST", "True"));
            s6Node.Nodes.Add(CreateCommandNode("S6F14 - Event Report Data", "6", "14", "EVENT_REPORT_DATA", "False"));
            quickCommandTreeView.Nodes.Add(s6Node);

            // S7系列 - 工艺程序
            var s7Node = new TreeNode("S7 - 工艺程序");
            s7Node.Nodes.Add(CreateCommandNode("S7F17 - Process Program Load", "7", "17", "PROCESS_PROGRAM_LOAD", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F18 - Process Program Load Acknowledge", "7", "18", "PROCESS_PROGRAM_LOAD_ACK", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F19 - Process Program", "7", "19", "PROCESS_PROGRAM", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F20 - Process Program Acknowledge", "7", "20", "PROCESS_PROGRAM_ACK", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F21 - Process Program Request", "7", "21", "PROCESS_PROGRAM_REQUEST", "True"));
            s7Node.Nodes.Add(CreateCommandNode("S7F22 - Process Program", "7", "22", "PROCESS_PROGRAM", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F23 - Process Program", "7", "23", "PROCESS_PROGRAM", "False"));
            s7Node.Nodes.Add(CreateCommandNode("S7F24 - Process Program Acknowledge", "7", "24", "PROCESS_PROGRAM_ACK", "False"));
            quickCommandTreeView.Nodes.Add(s7Node);

            // S9系列 - 系统错误
            var s9Node = new TreeNode("S9 - 系统错误");
            s9Node.Nodes.Add(CreateCommandNode("S9F1 - Unrecognized Message Type", "9", "1", "UNRECOGNIZED_MESSAGE_TYPE", "False"));
            s9Node.Nodes.Add(CreateCommandNode("S9F3 - Illegal Data", "9", "3", "ILLEGAL_DATA", "False"));
            s9Node.Nodes.Add(CreateCommandNode("S9F5 - Fragment Reassembly Sequence Error", "9", "5", "FRAGMENT_REASSEMBLY_SEQUENCE_ERROR", "False"));
            s9Node.Nodes.Add(CreateCommandNode("S9F7 - Fragment Length Error", "9", "7", "FRAGMENT_LENGTH_ERROR", "False"));
            quickCommandTreeView.Nodes.Add(s9Node);

            // 展开所有节点
            quickCommandTreeView.ExpandAll();
        }

        /// <summary>
        /// 创建命令节点
        /// </summary>
        private TreeNode CreateCommandNode(string text, string stream, string function, string content, string requireResponse)
        {
            var node = new TreeNode(text);
            node.Tag = new string[] { stream, function, content, requireResponse };
            return node;
        }

        #endregion

        #region 右键菜单和自定义发送

        /// <summary>
        /// 添加右键菜单到TreeView
        /// </summary>
        private void InitializeTreeViewContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var sendMenuItem = new ToolStripMenuItem("发送消息");
            sendMenuItem.Click += (s, e) => SendSelectedTreeNodeMessage();
            contextMenu.Items.Add(sendMenuItem);

            var sendCustomMenuItem = new ToolStripMenuItem("自定义发送...");
            sendCustomMenuItem.Click += (s, e) => ShowCustomSendDialog();
            contextMenu.Items.Add(sendCustomMenuItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var propertiesMenuItem = new ToolStripMenuItem("查看详情");
            propertiesMenuItem.Click += (s, e) => ShowSelectedNodeProperties();
            contextMenu.Items.Add(propertiesMenuItem);

            quickCommandTreeView.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// 发送选中的TreeNode消息
        /// </summary>
        private void SendSelectedTreeNodeMessage()
        {
            if (quickCommandTreeView.SelectedNode != null && quickCommandTreeView.SelectedNode.Tag != null)
            {
                var command = quickCommandTreeView.SelectedNode.Tag as string[];
                if (command != null && command.Length == 4)
                {
                    ushort stream = ushort.Parse(command[0]);
                    byte function = byte.Parse(command[1]);
                    string content = command[2];
                    bool requireResponse = bool.Parse(command[3]);

                    SendMessage(stream, function, content, requireResponse);
                }
            }
        }

        /// <summary>
        /// 显示自定义发送对话框
        /// </summary>
        private void ShowCustomSendDialog()
        {
            var dialog = new CustomSendDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SendMessage(dialog.Stream, dialog.Function, dialog.Content, dialog.RequireResponse);
            }
        }

        /// <summary>
        /// 显示选中节点属性
        /// </summary>
        private void ShowSelectedNodeProperties()
        {
            if (quickCommandTreeView.SelectedNode != null)
            {
                var node = quickCommandTreeView.SelectedNode;
                var message = $"节点: {node.Text}\n";
                if (node.Tag != null)
                {
                    var command = node.Tag as string[];
                    if (command.Length >= 4)
                    {
                        message += $"Stream: S{command[0]}\n";
                        message += $"Function: F{command[1]}\n";
                        message += $"Content: {command[2]}\n";
                        message += $"Require Response: {command[3]}\n";
                    }
                }
                MessageBox.Show(message, "消息详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 自定义发送按钮点击事件
        /// </summary>
        private void customSendButton_Click(object sender, EventArgs e)
        {
            ShowCustomSendDialog();
        }

        /// <summary>
        /// 过滤自动消息复选框状态改变事件
        /// </summary>
        private void filterAutoMessagesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _filterAutoMessages = filterAutoMessagesCheckBox.Checked;
            statusLabel.Text = _filterAutoMessages ? "已启用自动消息过滤" : "已禁用自动消息过滤";
        }

        /// <summary>
        /// 清空消息列表按钮点击事件
        /// </summary>
        private void clearMessagesButton_Click(object sender, EventArgs e)
        {
            messagesListView.Items.Clear();
            rawDataTextBox.Clear();
            asciiTextBox.Clear();
            hexTextBox.Clear();
            xmlTextBox.Clear();
            structureTreeView.Nodes.Clear();
            UpdateStatistics();
        }

        /// <summary>
        /// 复制消息列表按钮点击事件
        /// </summary>
        private void copyMessagesButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (messagesListView.Items.Count == 0)
                {
                    MessageBox.Show("消息列表为空，无法复制", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("时间\t方向\t消息类型\t内容");

                foreach (ListViewItem item in messagesListView.Items)
                {
                    sb.AppendLine($"{item.SubItems[0].Text}\t{item.SubItems[1].Text}\t{item.SubItems[2].Text}\t{item.SubItems[3].Text}");
                }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show($"已复制 {messagesListView.Items.Count} 条消息到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 心跳检测

        /// <summary>
        /// 心跳检测定时器事件
        /// </summary>
        private void HeartbeatTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                UpdateHeartbeatStatus();
            }));
        }

        /// <summary>
        /// 心跳动画定时器事件
        /// </summary>
        private void CycleTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                UpdateHeartbeatIndicator();
            }));
        }

        /// <summary>
        /// 更新心跳状态显示
        /// </summary>
        private void UpdateHeartbeatStatus()
        {
            bool isConnected = false;
            string status = "未连接";
            Color color = Color.Red;

            if (_isServerMode)
            {
                // 服务器模式：检查服务器是否运行
                if (_server != null && _server.IsRunning)
                {
                    isConnected = true;
                    int connectionCount = _server.ConnectionCount;
                    status = $"已连接 ({connectionCount} 客户端)";
                    color = Color.Green;
                }
            }
            else
            {
                // 客户端模式：检查客户端是否连接
                if (_client != null && _client.IsConnected)
                {
                    isConnected = true;
                    var timeSinceLastActivity = DateTime.Now - _client.LastActivity;
                    status = $"已连接 (最后活动: {(int)timeSinceLastActivity.TotalSeconds}秒前)";
                    color = Color.Green;
                }
            }

            // 更新心跳标签和状态图标
            if (isConnected)
            {
                heartbeatLabel.Text = $"心跳: {status}";
                heartbeatLabel.ForeColor = color;
                _lastHeartbeatTime = DateTime.Now;
            }
            else
            {
                heartbeatLabel.Text = "心跳: 未连接";
                heartbeatLabel.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// 更新心跳指示器动画
        /// </summary>
        private void UpdateHeartbeatIndicator()
        {
            bool isConnected = false;

            if (_isServerMode)
            {
                // 服务器模式：检查服务器是否运行
                if (_server != null && _server.IsRunning)
                {
                    isConnected = true;
                }
            }
            else
            {
                // 客户端模式：检查客户端是否连接
                if (_client != null && _client.IsConnected)
                {
                    isConnected = true;
                }
            }

            // 4步循环动画：0=颜色，1=灰色，2=颜色，3=灰色
            if (_cycleStep % 2 == 0)
            {
                // 显示颜色
                if (isConnected)
                {
                    _heartbeatIndicator!.Image = _greenIndicator; // 绿色 - 在线
                }
                else
                {
                    _heartbeatIndicator!.Image = _redIndicator; // 红色 - 离线
                }
            }
            else
            {
                // 显示灰色
                _heartbeatIndicator!.Image = _grayIndicator; // 灰色 - 默认
            }

            // 更新循环步数
            _cycleStep = (_cycleStep + 1) % 4;
        }

        /// <summary>
        /// 重置心跳时间
        /// </summary>
        private void ResetHeartbeat()
        {
            _lastHeartbeatTime = DateTime.Now;
            UpdateHeartbeatStatus();
        }

        #endregion

        #region 设置对话框

        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void settingsButton_Click(object sender, EventArgs e)
        {
            var dialog = new TimeoutSettingsDialog
            {
                T3Timeout = _t3Timeout,
                T5Timeout = _t5Timeout,
                T6Timeout = _t6Timeout,
                T7Timeout = _t7Timeout,
                T8Timeout = _t8Timeout
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _t3Timeout = dialog.T3Timeout;
                _t5Timeout = dialog.T5Timeout;
                _t6Timeout = dialog.T6Timeout;
                _t7Timeout = dialog.T7Timeout;
                _t8Timeout = dialog.T8Timeout;

                MessageBox.Show(
                    $"超时设置已更新:\n" +
                    $"T3 (Reply Timeout): {_t3Timeout}秒\n" +
                    $"T5 (Separation Timeout): {_t5Timeout}秒\n" +
                    $"T6 (Control Timeout): {_t6Timeout}秒\n" +
                    $"T7 (Not Selected Timeout): {_t7Timeout}秒\n" +
                    $"T8 (Network Timeout): {_t8Timeout}秒",
                    "设置",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        #endregion
    }
}
