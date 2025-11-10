namespace CIMMonitor.Forms
{
    public partial class KepServerHistoryForm : Form
    {
        private TabControl? tabControl;
        private DataGridView? dgvDataChanges;
        private DataGridView? dgvMappingTriggers;
        private string _serverId;
        private Button? btnRefresh;
        private Button? btnClear;

        public KepServerHistoryForm(string serverId)
        {
            _serverId = serverId;
            InitializeComponent();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "KepServer 历史记录";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);

            // 标签页控件
            tabControl = new TabControl();
            tabControl.Location = new Point(20, 20);
            tabControl.Size = new Size(940, 550);
            this.Controls.Add(tabControl);

            // 数据变化页
            var tabDataChanges = new TabPage();
            tabDataChanges.Text = "数据变化历史";
            tabControl!.Controls.Add(tabDataChanges);

            dgvDataChanges = new DataGridView();
            dgvDataChanges.Dock = DockStyle.Fill;
            dgvDataChanges.ReadOnly = true;
            dgvDataChanges.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvDataChanges.Columns.Add("Timestamp", "时间戳");
            dgvDataChanges.Columns.Add("Address", "地址");
            dgvDataChanges.Columns.Add("DataType", "类型");
            dgvDataChanges.Columns.Add("OldValue", "原值");
            dgvDataChanges.Columns.Add("NewValue", "新值");
            dgvDataChanges.Columns.Add("ChangeType", "变化类型");

            dgvDataChanges.Columns[0].Width = 150;
            dgvDataChanges.Columns[1].Width = 250;
            dgvDataChanges.Columns[2].Width = 80;
            dgvDataChanges.Columns[3].Width = 150;
            dgvDataChanges.Columns[4].Width = 150;
            dgvDataChanges.Columns[5].Width = 120;

            tabDataChanges.Controls.Add(dgvDataChanges);

            // 映射触发页
            var tabMappingTriggers = new TabPage();
            tabMappingTriggers.Text = "映射触发历史";
            tabControl.Controls.Add(tabMappingTriggers);

            dgvMappingTriggers = new DataGridView();
            dgvMappingTriggers.Dock = DockStyle.Fill;
            dgvMappingTriggers.ReadOnly = true;
            dgvMappingTriggers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvMappingTriggers.Columns.Add("TriggeredTime", "触发时间");
            dgvMappingTriggers.Columns.Add("MappingId", "映射ID");
            dgvMappingTriggers.Columns.Add("BitAddress", "Bit地址");
            dgvMappingTriggers.Columns.Add("WordAddress", "Word地址");
            dgvMappingTriggers.Columns.Add("BitNewValue", "Bit新值");
            dgvMappingTriggers.Columns.Add("WordValue", "Word值");
            dgvMappingTriggers.Columns.Add("TriggerCondition", "触发条件");

            dgvMappingTriggers.Columns[0].Width = 150;
            dgvMappingTriggers.Columns[1].Width = 120;
            dgvMappingTriggers.Columns[2].Width = 200;
            dgvMappingTriggers.Columns[3].Width = 200;
            dgvMappingTriggers.Columns[4].Width = 100;
            dgvMappingTriggers.Columns[5].Width = 200;
            dgvMappingTriggers.Columns[6].Width = 120;

            tabMappingTriggers.Controls.Add(dgvMappingTriggers);

            // 按钮区域
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(20, 580);
            btnRefresh.Size = new Size(100, 30);
            btnRefresh.Click += BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            btnClear = new Button();
            btnClear.Text = "清空";
            btnClear.Location = new Point(130, 580);
            btnClear.Size = new Size(100, 30);
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);
        }

        private void LoadHistory()
        {
            try
            {
                // 加载数据变化历史
                dgvDataChanges!.Rows.Clear();
                var dataChanges = Services.HardwareService.GetDataChangeHistory(_serverId);
                if (dataChanges != null)
                {
                    foreach (var dataChange in dataChanges.OrderByDescending(d => d.Timestamp).Take(100))
                    {
                        dgvDataChanges.Rows.Add(
                            dataChange.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            dataChange.Address,
                            dataChange.DataType,
                            dataChange.OldValue?.ToString() ?? "",
                            dataChange.NewValue?.ToString() ?? "",
                            dataChange.ChangeType
                        );
                    }
                }

                // 加载映射触发历史
                dgvMappingTriggers!.Rows.Clear();
                var mappingTriggers = Services.HardwareService.GetMappingHistory(_serverId);
                if (mappingTriggers != null)
                {
                    foreach (var mapping in mappingTriggers.OrderByDescending(m => m.TriggeredTime).Take(100))
                    {
                        dgvMappingTriggers.Rows.Add(
                            mapping.TriggeredTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            mapping.MappingId,
                            mapping.BitAddressId,
                            mapping.WordAddressId,
                            mapping.BitNewValue.ToString(),
                            mapping.WordValue,
                            mapping.TriggerCondition
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史记录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Services.LoggingService.LogException("加载KepServer历史记录失败", ex);
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadHistory();
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要清空历史记录吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Services.HardwareService.ClearHistory(_serverId);
                LoadHistory();
                MessageBox.Show("历史记录已清空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
