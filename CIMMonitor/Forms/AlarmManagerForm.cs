namespace CIMMonitor.Forms
{
    public partial class AlarmManagerForm : Form
    {
        private DataGridView? dgvAlarms;
        private Button? btnAcknowledge;
        private Button? btnClear;
        private Button? btnRefresh;
        private RadioButton? rbActive;
        private RadioButton? rbAll;
        private System.Windows.Forms.Timer? refreshTimer;

        public AlarmManagerForm()
        {
            InitializeComponent();
            LoadAlarms();
            StartAutoRefresh();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "报警管理";
            this.Name = "AlarmManagerForm";
            this.ResumeLayout(false);

            dgvAlarms = new DataGridView();
            dgvAlarms.Location = new Point(20, 70);
            dgvAlarms.Size = new Size(1200, 500);
            dgvAlarms.ReadOnly = true;
            dgvAlarms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvAlarms.Columns.Add("AlarmId", "报警ID");
            dgvAlarms.Columns.Add("DeviceId", "设备ID");
            dgvAlarms.Columns.Add("AlarmType", "报警类型");
            dgvAlarms.Columns.Add("AlarmLevel", "报警级别");
            dgvAlarms.Columns.Add("Description", "描述");
            dgvAlarms.Columns.Add("OccurTime", "发生时间");
            dgvAlarms.Columns.Add("Status", "状态");

            dgvAlarms.Columns[0].Width = 80;
            dgvAlarms.Columns[1].Width = 100;
            dgvAlarms.Columns[2].Width = 120;
            dgvAlarms.Columns[3].Width = 100;
            dgvAlarms.Columns[4].Width = 300;
            dgvAlarms.Columns[5].Width = 150;
            dgvAlarms.Columns[6].Width = 100;

            this.Controls.Add(dgvAlarms);

            // 筛选条件
            var lblFilter = new Label();
            lblFilter.Text = "显示条件:";
            lblFilter.Location = new Point(20, 30);
            lblFilter.Size = new Size(80, 25);
            this.Controls.Add(lblFilter);

            rbActive = new RadioButton();
            rbActive.Text = "激活报警";
            rbActive.Location = new Point(100, 30);
            rbActive.Checked = true;
            rbActive.CheckedChanged += (s, e) => LoadAlarms();
            this.Controls.Add(rbActive);

            rbAll = new RadioButton();
            rbAll.Text = "全部报警";
            rbAll.Location = new Point(200, 30);
            rbAll.CheckedChanged += (s, e) => LoadAlarms();
            this.Controls.Add(rbAll);

            // 按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(20, 590);
            btnRefresh.Size = new Size(100, 30);
            btnRefresh.Click += (s, e) => LoadAlarms();
            this.Controls.Add(btnRefresh);

            btnAcknowledge = new Button();
            btnAcknowledge.Text = "确认报警";
            btnAcknowledge.Location = new Point(140, 590);
            btnAcknowledge.Size = new Size(100, 30);
            btnAcknowledge.Click += BtnAcknowledge_Click;
            this.Controls.Add(btnAcknowledge);

            btnClear = new Button();
            btnClear.Text = "清除报警";
            btnClear.Location = new Point(260, 590);
            btnClear.Size = new Size(100, 30);
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);
        }

        private void LoadAlarms()
        {
            var alarms = rbActive!.Checked ? Services.AlarmService.GetActiveAlarms() : Services.AlarmService.GetAllAlarms();
            dgvAlarms!.Rows.Clear();

            foreach (var alarm in alarms)
            {
                var rowIndex = dgvAlarms.Rows.Add(
                    alarm.AlarmId,
                    alarm.DeviceId,
                    alarm.AlarmType,
                    alarm.AlarmLevel,
                    alarm.Description,
                    alarm.OccurTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    alarm.Status
                );

                if (alarm.AlarmLevel == "高")
                {
                    dgvAlarms.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Red;
                }
                else if (alarm.AlarmLevel == "中")
                {
                    dgvAlarms.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Orange;
                }
                else if (alarm.AlarmLevel == "低")
                {
                    dgvAlarms.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Yellow;
                }
            }
        }

        private void BtnAcknowledge_Click(object? sender, EventArgs e)
        {
            if (dgvAlarms!.SelectedRows.Count > 0)
            {
                var row = dgvAlarms.SelectedRows[0];
                var alarmId = Convert.ToInt32(row.Cells[0].Value);
                if (Services.AlarmService.AcknowledgeAlarm(alarmId))
                {
                    LoadAlarms();
                    MessageBox.Show($"报警 {alarmId} 已确认", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("请选择要确认的报警", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            if (dgvAlarms!.SelectedRows.Count > 0)
            {
                var row = dgvAlarms.SelectedRows[0];
                var alarmId = Convert.ToInt32(row.Cells[0].Value);
                if (Services.AlarmService.ClearAlarm(alarmId))
                {
                    LoadAlarms();
                    MessageBox.Show($"报警 {alarmId} 已清除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("请选择要清除的报警", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += (s, e) => LoadAlarms();
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
