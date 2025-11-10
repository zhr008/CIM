namespace CIMMonitor.Services
{
    public static class TibcoService
    {
        private static readonly List<TibcoMessage> _messages = new();
        private static readonly Random _random = new();

        public class TibcoMessage
        {
            public string Subject { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string SenderId { get; set; } = string.Empty;
        }

        public static List<TibcoMessage> GetRecentMessages()
        {
            return _messages.OrderByDescending(m => m.Timestamp).Take(20).ToList();
        }

        public static bool SendMessage(string subject, string content, string senderId = "CIM")
        {
            _messages.Add(new TibcoMessage
            {
                Subject = subject,
                Content = content,
                SenderId = senderId,
                Timestamp = DateTime.Now
            });
            return true;
        }

        public static List<string> GetSubjects()
        {
            return new List<string>
            {
                "PRODUCTION.DATA",
                "PRODUCTION.COMMAND",
                "DEVICE.CONTROL",
                "DEVICE.STATUS",
                "ALARM.EVENT",
                "ALARM.ACK",
                "ORDER.UPDATE",
                "ORDER.COMPLETE",
                "SYSTEM.HEARTBEAT"
            };
        }

        public static void SimulateIncomingMessages()
        {
            if (_random.Next(0, 100) < 30)
            {
                var subjects = GetSubjects();
                var subject = subjects[_random.Next(subjects.Count)];
                var content = $"来自 {subject} 的消息 - {DateTime.Now:HH:mm:ss}";
                SendMessage(subject, content, "TibcoRV");
            }
        }
    }
}
