using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CIMMonitor.Services
{
    /// <summary>
    /// 性能监控服务
    /// 监控应用程序的CPU、内存、网络等性能指标
    /// </summary>
    public interface IPerformanceMonitor
    {
        void StartMonitoring();
        void StopMonitoring();
        PerformanceMetrics GetCurrentMetrics();
    }

    public class PerformanceMetrics
    {
        public double CpuUsage { get; set; }
        public long MemoryUsageMB { get; set; }
        public long WorkingSetMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime Timestamp { get; set; }
        public int ActiveConnections { get; set; }
        public int MessagesPerSecond { get; set; }
    }

    public class PerformanceMonitor : IPerformanceMonitor, IDisposable
    {
        private readonly System.Threading.Timer _monitoringTimer;
        private readonly ConcurrentQueue<PerformanceMetrics> _metricsHistory = new();
        private readonly int _maxHistorySize = 100; // 保存最近100次数据
        private readonly object _lockObject = new object();

        private bool _isRunning = false;
        private int _messageCount = 0;
        private DateTime _lastMessageTime = DateTime.Now;
        private readonly Process _currentProcess;

        public PerformanceMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
            // 每5秒监控一次
            _monitoringTimer = new System.Threading.Timer(MonitorCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMonitoring()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _monitoringTimer.Change(0, 5000); // 5秒间隔
            }
        }

        public void StopMonitoring()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            _currentProcess.Refresh();

            var cpuUsage = _currentProcess.TotalProcessorTime;
            var timestamp = DateTime.Now;

            return new PerformanceMetrics
            {
                CpuUsage = CalculateCpuUsage(),
                MemoryUsageMB = _currentProcess.WorkingSet64 / 1024 / 1024,
                WorkingSetMB = _currentProcess.WorkingSet64 / 1024 / 1024,
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount,
                Timestamp = timestamp,
                ActiveConnections = GetActiveConnections(),
                MessagesPerSecond = CalculateMessagesPerSecond()
            };
        }

        private void MonitorCallback(object? state)
        {
            var metrics = GetCurrentMetrics();

            // 添加到历史记录
            _metricsHistory.Enqueue(metrics);

            // 限制历史记录大小
            while (_metricsHistory.Count > _maxHistorySize)
            {
                _metricsHistory.TryDequeue(out _);
            }

            // 输出性能日志
            LogMetrics(metrics);
        }

        private double CalculateCpuUsage()
        {
            try
            {
                _currentProcess.Refresh();
                var cpuTime = _currentProcess.TotalProcessorTime;
                var elapsed = DateTime.Now - Process.GetCurrentProcess().StartTime;
                var cpuUsagePercent = (cpuTime.TotalMilliseconds / (elapsed.TotalMilliseconds * Environment.ProcessorCount)) * 100;
                return Math.Round(cpuUsagePercent, 2);
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveConnections()
        {
            // 这里应该返回实际的连接数
            // 暂时返回模拟数据
            return 0;
        }

        private int CalculateMessagesPerSecond()
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastMessageTime).TotalSeconds;

            if (elapsed >= 1)
            {
                var messagesPerSecond = _messageCount / elapsed;
                _messageCount = 0;
                _lastMessageTime = now;
                return (int)messagesPerSecond;
            }

            return 0;
        }

        private void LogMetrics(PerformanceMetrics metrics)
        {
            // 使用日志记录性能指标
            System.Diagnostics.Debug.WriteLine(
                $"[性能监控] CPU: {metrics.CpuUsage}%, " +
                $"内存: {metrics.MemoryUsageMB}MB, " +
                $"线程: {metrics.ThreadCount}, " +
                $"连接: {metrics.ActiveConnections}, " +
                $"消息/秒: {metrics.MessagesPerSecond}");
        }

        public void RecordMessage()
        {
            Interlocked.Increment(ref _messageCount);
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _currentProcess?.Dispose();
        }
    }
}
