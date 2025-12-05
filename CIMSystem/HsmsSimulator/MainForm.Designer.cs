namespace HsmsSimulator
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            toolStrip = new ToolStrip();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            toolStripSeparator1 = new ToolStripSeparator();
            messageCountLabel = new ToolStripStatusLabel();
            toolStripSeparator2 = new ToolStripSeparator();
            connectionCountLabel = new ToolStripStatusLabel();
            toolStripSeparator3 = new ToolStripSeparator();
            deviceCountLabel = new ToolStripStatusLabel();
            toolStripSeparator4 = new ToolStripSeparator();
            heartbeatLabel = new ToolStripStatusLabel();
            heartbeatIndicator = new ToolStripStatusLabel();
            toolStripSeparator5 = new ToolStripSeparator();
            versionLabel = new ToolStripStatusLabel();
            mainPanel = new Panel();
            messagesPanel = new Panel();
            clearMessagesButton = new Button();
            copyMessagesButton = new Button();
            messagesLabel = new Label();
            filterAutoMessagesCheckBox = new CheckBox();
            messagesListView = new ListView();
            messagesColumnHeader1 = new ColumnHeader();
            messagesColumnHeader2 = new ColumnHeader();
            messagesColumnHeader3 = new ColumnHeader();
            messagesColumnHeader4 = new ColumnHeader();
            quickCommandPanel = new Panel();
            quickCommandLabel = new Label();
            customSendButton = new Button();
            quickCommandTreeView = new TreeView();
            devicesPanel = new Panel();
            devicesLabel = new Label();
            devicesListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            contentPanel = new Panel();
            detailsLabel = new Label();
            detailsTabControl = new TabControl();
            rawDataTabPage = new TabPage();
            rawDataTextBox = new TextBox();
            asciiTabPage = new TabPage();
            asciiTextBox = new TextBox();
            hexTabPage = new TabPage();
            hexTextBox = new TextBox();
            xmlTabPage = new TabPage();
            xmlTextBox = new TextBox();
            structureTabPage = new TabPage();
            structureTreeView = new TreeView();
            modeLabel = new ToolStripLabel();
            modeComboBox = new ToolStripComboBox();
            connectButton = new ToolStripButton();
            portLabel = new ToolStripLabel();
            portTextBox = new ToolStripTextBox();
            hostLabel = new ToolStripLabel();
            hostTextBox = new ToolStripTextBox();
            nameLabel = new ToolStripLabel();
            nameTextBox = new ToolStripTextBox();
            settingsButton = new ToolStripButton();
            statusStrip.SuspendLayout();
            mainPanel.SuspendLayout();
            messagesPanel.SuspendLayout();
            quickCommandPanel.SuspendLayout();
            devicesPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            detailsTabControl.SuspendLayout();
            rawDataTabPage.SuspendLayout();
            asciiTabPage.SuspendLayout();
            hexTabPage.SuspendLayout();
            xmlTabPage.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(20, 20);
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1134, 25);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip";
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, toolStripSeparator1, messageCountLabel, toolStripSeparator2, connectionCountLabel, toolStripSeparator3, deviceCountLabel, toolStripSeparator4, heartbeatLabel, heartbeatIndicator, toolStripSeparator5, versionLabel });
            statusStrip.Location = new Point(0, 858);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1134, 23);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(32, 18);
            statusLabel.Text = "就绪";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 23);
            // 
            // messageCountLabel
            // 
            messageCountLabel.Name = "messageCountLabel";
            messageCountLabel.Size = new Size(46, 18);
            messageCountLabel.Text = "消息: 0";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 23);
            // 
            // connectionCountLabel
            // 
            connectionCountLabel.Name = "connectionCountLabel";
            connectionCountLabel.Size = new Size(46, 18);
            connectionCountLabel.Text = "连接: 0";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 23);
            // 
            // deviceCountLabel
            // 
            deviceCountLabel.Name = "deviceCountLabel";
            deviceCountLabel.Size = new Size(46, 18);
            deviceCountLabel.Text = "设备: 3";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 23);
            // 
            // heartbeatLabel
            // 
            heartbeatLabel.Name = "heartbeatLabel";
            heartbeatLabel.Size = new Size(75, 18);
            heartbeatLabel.Text = "心跳: 检测中";
            // 
            // heartbeatIndicator
            // 
            heartbeatIndicator.Name = "heartbeatIndicator";
            heartbeatIndicator.Size = new Size(0, 18);
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 23);
            // 
            // versionLabel
            // 
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new Size(129, 18);
            versionLabel.Text = "HSMS Simulator v1.0";
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.White;
            mainPanel.Controls.Add(messagesPanel);
            mainPanel.Controls.Add(quickCommandPanel);
            mainPanel.Controls.Add(devicesPanel);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 25);
            mainPanel.Name = "mainPanel";
            mainPanel.Padding = new Padding(10);
            mainPanel.Size = new Size(1134, 817);
            mainPanel.TabIndex = 1;
            // 
            // messagesPanel
            // 
            messagesPanel.BackColor = Color.White;
            messagesPanel.BorderStyle = BorderStyle.FixedSingle;
            messagesPanel.Controls.Add(clearMessagesButton);
            messagesPanel.Controls.Add(copyMessagesButton);
            messagesPanel.Controls.Add(messagesLabel);
            messagesPanel.Controls.Add(filterAutoMessagesCheckBox);
            messagesPanel.Controls.Add(messagesListView);
            messagesPanel.Location = new Point(363, 10);
            messagesPanel.Name = "messagesPanel";
            messagesPanel.Padding = new Padding(10);
            messagesPanel.Size = new Size(749, 297);
            messagesPanel.TabIndex = 3;
            // 
            // clearMessagesButton
            // 
            clearMessagesButton.Location = new Point(567, 6);
            clearMessagesButton.Name = "clearMessagesButton";
            clearMessagesButton.Size = new Size(75, 25);
            clearMessagesButton.TabIndex = 1;
            clearMessagesButton.Text = "清空";
            clearMessagesButton.UseVisualStyleBackColor = true;
            clearMessagesButton.Click += clearMessagesButton_Click;
            // 
            // copyMessagesButton
            // 
            copyMessagesButton.Location = new Point(648, 6);
            copyMessagesButton.Name = "copyMessagesButton";
            copyMessagesButton.Size = new Size(75, 25);
            copyMessagesButton.TabIndex = 2;
            copyMessagesButton.Text = "复制";
            copyMessagesButton.UseVisualStyleBackColor = true;
            copyMessagesButton.Click += copyMessagesButton_Click;
            // 
            // messagesLabel
            // 
            messagesLabel.AutoSize = true;
            messagesLabel.Location = new Point(20, 20);
            messagesLabel.Name = "messagesLabel";
            messagesLabel.Size = new Size(56, 17);
            messagesLabel.TabIndex = 0;
            messagesLabel.Text = "消息列表";
            // 
            // filterAutoMessagesCheckBox
            // 
            filterAutoMessagesCheckBox.Checked = true;
            filterAutoMessagesCheckBox.CheckState = CheckState.Checked;
            filterAutoMessagesCheckBox.Location = new Point(432, 7);
            filterAutoMessagesCheckBox.Name = "filterAutoMessagesCheckBox";
            filterAutoMessagesCheckBox.Size = new Size(117, 24);
            filterAutoMessagesCheckBox.TabIndex = 3;
            filterAutoMessagesCheckBox.Text = "过滤自动消息";
            filterAutoMessagesCheckBox.UseVisualStyleBackColor = true;
            filterAutoMessagesCheckBox.CheckedChanged += filterAutoMessagesCheckBox_CheckedChanged;
            // 
            // messagesListView
            // 
            messagesListView.BorderStyle = BorderStyle.FixedSingle;
            messagesListView.Columns.AddRange(new ColumnHeader[] { messagesColumnHeader1, messagesColumnHeader2, messagesColumnHeader3, messagesColumnHeader4 });
            messagesListView.FullRowSelect = true;
            messagesListView.GridLines = true;
            messagesListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            messagesListView.Location = new Point(10, 36);
            messagesListView.Name = "messagesListView";
            messagesListView.Size = new Size(727, 246);
            messagesListView.TabIndex = 1;
            messagesListView.UseCompatibleStateImageBehavior = false;
            messagesListView.View = View.Details;
            // 
            // messagesColumnHeader1
            // 
            messagesColumnHeader1.Text = "时间";
            messagesColumnHeader1.Width = 100;
            // 
            // messagesColumnHeader2
            // 
            messagesColumnHeader2.Text = "方向";
            messagesColumnHeader2.Width = 80;
            // 
            // messagesColumnHeader3
            // 
            messagesColumnHeader3.Text = "消息类型";
            messagesColumnHeader3.Width = 100;
            // 
            // messagesColumnHeader4
            // 
            messagesColumnHeader4.Text = "内容";
            messagesColumnHeader4.Width = 450;
            // 
            // quickCommandPanel
            // 
            quickCommandPanel.BackColor = Color.White;
            quickCommandPanel.BorderStyle = BorderStyle.FixedSingle;
            quickCommandPanel.Controls.Add(quickCommandLabel);
            quickCommandPanel.Controls.Add(customSendButton);
            quickCommandPanel.Controls.Add(quickCommandTreeView);
            quickCommandPanel.Location = new Point(10, 201);
            quickCommandPanel.Name = "quickCommandPanel";
            quickCommandPanel.Padding = new Padding(10);
            quickCommandPanel.Size = new Size(338, 609);
            quickCommandPanel.TabIndex = 0;
            // 
            // quickCommandLabel
            //
            quickCommandLabel.AutoSize = true;
            quickCommandLabel.Location = new Point(10, 10);
            quickCommandLabel.Name = "quickCommandLabel";
            quickCommandLabel.Size = new Size(84, 17);
            quickCommandLabel.TabIndex = 0;
            quickCommandLabel.Text = "快捷命令 (双击发送)";
            // 
            // customSendButton
            // 
            customSendButton.Location = new Point(600, 7);
            customSendButton.Name = "customSendButton";
            customSendButton.Size = new Size(100, 25);
            customSendButton.TabIndex = 2;
            customSendButton.Text = "自定义发送";
            customSendButton.UseVisualStyleBackColor = true;
            customSendButton.Click += customSendButton_Click;
            // 
            // quickCommandTreeView
            // 
            quickCommandTreeView.BorderStyle = BorderStyle.FixedSingle;
            quickCommandTreeView.Font = new Font("微软雅黑", 9F);
            quickCommandTreeView.FullRowSelect = true;
            quickCommandTreeView.ItemHeight = 22;
            quickCommandTreeView.Location = new Point(10, 30);
            quickCommandTreeView.Name = "quickCommandTreeView";
            quickCommandTreeView.Size = new Size(316, 567);
            quickCommandTreeView.TabIndex = 1;
            quickCommandTreeView.NodeMouseDoubleClick += quickCommandTreeView_NodeMouseDoubleClick;
            // 
            // devicesPanel
            // 
            devicesPanel.BackColor = Color.White;
            devicesPanel.BorderStyle = BorderStyle.FixedSingle;
            devicesPanel.Controls.Add(devicesLabel);
            devicesPanel.Controls.Add(devicesListView);
            devicesPanel.Location = new Point(10, 10);
            devicesPanel.Name = "devicesPanel";
            devicesPanel.Padding = new Padding(10);
            devicesPanel.Size = new Size(338, 185);
            devicesPanel.TabIndex = 0;
            // 
            // devicesLabel
            // 
            devicesLabel.AutoSize = true;
            devicesLabel.Location = new Point(10, 3);
            devicesLabel.Name = "devicesLabel";
            devicesLabel.Size = new Size(56, 17);
            devicesLabel.TabIndex = 0;
            devicesLabel.Text = "设备状态";
            // 
            // devicesListView
            // 
            devicesListView.BorderStyle = BorderStyle.FixedSingle;
            devicesListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            devicesListView.Enabled = false;
            devicesListView.FullRowSelect = true;
            devicesListView.GridLines = true;
            devicesListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            devicesListView.Location = new Point(10, 23);
            devicesListView.Name = "devicesListView";
            devicesListView.Size = new Size(316, 152);
            devicesListView.TabIndex = 1;
            devicesListView.UseCompatibleStateImageBehavior = false;
            devicesListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "设备ID";
            columnHeader1.Width = 80;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "设备名称";
            columnHeader2.Width = 90;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "状态";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "描述";
            columnHeader4.Width = 150;
            // 
            // contentPanel
            // 
            contentPanel.BackColor = Color.White;
            contentPanel.BorderStyle = BorderStyle.FixedSingle;
            contentPanel.Controls.Add(detailsLabel);
            contentPanel.Controls.Add(detailsTabControl);
            contentPanel.Location = new Point(363, 324);
            contentPanel.Name = "contentPanel";
            contentPanel.Padding = new Padding(10);
            contentPanel.Size = new Size(749, 486);
            contentPanel.TabIndex = 2;
            // 
            // detailsLabel
            // 
            detailsLabel.AutoSize = true;
            detailsLabel.Location = new Point(10, 10);
            detailsLabel.Name = "detailsLabel";
            detailsLabel.Size = new Size(56, 17);
            detailsLabel.TabIndex = 0;
            detailsLabel.Text = "消息详情";
            // 
            // detailsTabControl
            // 
            detailsTabControl.Controls.Add(rawDataTabPage);
            detailsTabControl.Controls.Add(asciiTabPage);
            detailsTabControl.Controls.Add(hexTabPage);
            detailsTabControl.Controls.Add(xmlTabPage);
            detailsTabControl.Controls.Add(structureTabPage);
            detailsTabControl.Font = new Font("微软雅黑", 9F);
            detailsTabControl.Location = new Point(10, 30);
            detailsTabControl.Name = "detailsTabControl";
            detailsTabControl.SelectedIndex = 0;
            detailsTabControl.Size = new Size(727, 444);
            detailsTabControl.TabIndex = 1;
            // 
            // rawDataTabPage
            // 
            rawDataTabPage.Controls.Add(rawDataTextBox);
            rawDataTabPage.Location = new Point(4, 26);
            rawDataTabPage.Name = "rawDataTabPage";
            rawDataTabPage.Padding = new Padding(3);
            rawDataTabPage.Size = new Size(719, 414);
            rawDataTabPage.TabIndex = 0;
            rawDataTabPage.Text = "原始数据";
            rawDataTabPage.UseVisualStyleBackColor = true;
            // 
            // rawDataTextBox
            // 
            rawDataTextBox.Font = new Font("Consolas", 9F);
            rawDataTextBox.Location = new Point(6, 6);
            rawDataTextBox.Multiline = true;
            rawDataTextBox.Name = "rawDataTextBox";
            rawDataTextBox.ReadOnly = true;
            rawDataTextBox.ScrollBars = ScrollBars.Both;
            rawDataTextBox.Size = new Size(707, 402);
            rawDataTextBox.TabIndex = 0;
            rawDataTextBox.WordWrap = false;
            // 
            // asciiTabPage
            // 
            asciiTabPage.Controls.Add(asciiTextBox);
            asciiTabPage.Location = new Point(4, 26);
            asciiTabPage.Name = "asciiTabPage";
            asciiTabPage.Padding = new Padding(3);
            asciiTabPage.Size = new Size(719, 414);
            asciiTabPage.TabIndex = 1;
            asciiTabPage.Text = "ASCII";
            asciiTabPage.UseVisualStyleBackColor = true;
            // 
            // asciiTextBox
            // 
            asciiTextBox.Font = new Font("Consolas", 9F);
            asciiTextBox.Location = new Point(6, 6);
            asciiTextBox.Multiline = true;
            asciiTextBox.Name = "asciiTextBox";
            asciiTextBox.ReadOnly = true;
            asciiTextBox.ScrollBars = ScrollBars.Both;
            asciiTextBox.Size = new Size(707, 402);
            asciiTextBox.TabIndex = 0;
            asciiTextBox.WordWrap = false;
            // 
            // hexTabPage
            // 
            hexTabPage.Controls.Add(hexTextBox);
            hexTabPage.Location = new Point(4, 26);
            hexTabPage.Name = "hexTabPage";
            hexTabPage.Padding = new Padding(3);
            hexTabPage.Size = new Size(719, 414);
            hexTabPage.TabIndex = 2;
            hexTabPage.Text = "十六进制";
            hexTabPage.UseVisualStyleBackColor = true;
            // 
            // hexTextBox
            // 
            hexTextBox.Font = new Font("Consolas", 9F);
            hexTextBox.Location = new Point(7, 6);
            hexTextBox.Multiline = true;
            hexTextBox.Name = "hexTextBox";
            hexTextBox.ReadOnly = true;
            hexTextBox.ScrollBars = ScrollBars.Both;
            hexTextBox.Size = new Size(707, 402);
            hexTextBox.TabIndex = 0;
            hexTextBox.WordWrap = false;
            // 
            // xmlTabPage
            // 
            xmlTabPage.Controls.Add(xmlTextBox);
            xmlTabPage.Location = new Point(4, 26);
            xmlTabPage.Name = "xmlTabPage";
            xmlTabPage.Padding = new Padding(3);
            xmlTabPage.Size = new Size(719, 414);
            xmlTabPage.TabIndex = 3;
            xmlTabPage.Text = "XML";
            xmlTabPage.UseVisualStyleBackColor = true;
            //
            // xmlTextBox
            //
            xmlTextBox.Font = new Font("Consolas", 9F);
            xmlTextBox.Location = new Point(7, 6);
            xmlTextBox.Multiline = true;
            xmlTextBox.Name = "xmlTextBox";
            xmlTextBox.ReadOnly = true;
            xmlTextBox.ScrollBars = ScrollBars.Both;
            xmlTextBox.Size = new Size(707, 402);
            xmlTextBox.TabIndex = 0;
            xmlTextBox.WordWrap = false;
            //
            // structureTabPage
            //
            structureTabPage.Controls.Add(structureTreeView);
            structureTabPage.Location = new Point(4, 26);
            structureTabPage.Name = "structureTabPage";
            structureTabPage.Padding = new Padding(3);
            structureTabPage.Size = new Size(719, 414);
            structureTabPage.TabIndex = 4;
            structureTabPage.Text = "信息结构";
            structureTabPage.UseVisualStyleBackColor = true;
            //
            // structureTreeView
            //
            structureTreeView.BorderStyle = BorderStyle.FixedSingle;
            structureTreeView.Font = new Font("Consolas", 9F);
            structureTreeView.FullRowSelect = true;
            structureTreeView.Location = new Point(6, 6);
            structureTreeView.Name = "structureTreeView";
            structureTreeView.Size = new Size(707, 402);
            structureTreeView.TabIndex = 0;
            //
            // modeLabel
            // 
            modeLabel.Name = "modeLabel";
            modeLabel.Size = new Size(35, 15);
            modeLabel.Text = "模式:";
            // 
            // modeComboBox
            // 
            modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            modeComboBox.Items.AddRange(new object[] { "服务器模式", "客户端模式" });
            modeComboBox.Name = "modeComboBox";
            modeComboBox.Size = new Size(121, 23);
            modeComboBox.Text = "服务器模式";
            modeComboBox.SelectedIndexChanged += modeComboBox_SelectedIndexChanged;
            //
            // connectButton
            // 
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(75, 23);
            connectButton.Text = "连接";
            connectButton.Click += connectButton_Click;
            // 
            // portLabel
            // 
            portLabel.Name = "portLabel";
            portLabel.Size = new Size(35, 15);
            portLabel.Text = "端口:";
            // 
            // portTextBox
            // 
            portTextBox.Margin = new Padding(0, 0, 0, 0);
            portTextBox.Name = "portTextBox";
            portTextBox.Size = new Size(73, 23);
            portTextBox.Text = "5000";
            // 
            // hostLabel
            // 
            hostLabel.Name = "hostLabel";
            hostLabel.Size = new Size(35, 15);
            hostLabel.Text = "主机:";
            // 
            // hostTextBox
            // 
            hostTextBox.Margin = new Padding(0, 0, 0, 0);
            hostTextBox.Name = "hostTextBox";
            hostTextBox.Size = new Size(113, 23);
            hostTextBox.Text = "127.0.0.1";
            // 
            // nameLabel
            // 
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new Size(35, 15);
            nameLabel.Text = "名称:";
            // 
            // nameTextBox
            // 
            nameTextBox.Margin = new Padding(0, 0, 0, 0);
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new Size(113, 23);
            nameTextBox.Text = "CLIENT";
            // 
            // settingsButton
            // 
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(75, 23);
            settingsButton.Text = "设置";
            settingsButton.Click += settingsButton_Click;
            // 
            // MainForm
            //
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(1134, 842);
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.WindowState = FormWindowState.Normal;
            Controls.Add(mainPanel);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            Font = new Font("微软雅黑", 9F);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HSMS Simulator - 设备通信模拟器";
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            mainPanel.ResumeLayout(false);
            messagesPanel.ResumeLayout(false);
            messagesPanel.PerformLayout();
            quickCommandPanel.ResumeLayout(false);
            quickCommandPanel.PerformLayout();
            devicesPanel.ResumeLayout(false);
            devicesPanel.PerformLayout();
            contentPanel.ResumeLayout(false);
            contentPanel.PerformLayout();
            detailsTabControl.ResumeLayout(false);
            rawDataTabPage.ResumeLayout(false);
            rawDataTabPage.PerformLayout();
            asciiTabPage.ResumeLayout(false);
            asciiTabPage.PerformLayout();
            hexTabPage.ResumeLayout(false);
            hexTabPage.PerformLayout();
            xmlTabPage.ResumeLayout(false);
            xmlTabPage.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripLabel modeLabel;
        private ToolStripComboBox modeComboBox;
        private ToolStripButton connectButton;
        private ToolStripLabel portLabel;
        private ToolStripTextBox portTextBox;
        private ToolStripLabel hostLabel;
        private ToolStripTextBox hostTextBox;
        private ToolStripLabel nameLabel;
        private ToolStripTextBox nameTextBox;
        private ToolStripButton settingsButton;
        private Panel mainPanel;
        private Panel devicesPanel;
        private Label devicesLabel;
        private ListView devicesListView;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private Panel quickCommandPanel;
        private Label quickCommandLabel;
        private Button customSendButton;
        private TreeView quickCommandTreeView;
        private Panel contentPanel;
        private Label detailsLabel;
        private TabControl detailsTabControl;
        private TabPage rawDataTabPage;
        private TextBox rawDataTextBox;
        private TabPage asciiTabPage;
        private TextBox asciiTextBox;
        private TabPage hexTabPage;
        private TextBox hexTextBox;
        private TabPage xmlTabPage;
        private TextBox xmlTextBox;
        private TabPage structureTabPage;
        private TreeView structureTreeView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripStatusLabel messageCountLabel;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripStatusLabel connectionCountLabel;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripStatusLabel deviceCountLabel;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripStatusLabel heartbeatLabel;
        private ToolStripStatusLabel heartbeatIndicator;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripStatusLabel versionLabel;
        private Panel messagesPanel;
        private Button clearMessagesButton;
        private Button copyMessagesButton;
        private Label messagesLabel;
        private CheckBox filterAutoMessagesCheckBox;
        private ListView messagesListView;
        private ColumnHeader messagesColumnHeader1;
        private ColumnHeader messagesColumnHeader2;
        private ColumnHeader messagesColumnHeader3;
        private ColumnHeader messagesColumnHeader4;
    }
}
