namespace CIMMonitor.Forms
{
    public partial class TibcoMessageForm : Form
    {
        private DataGridView? dgvMessages;
        private Button? btnRefresh;
        private Button? btnSend;
        private TextBox? txtSubject;
        private TextBox? txtContent;
        private ComboBox? cmbSubject;
        private System.Windows.Forms.Timer? refreshTimer;

        public TibcoMessageForm()
        {
            InitializeComponent();
            LoadMessages();
            StartAutoRefresh();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Tibco RV消息";
            this.Name = "TibcoMessageForm";
            this.ResumeLayout(false);

            // 消息列表
            dgvMessages = new DataGridView();
            dgvMessages.Location = new Point(20, 120);
            dgvMessages.Size = new Size(1200, 400);
            dgvMessages.ReadOnly = true;

            dgvMessages.Columns.Add("Subject", "主题");
            dgvMessages.Columns.Add("Content", "内容");
            dgvMessages.Columns.Add("Sender", "发送者");
            dgvMessages.Columns.Add("Timestamp", "时间戳");

            dgvMessages.Columns[0].Width = 200;
            dgvMessages.Columns[1].Width = 400;
            dgvMessages.Columns[2].Width = 150;
            dgvMessages.Columns[3].Width = 150;

            this.Controls.Add(dgvMessages);

            // 发送消息区域
            var lblSubject = new Label();
            lblSubject.Text = "主题:";
            lblSubject.Location = new Point(20, 20);
            lblSubject.Size = new Size(60, 25);
            this.Controls.Add(lblSubject);

            cmbSubject = new ComboBox();
            cmbSubject.Location = new Point(80, 17);
            cmbSubject.Size = new Size(200, 25);
            cmbSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSubject.Items.AddRange(CIMMonitor.Services.TibcoService.Instance.GetSubjects().ToArray());
            this.Controls.Add(cmbSubject);

            var lblContent = new Label();
            lblContent.Text = "内容:";
            lblContent.Location = new Point(300, 20);
            lblContent.Size = new Size(60, 25);
            this.Controls.Add(lblContent);

            txtContent = new TextBox();
            txtContent.Location = new Point(360, 17);
            txtContent.Size = new Size(400, 25);
            this.Controls.Add(txtContent);

            btnSend = new Button();
            btnSend.Text = "发送消息";
            btnSend.Location = new Point(780, 15);
            btnSend.Size = new Size(100, 30);
            btnSend.Click += BtnSend_Click;
            this.Controls.Add(btnSend);

            btnRefresh = new Button();
            btnRefresh.Text = "刷新消息";
            btnRefresh.Location = new Point(900, 15);
            btnRefresh.Size = new Size(100, 30);
            btnRefresh.Click += async (s, e) => await LoadMessagesAsync();
            this.Controls.Add(btnRefresh);

            // 状态信息
            var lblStatus = new Label();
            lblStatus.Text = "Tibco RV中间件状态: 已连接";
            lblStatus.Location = new Point(20, 550);
            lblStatus.Size = new Size(300, 25);
            lblStatus.ForeColor = Color.Green;
            this.Controls.Add(lblStatus);
        }

        private async Task LoadMessagesAsync()
        {
            var messages = CIMMonitor.Services.TibcoService.Instance.GetRecentMessages();
            dgvMessages!.Rows.Clear();

            foreach (var msg in messages)
            {
                dgvMessages.Rows.Add(msg.Subject, msg.Content, msg.SenderId, msg.Timestamp.ToString("HH:mm:ss"));
            }
        }
        
        private void LoadMessages()
        {
            LoadMessagesAsync().GetAwaiter().GetResult();
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            if (cmbSubject!.SelectedItem != null && !string.IsNullOrEmpty(txtContent!.Text))
            {
                var result = await CIMMonitor.Services.TibcoService.Instance.SendMessageAsync(
                    cmbSubject.SelectedItem.ToString(), 
                    txtContent.Text);
                
                if (result)
                {
                    txtContent.Text = "";
                    await LoadMessagesAsync();
                    MessageBox.Show("消息发送成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("消息发送失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("请选择主题并输入内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 2000;
            refreshTimer.Tick += (s, e) => LoadMessages();
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
