using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using CIMMonitor.Models;

namespace CIMMonitor.Forms
{
    public partial class MonitorDetail : Form
    {
        private readonly DeviceInfo _deviceInfo;
        private TextBox txtLog;
        private Label lblTitle;

        public MonitorDetail(DeviceInfo deviceInfo)
        {
            _deviceInfo = deviceInfo;
            InitializeComponent();
            InitializeCustomComponents();
            LoadDeviceDetails();
        }


        private void InitializeCustomComponents()
        {
            // 标题标签
            lblTitle = new Label
            {
                Text = $"设备详情 - {_deviceInfo.ServerName}",
                Location = new Point(20, 20),
                Size = new Size(400, 25),
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                ForeColor = Color.Navy
            };
            this.Controls.Add(lblTitle);

            // 创建设备详情面板
            CreateDeviceDetailsPanel();

            // 消息日志文本框
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(20, 320),
                Size = new Size(750, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true,
                Font = new Font("Consolas", 9F)
            };
            this.Controls.Add(txtLog);

            // 添加消息日志标签
            var logLabel = new Label
            {
                Text = "消息日志:",
                Location = new Point(20, 300),
                Size = new Size(100, 20),
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold)
            };
            this.Controls.Add(logLabel);
        }

        private void CreateDeviceDetailsPanel()
        {
            // 设备详情面板
            var panel = new Panel
            {
                Name = "detailsPanel", // 添加名称便于查找
                Location = new Point(20, 60),
                Size = new Size(750, 160),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panel);

            // 添加设备详情标签和值
            int yPos = 10;
            int labelWidth = 120;
            int valueWidth = 200;
            int lineHeight = 25;
            int spacing = 5;

            // 设备ID
            AddDetailRow(panel, "设备ID:", _deviceInfo.ServerId, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 设备名称
            AddDetailRow(panel, "设备名称:", _deviceInfo.ServerName, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 协议类型
            AddDetailRow(panel, "协议类型:", _deviceInfo.ProtocolType, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 设备类型
            AddDetailRow(panel, "设备类型:", _deviceInfo.DeviceType, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // IP地址
            AddDetailRow(panel, "IP地址:", _deviceInfo.Host, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 端口
            AddDetailRow(panel, "端口:", _deviceInfo.Port.ToString(), 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 启用状态
            AddDetailRow(panel, "启用:", _deviceInfo.Enabled ? "启用" : "禁用", 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 在线状态
            AddDetailRow(panel, "在线状态:", _deviceInfo.IsOnline ? "在线" : "离线", 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 添加心跳指示器
            var heartbeatLabel = new Label
            {
                Text = "心跳:",
                Location = new Point(10, yPos),
                Size = new Size(labelWidth, lineHeight),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft Sans Serif", 9F)
            };
            panel.Controls.Add(heartbeatLabel);

            var heartbeatPictureBox = new PictureBox
            {
                Name = "pbHeartbeat",
                Location = new Point(10 + labelWidth + 10, yPos),
                Size = new Size(20, 20),
                BackColor = Color.Gray, // 默认灰色
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(heartbeatPictureBox);

            // 启动心跳定时器
            StartHeartbeatIndicator(heartbeatPictureBox);

            yPos += lineHeight + spacing;

            // 响应时间
            AddDetailRow(panel, "响应时间:", _deviceInfo.ResponseTimeMs > 0 ? _deviceInfo.ResponseTimeMs + "ms" : "-", 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 连接质量
            AddDetailRow(panel, "连接质量:", _deviceInfo.ConnectionQuality, 10, yPos, labelWidth, valueWidth, lineHeight);
            yPos += lineHeight + spacing;

            // 配置文件来源
            AddDetailRow(panel, "配置文件:", _deviceInfo.SourceFile, 10, yPos, labelWidth, valueWidth, lineHeight);
        }

        private void AddDetailRow(Control parent, string labelText, string valueText, int x, int y, int labelWidth, int valueWidth, int height)
        {
            // 标签
            var label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(labelWidth, height),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft Sans Serif", 9F)
            };
            parent.Controls.Add(label);

            // 值
            var valueLabel = new Label
            {
                Text = valueText,
                Location = new Point(x + labelWidth + 10, y),
                Size = new Size(valueWidth, height),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft Sans Serif", 9F),
                ForeColor = Color.Blue
            };
            parent.Controls.Add(valueLabel);
        }

        private void LoadDeviceDetails()
        {
            // 初始日志消息
            AppendLog($"设备详情已加载 - {_deviceInfo.ServerName}");
            AppendLog($"设备ID: {_deviceInfo.ServerId}");
            AppendLog($"协议类型: {_deviceInfo.ProtocolType}");
            AppendLog($"IP地址: {_deviceInfo.Host}:{_deviceInfo.Port}");
            AppendLog($"当前状态: {(_deviceInfo.IsOnline ? "在线" : "离线")}");
            AppendLog("");
            
            // 开始心跳定时器
            StartHeartbeatTimer();
        }

        public void AppendLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new MethodInvoker(() =>
                {
                    AppendLog(message);
                }));
            }
            else
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                
                // 自动滚动到底部
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        public void UpdateDeviceStatus(DeviceInfo updatedDeviceInfo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    UpdateDeviceStatus(updatedDeviceInfo);
                }));
            }
            else
            {
                // 更新内部设备信息
                _deviceInfo.ServerId = updatedDeviceInfo.ServerId;
                _deviceInfo.ServerName = updatedDeviceInfo.ServerName;
                _deviceInfo.ProtocolType = updatedDeviceInfo.ProtocolType;
                _deviceInfo.DeviceType = updatedDeviceInfo.DeviceType;
                _deviceInfo.Host = updatedDeviceInfo.Host;
                _deviceInfo.Port = updatedDeviceInfo.Port;
                _deviceInfo.Enabled = updatedDeviceInfo.Enabled;
                _deviceInfo.IsOnline = updatedDeviceInfo.IsOnline;
                _deviceInfo.HeartbeatCount = updatedDeviceInfo.HeartbeatCount;
                _deviceInfo.ResponseTimeMs = updatedDeviceInfo.ResponseTimeMs;
                _deviceInfo.ConnectionQuality = updatedDeviceInfo.ConnectionQuality;
                _deviceInfo.LastUpdate = updatedDeviceInfo.LastUpdate;

                // 重新加载设备详情（更新UI）
                ReloadDeviceDetails();
            }
        }

        private void ReloadDeviceDetails()
        {
            // 清除现有的详情控件（除了面板本身）
            foreach (Control control in this.Controls.Find("detailsPanel", false))
            {
                control.Controls.Clear();
            }

            // 重新创建详情
            CreateDeviceDetailsPanel();
        }

        private void StartHeartbeatIndicator(PictureBox pictureBox)
        {
            var heartbeatTimer = new System.Windows.Forms.Timer();
            heartbeatTimer.Interval = 3000; // 3秒一跳
            Color[] colors = { Color.Red, Color.Gray, Color.Green }; // 红-灰-绿
            int colorIndex = 0;
            
            heartbeatTimer.Tick += (sender, e) =>
            {
                pictureBox.BackColor = colors[colorIndex];
                colorIndex = (colorIndex + 1) % colors.Length; // 循环颜色
                
                // 更新设备状态信息
                if (_deviceInfo.IsOnline)
                {
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 心跳状态: {pictureBox.BackColor.Name}, 响应时间: {_deviceInfo.ResponseTimeMs}ms");
                }
            };
            heartbeatTimer.Start();
        }
        
        private void StartHeartbeatTimer()
        {
            var timer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // 每5秒更新一次
            };
            timer.Tick += (sender, e) =>
            {
                // 更新设备状态信息
                if (_deviceInfo.IsOnline)
                {
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 心跳: {_deviceInfo.HeartbeatCount}, 响应时间: {_deviceInfo.ResponseTimeMs}ms");
                }
            };
            timer.Start();
        }
    }
}