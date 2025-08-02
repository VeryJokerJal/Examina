using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 会话管理服务接口
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// 创建用户会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="sessionToken">会话令牌</param>
    /// <param name="sessionType">会话类型</param>
    /// <param name="deviceId">设备ID（可选）</param>
    /// <param name="refreshToken">刷新令牌（可选）</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="location">地理位置</param>
    /// <param name="expiresAt">过期时间</param>
    /// <returns>创建的会话</returns>
    Task<UserSession> CreateSessionAsync(int userId, string sessionToken, SessionType sessionType, 
        int? deviceId = null, string? refreshToken = null, string? ipAddress = null, 
        string? userAgent = null, string? location = null, DateTime? expiresAt = null);
    
    /// <summary>
    /// 验证会话是否有效
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <returns>会话信息，如果无效则返回null</returns>
    Task<UserSession?> ValidateSessionAsync(string sessionToken);
    
    /// <summary>
    /// 更新会话活动时间
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="location">地理位置</param>
    /// <returns>是否成功更新</returns>
    Task<bool> UpdateSessionActivityAsync(int sessionId, string? ipAddress = null, string? location = null);
    
    /// <summary>
    /// 结束会话（登出）
    /// </summary>
    /// <param name="sessionToken">会话令牌</param>
    /// <returns>是否成功结束</returns>
    Task<bool> EndSessionAsync(string sessionToken);
    
    /// <summary>
    /// 结束用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="excludeSessionId">排除的会话ID（可选）</param>
    /// <returns>结束的会话数量</returns>
    Task<int> EndAllUserSessionsAsync(int userId, int? excludeSessionId = null);
    
    /// <summary>
    /// 获取用户的活跃会话列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>会话信息列表</returns>
    Task<List<SessionInfo>> GetUserActiveSessionsAsync(int userId);
    
    /// <summary>
    /// 通过刷新令牌获取会话
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>会话信息，如果无效则返回null</returns>
    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// 更新会话的刷新令牌
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="newRefreshToken">新的刷新令牌</param>
    /// <param name="newExpiresAt">新的过期时间</param>
    /// <returns>是否成功更新</returns>
    Task<bool> UpdateSessionRefreshTokenAsync(int sessionId, string newRefreshToken, DateTime newExpiresAt);
    
    /// <summary>
    /// 清理过期会话
    /// </summary>
    /// <returns>清理的会话数量</returns>
    Task<int> CleanupExpiredSessionsAsync();
    
    /// <summary>
    /// 获取会话统计信息
    /// </summary>
    /// <returns>会话统计</returns>
    Task<SessionStatistics> GetSessionStatisticsAsync();
}

/// <summary>
/// 会话统计信息
/// </summary>
public class SessionStatistics
{
    /// <summary>
    /// 总活跃会话数
    /// </summary>
    public int TotalActiveSessions { get; set; }
    
    /// <summary>
    /// JWT会话数
    /// </summary>
    public int JwtSessions { get; set; }
    
    /// <summary>
    /// Cookie会话数
    /// </summary>
    public int CookieSessions { get; set; }
    
    /// <summary>
    /// 今日新增会话数
    /// </summary>
    public int TodayNewSessions { get; set; }
    
    /// <summary>
    /// 在线用户数
    /// </summary>
    public int OnlineUsers { get; set; }
}
