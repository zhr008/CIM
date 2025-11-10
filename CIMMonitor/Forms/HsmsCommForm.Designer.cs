namespace CIMMonitor.Forms
{
    partial class HsmsCommForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblDeviceInfo = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblLog = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            //
            // lblDeviceInfo
            //
            this.lblDeviceInfo.Location = new System.Drawing.Point(20, 15);
            this.lblDeviceInfo.Name = "lblDeviceInfo";
            this.lblDeviceInfo.Size = new System.Drawing.Size(400, 60);
            this.lblDeviceInfo.TabIndex = 0;
            this.lblDeviceInfo.Text = "设备信息";
            //
            // btnConnect
            //
            this.btnConnect.Location = new System.Drawing.Point(440, 20);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(100, 35);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.BtnConnect_Click);
            //
            // lblMessage
            //
            this.lblMessage.Location = new System.Drawing.Point(20, 85);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(150, 25);
            this.lblMessage.TabIndex = 2;
            this.lblMessage.Text = "HSMS消息 (S{stream}F{function}:{content}):";
            //
            // txtMessage
            //
            this.txtMessage.Location = new System.Drawing.Point(20, 115);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(400, 25);
            this.txtMessage.TabIndex = 3;
            this.txtMessage.Text = "S1F13:ARE_YOU_THERE";
            //
            // btnSend
            //
            this.btnSend.Location = new System.Drawing.Point(440, 115);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 25);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.BtnSend_Click);
            //
            // lblLog
            //
            this.lblLog.Location = new System.Drawing.Point(20, 150);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(100, 25);
            this.lblLog.TabIndex = 5;
            this.lblLog.Text = "消息日志:";
            //
            // txtLog
            //
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new System.Drawing.Point(20, 180);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(540, 350);
            this.txtLog.TabIndex = 6;
            //
            // HsmsCommForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 550);
            this.Controls.Add(this.lblDeviceInfo);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.txtLog);
            this.Name = "HsmsCommForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HSMS 通信 - 半导体设备消息交互";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblDeviceInfo;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.TextBox txtLog;
    }
}
