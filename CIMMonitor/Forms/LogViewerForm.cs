using log4net;

namespace CIMMonitor.Forms
{
    public partial class LogViewerForm : Form
    {
        private ComboBox? cmbLogType;
        private TextBox? txtLog;
        private Button? btnRefresh;
        private Button? btnClear;
        private Button? btnOpenLogFile;

        public LogViewerForm()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "日志查看器";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            // 日志类型选择
            var lblType = new Label();
            lblType.Text = "日志类型:";
            lblType.Location = new Point(20, 20);
            lblType.Size = new Size(80, 25);
            this.Controls.Add(lblType);

            cmbLogType = new ComboBox();
            cmbLogType.Location = new Point(100, 20);
            cmbLogType.Size = new Size(150, 25);
            cmbLogType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLogType.Items.AddRange(new[]
            {
                "所有日志",
                "数据交互",
                "设备操作",
                "XML消息",
                "异常日志"
            });
            cmbLogType.SelectedIndex = 0;
            cmbLogType.SelectedIndexChanged += (s, e) => LoadLogs();
            this.Controls.Add(cmbLogType);

            // 刷新按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(270, 20);
            btnRefresh.Size = new Size(80, 25);
            btnRefresh.Click += (s, e) => LoadLogs();
            this.Controls.Add(btnRefresh);

            // 清空按钮
            btnClear = new Button();
            btnClear.Text = "清空显示";
            btnClear.Location = new Point(360, 20);
            btnClear.Size = new Size(100, 25);
            btnClear.Click += (s, e) => txtLog!.Clear();
            this.Controls.Add(btnClear);

            // 打开日志文件按钮
            btnOpenLogFile = new Button();
            btnOpenLogFile.Text = "打开日志文件";
            btnOpenLogFile.Location = new Point(470, 20);
            btnOpenLogFile.Size = new Size(120, 25);
            btnOpenLogFile.Click += BtnOpenLogFile_Click;
            this.Controls.Add(btnOpenLogFile);

            // 日志显示区域
            txtLog = new TextBox();
            txtLog.Location = new Point(20, 60);
            txtLog.Size = new Size(940, 580);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Both;
            txtLog.Font = new Font("Consolas", 9);
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.LightGray;
            this.Controls.Add(txtLog);

            this.ResumeLayout(false);
        }

        private void LoadLogs()
        {
            try
            {
                txtLog!.Clear();
                var logType = cmbLogType?.SelectedItem?.ToString() ?? "所有日志";
                var logFiles = GetLogFiles(logType);

                foreach (var filePath in logFiles)
                {
                    if (File.Exists(filePath))
                    {
                        txtLog.AppendText($"=== {Path.GetFileName(filePath)} ===\n");
                        var lines = File.ReadAllLines(filePath);
                        var recentLines = lines.TakeLast(100).ToArray();
                        txtLog.AppendText(string.Join(Environment.NewLine, recentLines));
                        txtLog.AppendText("\n\n");
                    }
                }

                Services.LoggingService.Info($"已加载日志查看器 - 类型: {logType}");
            }
            catch (Exception ex)
            {
                txtLog!.Text = $"加载日志失败: {ex.Message}";
                Services.LoggingService.LogException("加载日志文件失败", ex);
            }
        }

        private List<string> GetLogFiles(string logType)
        {
            var logDir = Path.Combine(Application.StartupPath, "logs");
            var files = new List<string>();

            var today = DateTime.Now.ToString("yyyy-MM-dd");

            switch (logType)
            {
                case "数据交互":
                    files.Add(Path.Combine(logDir, $"data_{today}.log"));
                    break;
                case "设备操作":
                    files.Add(Path.Combine(logDir, $"device_{today}.log"));
                    break;
                case "XML消息":
                    files.Add(Path.Combine(logDir, $"xml_{today}.log"));
                    break;
                case "异常日志":
                    files.Add(Path.Combine(logDir, $"error_{today}.log"));
                    break;
                default:
                    files.Add(Path.Combine(logDir, $"automation_{today}.log"));
                    files.Add(Path.Combine(logDir, $"data_{today}.log"));
                    files.Add(Path.Combine(logDir, $"device_{today}.log"));
                    files.Add(Path.Combine(logDir, $"xml_{today}.log"));
                    files.Add(Path.Combine(logDir, $"error_{today}.log"));
                    break;
            }

            return files;
        }

        private void BtnOpenLogFile_Click(object? sender, EventArgs e)
        {
            try
            {
                var logType = cmbLogType?.SelectedItem?.ToString() ?? "所有日志";
                var files = GetLogFiles(logType);
                var firstFile = files.FirstOrDefault(File.Exists);

                if (firstFile != null)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = firstFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("没有找到对应的日志文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开日志文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Services.LoggingService.LogException("打开日志文件失败", ex);
            }
        }

        private void LoadLogsOnTimer()
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) => LoadLogs();
            timer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadLogsOnTimer();
        }
    }
}
