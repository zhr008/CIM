using System;
using System.Drawing;
using System.Windows.Forms;

namespace HsmsSimulator
{
    public partial class CustomSendDialog : Form
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ushort Stream { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public byte Function { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Content { get; set; } = string.Empty;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool RequireResponse { get; set; }

        private TextBox streamTextBox;
        private TextBox functionTextBox;
        private TextBox contentTextBox;
        private CheckBox requireResponseCheckBox;
        private Button okButton;
        private Button cancelButton;

        public CustomSendDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "自定义发送消息";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;

            // Stream标签和文本框
            var streamLabel = new Label();
            streamLabel.Text = "Stream (S):";
            streamLabel.Location = new Point(20, 20);
            streamLabel.Size = new Size(100, 23);
            streamLabel.Font = new Font("Microsoft YaHei", 9F);

            streamTextBox = new TextBox();
            streamTextBox.Location = new Point(130, 20);
            streamTextBox.Size = new Size(100, 23);
            streamTextBox.Font = new Font("Microsoft YaHei", 9F);
            streamTextBox.Text = "1";

            // Function标签和文本框
            var functionLabel = new Label();
            functionLabel.Text = "Function (F):";
            functionLabel.Location = new Point(20, 60);
            functionLabel.Size = new Size(100, 23);
            functionLabel.Font = new Font("Microsoft YaHei", 9F);

            functionTextBox = new TextBox();
            functionTextBox.Location = new Point(130, 60);
            functionTextBox.Size = new Size(100, 23);
            functionTextBox.Font = new Font("Microsoft YaHei", 9F);
            functionTextBox.Text = "13";

            // Content标签和文本框
            var contentLabel = new Label();
            contentLabel.Text = "消息内容:";
            contentLabel.Location = new Point(20, 100);
            contentLabel.Size = new Size(100, 23);
            contentLabel.Font = new Font("Microsoft YaHei", 9F);

            contentTextBox = new TextBox();
            contentTextBox.Location = new Point(20, 125);
            contentTextBox.Size = new Size(400, 100);
            contentTextBox.Multiline = true;
            contentTextBox.ScrollBars = ScrollBars.Vertical;
            contentTextBox.Font = new Font("Microsoft YaHei", 9F);
            contentTextBox.Text = "CUSTOM_MESSAGE";

            // Require Response复选框
            requireResponseCheckBox = new CheckBox();
            requireResponseCheckBox.Text = "要求响应";
            requireResponseCheckBox.Location = new Point(20, 235);
            requireResponseCheckBox.Size = new Size(100, 24);
            requireResponseCheckBox.Font = new Font("Microsoft YaHei", 9F);

            // OK按钮
            okButton = new Button();
            okButton.Text = "确定";
            okButton.Location = new Point(230, 275);
            okButton.Size = new Size(75, 30);
            okButton.BackColor = SystemColors.Control;
            okButton.Font = new Font("Microsoft YaHei", 9F);
            okButton.Click += OkButton_Click;

            // Cancel按钮
            cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new Point(315, 275);
            cancelButton.Size = new Size(75, 30);
            cancelButton.BackColor = SystemColors.Control;
            cancelButton.Font = new Font("Microsoft YaHei", 9F);
            cancelButton.Click += CancelButton_Click;

            // 添加控件到窗体
            this.Controls.Add(streamLabel);
            this.Controls.Add(streamTextBox);
            this.Controls.Add(functionLabel);
            this.Controls.Add(functionTextBox);
            this.Controls.Add(contentLabel);
            this.Controls.Add(contentTextBox);
            this.Controls.Add(requireResponseCheckBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            // 设置AcceptButton和CancelButton
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 验证输入
                if (!ushort.TryParse(streamTextBox.Text, out var stream) || stream < 1 || stream > 99)
                {
                    MessageBox.Show("Stream必须是1-99之间的数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    streamTextBox.Focus();
                    return;
                }

                if (!byte.TryParse(functionTextBox.Text, out var function) || function < 1 || function > 255)
                {
                    MessageBox.Show("Function必须是1-255之间的数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    functionTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(contentTextBox.Text))
                {
                    MessageBox.Show("消息内容不能为空", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    contentTextBox.Focus();
                    return;
                }

                // 设置返回值
                Stream = stream;
                Function = function;
                Content = contentTextBox.Text.Trim();
                RequireResponse = requireResponseCheckBox.Checked;

                // 关闭对话框
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
