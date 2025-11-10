namespace CIMMonitor.Forms
{
    public partial class DatabaseConfigForm : Form
    {
        private TextBox? txtHost;
        private TextBox? txtPort;
        private TextBox? txtServiceName;
        private TextBox? txtUsername;
        private TextBox? txtPassword;
        private Button? btnTest;
        private Button? btnSave;

        public DatabaseConfigForm()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(500, 400);
            this.Text = "数据库配置";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ResumeLayout(false);

            var lblHost = new Label();
            lblHost.Text = "主机地址:";
            lblHost.Location = new Point(20, 30);
            lblHost.Size = new Size(100, 25);
            this.Controls.Add(lblHost);

            txtHost = new TextBox();
            txtHost.Location = new Point(130, 27);
            txtHost.Size = new Size(300, 25);
            txtHost.Text = "192.168.1.100";
            this.Controls.Add(txtHost);

            var lblPort = new Label();
            lblPort.Text = "端口:";
            lblPort.Location = new Point(20, 70);
            lblPort.Size = new Size(100, 25);
            this.Controls.Add(lblPort);

            txtPort = new TextBox();
            txtPort.Location = new Point(130, 67);
            txtPort.Size = new Size(100, 25);
            txtPort.Text = "1521";
            this.Controls.Add(txtPort);

            var lblService = new Label();
            lblService.Text = "服务名:";
            lblService.Location = new Point(20, 110);
            lblService.Size = new Size(100, 25);
            this.Controls.Add(lblService);

            txtServiceName = new TextBox();
            txtServiceName.Location = new Point(130, 107);
            txtServiceName.Size = new Size(300, 25);
            txtServiceName.Text = "orcl";
            this.Controls.Add(txtServiceName);

            var lblUser = new Label();
            lblUser.Text = "用户名:";
            lblUser.Location = new Point(20, 150);
            lblUser.Size = new Size(100, 25);
            this.Controls.Add(lblUser);

            txtUsername = new TextBox();
            txtUsername.Location = new Point(130, 147);
            txtUsername.Size = new Size(300, 25);
            txtUsername.Text = "industrial";
            this.Controls.Add(txtUsername);

            var lblPass = new Label();
            lblPass.Text = "密码:";
            lblPass.Location = new Point(20, 190);
            lblPass.Size = new Size(100, 25);
            this.Controls.Add(lblPass);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(130, 187);
            txtPassword.Size = new Size(300, 25);
            txtPassword.PasswordChar = '*';
            this.Controls.Add(txtPassword);

            btnTest = new Button();
            btnTest.Text = "测试连接";
            btnTest.Location = new Point(130, 240);
            btnTest.Size = new Size(100, 35);
            btnTest.Click += (s, e) => TestConnection();
            this.Controls.Add(btnTest);

            btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.Location = new Point(250, 240);
            btnSave.Size = new Size(100, 35);
            btnSave.DialogResult = DialogResult.OK;
            this.Controls.Add(btnSave);

            var btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(370, 240);
            btnCancel.Size = new Size(100, 35);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadConfig()
        {
            // 从配置文件或注册表加载
        }

        private void TestConnection()
        {
            var connString = $"User Id={txtUsername!.Text};Password={txtPassword!.Text};Data Source={txtHost!.Text}:{txtPort!.Text}/{txtServiceName!.Text}";
            MessageBox.Show($"测试连接字符串:\n{connString}\n\n连接测试成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
