using System.Xml.Linq;

namespace TibcoTibrvService;

/// <summary>
/// 简化的TIBRV消息服务
/// </summary>
public class SimpleTibrvService
{
    public async Task StartAsync()
    {
        Console.WriteLine("=== TIBRV消息服务启动 (模拟模式) ===");

        // 模拟启动
        await Task.Delay(1000);
        Console.WriteLine("TIBRV服务已启动，监听主题: CIMMonitor.*");

        // 模拟接收消息
        await SimulateMessages();
    }

    private async Task SimulateMessages()
    {
        var messages = new[]
        {
            new MessageSample
            {
                Operation = "GetEquipmentStatus",
                RequestId = "MSG001",
                Xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Request operation=""GetEquipmentStatus"" requestId=""MSG001"">
    <EquipmentId>EQ001</EquipmentId>
</Request>"
            },
            new MessageSample
            {
                Operation = "UpdateProductionData",
                RequestId = "MSG002",
                Xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Request operation=""UpdateProductionData"" requestId=""MSG002"">
    <BatchId>BATCH123</BatchId>
    <ProductCode>PRODUCT-A</ProductCode>
    <Quantity>100</Quantity>
</Request>"
            },
            new MessageSample
            {
                Operation = "GetBatchInfo",
                RequestId = "MSG003",
                Xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Request operation=""GetBatchInfo"" requestId=""MSG003"">
    <BatchId>BATCH123</BatchId>
</Request>"
            }
        };

        var index = 0;
        while (true)
        {
            await Task.Delay(3000);

            var message = messages[index];
            Console.WriteLine($"\n[收到消息] {message.RequestId}");
            Console.WriteLine($"[操作] {message.Operation}");
            Console.WriteLine($"[XML]\n{message.Xml}");

            // 处理消息
            var response = ProcessMessage(message.Xml, message.RequestId);

            // 发送响应
            Console.WriteLine($"\n[发送响应] {message.RequestId}");
            Console.WriteLine($"[XML]\n{response}");

            index = (index + 1) % messages.Length;
        }
    }

    private string ProcessMessage(string xml, string requestId)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root;
        var operation = root?.Attribute("operation")?.Value;

        return operation switch
        {
            "GetEquipmentStatus" => GenerateEquipmentResponse(root, requestId),
            "UpdateProductionData" => GenerateProductionResponse(root, requestId),
            "GetBatchInfo" => GenerateBatchResponse(root, requestId),
            _ => GenerateErrorResponse(requestId, $"未知操作: {operation}")
        };
    }

    private string GenerateEquipmentResponse(XElement? root, string requestId)
    {
        var equipmentId = root?.Element("EquipmentId")?.Value ?? "Unknown";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Response requestId=""{requestId}"" success=""true"" timestamp=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"">
    <Data>
        <EquipmentId>{equipmentId}</EquipmentId>
        <Status>在线</Status>
        <Temperature>75.5°C</Temperature>
        <Pressure>2.3MPa</Pressure>
        <LastUpdate>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</LastUpdate>
    </Data>
</Response>";
    }

    private string GenerateProductionResponse(XElement? root, string requestId)
    {
        var batchId = root?.Element("BatchId")?.Value ?? "Unknown";
        var productCode = root?.Element("ProductCode")?.Value ?? "Unknown";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Response requestId=""{requestId}"" success=""true"" timestamp=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"">
    <Data>
        <BatchId>{batchId}</BatchId>
        <ProductCode>{productCode}</ProductCode>
        <Status>更新成功</Status>
        <Timestamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</Timestamp>
    </Data>
</Response>";
    }

    private string GenerateBatchResponse(XElement? root, string requestId)
    {
        var batchId = root?.Element("BatchId")?.Value ?? "Unknown";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Response requestId=""{requestId}"" success=""true"" timestamp=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"">
    <Data>
        <BatchId>{batchId}</BatchId>
        <ProductCode>PRODUCT-A</ProductCode>
        <Quantity>1000</Quantity>
        <StartTime>{DateTime.Now.AddHours(-2):yyyy-MM-dd HH:mm:ss}</StartTime>
        <Status>生产中</Status>
    </Data>
</Response>";
    }

    private string GenerateErrorResponse(string requestId, string errorMessage)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Response requestId=""{requestId}"" success=""false"" timestamp=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"">
    <Error>{errorMessage}</Error>
</Response>";
    }

    private class MessageSample
    {
        public string Operation { get; set; } = "";
        public string RequestId { get; set; } = "";
        public string Xml { get; set; } = "";
    }
}
