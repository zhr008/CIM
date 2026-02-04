namespace CIMMonitor.Forms
{
    public partial class KepServerMonitorForm : Form
    {
        private ComboBox? cmbServers;
        private ComboBox? cmbProjects;
        private DataGridView? dgvBitAddresses;
        private DataGridView? dgvWordAddresses;
        private DataGridView? dgvMappings;
        private TextBox? txtLog;
        private Button? btnStart;
        private Button? btnStop;
        private Button? btnRefresh;
        private Button? btnViewHistory;
        private System.Windows.Forms.Timer? refreshTimer;
        private string selectedServerId = string.Empty;
        private Label lblServer;
        private Label lblProject;
        private Label lblBits;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private Label lblWords;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private Label lblMappings;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private Label lblLog;
        private string selectedProjectId = string.Empty;

        public KepServerMonitorForm()
        {
            InitializeComponent();
            LoadServers();
            Services.LoggingService.Info("KepServer监控界面已打开");
        }

        private void InitializeComponent()
        {
            lblServer = new Label();
            cmbServers = new ComboBox();
            lblProject = new Label();
            cmbProjects = new ComboBox();
            btnStart = new Button();
            btnStop = new Button();
            btnRefresh = new Button();
            btnViewHistory = new Button();
            lblBits = new Label();
            dgvBitAddresses = new DataGridView();
            lblWords = new Label();
            dgvWordAddresses = new DataGridView();
            lblMappings = new Label();
            dgvMappings = new DataGridView();
            lblLog = new Label();
            txtLog = new TextBox();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn18 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn19 = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvBitAddresses).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvWordAddresses).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvMappings).BeginInit();
            SuspendLayout();
            // 
            // lblServer
            // 
            lblServer.Location = new Point(20, 20);
            lblServer.Name = "lblServer";
            lblServer.Size = new Size(60, 25);
            lblServer.TabIndex = 0;
            lblServer.Text = "服务器:";
            // 
            // cmbServers
            // 
            cmbServers.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbServers.Location = new Point(80, 20);
            cmbServers.Name = "cmbServers";
            cmbServers.Size = new Size(200, 25);
            cmbServers.TabIndex = 1;
            cmbServers.SelectedIndexChanged += CmbServers_SelectedIndexChanged;
            // 
            // lblProject
            // 
            lblProject.Location = new Point(300, 20);
            lblProject.Name = "lblProject";
            lblProject.Size = new Size(60, 25);
            lblProject.TabIndex = 2;
            lblProject.Text = "项目:";
            // 
            // cmbProjects
            // 
            cmbProjects.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProjects.Location = new Point(360, 20);
            cmbProjects.Name = "cmbProjects";
            cmbProjects.Size = new Size(200, 25);
            cmbProjects.TabIndex = 3;
            cmbProjects.SelectedIndexChanged += CmbProjects_SelectedIndexChanged;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(580, 20);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(100, 30);
            btnStart.TabIndex = 4;
            btnStart.Text = "启动监控";
            btnStart.Click += BtnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(690, 20);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 30);
            btnStop.TabIndex = 5;
            btnStop.Text = "停止监控";
            btnStop.Click += BtnStop_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(800, 20);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(80, 30);
            btnRefresh.TabIndex = 6;
            btnRefresh.Text = "刷新";
            btnRefresh.Click += BtnRefresh_Click;
            // 
            // btnViewHistory
            // 
            btnViewHistory.Location = new Point(890, 20);
            btnViewHistory.Name = "btnViewHistory";
            btnViewHistory.Size = new Size(100, 30);
            btnViewHistory.TabIndex = 7;
            btnViewHistory.Text = "历史记录";
            btnViewHistory.Click += BtnViewHistory_Click;
            // 
            // lblBits
            // 
            lblBits.Location = new Point(20, 60);
            lblBits.Name = "lblBits";
            lblBits.Size = new Size(150, 25);
            lblBits.TabIndex = 8;
            lblBits.Text = "Bit地址 (触发器):";
            // 
            // dgvBitAddresses
            // 
            dgvBitAddresses.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3, dataGridViewTextBoxColumn4, dataGridViewTextBoxColumn5, dataGridViewTextBoxColumn6 });
            dgvBitAddresses.Location = new Point(20, 90);
            dgvBitAddresses.Name = "dgvBitAddresses";
            dgvBitAddresses.ReadOnly = true;
            dgvBitAddresses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBitAddresses.Size = new Size(940, 200);
            dgvBitAddresses.TabIndex = 9;
            // 
            // lblWords
            // 
            lblWords.Location = new Point(20, 300);
            lblWords.Name = "lblWords";
            lblWords.Size = new Size(150, 25);
            lblWords.TabIndex = 10;
            lblWords.Text = "Word地址 (数据):";
            // 
            // dgvWordAddresses
            // 
            dgvWordAddresses.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn7, dataGridViewTextBoxColumn8, dataGridViewTextBoxColumn9, dataGridViewTextBoxColumn10, dataGridViewTextBoxColumn11, dataGridViewTextBoxColumn12 });
            dgvWordAddresses.Location = new Point(20, 330);
            dgvWordAddresses.Name = "dgvWordAddresses";
            dgvWordAddresses.ReadOnly = true;
            dgvWordAddresses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvWordAddresses.Size = new Size(940, 200);
            dgvWordAddresses.TabIndex = 11;
            // 
            // lblMappings
            // 
            lblMappings.Location = new Point(20, 540);
            lblMappings.Name = "lblMappings";
            lblMappings.Size = new Size(150, 25);
            lblMappings.TabIndex = 12;
            lblMappings.Text = "Bit-Word映射关系:";
            // 
            // dgvMappings
            // 
            dgvMappings.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn13, dataGridViewTextBoxColumn14, dataGridViewTextBoxColumn15, dataGridViewTextBoxColumn16, dataGridViewTextBoxColumn17, dataGridViewTextBoxColumn18, dataGridViewTextBoxColumn19 });
            dgvMappings.Location = new Point(20, 570);
            dgvMappings.Name = "dgvMappings";
            dgvMappings.ReadOnly = true;
            dgvMappings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMappings.Size = new Size(940, 150);
            dgvMappings.TabIndex = 13;
            // 
            // lblLog
            // 
            lblLog.Location = new Point(20, 730);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(100, 25);
            lblLog.TabIndex = 14;
            lblLog.Text = "监控日志:";
            // 
            // txtLog
            // 
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(20, 760);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(940, 150);
            txtLog.TabIndex = 15;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "地址ID";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "地址";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "描述";
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "当前值";
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.HeaderText = "最后变化";
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.HeaderText = "状态";
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.HeaderText = "地址ID";
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.HeaderText = "地址";
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.HeaderText = "描述";
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.HeaderText = "当前值";
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.HeaderText = "最后变化";
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.HeaderText = "状态";
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.HeaderText = "映射ID";
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.HeaderText = "Bit地址";
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.HeaderText = "Word地址";
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.HeaderText = "触发条件";
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.HeaderText = "动作";
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.HeaderText = "最后触发";
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.HeaderText = "状态";
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            // 
            // KepServerMonitorForm
            // 
            ClientSize = new Size(970, 699);
            Controls.Add(lblServer);
            Controls.Add(cmbServers);
            Controls.Add(lblProject);
            Controls.Add(cmbProjects);
            Controls.Add(btnStart);
            Controls.Add(btnStop);
            Controls.Add(btnRefresh);
            Controls.Add(btnViewHistory);
            Controls.Add(lblBits);
            Controls.Add(dgvBitAddresses);
            Controls.Add(lblWords);
            Controls.Add(dgvWordAddresses);
            Controls.Add(lblMappings);
            Controls.Add(dgvMappings);
            Controls.Add(lblLog);
            Controls.Add(txtLog);
            Name = "KepServerMonitorForm";
            Text = "KepServer EX 监控中心";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)dgvBitAddresses).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvWordAddresses).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvMappings).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadServers()
        {
            try
            {
                cmbServers!.Items.Clear();
                
                // 从KEPServer配置文件加载服务器信息
                var configPath = Path.Combine(Application.StartupPath, "Config", "KepServerConfig.xml");
                if (File.Exists(configPath))
                {
                    var xmlContent = File.ReadAllText(configPath);
                    var doc = XDocument.Parse(xmlContent);
                    
                    var channels = doc.Root?.Element("Channels");
                    if (channels != null)
                    {
                        foreach (var channel in channels.Elements("Channel"))
                        {
                            var devices = channel.Element("Devices");
                            if (devices != null)
                            {
                                foreach (var device in devices.Elements("Device"))
                                {
                                    var serverId = device.Attribute("Name")?.Value ?? "Unknown";
                                    var serverName = $"KepServer - {channel.Attribute("Name")?.Value ?? "Unknown Channel"} - {device.Attribute("Name")?.Value}";
                                    var protocolType = channel.Attribute("Driver")?.Value ?? "OPC";
                                    
                                    cmbServers.Items.Add(new { 
                                        ServerId = serverId, 
                                        ServerName = serverName, 
                                        ProtocolType = protocolType, 
                                        ConnectionStatus = "Unknown" 
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    AppendLog($"配置文件不存在: {configPath}");
                }

                if (cmbServers.Items.Count > 0)
                {
                    cmbServers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"加载服务器失败: {ex.Message}");
                Services.LoggingService.LogException("加载服务器列表失败", ex);
            }
        }

        private void LoadProjects(string serverId)
        {
            try
            {
                cmbProjects!.Items.Clear();
                var server = Services.HardwareService.GetServerById(serverId);
                if (server != null)
                {
                    foreach (var project in server.Projects)
                    {
                        cmbProjects.Items.Add(new { project.ProjectId, project.ProjectName });
                    }

                    if (cmbProjects.Items.Count > 0)
                    {
                        cmbProjects.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"加载项目失败: {ex.Message}");
                Services.LoggingService.LogException("加载项目列表失败", ex);
            }
        }

        private void LoadData(string serverId, string projectId)
        {
            try
            {
                // 加载Bit地址
                dgvBitAddresses!.Rows.Clear();
                var bitAddresses = Services.HardwareService.GetBitAddresses(serverId, projectId);
                foreach (var bit in bitAddresses)
                {
                    var rowIndex = dgvBitAddresses.Rows.Add(
                        bit.AddressId,
                        bit.Address,
                        bit.Description,
                        bit.CurrentValue.ToString(),
                        bit.LastChanged?.ToString("HH:mm:ss") ?? "",
                        bit.Status
                    );

                    // 根据状态设置颜色
                    if (bit.CurrentValue)
                    {
                        dgvBitAddresses.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        dgvBitAddresses.Rows[rowIndex].DefaultCellStyle.BackColor = Color.White;
                    }
                }

                // 加载Word地址
                dgvWordAddresses!.Rows.Clear();
                var wordAddresses = Services.HardwareService.GetWordAddresses(serverId, projectId);
                foreach (var word in wordAddresses)
                {
                    dgvWordAddresses.Rows.Add(
                        word.AddressId,
                        word.Address,
                        word.Description,
                        word.CurrentValue,
                        word.LastChanged?.ToString("HH:mm:ss") ?? "",
                        word.Status
                    );
                }

                // 加载映射关系
                dgvMappings!.Rows.Clear();
                var server = Services.HardwareService.GetServerById(serverId);
                var project = server?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                if (project != null)
                {
                    foreach (var group in project.DataGroups)
                    {
                        foreach (var mapping in group.BitWordMappings)
                        {
                            dgvMappings.Rows.Add(
                                mapping.MappingId,
                                mapping.BitAddressId,
                                mapping.WordAddressId,
                                mapping.TriggerCondition,
                                mapping.Action,
                                mapping.LastTriggered?.ToString("HH:mm:ss") ?? "",
                                mapping.Status
                            );
                        }
                    }
                }

                AppendLog($"已加载数据: {bitAddresses.Count} Bit, {wordAddresses.Count} Word, {project?.DataGroups.Sum(g => g.BitWordMappings.Count) ?? 0} 映射");
            }
            catch (Exception ex)
            {
                AppendLog($"加载数据失败: {ex.Message}");
                Services.LoggingService.LogException("加载监控数据失败", ex);
            }
        }

        private void AppendLog(string message)
        {
            if (txtLog != null)
            {
                var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
                txtLog.AppendText(logEntry + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        private void CmbServers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbServers!.SelectedItem != null)
            {
                var selectedServer = (dynamic)cmbServers.SelectedItem;
                selectedServerId = selectedServer.ServerId;
                LoadProjects(selectedServerId);
                AppendLog($"选择服务器: {selectedServer.ServerName} [协议: {selectedServer.ProtocolType}] ({selectedServer.ConnectionStatus})");
            }
        }

        private void CmbProjects_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbProjects!.SelectedItem != null)
            {
                var selectedProject = (dynamic)cmbProjects.SelectedItem;
                selectedProjectId = selectedProject.ProjectId;
                LoadData(selectedServerId, selectedProjectId);
                AppendLog($"选择项目: {selectedProject.ProjectName}");
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                await Services.HardwareService.StartMonitoringAsync();
                btnStart!.Enabled = false;
                btnStop!.Enabled = true;
                AppendLog("KepServer监控已启动");

                // 启动定时刷新
                refreshTimer = new System.Windows.Forms.Timer();
                refreshTimer.Interval = 1000;
                refreshTimer.Tick += (s, e) => RefreshData();
                refreshTimer.Start();
            }
            catch (Exception ex)
            {
                AppendLog($"启动监控失败: {ex.Message}");
                Services.LoggingService.LogException("启动KepServer监控失败", ex);
            }
        }

        private async void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                await Services.HardwareService.StopMonitoringAsync();
                btnStart!.Enabled = true;
                btnStop!.Enabled = false;
                AppendLog("KepServer监控已停止");

                refreshTimer?.Stop();
                refreshTimer?.Dispose();
                refreshTimer = null;
            }
            catch (Exception ex)
            {
                AppendLog($"停止监控失败: {ex.Message}");
                Services.LoggingService.LogException("停止KepServer监控失败", ex);
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            RefreshData();
        }

        private void BtnViewHistory_Click(object? sender, EventArgs e)
        {
            try
            {
                var historyForm = new KepServerHistoryForm(selectedServerId);
                historyForm.ShowDialog();
            }
            catch (Exception ex)
            {
                AppendLog($"打开历史记录失败: {ex.Message}");
                Services.LoggingService.LogException("打开KepServer历史记录失败", ex);
            }
        }

        private void RefreshData()
        {
            if (!string.IsNullOrEmpty(selectedServerId) && !string.IsNullOrEmpty(selectedProjectId))
            {
                LoadData(selectedServerId, selectedProjectId);

                // 显示统计信息
                var stats = Services.HardwareService.GetStatistics(selectedServerId);
                if (stats != null)
                {
                    AppendLog($"统计: Bit变化={stats.TotalBitChanges}, Word变化={stats.TotalWordChanges}, 映射触发={stats.TotalMappingTriggers}");
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            Services.LoggingService.Info("KepServer监控界面已关闭");
            base.OnFormClosed(e);
        }
    }
}
