using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIMMonitor.Forms
{
    public partial class Monitor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            dgvDevices = new DataGridView();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new DataGridViewTextBoxColumn();
            dataGridViewCheckBoxColumn8 = new DataGridViewCheckBoxColumn();
            dataGridViewTextBoxColumn9 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            btnRefresh = new Button();
            btnStart = new Button();
            btnStop = new Button();
            btnRestart = new Button();
            btnClearLog = new Button();
            btnConnect = new Button();
            btnDisconnect = new Button();
            lblInfo = new Label();
            txtInfo = new TextBox();
            btnTestMessage = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvDevices).BeginInit();
            SuspendLayout();
            // 
            // dgvDevices
            // 
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDevices.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn3, dataGridViewTextBoxColumn4, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn5, dataGridViewTextBoxColumn6, dataGridViewTextBoxColumn7, dataGridViewCheckBoxColumn8, dataGridViewTextBoxColumn9, dataGridViewTextBoxColumn10, dataGridViewTextBoxColumn11, dataGridViewTextBoxColumn12, dataGridViewTextBoxColumn1 });
            dgvDevices.Location = new Point(18, 21);
            dgvDevices.Name = "dgvDevices";
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.RowHeadersWidth = 62;
            dgvDevices.RowTemplate.Height = 33;
            dgvDevices.Size = new Size(1225, 312);
            dgvDevices.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "设备ID";
            dataGridViewTextBoxColumn3.MinimumWidth = 8;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.Width = 150;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "设备名称";
            dataGridViewTextBoxColumn4.MinimumWidth = 8;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.Width = 200;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.HeaderText = "IP地址";
            dataGridViewTextBoxColumn6.MinimumWidth = 8;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.Width = 150;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.HeaderText = "端口";
            dataGridViewTextBoxColumn7.MinimumWidth = 8;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.Width = 80;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "协议类型";
            dataGridViewTextBoxColumn2.MinimumWidth = 8;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.Width = 100;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.HeaderText = "设备类型";
            dataGridViewTextBoxColumn5.MinimumWidth = 8;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.Width = 100;
            // 
            // dataGridViewCheckBoxColumn8
            // 
            dataGridViewCheckBoxColumn8.HeaderText = "启用";
            dataGridViewCheckBoxColumn8.MinimumWidth = 8;
            dataGridViewCheckBoxColumn8.Name = "dataGridViewCheckBoxColumn8";
            dataGridViewCheckBoxColumn8.Width = 60;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.HeaderText = "在线状态";
            dataGridViewTextBoxColumn9.MinimumWidth = 8;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.Width = 80;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.HeaderText = "心跳";
            dataGridViewTextBoxColumn10.MinimumWidth = 8;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.Width = 60;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.HeaderText = "响应时间";
            dataGridViewTextBoxColumn11.MinimumWidth = 8;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.Width = 80;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.HeaderText = "连接质量";
            dataGridViewTextBoxColumn12.MinimumWidth = 8;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.Width = 80;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "配置文件";
            dataGridViewTextBoxColumn1.MinimumWidth = 8;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.Width = 150;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(17, 350);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(105, 37);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "刷新数据";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(139, 350);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(105, 37);
            btnStart.TabIndex = 2;
            btnStart.Text = "启动监控";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(261, 350);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(105, 37);
            btnStop.TabIndex = 3;
            btnStop.Text = "停止监控";
            btnStop.UseVisualStyleBackColor = true;
            // 
            // btnRestart
            // 
            btnRestart.Location = new Point(384, 350);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(105, 37);
            btnRestart.TabIndex = 4;
            btnRestart.Text = "重启监控";
            btnRestart.UseVisualStyleBackColor = true;
            // 
            // btnClearLog
            // 
            btnClearLog.Location = new Point(507, 350);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(105, 37);
            btnClearLog.TabIndex = 5;
            btnClearLog.Text = "清理日志";
            btnClearLog.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(630, 350);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(105, 37);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "连接设备";
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new Point(753, 350);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(105, 37);
            btnDisconnect.TabIndex = 7;
            btnDisconnect.Text = "断开设备";
            btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(1012, 360);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(0, 25);
            lblInfo.TabIndex = 8;
            // 
            // txtInfo
            // 
            txtInfo.Location = new Point(18, 393);
            txtInfo.Multiline = true;
            txtInfo.Name = "txtInfo";
            txtInfo.ScrollBars = ScrollBars.Vertical;
            txtInfo.Size = new Size(1226, 393);
            txtInfo.TabIndex = 9;
            // 
            // btnTestMessage
            // 
            btnTestMessage.Location = new Point(876, 350);
            btnTestMessage.Name = "btnTestMessage";
            btnTestMessage.Size = new Size(120, 37);
            btnTestMessage.TabIndex = 10;
            btnTestMessage.Text = "测试消息";
            btnTestMessage.UseVisualStyleBackColor = true;
            // 
            // Monitor
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1260, 808);
            Controls.Add(btnTestMessage);
            Controls.Add(txtInfo);
            Controls.Add(lblInfo);
            Controls.Add(btnDisconnect);
            Controls.Add(btnConnect);
            Controls.Add(btnClearLog);
            Controls.Add(btnRestart);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(btnRefresh);
            Controls.Add(dgvDevices);
            Name = "Monitor";
            Text = "设备监控 - 统一设备监控";
            ((System.ComponentModel.ISupportInitialize)dgvDevices).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvDevices;
        private Button btnRefresh;
        private Button btnStart;
        private Button btnStop;
        private Button btnRestart;
        private Button btnClearLog;
        private Button btnConnect;
        private Button btnDisconnect;
        private Label lblInfo;
        private TextBox txtInfo;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn8;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private Button btnTestMessage;
    }
}