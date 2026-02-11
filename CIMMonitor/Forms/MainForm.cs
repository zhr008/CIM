using System.Windows.Forms;
using CIMMonitor.Services;
using Common.Services;

namespace CIMMonitor.Forms
{
    public partial class MainForm : Form
    {
        private MenuStrip? menuStrip;
        private TabControl? mainTabControl;
        private TabPage? tabPageWelcome;
        private TabPage? tabPageDevice;
        private TabPage? tabPageKepServer;
        private TabPage? tabPageHardware;
        private TabPage? tabPageProduction;
        private TabPage? tabPageAlarm;
        private TabPage? tabPageTibco;
        private Monitor? deviceMonitorForm;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "CIM Monitor - 工业自动化监控中心 v2.0";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

            // 创建主TabControl
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.Controls.Add(mainTabControl);

            // 创建欢迎页
            tabPageWelcome = new TabPage("欢迎");
            tabPageWelcome.Controls.Add(CreateWelcomePanel());
            mainTabControl.TabPages.Add(tabPageWelcome);

            // 初始化各个功能页面（延迟加载）
            InitializeTabPages();
        }

        private Panel CreateWelcomePanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = System.Drawing.Color.White;

            var label = new Label();
            label.Text = "CIM Monitor - 工业自动化监控中心";
            label.Font = new System.Drawing.Font("Microsoft YaHei", 16F, System.Drawing.FontStyle.Bold);
            label.ForeColor = System.Drawing.Color.FromArgb(0, 123, 255);
            label.AutoSize = true;
            label.Location = new System.Drawing.Point(50, 50);
            panel.Controls.Add(label);

            var version = new Label();
            version.Text = "版本 2.0";
            version.Font = new System.Drawing.Font("Microsoft YaHei", 11F);
            version.Location = new System.Drawing.Point(50, 100);
            panel.Controls.Add(version);

            var features = new Label();
            features.Text = "功能特性：\n\n" +
                           "• 设备状态实时监控\n" +
                           "• KepServer数据采集\n" +
                           "• 硬件设备管理\n" +
                           "• 生产数据跟踪\n" +
                           "• 报警管理\n" +
                           "• TIBCO消息集成\n" +
                           "• 日志记录与查看";
            features.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            features.Location = new System.Drawing.Point(50, 150);
            panel.Controls.Add(features);

            return panel;
        }

        private void InitializeTabPages()
        {
            // 设备监控页
            tabPageDevice = new TabPage("设备监控");
            mainTabControl.TabPages.Add(tabPageDevice);

            // KepServer监控页
            tabPageKepServer = new TabPage("KepServer监控");
            mainTabControl.TabPages.Add(tabPageKepServer);

            // 硬件监控页
            tabPageHardware = new TabPage("硬件监控");
            mainTabControl.TabPages.Add(tabPageHardware);

            // 生产数据页
            tabPageProduction = new TabPage("生产数据");
            mainTabControl.TabPages.Add(tabPageProduction);

            // 报警管理页
            tabPageAlarm = new TabPage("报警管理");
            mainTabControl.TabPages.Add(tabPageAlarm);

            // TIBCO消息页
            tabPageTibco = new TabPage("TIBCO消息");
            mainTabControl.TabPages.Add(tabPageTibco);

            // 绑定TabControl事件
            mainTabControl.SelectedIndexChanged += MainTabControl_SelectedIndexChanged;

            // ⚡ 立即加载设备监控页面，启动服务端监听
            LoadDeviceMonitor();
        }

        private void MainTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (mainTabControl?.SelectedTab == null) return;

            // 延迟加载对应的窗体
            if (mainTabControl.SelectedTab == tabPageDevice && deviceMonitorForm == null)
            {
                LoadDeviceMonitor();
            }
        }

        // 延迟加载各个功能模块
        private void LoadDeviceMonitor()
        {
            if (deviceMonitorForm != null) return;

            deviceMonitorForm = new Monitor();
            deviceMonitorForm.TopLevel = false;
            deviceMonitorForm.FormBorderStyle = FormBorderStyle.None;
            deviceMonitorForm.Dock = DockStyle.Fill;

            tabPageDevice.Controls.Add(deviceMonitorForm);
            deviceMonitorForm.Show();
        }


        // 菜单事件处理（切换Tab页）
        private void ShowWelcome()
        {
            mainTabControl.SelectedTab = tabPageWelcome;
        }

        private void ShowDeviceMonitor()
        {
            LoadDeviceMonitor();
            mainTabControl.SelectedTab = tabPageDevice;
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "CIM Monitor - 工业自动化监控中心\n\n" +
                "版本: 2.0\n" +
                "开发时间: 2025-10-30\n\n" +
                "功能特性:\n" +
                "• 设备状态实时监控\n" +
                "• KepServer数据采集\n" +
                "• 硬件设备管理\n" +
                "• 生产数据跟踪\n" +
                "• 报警管理\n" +
                "• TIBCO消息集成\n" +
                "• 日志记录与查看\n\n" +
                "版权所有 © 2025",
                "关于CIM Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 创建菜单栏
            CreateMenuStrip();
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();
            menuStrip.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

            // 系统菜单
            var systemMenu = new ToolStripMenuItem("系统(&S)");
            var welcomeItem = new ToolStripMenuItem("欢迎界面(&W)", null, (s, e) => ShowWelcome());
            systemMenu.DropDownItems.Add(welcomeItem);
            systemMenu.DropDownItems.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("退出(&X)", null, (s, e) => Application.Exit());
            systemMenu.DropDownItems.Add(exitItem);
            menuStrip.Items.Add(systemMenu);

            // 监控菜单
            var monitorMenu = new ToolStripMenuItem("监控(&M)");
            var deviceItem = new ToolStripMenuItem("设备监控(&D)", null, (s, e) => ShowDeviceMonitor());
            monitorMenu.DropDownItems.Add(deviceItem);
            menuStrip.Items.Add(monitorMenu);

            // 帮助菜单
            var helpMenu = new ToolStripMenuItem("帮助(&H)");
            var aboutItem = new ToolStripMenuItem("关于(&A)", null, (s, e) => ShowAbout());
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);

            // 将菜单栏添加到窗体
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }
    }
}
