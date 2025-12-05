using System;
using System.Drawing;
using System.Windows.Forms;
using CIMMonitor.Services;
using Common.Models;
using log4net;

namespace CIMMonitor.Forms
{
    public partial class MainForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MainForm));
        
        private HsmsService hsmsService;
        private KepServerService kepServerService;
        private TibcoService tibcoService;
        
        private ListBox messageListBox;
        private ListBox statusListBox;
        private Button startButton;
        private Button stopButton;
        private Button refreshButton;
        private DataGridView equipmentGrid;
        private Timer statusTimer;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            SetupUI();
        }
        
        private void InitializeComponent()
        {
            this.Text = "CIM Monitor - Computer Integrated Manufacturing System";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;
        }
        
        private void InitializeServices()
        {
            hsmsService = new HsmsService();
            hsmsService.OnMessageReceived += OnHsmsMessageReceived;
            hsmsService.OnStatusChanged += OnHsmsStatusChanged;
            
            kepServerService = new KepServerService();
            kepServerService.OnDataReceived += OnKepServerDataReceived;
            kepServerService.OnStatusChanged += OnKepServerStatusChanged;
            
            tibcoService = new TibcoService();
            tibcoService.OnMessageReceived += OnTibcoMessageReceived;
            
            // Load configurations
            try
            {
                hsmsService.LoadConfiguration("Config/HsmsConfig.xml");
                kepServerService.LoadConfiguration("Config/KepServerConfig.xml");
                
                log.Info("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading configuration: {ex.Message}", ex);
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetupUI()
        {
            // Create controls
            var titleLabel = new Label
            {
                Text = "CIM Monitor - Equipment Status and Communication",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 30)
            };
            
            startButton = new Button
            {
                Text = "Start Monitoring",
                Location = new Point(20, 60),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            startButton.Click += StartButton_Click;
            
            stopButton = new Button
            {
                Text = "Stop Monitoring",
                Location = new Point(150, 60),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral
            };
            stopButton.Click += StopButton_Click;
            
            refreshButton = new Button
            {
                Text = "Refresh Status",
                Location = new Point(280, 60),
                Size = new Size(120, 30),
                BackColor = Color.LightBlue
            };
            refreshButton.Click += RefreshButton_Click;
            
            // Equipment grid
            equipmentGrid = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(600, 200),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            equipmentGrid.Columns.Add("ID", "Equipment ID");
            equipmentGrid.Columns.Add("Name", "Name");
            equipmentGrid.Columns.Add("IP", "IP Address");
            equipmentGrid.Columns.Add("Port", "Port");
            equipmentGrid.Columns.Add("Protocol", "Protocol");
            equipmentGrid.Columns.Add("Status", "Status");
            
            // Status messages
            statusListBox = new ListBox
            {
                Location = new Point(20, 320),
                Size = new Size(600, 150),
                Font = new Font("Consolas", 9)
            };
            
            // Message log
            messageListBox = new ListBox
            {
                Location = new Point(650, 100),
                Size = new Size(320, 470),
                Font = new Font("Consolas", 9)
            };
            
            var messageLabel = new Label
            {
                Text = "Messages:",
                Location = new Point(650, 80),
                Size = new Size(100, 20)
            };
            
            var statusLabel = new Label
            {
                Text = "System Status:",
                Location = new Point(20, 300),
                Size = new Size(100, 20)
            };
            
            var equipmentLabel = new Label
            {
                Text = "Equipment Status:",
                Location = new Point(20, 80),
                Size = new Size(120, 20)
            };
            
            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                titleLabel, startButton, stopButton, refreshButton,
                equipmentGrid, statusListBox, messageListBox,
                messageLabel, statusLabel, equipmentLabel
            });
            
            // Set up timer for periodic updates
            statusTimer = new Timer { Interval = 5000 }; // Update every 5 seconds
            statusTimer.Tick += StatusTimer_Tick;
        }
        
        private async void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                await hsmsService.StartAsync();
                await kepServerService.StartAsync();
                
                // Connect to TIBCO
                await tibcoService.ConnectAsync("127.0.0.1", "7500", "tcp:7500");
                
                startButton.Enabled = false;
                stopButton.Enabled = true;
                
                statusTimer.Start();
                
                AddStatusMessage("Monitoring started successfully");
                log.Info("Monitoring started");
            }
            catch (Exception ex)
            {
                log.Error($"Error starting monitoring: {ex.Message}", ex);
                MessageBox.Show($"Error starting monitoring: {ex.Message}", "Start Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                hsmsService.Stop();
                kepServerService.Stop();
                tibcoService.Disconnect();
                
                startButton.Enabled = true;
                stopButton.Enabled = false;
                
                statusTimer.Stop();
                
                AddStatusMessage("Monitoring stopped");
                log.Info("Monitoring stopped");
            }
            catch (Exception ex)
            {
                log.Error($"Error stopping monitoring: {ex.Message}", ex);
                MessageBox.Show($"Error stopping monitoring: {ex.Message}", "Stop Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            UpdateEquipmentGrid();
        }
        
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            UpdateEquipmentGrid();
        }
        
        private void UpdateEquipmentGrid()
        {
            // Clear existing rows
            equipmentGrid.Rows.Clear();
            
            // Add HSMS devices
            foreach (var device in hsmsService.GetConnectedDevices())
            {
                equipmentGrid.Rows.Add(device.ID, device.Name, device.IPAddress, device.Port, "HSMS", "Connected");
            }
            
            // Add KepServer instances
            foreach (var server in kepServerService.GetConnectedServers())
            {
                equipmentGrid.Rows.Add(server.ID, server.Name, server.HostName, server.Port, "OPC/KepServer", "Connected");
            }
            
            // Add disconnected devices
            foreach (var device in hsmsService.GetConnectedDevices())
            {
                var rowExists = false;
                foreach (DataGridViewRow row in equipmentGrid.Rows)
                {
                    if (row.Cells["ID"].Value?.ToString() == device.ID)
                    {
                        rowExists = true;
                        break;
                    }
                }
                
                if (!rowExists)
                {
                    equipmentGrid.Rows.Add(device.ID, device.Name, device.IPAddress, device.Port, "HSMS", "Disconnected");
                }
            }
        }
        
        private void OnHsmsMessageReceived(object sender, EquipmentMessage e)
        {
            AddMessage($"[HSMS-{e.EquipmentID}] {e.MessageContent}");
        }
        
        private void OnHsmsStatusChanged(object sender, string e)
        {
            AddStatusMessage($"HSMS: {e}");
        }
        
        private void OnKepServerDataReceived(object sender, EquipmentMessage e)
        {
            AddMessage($"[OPC-{e.EquipmentID}] {e.MessageContent}");
        }
        
        private void OnKepServerStatusChanged(object sender, string e)
        {
            AddStatusMessage($"OPC/KepServer: {e}");
        }
        
        private void OnTibcoMessageReceived(object sender, EquipmentMessage e)
        {
            AddMessage($"[TIBCO] {e.MessageContent}");
        }
        
        private void AddMessage(string message)
        {
            if (messageListBox.InvokeRequired)
            {
                messageListBox.Invoke(new Action(() => AddMessage(message)));
            }
            else
            {
                messageListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                messageListBox.TopIndex = messageListBox.Items.Count - 1;
            }
        }
        
        private void AddStatusMessage(string message)
        {
            if (statusListBox.InvokeRequired)
            {
                statusListBox.Invoke(new Action(() => AddStatusMessage(message)));
            }
            else
            {
                statusListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                statusListBox.TopIndex = statusListBox.Items.Count - 1;
            }
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                hsmsService.Stop();
                kepServerService.Stop();
                tibcoService.Disconnect();
                
                statusTimer?.Stop();
            }
            catch (Exception ex)
            {
                log.Error($"Error during form closing: {ex.Message}", ex);
            }
        }
    }
}