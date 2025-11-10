using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIMMonitor.Forms
{
    /// <summary>
    /// 设备信息结构体
    /// </summary>
    public struct DeviceInfo
    {
        public string ServerId { get; set; }
        public string ServerName { get; set; }
        public string ProtocolType { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public partial class HsmsCommForm : Form
    {
        private readonly DeviceInfo _device;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private bool _isConnected = false;

        public HsmsCommForm(DeviceInfo device)
        {
            _device = device;
            InitializeComponent();
            LoadDeviceInfo();
            AppendLog($"HSMS通信窗口已打开 - 设备: {device.ServerName} ({device.Host}:{device.Port})");
        }

        private void LoadDeviceInfo()
        {
            lblDeviceInfo.Text = $"设备: {_device.ServerName}\n" +
                                $"协议: {_device.ProtocolType.ToUpper()}\n" +
                                $"地址: {_device.Host}:{_device.Port}";
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;

                _tcpClient = new TcpClient();
                AppendLog($"正在连接到 {_device.Host}:{_device.Port}...");

                await _tcpClient.ConnectAsync(_device.Host, _device.Port);

                _networkStream = _tcpClient.GetStream();
                _isConnected = true;

                AppendLog("HSMS连接已建立");

                btnConnect.Text = "断开连接";
                btnConnect.Click -= BtnConnect_Click;
                btnConnect.Click += BtnDisconnect_Click;

                // 启动消息接收
                _ = Task.Run(ReceiveMessagesAsync);

                // 发送握手消息
                await SendHsmsMessageAsync(1, 13, "ARE_YOU_THERE", true);
            }
            catch (Exception ex)
            {
                AppendLog($"连接失败: {ex.Message}");
                MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnect.Enabled = true;
            }
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            try
            {
                _isConnected = false;
                _networkStream?.Close();
                _tcpClient?.Close();
                _networkStream?.Dispose();
                _tcpClient?.Dispose();

                AppendLog("HSMS连接已断开");

                btnConnect.Text = "连接";
                btnConnect.Click -= BtnDisconnect_Click;
                btnConnect.Click += BtnConnect_Click;
                btnConnect.Enabled = true;
            }
            catch (Exception ex)
            {
                AppendLog($"断开连接时发生错误: {ex.Message}");
            }
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_isConnected)
                {
                    AppendLog("未连接到设备");
                    return;
                }

                var message = txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(message))
                {
                    AppendLog("请输入要发送的消息");
                    return;
                }

                // 尝试解析消息格式：S{stream}F{function}:{content}
                if (TryParseMessage(message, out var stream, out var function, out var content))
                {
                    await SendHsmsMessageAsync(stream, function, content);
                    txtMessage.Clear();
                }
                else
                {
                    AppendLog("消息格式不正确，请使用格式：S{stream}F{function}:{content}");
                    MessageBox.Show("消息格式不正确\n\n正确格式：S{stream}F{function}:{content}\n\n示例：\nS1F13:ARE_YOU_THERE\nS2F33:EQUIPMENT_STATUS_REQUEST", "格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"发送消息失败: {ex.Message}");
                MessageBox.Show($"发送消息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendHsmsMessageAsync(ushort stream, byte function, string content, bool requireResponse = false)
        {
            try
            {
                var header = new byte[10];
                header[0] = 0x00;
                header[1] = (byte)(stream >> 8);
                header[2] = (byte)(stream & 0xFF);
                header[3] = function;
                header[4] = (byte)(requireResponse ? 0x80 : 0x00);
                header[5] = 0x00;
                header[6] = 0x00;
                header[7] = 0x00;
                header[8] = 0x00;
                header[9] = 0x00;

                var contentBytes = Encoding.ASCII.GetBytes(content);
                var data = new byte[10 + contentBytes.Length];
                Array.Copy(header, 0, data, 0, 10);
                Array.Copy(contentBytes, 0, data, 10, contentBytes.Length);

                await _networkStream!.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();

                AppendLog($"[发送] S{stream}F{function}: {content}");
            }
            catch (Exception ex)
            {
                AppendLog($"发送失败: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (_isConnected && _networkStream != null)
                {
                    var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        AppendLog("连接已关闭");
                        this.Invoke((MethodInvoker)(() => BtnDisconnect_Click(null, null)));
                        break;
                    }

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, bytesRead);

                    var message = ParseHsmsMessage(data);
                    this.Invoke((MethodInvoker)(() => AppendLog($"[接收] {message}")));
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)(() => AppendLog($"接收消息失败: {ex.Message}")));
            }
        }

        private string ParseHsmsMessage(byte[] data)
        {
            if (data.Length < 10)
                return "消息格式不正确";

            var stream = (ushort)(data[1] << 8 | data[2]);
            var function = data[3];

            string content = "";
            if (data.Length > 10)
            {
                var contentBytes = new byte[data.Length - 10];
                Array.Copy(data, 10, contentBytes, 0, contentBytes.Length);
                content = Encoding.ASCII.GetString(contentBytes).Trim();
            }

            var messageType = GetMessageType(stream, function);
            return $"S{stream}F{function} ({messageType}): {content}";
        }

        private string GetMessageType(ushort stream, byte function)
        {
            return stream switch
            {
                1 when function == 13 => "Are You There",
                1 when function == 14 => "I Am Here",
                2 when function == 33 => "Equipment Status Request",
                2 when function == 34 => "Equipment Status Data",
                6 when function == 12 => "Equipment Status",
                6 when function == 11 => "Event Report",
                _ => "Unknown"
            };
        }

        private bool TryParseMessage(string message, out ushort stream, out byte function, out string content)
        {
            stream = 0;
            function = 0;
            content = string.Empty;

            try
            {
                var parts = message.Split(':', 2);
                if (parts.Length < 2) return false;

                var sfPart = parts[0];
                content = parts[1];

                sfPart = sfPart.ToUpper();
                var streamIndex = sfPart.IndexOf('S');
                var functionIndex = sfPart.IndexOf('F');

                if (streamIndex == -1 || functionIndex == -1 || functionIndex <= streamIndex)
                    return false;

                stream = ushort.Parse(sfPart.Substring(streamIndex + 1, functionIndex - streamIndex - 1));
                function = byte.Parse(sfPart.Substring(functionIndex + 1));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _isConnected = false;
                _networkStream?.Close();
                _tcpClient?.Close();
                _networkStream?.Dispose();
                _tcpClient?.Dispose();
            }
            catch { }

            base.OnFormClosed(e);
        }
    }
}
