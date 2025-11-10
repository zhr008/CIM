namespace CIMMonitor.Services
{
    public static class AlarmService
    {
        private static readonly List<Models.AlarmLog> _alarms = new();

        static AlarmService()
        {
            InitializeAlarms();
        }

        private static void InitializeAlarms()
        {
            _alarms.Clear();
            _alarms.AddRange(new[]
            {
                new Models.AlarmLog { AlarmId = 1001, DeviceId = "PLC001", AlarmType = "温度过高", AlarmLevel = "高", Description = "设备温度超过设定值80°C", OccurTime = DateTime.Now.AddMinutes(-10), Status = "激活" },
                new Models.AlarmLog { AlarmId = 1002, DeviceId = "PLC002", AlarmType = "压力异常", AlarmLevel = "中", Description = "系统压力低于下限值", OccurTime = DateTime.Now.AddMinutes(-5), Status = "激活" },
                new Models.AlarmLog { AlarmId = 1003, DeviceId = "MOTOR01", AlarmType = "振动超标", AlarmLevel = "低", Description = "电机振动超过标准值", OccurTime = DateTime.Now.AddMinutes(-2), Status = "激活" },
                new Models.AlarmLog { AlarmId = 1004, DeviceId = "SENSOR01", AlarmType = "通信中断", AlarmLevel = "中", Description = "传感器通信异常", OccurTime = DateTime.Now.AddMinutes(-1), Status = "激活" }
            });
        }

        public static List<Models.AlarmLog> GetAllAlarms()
        {
            return _alarms.OrderByDescending(a => a.OccurTime).ToList();
        }

        public static List<Models.AlarmLog> GetActiveAlarms()
        {
            return _alarms.Where(a => a.Status == "激活").OrderByDescending(a => a.OccurTime).ToList();
        }

        public static bool AcknowledgeAlarm(int alarmId)
        {
            var alarm = _alarms.FirstOrDefault(a => a.AlarmId == alarmId);
            if (alarm != null)
            {
                alarm.Status = "已确认";
                return true;
            }
            return false;
        }

        public static bool ClearAlarm(int alarmId)
        {
            var alarm = _alarms.FirstOrDefault(a => a.AlarmId == alarmId);
            if (alarm != null)
            {
                alarm.Status = "已清除";
                return true;
            }
            return false;
        }

        public static void AddAlarm(string deviceId, string alarmType, string alarmLevel, string description)
        {
            var alarmId = _alarms.Any() ? _alarms.Max(a => a.AlarmId) + 1 : 1001;
            _alarms.Add(new Models.AlarmLog
            {
                AlarmId = alarmId,
                DeviceId = deviceId,
                AlarmType = alarmType,
                AlarmLevel = alarmLevel,
                Description = description,
                OccurTime = DateTime.Now,
                Status = "激活"
            });
        }
    }
}
