using System;
using System.Drawing;
using System.Windows.Forms;

namespace HsmsSimulator
{
    public partial class TimeoutSettingsDialog : Form
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public int T3Timeout { get; set; } = 45; // 回复超时 (秒)

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public int T5Timeout { get; set; } = 10; // 分隔超时 (秒)

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public int T6Timeout { get; set; } = 5;  // 控制超时 (秒)

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public int T7Timeout { get; set; } = 10; // 未选择超时 (秒)

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public int T8Timeout { get; set; } = 5;  // 网络超时 (秒)

        private TextBox t3TextBox;
        private TextBox t5TextBox;
        private TextBox t6TextBox;
        private TextBox t7TextBox;
        private TextBox t8TextBox;
        private Button okButton;
        private Button cancelButton;

        public TimeoutSettingsDialog()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "超时设置";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;

            int y = 20;
            int labelWidth = 150;
            int textBoxWidth = 100;
            int spacing = 40;

            // T3 - Reply Timeout
            CreateTimeoutRow("T3 - Reply Timeout (秒):", ref y, ref t3TextBox, spacing, labelWidth, textBoxWidth);

            // T5 - Separation Timeout
            CreateTimeoutRow("T5 - Separation Timeout (秒):", ref y, ref t5TextBox, spacing, labelWidth, textBoxWidth);

            // T6 - Control Timeout
            CreateTimeoutRow("T6 - Control Timeout (秒):", ref y, ref t6TextBox, spacing, labelWidth, textBoxWidth);

            // T7 - Not Selected Timeout
            CreateTimeoutRow("T7 - Not Selected Timeout (秒):", ref y, ref t7TextBox, spacing, labelWidth, textBoxWidth);

            // T8 - Network Timeout
            CreateTimeoutRow("T8 - Network Timeout (秒):", ref y, ref t8TextBox, spacing, labelWidth, textBoxWidth);

            // 按钮
            okButton = new Button();
            okButton.Text = "确定";
            okButton.Size = new Size(75, 30);
            okButton.Location = new Point(200, y + 20);
            okButton.BackColor = SystemColors.Control;
            okButton.Font = new Font("Microsoft YaHei", 9F);
            okButton.Click += OkButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Size = new Size(75, 30);
            cancelButton.Location = new Point(285, y + 20);
            cancelButton.BackColor = SystemColors.Control;
            cancelButton.Font = new Font("Microsoft YaHei", 9F);
            cancelButton.Click += CancelButton_Click;

            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void CreateTimeoutRow(string labelText, ref int y, ref TextBox textBox, int spacing, int labelWidth, int textBoxWidth)
        {
            var label = new Label();
            label.Text = labelText;
            label.Location = new Point(20, y);
            label.Size = new Size(labelWidth, 23);
            label.Font = new Font("Microsoft YaHei", 9F);

            textBox = new TextBox();
            textBox.Location = new Point(180, y);
            textBox.Size = new Size(textBoxWidth, 23);
            textBox.Font = new Font("Microsoft YaHei", 9F);
            textBox.KeyPress += TextBox_KeyPress;

            this.Controls.Add(label);
            this.Controls.Add(textBox);

            y += spacing;
        }

        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // 只允许输入数字和退格键
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void LoadCurrentSettings()
        {
            t3TextBox.Text = T3Timeout.ToString();
            t5TextBox.Text = T5Timeout.ToString();
            t6TextBox.Text = T6Timeout.ToString();
            t7TextBox.Text = T7Timeout.ToString();
            t8TextBox.Text = T8Timeout.ToString();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (int.TryParse(t3TextBox.Text, out var t3) && t3 > 0)
                    T3Timeout = t3;
                else
                {
                    MessageBox.Show("T3超时必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    t3TextBox.Focus();
                    return;
                }

                if (int.TryParse(t5TextBox.Text, out var t5) && t5 > 0)
                    T5Timeout = t5;
                else
                {
                    MessageBox.Show("T5超时必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    t5TextBox.Focus();
                    return;
                }

                if (int.TryParse(t6TextBox.Text, out var t6) && t6 > 0)
                    T6Timeout = t6;
                else
                {
                    MessageBox.Show("T6超时必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    t6TextBox.Focus();
                    return;
                }

                if (int.TryParse(t7TextBox.Text, out var t7) && t7 > 0)
                    T7Timeout = t7;
                else
                {
                    MessageBox.Show("T7超时必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    t7TextBox.Focus();
                    return;
                }

                if (int.TryParse(t8TextBox.Text, out var t8) && t8 > 0)
                    T8Timeout = t8;
                else
                {
                    MessageBox.Show("T8超时必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    t8TextBox.Focus();
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置超时值时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
