namespace CIMMonitor.Forms
{
    public partial class ProductionDataForm : Form
    {
        private DataGridView? dgvProduction;
        private TabControl? tabControl;
        private Button? btnRefresh;
        private ComboBox? cmbDeviceFilter;
        private Label? lblDeviceFilter;

        public ProductionDataForm()
        {
            InitializeComponent();
            LoadProductionData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "生产数据";
            this.Name = "ProductionDataForm";
            this.ResumeLayout(false);

            tabControl = new TabControl();
            tabControl.Location = new Point(20, 50);
            tabControl.Size = new Size(1200, 550);

            var tabGrid = new TabPage("数据表格");
            dgvProduction = new DataGridView();
            dgvProduction.Location = new Point(10, 10);
            dgvProduction.Size = new Size(1160, 500);
            dgvProduction.ReadOnly = true;
            dgvProduction.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvProduction.Columns.Add("Id", "序号");
            dgvProduction.Columns.Add("LineId", "生产线ID");
            dgvProduction.Columns.Add("DeviceId", "设备ID");
            dgvProduction.Columns.Add("Temperature", "温度(°C)");
            dgvProduction.Columns.Add("Pressure", "压力(MPa)");
            dgvProduction.Columns.Add("FlowRate", "流量(L/min)");
            dgvProduction.Columns.Add("Timestamp", "时间戳");
            dgvProduction.Columns.Add("Status", "状态");

            dgvProduction.Columns[0].Width = 60;
            dgvProduction.Columns[1].Width = 100;
            dgvProduction.Columns[2].Width = 100;
            dgvProduction.Columns[3].Width = 100;
            dgvProduction.Columns[4].Width = 100;
            dgvProduction.Columns[5].Width = 120;
            dgvProduction.Columns[6].Width = 150;
            dgvProduction.Columns[7].Width = 100;

            tabGrid.Controls.Add(dgvProduction);
            tabControl.TabPages.Add(tabGrid);

            var tabChart = new TabPage("趋势图");
            var label = new Label();
            label.Text = "生产数据趋势图\n\n功能开发中...\n将显示温度、压力、流量的实时趋势";
            label.Location = new Point(50, 50);
            label.Size = new Size(400, 200);
            label.Font = new Font("微软雅黑", 9F);
            tabChart.Controls.Add(label);
            tabControl.TabPages.Add(tabChart);

            this.Controls.Add(tabControl);

            // 筛选条件
            lblDeviceFilter = new Label();
            lblDeviceFilter.Text = "设备筛选:";
            lblDeviceFilter.Location = new Point(20, 20);
            lblDeviceFilter.Size = new Size(80, 25);
            this.Controls.Add(lblDeviceFilter);

            cmbDeviceFilter = new ComboBox();
            cmbDeviceFilter.Location = new Point(100, 17);
            cmbDeviceFilter.Size = new Size(150, 25);
            cmbDeviceFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDeviceFilter.Items.AddRange(new object[] { "全部设备", "PLC001", "PLC002", "MOTOR01", "SENSOR01" });
            cmbDeviceFilter.SelectedIndex = 0;
            cmbDeviceFilter.SelectedIndexChanged += (s, e) => LoadProductionData();
            this.Controls.Add(cmbDeviceFilter);

            // 按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新数据";
            btnRefresh.Location = new Point(270, 15);
            btnRefresh.Size = new Size(100, 30);
            btnRefresh.Click += (s, e) => LoadProductionData();
            this.Controls.Add(btnRefresh);
        }

        private void LoadProductionData()
        {
            var devices = Services.ProductionService.GetAllProductionData();
            var selectedDevice = cmbDeviceFilter!.SelectedItem?.ToString();

            if (selectedDevice != "全部设备")
            {
                devices = devices.Where(p => p.DeviceId == selectedDevice).ToList();
            }

            dgvProduction!.Rows.Clear();
            foreach (var item in devices)
            {
                dgvProduction.Rows.Add(
                    item.Id,
                    item.LineId,
                    item.DeviceId,
                    item.Temperature.ToString("F2"),
                    item.Pressure.ToString("F2"),
                    item.FlowRate.ToString("F2"),
                    item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.Status
                );
            }
        }
    }
}
