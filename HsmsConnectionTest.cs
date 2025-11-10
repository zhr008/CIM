using System;
using System.Threading.Tasks;
using CIMMonitor.Services;

namespace HsmsConnectionTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("================================================");
            Console.WriteLine("HSMS连接服务独立测试");
            Console.WriteLine("================================================\n");

            // 测试服务端模式
            await TestServerMode();

            Console.WriteLine("\n================================================");
            Console.WriteLine("测试完成");
            Console.WriteLine("================================================");
        }

        static async Task TestServerMode()
        {
            Console.WriteLine("测试1: 服务端模式 - 启动监听端口5001");
            Console.WriteLine("------------------------------------------------");

            try
            {
                // 创建连接服务
                var connection = new HsmsConnectionService("EQP_DEVICE_001", 2, 0x5678);

                // 绑定事件
                connection.ConnectionStatusChanged += (sender, isConnected) =>
                {
                    Console.WriteLine($"[事件] 连接状态变化: {isConnected}");
                };

                connection.MessageReceived += (sender, message) =>
                {
                    Console.WriteLine($"[事件] 收到消息: {message.MessageType}");
                };

                // 启动服务端
                Console.WriteLine("正在启动服务端...");
                var connected = await connection.ConnectAsync("EQP_MACHINE_01", 5001, isServerMode: true);

                if (connected)
                {
                    Console.WriteLine("✅ 服务端启动成功！");

                    // 检查连接状态
                    Console.WriteLine($"当前连接状态: {connection.IsConnected}");
                    Console.WriteLine($"客户端数量: {connection.ClientCount}");

                    // 等待一段时间，看是否有客户端连接
                    Console.WriteLine("等待客户端连接... (10秒)");
                    await Task.Delay(10000);

                    Console.WriteLine($"等待后连接状态: {connection.IsConnected}");
                    Console.WriteLine($"等待后客户端数量: {connection.ClientCount}");

                    // 断开连接
                    Console.WriteLine("正在断开连接...");
                    await connection.DisconnectAsync();
                    Console.WriteLine("✅ 连接已断开");
                }
                else
                {
                    Console.WriteLine("❌ 服务端启动失败！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
