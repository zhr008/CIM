namespace CIMMonitor.Forms
{
    public partial class ProductionOrderForm : Form
    {
        private DataGridView? dgvOrders;

        public ProductionOrderForm()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "生产订单";
            this.Name = "ProductionOrderForm";
            this.ResumeLayout(false);

            dgvOrders = new DataGridView();
            dgvOrders.Location = new Point(20, 20);
            dgvOrders.Size = new Size(1200, 600);
            dgvOrders.ReadOnly = true;

            dgvOrders.Columns.Add("OrderId", "订单号");
            dgvOrders.Columns.Add("ProductCode", "产品代码");
            dgvOrders.Columns.Add("Quantity", "数量");
            dgvOrders.Columns.Add("CompletedQuantity", "完成数量");
            dgvOrders.Columns.Add("Progress", "进度(%)");
            dgvOrders.Columns.Add("StartTime", "开始时间");
            dgvOrders.Columns.Add("EndTime", "结束时间");
            dgvOrders.Columns.Add("Status", "状态");
            dgvOrders.Columns.Add("LineId", "生产线");

            dgvOrders.Columns[0].Width = 120;
            dgvOrders.Columns[1].Width = 120;
            dgvOrders.Columns[2].Width = 100;
            dgvOrders.Columns[3].Width = 100;
            dgvOrders.Columns[4].Width = 100;
            dgvOrders.Columns[5].Width = 150;
            dgvOrders.Columns[6].Width = 150;
            dgvOrders.Columns[7].Width = 100;
            dgvOrders.Columns[8].Width = 100;

            this.Controls.Add(dgvOrders);
        }

        private void LoadOrders()
        {
            var orders = Services.ProductionService.GetAllProductionOrders();
            dgvOrders!.Rows.Clear();

            foreach (var order in orders)
            {
                var progress = order.Quantity > 0 ? (order.CompletedQuantity / order.Quantity * 100) : 0;
                var rowIndex = dgvOrders.Rows.Add(
                    order.OrderId,
                    order.ProductCode,
                    order.Quantity,
                    order.CompletedQuantity,
                    progress.ToString("F2"),
                    order.StartTime.ToString("yyyy-MM-dd HH:mm"),
                    order.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    order.Status,
                    order.LineId
                );

                if (order.Status == "已完成")
                {
                    dgvOrders.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else if (order.Status == "生产中")
                {
                    dgvOrders.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
                }
                else if (order.Status == "等待中")
                {
                    dgvOrders.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Yellow;
                }
            }
        }
    }
}
