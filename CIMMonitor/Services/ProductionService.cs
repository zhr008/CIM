namespace CIMMonitor.Services
{
    public static class ProductionService
    {
        private static readonly List<Models.ProductionData> _productionData = new();
        private static readonly List<Models.ProductionOrder> _productionOrders = new();
        private static readonly Random _random = new();

        static ProductionService()
        {
            InitializeData();
        }

        private static void InitializeData()
        {
            _productionData.Clear();
            _productionOrders.Clear();

            for (int i = 1; i <= 100; i++)
            {
                _productionData.Add(new Models.ProductionData
                {
                    Id = i,
                    LineId = "LINE_" + (i % 3 + 1),
                    DeviceId = "PLC" + (i % 5 + 1).ToString("000"),
                    Temperature = 60 + _random.NextDouble() * 20,
                    Pressure = 1.5 + _random.NextDouble() * 1.5,
                    FlowRate = 100 + _random.NextDouble() * 50,
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    Status = "OK"
                });
            }

            _productionOrders.AddRange(new[]
            {
                new Models.ProductionOrder { OrderId = "ORD-001", ProductCode = "P001", Quantity = 1000, CompletedQuantity = 650, StartTime = DateTime.Now.AddDays(-2), Status = "生产中", LineId = "LINE_1" },
                new Models.ProductionOrder { OrderId = "ORD-002", ProductCode = "P002", Quantity = 500, CompletedQuantity = 500, StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now, Status = "已完成", LineId = "LINE_2" },
                new Models.ProductionOrder { OrderId = "ORD-003", ProductCode = "P003", Quantity = 800, CompletedQuantity = 0, StartTime = DateTime.Now, Status = "等待中", LineId = "LINE_1" }
            });
        }

        public static List<Models.ProductionData> GetAllProductionData()
        {
            return _productionData.OrderByDescending(p => p.Timestamp).Take(50).ToList();
        }

        public static List<Models.ProductionData> GetProductionDataByDevice(string deviceId)
        {
            return _productionData.Where(p => p.DeviceId == deviceId)
                .OrderByDescending(p => p.Timestamp)
                .Take(20)
                .ToList();
        }

        public static List<Models.ProductionOrder> GetAllProductionOrders()
        {
            return _productionOrders.OrderByDescending(p => p.StartTime).ToList();
        }

        public static Models.ProductionData AddProductionData(Models.ProductionData data)
        {
            data.Id = _productionData.Any() ? _productionData.Max(p => p.Id) + 1 : 1;
            data.Timestamp = DateTime.Now;
            _productionData.Insert(0, data);
            return data;
        }

        public static bool UpdateProductionData(Models.ProductionData data)
        {
            var existing = _productionData.FirstOrDefault(p => p.Id == data.Id);
            if (existing != null)
            {
                existing.LineId = data.LineId;
                existing.DeviceId = data.DeviceId;
                existing.Temperature = data.Temperature;
                existing.Pressure = data.Pressure;
                existing.FlowRate = data.FlowRate;
                existing.Status = data.Status;
                return true;
            }
            return false;
        }

        public static bool DeleteProductionData(int id)
        {
            var data = _productionData.FirstOrDefault(p => p.Id == id);
            if (data != null)
            {
                _productionData.Remove(data);
                return true;
            }
            return false;
        }
    }
}
