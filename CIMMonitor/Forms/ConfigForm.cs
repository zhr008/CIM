namespace CIMMonitor.Forms
{
    public partial class ConfigForm : Form
    {
        private TabControl? tabControl;
        private TextBox? txtOracleConn;
        private TextBox? txtTibcoService;
        private TextBox? txtKepserverHost;
        private TextBox? txtHSMSHost;
        private NumericUpDown? numTimeout;

        public ConfigForm()
        {
            InitializeComponent();
            LoadCurrentConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(700, 600);
            this.Text = "系统配置";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ResumeLayout(false);

            tabControl = new TabControl();
            tabControl.Location = new Point(20, 20);
            tabControl.Size = new Size(640, 450);

            // 数据库配置
            var tabDb = new TabPage("数据库");
            var lblOracle = new Label();
            lblOracle.Text = "Oracle连接字符串:";
            lblOracle.Location = new Point(20, 30);
            lblOracle.Size = new Size(120, 25);

            txtOracleConn = new TextBox();
            txtOracleConn.Location = new Point(20, 60);
            txtOracleConn.Size = new Size(580, 25);
            txtOracleConn.PasswordChar = '*';

            tabDb.Controls.AddRange(new Control[] { lblOracle, txtOracleConn });
            tabControl.TabPages.Add(tabDb);

            // 中间件配置
            var tabMiddleware = new TabPage("中间件");
            var lblTibco = new Label();
            lblTibco.Text = "Tibco RV服务:";
            lblTibco.Location = new Point(20, 30);
            lblTibco.Size = new Size(120, 25);

            txtTibcoService = new TextBox();
            txtTibcoService.Location = new Point(20, 60);
            txtTibcoService.Size = new Size(580, 25);
            txtTibcoService.Text = "tcp:7474";

            tabMiddleware.Controls.AddRange(new Control[] { lblTibco, txtTibcoService });
            tabControl.TabPages.Add(tabMiddleware);

            // 硬件配置
            var tabHardware = new TabPage("硬件");
            var lblKepserver = new Label();
            lblKepserver.Text = "KepServer地址:";
            lblKepserver.Location = new Point(20, 30);
            lblKepserver.Size = new Size(120, 25);

            txtKepserverHost = new TextBox();
            txtKepserverHost.Location = new Point(20, 60);
            txtKepserverHost.Size = new Size(580, 25);

            var lblHSMS = new Label();
            lblHSMS.Text = "HSMS地址:";
            lblHSMS.Location = new Point(20, 100);
            lblHSMS.Size = new Size(120, 25);

            txtHSMSHost = new TextBox();
            txtHSMSHost.Location = new Point(20, 130);
            txtHSMSHost.Size = new Size(580, 25);

            tabHardware.Controls.AddRange(new Control[] { lblKepserver, txtKepserverHost, lblHSMS, txtHSMSHost });
            tabControl.TabPages.Add(tabHardware);

            // 其他配置
            var tabOther = new TabPage("其他");
            var lblTimeout = new Label();
            lblTimeout.Text = "超时时间(秒):";
            lblTimeout.Location = new Point(20, 30);
            lblTimeout.Size = new Size(120, 25);

            numTimeout = new NumericUpDown();
            numTimeout.Location = new Point(20, 60);
            numTimeout.Size = new Size(120, 25);
            numTimeout.Minimum = 1;
            numTimeout.Maximum = 300;
            numTimeout.Value = 60;

            tabOther.Controls.AddRange(new Control[] { lblTimeout, numTimeout });
            tabControl.TabPages.Add(tabOther);

            this.Controls.Add(tabControl);

            // 按钮
            var btnOK = new Button();
            btnOK.Text = "确定";
            btnOK.Location = new Point(400, 490);
            btnOK.Size = new Size(100, 35);
            btnOK.DialogResult = DialogResult.OK;
            this.Controls.Add(btnOK);

            var btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(520, 490);
            btnCancel.Size = new Size(100, 35);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadCurrentConfig()
        {
            txtOracleConn!.Text = "User Id=industrial;Password=****;Data Source=192.168.1.100:1521/orcl";
            txtTibcoService!.Text = "tcp:7474";
            txtKepserverHost!.Text = "192.168.1.101:49320";
            txtHSMSHost!.Text = "192.168.1.102:5007";
            numTimeout!.Value = 60;
        }
    }
}
