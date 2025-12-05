using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;

namespace HsmsSimulator.Forms
{
    public partial class HsmsSimulatorForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HsmsSimulatorForm));
        
        private TcpListener server;
        private List<TcpClient> clients;
        private CancellationTokenSource cancellationTokenSource;
        private bool isServerMode;
        
        private TextBox logTextBox;
        private TextBox ipAddressTextBox;
        private TextBox portTextBox;
        private Button startStopButton;
        private Button connectButton;
        private TextBox messageTextBox;
        private Button sendButton;
        private ComboBox deviceComboBox;
        private ListBox clientList;
        private Timer heartbeatTimer;
        
        public HsmsSimulatorForm()
        {
            InitializeComponent();
            InitializeComponents();
            clients = new List<TcpClient>();
            cancellationTokenSource = new CancellationTokenSource();
            isServerMode = true;
        }
        
        private void InitializeComponent()
        {
            this.Text = "HSMS/SECS-GEM Simulator";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += HsmsSimulatorForm_FormClosing;
        }
        
        private void InitializeComponents()
        {
            // Labels
            var modeLabel = new Label
            {
                Text = "Mode:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(80, 20)
            };
            
            var ipLabel = new Label
            {
                Text = "IP Address:",
                Location = new System.Drawing.Point(150, 20),
                Size = new System.Drawing.Size(80, 20)
            };
            
            var portLabel = new Label
            {
                Text = "Port:",
                Location = new System.Drawing.Point(320, 20),
                Size = new System.Drawing.Size(80, 20)
            };
            
            // Mode selection
            var modeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(20, 40),
                Size = new System.Drawing.Size(120, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            modeComboBox.Items.Add("Server");
            modeComboBox.Items.Add("Client");
            modeComboBox.SelectedIndex = 0;
            modeComboBox.SelectedIndexChanged += ModeComboBox_SelectedIndexChanged;
            
            // IP Address
            ipAddressTextBox = new TextBox
            {
                Text = "127.0.0.1",
                Location = new System.Drawing.Point(150, 40),
                Size = new System.Drawing.Size(160, 20)
            };
            
            // Port
            portTextBox = new TextBox
            {
                Text = "5000",
                Location = new System.Drawing.Point(320, 40),
                Size = new System.Drawing.Size(80, 20)
            };
            
            // Buttons
            startStopButton = new Button
            {
                Text = "Start Server",
                Location = new System.Drawing.Point(420, 40),
                Size = new System.Drawing.Size(100, 30),
                BackColor = System.Drawing.Color.LightGreen
            };
            startStopButton.Click += StartStopButton_Click;
            
            connectButton = new Button
            {
                Text = "Connect",
                Location = new System.Drawing.Point(530, 40),
                Size = new System.Drawing.Size(100, 30),
                BackColor = System.Drawing.Color.LightBlue
            };
            connectButton.Click += ConnectButton_Click;
            connectButton.Visible = false; // Initially hidden, shown in client mode
            
            // Device selection
            var deviceLabel = new Label
            {
                Text = "Quick Commands:",
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(120, 20)
            };
            
            deviceComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            deviceComboBox.Items.Add("S1F13 - Request Control");
            deviceComboBox.Items.Add("S1F14 - Control Granted");
            deviceComboBox.Items.Add("S2F17 - Request Online");
            deviceComboBox.Items.Add("S2F18 - Online Ack");
            deviceComboBox.Items.Add("S5F1 - Alarm Report");
            deviceComboBox.SelectedIndex = 0;
            
            // Send button
            sendButton = new Button
            {
                Text = "Send",
                Location = new System.Drawing.Point(230, 100),
                Size = new System.Drawing.Size(80, 20),
                BackColor = System.Drawing.Color.LightYellow
            };
            sendButton.Click += SendButton_Click;
            
            // Message input
            var messageLabel = new Label
            {
                Text = "Message:",
                Location = new System.Drawing.Point(20, 140),
                Size = new System.Drawing.Size(80, 20)
            };
            
            messageTextBox = new TextBox
            {
                Text = "S1F13",
                Location = new System.Drawing.Point(20, 160),
                Size = new System.Drawing.Size(400, 20)
            };
            
            // Client list
            var clientLabel = new Label
            {
                Text = "Connected Clients:",
                Location = new System.Drawing.Point(450, 100),
                Size = new System.Drawing.Size(120, 20)
            };
            
            clientList = new ListBox
            {
                Location = new System.Drawing.Point(450, 120),
                Size = new System.Drawing.Size(320, 100),
                Font = new System.Drawing.Font("Consolas", 8)
            };
            
            // Log output
            var logLabel = new Label
            {
                Text = "Communication Log:",
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(150, 20)
            };
            
            logTextBox = new TextBox
            {
                Location = new System.Drawing.Point(20, 220),
                Size = new System.Drawing.Size(750, 320),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 8)
            };
            
            // Add all controls
            this.Controls.AddRange(new Control[] {
                modeLabel, ipLabel, portLabel, modeComboBox,
                ipAddressTextBox, portTextBox, startStopButton, connectButton,
                deviceLabel, deviceComboBox, sendButton,
                messageLabel, messageTextBox, clientLabel, clientList,
                logLabel, logTextBox
            });
            
            // Heartbeat timer
            heartbeatTimer = new Timer();
            heartbeatTimer.Interval = 30000; // 30 seconds
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
        }
        
        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                isServerMode = comboBox.SelectedItem.ToString() == "Server";
                
                if (isServerMode)
                {
                    startStopButton.Text = "Start Server";
                    connectButton.Visible = false;
                    startStopButton.Visible = true;
                }
                else
                {
                    startStopButton.Text = "Stop Client";
                    startStopButton.Visible = false;
                    connectButton.Visible = true;
                }
            }
        }
        
        private async void StartStopButton_Click(object sender, EventArgs e)
        {
            if (isServerMode)
            {
                if (server == null)
                {
                    await StartServer();
                }
                else
                {
                    StopServer();
                }
            }
            else
            {
                // In client mode, this button acts as a stop button
                StopClient();
            }
        }
        
        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            await ConnectAsClient();
        }
        
        private async Task StartServer()
        {
            try
            {
                int port = int.Parse(portTextBox.Text);
                server = new TcpListener(IPAddress.Parse(ipAddressTextBox.Text), port);
                server.Start();
                
                startStopButton.Text = "Stop Server";
                startStopButton.BackColor = System.Drawing.Color.LightCoral;
                
                AddToLog($"HSMS Server started on {ipAddressTextBox.Text}:{port}");
                log.Info($"HSMS Server started on {ipAddressTextBox.Text}:{port}");
                
                // Start accepting clients
                _ = Task.Run(AcceptClients);
                
                heartbeatTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}", "Server Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Error starting HSMS server: {ex.Message}", ex);
            }
        }
        
        private void StopServer()
        {
            try
            {
                server?.Stop();
                server = null;
                
                // Close all client connections
                foreach (var client in clients)
                {
                    client?.Close();
                }
                clients.Clear();
                UpdateClientList();
                
                startStopButton.Text = "Start Server";
                startStopButton.BackColor = System.Drawing.Color.LightGreen;
                
                heartbeatTimer.Stop();
                
                AddToLog("HSMS Server stopped");
                log.Info("HSMS Server stopped");
            }
            catch (Exception ex)
            {
                log.Error($"Error stopping server: {ex.Message}", ex);
            }
        }
        
        private async Task AcceptClients()
        {
            while (server != null)
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    clients.Add(client);
                    
                    AddToLog($"New client connected: {client.Client.RemoteEndPoint}");
                    log.Info($"New client connected: {client.Client.RemoteEndPoint}");
                    
                    UpdateClientList();
                    
                    // Handle client communication
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped
                    break;
                }
                catch (Exception ex)
                {
                    log.Error($"Error accepting client: {ex.Message}", ex);
                    break;
                }
            }
        }
        
        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                var buffer = new byte[1024];
                
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        AddToLog($"Received from {client.Client.RemoteEndPoint}: {message}");
                        log.Info($"Received HSMS message from {client.Client.RemoteEndPoint}: {message}");
                        
                        // Process and respond to the message
                        await ProcessMessage(message, stream);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Client disconnected: {ex.Message}");
                AddToLog($"Client disconnected: {ex.Message}");
            }
            finally
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                    UpdateClientList();
                }
                
                stream?.Close();
                client?.Close();
            }
        }
        
        private async Task ProcessMessage(string message, NetworkStream stream)
        {
            // In a real implementation, this would parse the SECS/GEM message
            // For simulation, we'll just send a simple response
            
            try
            {
                // Generate response based on the message
                string response;
                if (message.StartsWith("S1F13"))
                {
                    response = "S1F14"; // Control Granted
                }
                else if (message.StartsWith("S2F17"))
                {
                    response = "S2F18"; // Online Ack
                }
                else
                {
                    response = $"ACK:{message}";
                }
                
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                
                AddToLog($"Sent response: {response}");
                log.Info($"Sent HSMS response: {response}");
            }
            catch (Exception ex)
            {
                log.Error($"Error processing message: {ex.Message}", ex);
            }
        }
        
        private async Task ConnectAsClient()
        {
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(ipAddressTextBox.Text, int.Parse(portTextBox.Text));
                
                AddToLog($"Connected to server at {ipAddressTextBox.Text}:{portTextBox.Text}");
                log.Info($"Connected to HSMS server at {ipAddressTextBox.Text}:{portTextBox.Text}");
                
                // Handle client communication
                _ = Task.Run(() => HandleClient(client));
                
                // Add to clients list (for UI purposes)
                clients.Add(client);
                UpdateClientList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Connection Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error($"Error connecting to HSMS server: {ex.Message}", ex);
            }
        }
        
        private void StopClient()
        {
            // Close all client connections
            foreach (var client in clients)
            {
                client?.Close();
            }
            clients.Clear();
            UpdateClientList();
            
            AddToLog("Client disconnected from server");
            log.Info("HSMS client disconnected");
        }
        
        private async void SendButton_Click(object sender, EventArgs e)
        {
            var selectedCommand = deviceComboBox.SelectedItem.ToString();
            string message;
            
            switch (selectedCommand)
            {
                case "S1F13 - Request Control":
                    message = "S1F13";
                    break;
                case "S1F14 - Control Granted":
                    message = "S1F14";
                    break;
                case "S2F17 - Request Online":
                    message = "S2F17";
                    break;
                case "S2F18 - Online Ack":
                    message = "S2F18";
                    break;
                case "S5F1 - Alarm Report":
                    message = "S5F1";
                    break;
                default:
                    message = messageTextBox.Text;
                    break;
            }
            
            // Send to all connected clients if in server mode, or to the server if in client mode
            await SendMessageToAll(message);
        }
        
        private async Task SendMessageToAll(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            var disconnectedClients = new List<TcpClient>();
            
            foreach (var client in clients)
            {
                try
                {
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                        
                        AddToLog($"Sent message to client: {message}");
                        log.Info($"Sent HSMS message to client: {message}");
                    }
                    else
                    {
                        disconnectedClients.Add(client);
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"Error sending message to client: {ex.Message}");
                    disconnectedClients.Add(client);
                }
            }
            
            // Remove disconnected clients
            foreach (var client in disconnectedClients)
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                }
            }
            
            if (disconnectedClients.Count > 0)
            {
                UpdateClientList();
            }
        }
        
        private void UpdateClientList()
        {
            if (clientList.InvokeRequired)
            {
                clientList.Invoke(new Action(UpdateClientList));
            }
            else
            {
                clientList.Items.Clear();
                foreach (var client in clients)
                {
                    clientList.Items.Add(client.Client.RemoteEndPoint);
                }
            }
        }
        
        private void AddToLog(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AddToLog(message)));
            }
            else
            {
                logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                logTextBox.ScrollToCaret();
            }
        }
        
        private void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            // Send heartbeat to connected clients
            _ = SendMessageToAll("S6F11"); // Spontaneous message as heartbeat
            AddToLog("Heartbeat sent");
        }
        
        private void HsmsSimulatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
            StopClient();
            heartbeatTimer?.Stop();
            cancellationTokenSource?.Cancel();
        }
    }
}