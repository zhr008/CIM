using TibcoTibrvService;

Console.WriteLine("=== TIBRV消息服务 ===");
Console.WriteLine("这是一个模拟的TIBRV消息服务，用于演示XML消息处理");
Console.WriteLine();

var service = new SimpleTibrvService();

try
{
    await service.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"错误: {ex.Message}");
    Console.WriteLine("\n按任意键退出...");
    Console.ReadKey();
}
