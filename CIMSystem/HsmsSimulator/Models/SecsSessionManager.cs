using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HsmsSimulator.Models
{
    /// <summary>
    /// SECS/GEM 会话管理器
    /// 负责管理Select、Separate、Linktest等会话流程
    /// </summary>
    public class SecsSessionManager
    {
        private readonly Dictionary<int, SecsSession> _sessions = new();
        private readonly object _lock = new();

        /// <summary>
        /// 事件：会话状态改变
        /// </summary>
        public event EventHandler<(int SessionId, SessionState OldState, SessionState NewState)>? SessionStateChanged;

        /// <summary>
        /// 事件：收到Select请求
        /// </summary>
        public event EventHandler<SecsSession>? SelectRequestReceived;

        /// <summary>
        /// 事件：收到Separate请求
        /// </summary>
        public event EventHandler<SecsSession>? SeparateRequestReceived;

        /// <summary>
        /// 事件：收到Linktest请求
        /// </summary>
        public event EventHandler<SecsSession>? LinktestRequestReceived;

        /// <summary>
        /// 创建新会话
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="clientId">客户端ID（可选）</param>
        /// <returns>新创建的会话对象</returns>
        public SecsSession CreateSession(byte deviceId, string? clientId = null)
        {
            lock (_lock)
            {
                // 生成唯一会话ID
                var sessionId = GenerateSessionId();
                var session = new SecsSession(sessionId, deviceId, clientId)
                {
                    State = SessionState.Connected
                };

                _sessions[sessionId] = session;
                return session;
            }
        }

        /// <summary>
        /// 根据会话ID获取会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>会话对象，如果不存在返回null</returns>
        public SecsSession? GetSession(int sessionId)
        {
            lock (_lock)
            {
                _sessions.TryGetValue(sessionId, out var session);
                return session;
            }
        }

        /// <summary>
        /// 处理Select请求 (S1F13)
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="clientId">客户端ID</param>
        /// <returns>选择结果：成功返回S1F14，失败返回错误代码</returns>
        public async Task<HsmsMessage> ProcessSelectRequest(int sessionId, byte deviceId, string clientId)
        {
            SecsSession? session;

            lock (_lock)
            {
                if (_sessions.ContainsKey(sessionId))
                {
                    // 会话已存在
                    session = _sessions[sessionId];
                }
                else
                {
                    // 创建新会话
                    session = CreateSession(deviceId, clientId);
                }
            }

            if (session == null)
            {
                // 创建失败，返回错误
                return CreateErrorMessage(1, 3, "Failed to create session");
            }

            var oldState = session.State;
            session.SetSelecting();
            SessionStateChanged?.Invoke(this, (sessionId, oldState, session.State));

            // 触发Select请求事件
            SelectRequestReceived?.Invoke(this, session);

            // 模拟处理延迟
            await Task.Delay(10);

            // 检查是否允许选择（这里简化处理，实际应检查设备ID等）
            if (deviceId == 0)
            {
                session.SetCommunicationFailure();
                SessionStateChanged?.Invoke(this, (sessionId, SessionState.Selecting, session.State));
                return CreateErrorMessage(1, 3, "Invalid device ID");
            }

            // 成功选择
            session.SetSelected();
            SessionStateChanged?.Invoke(this, (sessionId, SessionState.Selecting, session.State));

            // 返回S1F14响应
            return CreateSelectResponse(sessionId, deviceId, success: true);
        }

        /// <summary>
        /// 处理Select响应 (S1F14)
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="success">是否成功</param>
        public void ProcessSelectResponse(int sessionId, bool success)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    var oldState = session.State;
                    if (success)
                    {
                        session.SetSelected();
                    }
                    else
                    {
                        session.SetCommunicationFailure();
                    }
                    SessionStateChanged?.Invoke(this, (sessionId, oldState, session.State));
                }
            }
        }

        /// <summary>
        /// 处理Separate请求 (S1F15)
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>分离响应消息</returns>
        public async Task<HsmsMessage> ProcessSeparateRequest(int sessionId)
        {
            SecsSession? session;

            lock (_lock)
            {
                _sessions.TryGetValue(sessionId, out session);
            }

            if (session == null)
            {
                // 会话不存在，返回错误
                return CreateErrorMessage(1, 15, "Session not found");
            }

            var oldState = session.State;
            session.SetSeparating();
            SessionStateChanged?.Invoke(this, (sessionId, oldState, session.State));

            // 触发Separate请求事件
            SeparateRequestReceived?.Invoke(this, session);

            // 模拟处理延迟
            await Task.Delay(10);

            // 执行分离
            session.SetSeparated();

            // 删除会话
            lock (_lock)
            {
                _sessions.Remove(sessionId);
            }

            SessionStateChanged?.Invoke(this, (sessionId, SessionState.Separating, session.State));

            // 返回S1F16响应
            return CreateSeparateResponse(sessionId, success: true);
        }

        /// <summary>
        /// 处理Linktest请求 (S1F17)
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>链路测试响应</returns>
        public HsmsMessage ProcessLinktestRequest(int sessionId)
        {
            SecsSession? session;

            lock (_lock)
            {
                _sessions.TryGetValue(sessionId, out session);
            }

            if (session == null)
            {
                // 会话不存在
                return CreateErrorMessage(1, 17, "Session not found");
            }

            // 更新活动时间
            session.UpdateActivity();
            session.PerformLinkTest();

            // 触发Linktest事件
            LinktestRequestReceived?.Invoke(this, session);

            // 返回S1F18响应
            return CreateLinktestResponse(sessionId);
        }

        /// <summary>
        /// 结束会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="reason">结束原因</param>
        public void EndSession(int sessionId, string? reason = null)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    var oldState = session.State;
                    session.SetSeparated();
                    _sessions.Remove(sessionId);
                    SessionStateChanged?.Invoke(this, (sessionId, oldState, session.State));
                }
            }
        }

        /// <summary>
        /// 获取所有会话
        /// </summary>
        /// <returns>会话列表</returns>
        public List<SecsSession> GetAllSessions()
        {
            lock (_lock)
            {
                return _sessions.Values.ToList();
            }
        }

        /// <summary>
        /// 获取已选择的会话
        /// </summary>
        /// <returns>已选择的会话列表</returns>
        public List<SecsSession> GetSelectedSessions()
        {
            lock (_lock)
            {
                return _sessions.Values.Where(s => s.IsSelected).ToList();
            }
        }

        /// <summary>
        /// 清理超时会话
        /// </summary>
        /// <param name="timeoutMinutes">超时分钟数</param>
        /// <returns>清理的会话数量</returns>
        public int CleanupTimeoutSessions(int timeoutMinutes = 30)
        {
            var timeoutSessions = new List<int>();
            lock (_lock)
            {
                foreach (var kvp in _sessions)
                {
                    if (kvp.Value.IsTimeout(timeoutMinutes))
                    {
                        timeoutSessions.Add(kvp.Key);
                    }
                }

                foreach (var sessionId in timeoutSessions)
                {
                    _sessions.Remove(sessionId);
                }
            }

            return timeoutSessions.Count;
        }

        /// <summary>
        /// 生成唯一会话ID
        /// </summary>
        private int GenerateSessionId()
        {
            // 使用时间戳和随机数生成会话ID
            var timestamp = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() & 0xFFFF);
            var random = new Random().Next(0, 0xFFFF);
            return (timestamp << 16) | random;
        }

        /// <summary>
        /// 创建选择响应消息 (S1F14)
        /// </summary>
        private HsmsMessage CreateSelectResponse(int sessionId, byte deviceId, bool success)
        {
            var message = new HsmsMessage
            {
                Stream = 1,
                Function = 14,
                DeviceId = deviceId,
                SessionId = sessionId,
                RequireResponse = false,
                SenderId = "SERVER",
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                MessageType = "S1F14"
            };

            // 使用LIST格式（SECS-II标准）
            var data = new List<object> { success ? 1 : 0 };
            var bytes = SecsData.EncodeList(data);
            message.RawData = bytes;
            message.Content = success ? "SELECT_SUCCESS" : "SELECT_FAILED";

            return message;
        }

        /// <summary>
        /// 创建分离响应消息 (S1F16)
        /// </summary>
        private HsmsMessage CreateSeparateResponse(int sessionId, bool success)
        {
            var message = new HsmsMessage
            {
                Stream = 1,
                Function = 16,
                DeviceId = 1,
                SessionId = sessionId,
                RequireResponse = false,
                SenderId = "SERVER",
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                MessageType = "S1F16"
            };

            message.Content = success ? "SEPARATE_SUCCESS" : "SEPARATE_FAILED";
            return message;
        }

        /// <summary>
        /// 创建链路测试响应消息 (S1F18)
        /// </summary>
        private HsmsMessage CreateLinktestResponse(int sessionId)
        {
            var message = new HsmsMessage
            {
                Stream = 1,
                Function = 18,
                DeviceId = 1,
                SessionId = sessionId,
                RequireResponse = false,
                SenderId = "SERVER",
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                MessageType = "S1F18"
            };

            message.Content = "LINKTEST_OK";
            return message;
        }

        /// <summary>
        /// 创建错误消息
        /// </summary>
        private HsmsMessage CreateErrorMessage(int stream, int function, string error)
        {
            var message = new HsmsMessage
            {
                Stream = (ushort)stream,
                Function = (byte)function,
                DeviceId = 1,
                SessionId = 0,
                RequireResponse = false,
                SenderId = "SERVER",
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTime.Now,
                MessageType = $"S{stream}F{function}"
            };

            message.Content = error;
            return message;
        }

        /// <summary>
        /// 检查消息是否需要响应
        /// </summary>
        /// <param name="stream">Stream号码</param>
        /// <param name="function">Function号码</param>
        /// <returns>是否需要响应</returns>
        public static bool RequiresResponse(ushort stream, byte function)
        {
            // SECS-II标准：奇数Function需要响应
            return function % 2 == 1;
        }

        /// <summary>
        /// 获取对应的响应消息类型
        /// </summary>
        /// <param name="stream">Stream号码</param>
        /// <param name="function">Function号码</param>
        /// <returns>响应消息类型（S x F y+1）</returns>
        public static string GetResponseMessageType(ushort stream, byte function)
        {
            return $"S{stream}F{function + 1}";
        }
    }
}
