namespace CIMMonitor.Forms
{
    public partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();
            ShowWelcomeInfo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(600, 400);
            this.Text = "欢迎";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
        }

        private void ShowWelcomeInfo()
        {
            var label = new Label
            {
                Text = @"工业自动化系统 - CIM界面

系统架构:
┌─────────────────────────────────────────┐
│           CIM层 (WINFORM)              │
│   主界面 | 设备监控 | 报警管理 | 配置     │
└─────────────┬───────────────────┬────────┘
              │ WCF服务调用
┌─────────────┴──────── WCF服务层 ────┬──────┐
│   生产服务 | 设备服务 | 报警服务 | 通信服务  │
└─────────────┬─────────────────┬────┴──────┘
              │ Tibco消息
┌─────────────┴───── Tibco RV中间件 ──┬─────┐
│   消息处理 | 主题管理 | 队列转发      │
└─────────────┬─────────────────┬────┴──────┘
              │ 硬件通信
┌─────────────┴───── 硬件通信层 ──────┬─────┐
│  KepServer EX | HSMS | PLC数据采集  │
└─────────────┬─────────────────┬────┴──────┘
              │ 数据存储
┌─────────────┴───── Oracle数据库 ──────┘
│ PRODUCTION_DATA | DEVICES | ALARM_LOGS │
└────────────────────────────────────────┘

点击确定进入系统",
                AutoSize = true,
                Font = new Font("微软雅黑", 10),
                Location = new Point(20, 20),
                Size = new Size(550, 300)
            };

            var okButton = new Button
            {
                Text = "确定",
                Size = new Size(100, 35),
                Location = new Point(240, 320),
                DialogResult = DialogResult.OK
            };

            this.Controls.AddRange(new Control[] { label, okButton });
            this.AcceptButton = okButton;
        }
    }
}
