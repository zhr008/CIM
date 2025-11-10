namespace CIMMonitor.Forms
{
    public partial class HardwareMonitorForm : Form
    {
        private GroupBox? gbKepserver;
        private GroupBox? gbHSMS;
        private GroupBox? gbOPC;
        private Button? btnConnectKep;
        private Button? btnDisconnectKep;
        private Button? btnReadPLC;
        private Button? btnWritePLC;
        private Button? btnSendHSMS;
        private TextBox? txtLog;
        private System.Windows.Forms.Timer? refreshTimer;

        public HardwareMonitorForm()
        {
            InitializeComponent();
            StartSimulation();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "硬件监控";
            this.Name = "HardwareMonitorForm";
            this.ResumeLayout(false);

            // KepServer区域
            gbKepserver = new GroupBox();
            gbKepserver.Text = "KepServer EX (OPC)";
            gbKepserver.Location = new Point(20, 20);
            gbKepserver.Size = new Size(400, 200);
            this.Controls.Add(gbKepserver);

            btnConnectKep = new Button();
            btnConnectKep.Text = "连接";
            btnConnectKep.Location = new Point(20, 30);
            btnConnectKep.Size = new Size(80, 30);
            btnConnectKep.Click += (s, e) =>
            {
                try
                {
                    Services.HardwareService.ConnectKepserver();
                    AppendLog("KepServer", "已连接到KepServer EX");
                    Services.LoggingService.Info("KepServer连接成功");
                }
                catch (Exception ex)
                {
                    AppendLog("KepServer", $"连接失败: {ex.Message}");
                    Services.LoggingService.LogException("KepServer连接失败", ex);
                    MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            gbKepserver.Controls.Add(btnConnectKep);

            btnDisconnectKep = new Button();
            btnDisconnectKep.Text = "断开";
            btnDisconnectKep.Location = new Point(120, 30);
            btnDisconnectKep.Size = new Size(80, 30);
            btnDisconnectKep.Click += (s, e) =>
            {
                try
                {
                    Services.HardwareService.DisconnectKepserver();
                    AppendLog("KepServer", "已断开KepServer EX连接");
                    Services.LoggingService.Info("KepServer连接已断开");
                }
                catch (Exception ex)
                {
                    AppendLog("KepServer", $"断开失败: {ex.Message}");
                    Services.LoggingService.LogException("KepServer断开连接失败", ex);
                    MessageBox.Show($"断开失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            gbKepserver.Controls.Add(btnDisconnectKep);

            var lblTags = new Label();
            lblTags.Text = "OPC标签:";
            lblTags.Location = new Point(20, 80);
            lblTags.Size = new Size(60, 25);
            gbKepserver.Controls.Add(lblTags);

            var lstTags = new ListBox();
            lstTags.Location = new Point(20, 110);
            lstTags.Size = new Size(350, 70);
            Services.HardwareService.GetOPCTags().ForEach(tag => lstTags.Items.Add(tag));
            gbKepserver.Controls.Add(lstTags);

            // HSMS区域
            gbHSMS = new GroupBox();
            gbHSMS.Text = "HSMS通信";
            gbHSMS.Location = new Point(440, 20);
            gbHSMS.Size = new Size(400, 200);
            this.Controls.Add(gbHSMS);

            btnSendHSMS = new Button();
            btnSendHSMS.Text = "发送HSMS命令";
            btnSendHSMS.Location = new Point(20, 30);
            btnSendHSMS.Size = new Size(120, 30);
            btnSendHSMS.Click += (s, e) =>
            {
                Services.HardwareService.SendHSMSCommand("PLC001", "SELECT");
                AppendLog("HSMS", "发送命令: SELECT");
            };
            gbHSMS.Controls.Add(btnSendHSMS);

            // PLC控制区域
            gbOPC = new GroupBox();
            gbOPC.Text = "PLC控制";
            gbOPC.Location = new Point(20, 240);
            gbOPC.Size = new Size(820, 150);
            this.Controls.Add(gbOPC);

            btnReadPLC = new Button();
            btnReadPLC.Text = "读取PLC数据";
            btnReadPLC.Location = new Point(20, 30);
            btnReadPLC.Size = new Size(120, 30);
            btnReadPLC.Click += (s, e) =>
            {
                var tags = new[] { "Temperature", "Pressure" };
                var data = Services.HardwareService.ReadPLCData("PLC001", tags);
                if (data != null)
                {
                    AppendLog("PLC", $"读取数据: 设备={data.DeviceId}, 状态={data.Status}");
                    foreach (var kvp in data.TagValues)
                    {
                        AppendLog("PLC", $"  {kvp.Key} = {kvp.Value}");
                    }
                }
            };
            gbOPC.Controls.Add(btnReadPLC);

            btnWritePLC = new Button();
            btnWritePLC.Text = "写入PLC数据";
            btnWritePLC.Location = new Point(160, 30);
            btnWritePLC.Size = new Size(120, 30);
            btnWritePLC.Click += (s, e) =>
            {
                var values = new Dictionary<string, object> { { "Setpoint", 75.5 } };
                Services.HardwareService.WritePLCData("PLC001", values);
                AppendLog("PLC", "写入数据: Setpoint = 75.5");
            };
            gbOPC.Controls.Add(btnWritePLC);

            // 日志区域
            var lblLog = new Label();
            lblLog.Text = "通信日志:";
            lblLog.Location = new Point(20, 410);
            lblLog.Size = new Size(100, 25);
            this.Controls.Add(lblLog);

            txtLog = new TextBox();
            txtLog.Location = new Point(20, 440);
            txtLog.Size = new Size(1200, 300);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = new Font("Consolas", 9);
            this.Controls.Add(txtLog);
        }

        private void AppendLog(string component, string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{component}] {message}";
            txtLog!.AppendText(logEntry + Environment.NewLine);
        }

        private void StartSimulation()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += (s, e) =>
            {
                var msg = Services.HardwareService.ReceiveHSMSMessage();
                if (msg != null)
                {
                    AppendLog("HSMS", $"接收消息: Session={msg.SessionId}, Type={msg.Header.MessageType}");
                }
            };
            refreshTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
