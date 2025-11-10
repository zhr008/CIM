using System;
using System.Drawing;
using System.Windows.Forms;
using CIMMonitor.Models;

namespace CIMMonitor.Forms
{
    public partial class HsmsDeviceEditForm : Form
    {
        public HsmsDeviceConfig Device { get; private set; }

        public HsmsDeviceEditForm(HsmsDeviceConfig? device = null)
        {
            Device = device?.Clone() ?? new HsmsDeviceConfig();
            InitializeComponent();
            LoadDeviceData();
        }

        private void InitializeComponent()
        {
            //this.Text = Device.DeviceId == null ? "添加HSMS设备" : "编辑HSMS设备";
            this.Size = new Size(500, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            int y = 20;
            int labelWidth = 120;
            int textBoxWidth = 250;
            int leftMargin = 20;
            int rowHeight = 35;

            // 设备ID
            var lblDeviceId = new Label
            {
                Text = "设备ID:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblDeviceId);

            txtDeviceId = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23)
            };
            this.Controls.Add(txtDeviceId);
            y += rowHeight;

            // 设备名称
            var lblDeviceName = new Label
            {
                Text = "设备名称:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblDeviceName);

            txtDeviceName = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23)
            };
            this.Controls.Add(txtDeviceName);
            y += rowHeight;

            // 协议类型
            var lblProtocol = new Label
            {
                Text = "协议类型:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblProtocol);

            cmbProtocolType = new ComboBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbProtocolType.Items.Add("HSMS");
            cmbProtocolType.SelectedIndex = 0;
            this.Controls.Add(cmbProtocolType);
            y += rowHeight;

            // 主机地址
            var lblHost = new Label
            {
                Text = "主机地址:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblHost);

            txtHost = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                Text = "127.0.0.1"
            };
            this.Controls.Add(txtHost);
            y += rowHeight;

            // 端口
            var lblPort = new Label
            {
                Text = "端口:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPort);

            numPort = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                Minimum = 1,
                Maximum = 65535,
                Value = 5000
            };
            this.Controls.Add(numPort);
            y += rowHeight;

            // 设备ID值
            var lblDeviceIdValue = new Label
            {
                Text = "设备ID值:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblDeviceIdValue);

            numDeviceIdValue = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                Minimum = 1,
                Maximum = 255,
                Value = 1
            };
            this.Controls.Add(numDeviceIdValue);
            y += rowHeight;

            // 会话ID值
            var lblSessionIdValue = new Label
            {
                Text = "会话ID值:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblSessionIdValue);

            numSessionIdValue = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                Minimum = 0,
                Maximum = 65535,
                Value = 4660
            };
            this.Controls.Add(numSessionIdValue);
            y += rowHeight;

            // 连接超时
            var lblTimeout = new Label
            {
                Text = "连接超时(ms):",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblTimeout);

            numTimeout = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 23),
                Minimum = 1000,
                Maximum = 60000,
                Value = 5000
            };
            this.Controls.Add(numTimeout);
            y += rowHeight;

            // 自动连接
            var lblAutoConnect = new Label
            {
                Text = "自动连接:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblAutoConnect);

            chkAutoConnect = new CheckBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y + 5),
                Size = new Size(100, 18)
            };
            this.Controls.Add(chkAutoConnect);
            y += rowHeight;

            // 描述
            var lblDescription = new Label
            {
                Text = "描述:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblDescription);

            txtDescription = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(textBoxWidth, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(txtDescription);
            y += rowHeight + 10;

            // 按钮
            var buttonPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(480, 50)
            };

            btnOK = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(180, 10),
                DialogResult = DialogResult.OK,
                BackColor = Color.LightGreen
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(280, 10),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.LightGray
            };

            buttonPanel.Controls.Add(btnOK);
            buttonPanel.Controls.Add(btnCancel);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private TextBox txtDeviceId;
        private TextBox txtDeviceName;
        private ComboBox cmbProtocolType;
        private TextBox txtHost;
        private NumericUpDown numPort;
        private NumericUpDown numDeviceIdValue;
        private NumericUpDown numSessionIdValue;
        private NumericUpDown numTimeout;
        private CheckBox chkAutoConnect;
        private TextBox txtDescription;
        private Button btnOK;
        private Button btnCancel;

        private void LoadDeviceData()
        {
            txtDeviceId.Text = Device.DeviceId ?? "";
            txtDeviceName.Text = Device.DeviceName ?? "";
            cmbProtocolType.SelectedIndex = cmbProtocolType.Items.IndexOf(Device.ProtocolType ?? "HSMS");
            txtHost.Text = Device.Host ?? "127.0.0.1";
            numPort.Value = Device.Port;
            numDeviceIdValue.Value = Device.DeviceIdValue;
            numSessionIdValue.Value = Device.SessionIdValue;
            numTimeout.Value = Device.ConnectionTimeout;
            chkAutoConnect.Checked = Device.AutoConnect;
            txtDescription.Text = Device.Description ?? "";

            // 如果是编辑模式，设备ID不可修改
            if (!string.IsNullOrEmpty(Device.DeviceId))
            {
                txtDeviceId.Enabled = false;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtDeviceId.Text))
            {
                MessageBox.Show("请输入设备ID", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDeviceId.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                MessageBox.Show("请输入设备名称", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDeviceName.Focus();
                return;
            }

            // 保存数据
            Device.DeviceId = txtDeviceId.Text.Trim();
            Device.DeviceName = txtDeviceName.Text.Trim();
            Device.ProtocolType = cmbProtocolType.SelectedItem?.ToString() ?? "HSMS";
            Device.Host = txtHost.Text.Trim();
            Device.Port = (int)numPort.Value;
            Device.DeviceIdValue = (byte)numDeviceIdValue.Value;
            Device.SessionIdValue = (int)numSessionIdValue.Value;
            Device.ConnectionTimeout = (int)numTimeout.Value;
            Device.AutoConnect = chkAutoConnect.Checked;
            Device.Description = txtDescription.Text.Trim();
            Device.LastUpdated = DateTime.Now;
        }
    }

    /// <summary>
    /// HSMS设备配置扩展方法
    /// </summary>
    public static class HsmsDeviceConfigExtensions
    {
        public static HsmsDeviceConfig Clone(this HsmsDeviceConfig device)
        {
            return new HsmsDeviceConfig
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                ProtocolType = device.ProtocolType,
                Host = device.Host,
                Port = device.Port,
                DeviceIdValue = device.DeviceIdValue,
                SessionIdValue = device.SessionIdValue,
                Description = device.Description,
                AutoConnect = device.AutoConnect,
                ConnectionTimeout = device.ConnectionTimeout,
                LastUpdated = device.LastUpdated,
                IsConnected = device.IsConnected,
                LastConnectionTime = device.LastConnectionTime,
                Status = device.Status,
                MessageCount = device.MessageCount,
                ErrorMessage = device.ErrorMessage
            };
        }
    }
}
