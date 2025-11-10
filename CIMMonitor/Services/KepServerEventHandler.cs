using CIMMonitor.Models.KepServer;

namespace CIMMonitor.Services
{
    /// <summary>
    /// KepServer事件处理器
    /// 负责处理数据变化事件和映射触发事件
    /// </summary>
    public interface IKepServerEventHandler
    {
        Task InitializeAsync(IKepServerMonitoringService monitoringService);
        List<DataChangedEvent> GetDataChangeHistory(string serverId);
        List<MappingTriggeredEvent> GetMappingHistory(string serverId);
        void ClearHistory(string serverId);
    }

    public class KepServerEventHandler : IKepServerEventHandler, IDisposable
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(KepServerEventHandler));
        private IKepServerMonitoringService? _monitoringService;
        private readonly Dictionary<string, List<DataChangedEvent>> _dataChangeHistory = new();
        private readonly Dictionary<string, List<MappingTriggeredEvent>> _mappingHistory = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private const int MAX_HISTORY_SIZE = 1000;

        public KepServerEventHandler()
        {
        }

        /// <summary>
        /// 初始化事件处理器
        /// </summary>
        public async Task InitializeAsync(IKepServerMonitoringService monitoringService)
        {
            await _semaphore.WaitAsync();
            try
            {
                _monitoringService = monitoringService;

                // 订阅事件
                _monitoringService.DataChanged += OnDataChanged;
                _monitoringService.MappingTriggered += OnMappingTriggered;

                _logger.Info("KepServer事件处理器已初始化");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 处理数据变化事件
        /// </summary>
        private async void OnDataChanged(object? sender, DataChangedEvent e)
        {
            try
            {
                _logger.Info($"数据变化: [{e.ServerId}] {e.Address} - {e.ChangeType}");

                // 添加到历史记录
                await AddDataChangeToHistoryAsync(e);

                // 根据数据类型执行不同处理
                switch (e.ChangeType)
                {
                    case "BitChange":
                        await HandleBitChangeAsync(e);
                        break;
                    case "WordChange":
                        await HandleWordChangeAsync(e);
                        break;
                }

                // 检查是否需要发布事件
                await CheckPublishEventAsync(e);
            }
            catch (Exception ex)
            {
                _logger.Error("处理数据变化事件失败", ex);
            }
        }

        /// <summary>
        /// 处理映射触发事件
        /// </summary>
        private async void OnMappingTriggered(object? sender, MappingTriggeredEvent e)
        {
            try
            {
                _logger.Info($"映射触发: [{e.ServerId}] {e.MappingId} - Bit: {e.BitAddressId} -> Word: {e.WordAddressId} = {e.WordValue}");

                // 添加到历史记录
                await AddMappingToHistoryAsync(e);

                // 执行映射动作
                await ExecuteMappingActionAsync(e);

                // 发送数据交互
                await PublishDataInteractionAsync(e);
            }
            catch (Exception ex)
            {
                _logger.Error("处理映射触发事件失败", ex);
            }
        }

        /// <summary>
        /// 处理Bit变化
        /// </summary>
        private async Task HandleBitChangeAsync(DataChangedEvent e)
        {
            var oldValue = (bool)(e.OldValue ?? false);
            var newValue = (bool)(e.NewValue ?? false);

            if (newValue && !oldValue)
            {
                _logger.Info($"Bit上升沿触发: {e.Address}");
            }
            else if (!newValue && oldValue)
            {
                _logger.Info($"Bit下降沿触发: {e.Address}");
            }
        }

        /// <summary>
        /// 处理Word变化
        /// </summary>
        private async Task HandleWordChangeAsync(DataChangedEvent e)
        {
            var oldValue = e.OldValue?.ToString() ?? "";
            var newValue = e.NewValue?.ToString() ?? "";

            if (!string.IsNullOrEmpty(newValue))
            {
                _logger.Info($"Word值更新: {e.Address} = {newValue}");
            }
        }

        /// <summary>
        /// 检查是否需要发布事件
        /// </summary>
        private async Task CheckPublishEventAsync(DataChangedEvent e)
        {
            try
            {
                // 实际实现中会查询配置文件中的发布事件规则
                // 这里只是模拟
                if (e.ChangeType == "BitChange")
                {
                    await PublishEventAsync("EQUIPMENT.STATUS.CHANGE", e);
                }
                else if (e.ChangeType == "WordChange")
                {
                    await PublishEventAsync("PRODUCTION.DATA.UPDATE", e);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("发布事件失败", ex);
            }
        }

        /// <summary>
        /// 执行映射动作
        /// </summary>
        private async Task ExecuteMappingActionAsync(MappingTriggeredEvent e)
        {
            // 这里可以根据映射配置执行不同动作
            // 例如：写入数据库、发送消息、调用API等
            _logger.Debug($"执行映射动作: {e.MappingId}");
        }

        /// <summary>
        /// 发布数据交互事件
        /// </summary>
        private async Task PublishDataInteractionAsync(MappingTriggeredEvent e)
        {
            try
            {
                var message = new
                {
                    Timestamp = e.TriggeredTime,
                    ServerId = e.ServerId,
                    MappingId = e.MappingId,
                    BitAddress = e.BitAddressId,
                    WordAddress = e.WordAddressId,
                    WordValue = e.WordValue,
                    TriggerCondition = e.TriggerCondition
                };

                _logger.Info($"数据交互: {System.Text.Json.JsonSerializer.Serialize(message)}");

                // 实际实现中会发送到TIBCO或其他消息中间件
            }
            catch (Exception ex)
            {
                _logger.Error("发布数据交互失败", ex);
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        private async Task PublishEventAsync(string topic, DataChangedEvent e)
        {
            try
            {
                var message = new
                {
                    Timestamp = e.Timestamp,
                    ServerId = e.ServerId,
                    Address = e.Address,
                    DataType = e.DataType,
                    OldValue = e.OldValue,
                    NewValue = e.NewValue,
                    ChangeType = e.ChangeType
                };

                _logger.Info($"发布事件 [{topic}]: {System.Text.Json.JsonSerializer.Serialize(message)}");

                // 实际实现中会发送到消息中间件
            }
            catch (Exception ex)
            {
                _logger.Error($"发布事件 [{topic}] 失败", ex);
            }
        }

        /// <summary>
        /// 添加数据变化到历史记录
        /// </summary>
        private async Task AddDataChangeToHistoryAsync(DataChangedEvent e)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_dataChangeHistory.ContainsKey(e.ServerId))
                {
                    _dataChangeHistory[e.ServerId] = new List<DataChangedEvent>();
                }

                var history = _dataChangeHistory[e.ServerId];
                history.Add(e);

                // 限制历史记录数量
                if (history.Count > MAX_HISTORY_SIZE)
                {
                    history.RemoveAt(0);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 添加映射触发到历史记录
        /// </summary>
        private async Task AddMappingToHistoryAsync(MappingTriggeredEvent e)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_mappingHistory.ContainsKey(e.ServerId))
                {
                    _mappingHistory[e.ServerId] = new List<MappingTriggeredEvent>();
                }

                var history = _mappingHistory[e.ServerId];
                history.Add(e);

                // 限制历史记录数量
                if (history.Count > MAX_HISTORY_SIZE)
                {
                    history.RemoveAt(0);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取数据变化历史
        /// </summary>
        public List<DataChangedEvent> GetDataChangeHistory(string serverId)
        {
            _semaphore.Wait();
            try
            {
                return _dataChangeHistory.ContainsKey(serverId)
                    ? new List<DataChangedEvent>(_dataChangeHistory[serverId])
                    : new List<DataChangedEvent>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 获取映射触发历史
        /// </summary>
        public List<MappingTriggeredEvent> GetMappingHistory(string serverId)
        {
            _semaphore.Wait();
            try
            {
                return _mappingHistory.ContainsKey(serverId)
                    ? new List<MappingTriggeredEvent>(_mappingHistory[serverId])
                    : new List<MappingTriggeredEvent>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory(string serverId)
        {
            _semaphore.Wait();
            try
            {
                _dataChangeHistory.Remove(serverId);
                _mappingHistory.Remove(serverId);
                _logger.Info($"已清空服务器 {serverId} 的历史记录");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
