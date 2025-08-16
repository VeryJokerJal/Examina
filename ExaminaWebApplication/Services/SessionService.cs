using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 会话管理服务实现
/// </summary>
public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;
    private readonly IConfiguration _configuration;

    public SessionService(ApplicationDbContext context, ILogger<SessionService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserSession> CreateSessionAsync(int userId, string sessionToken, SessionType sessionType,
        int? deviceId = null, string? refreshToken = null, string? ipAddress = null,
        string? userAgent = null, string? location = null, DateTime? expiresAt = null)
    {
        try
        {
            // 设置默认过期时间
            if (expiresAt == null)
            {
                int expirationDays = sessionType == SessionType.JwtToken ? 7 : 7; // 默认7天
                expiresAt = DateTime.UtcNow.AddDays(expirationDays);
            }

            // 对于JWT会话，如果sessionToken过长，生成一个较短的会话标识符
            string actualSessionToken = sessionToken;
            if (sessionType == SessionType.JwtToken && sessionToken.Length > 450)
            {
                // 生成一个唯一的会话标识符，格式：JWT_用户ID_时间戳_随机数
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string randomPart = Guid.NewGuid().ToString("N")[..8]; // 取前8位
                actualSessionToken = $"JWT_{userId}_{timestamp}_{randomPart}";

                _logger.LogInformation("JWT令牌过长({Length}字符)，生成会话标识符: {SessionId}",
                    sessionToken.Length, actualSessionToken);
            }

            UserSession session = new()
            {
                UserId = userId,
                DeviceId = deviceId,
                SessionToken = actualSessionToken,
                RefreshToken = refreshToken,
                SessionType = sessionType,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Location = location,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = expiresAt.Value,
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("创建用户会话成功，用户ID: {UserId}, 会话类型: {SessionType}, 会话标识: {SessionToken}",
                userId, sessionType, actualSessionToken);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户会话失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSession?> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken 
                    && s.IsActive 
                    && s.ExpiresAt > DateTime.UtcNow);

            if (session != null && session.User.IsActive)
            {
                return session;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证会话失败，会话令牌: {SessionToken}", sessionToken);
            return null;
        }
    }

    public async Task<bool> UpdateSessionActivityAsync(int sessionId, string? ipAddress = null, string? location = null)
    {
        try
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session == null || !session.IsActive)
            {
                return false;
            }

            session.LastActivityAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(ipAddress))
            {
                session.IpAddress = ipAddress;
            }
            if (!string.IsNullOrEmpty(location))
            {
                session.Location = location;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新会话活动时间失败，会话ID: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> EndSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

            if (session == null)
            {
                return false;
            }

            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("结束会话成功，会话ID: {SessionId}", session.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束会话失败，会话令牌: {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<int> EndAllUserSessionsAsync(int userId, int? excludeSessionId = null)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (excludeSessionId.HasValue)
            {
                sessions = sessions.Where(s => s.Id != excludeSessionId.Value).ToList();
            }

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("结束用户所有会话，用户ID: {UserId}, 结束会话数: {Count}", userId, sessions.Count);
            return sessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束用户所有会话失败，用户ID: {UserId}", userId);
            return 0;
        }
    }

    public async Task<List<SessionInfo>> GetUserActiveSessionsAsync(int userId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Include(s => s.Device)
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => new SessionInfo
                {
                    Id = s.Id,
                    SessionType = s.SessionType,
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    Location = s.Location,
                    CreatedAt = s.CreatedAt,
                    LastActivityAt = s.LastActivityAt,
                    ExpiresAt = s.ExpiresAt,
                    IsActive = s.IsActive,
                    Device = s.Device != null ? new DeviceInfo
                    {
                        Id = s.Device.Id,
                        DeviceName = s.Device.DeviceName,
                        DeviceType = s.Device.DeviceType,
                        OperatingSystem = s.Device.OperatingSystem,
                        BrowserInfo = s.Device.BrowserInfo,
                        IpAddress = s.Device.IpAddress,
                        Location = s.Device.Location,
                        CreatedAt = s.Device.CreatedAt,
                        LastUsedAt = s.Device.LastUsedAt,
                        IsActive = s.Device.IsActive,
                        IsTrusted = s.Device.IsTrusted
                    } : null
                })
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户活跃会话失败，用户ID: {UserId}", userId);
            return [];
        }
    }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken 
                    && s.IsActive 
                    && s.ExpiresAt > DateTime.UtcNow);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过刷新令牌获取会话失败，刷新令牌: {RefreshToken}", refreshToken);
            return null;
        }
    }

    public async Task<bool> UpdateSessionRefreshTokenAsync(int sessionId, string newRefreshToken, DateTime newExpiresAt)
    {
        try
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session == null || !session.IsActive)
            {
                return false;
            }

            session.RefreshToken = newRefreshToken;
            session.ExpiresAt = newExpiresAt;
            session.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新会话刷新令牌失败，会话ID: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow || !s.IsActive)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                if (session.LogoutAt == null)
                {
                    session.LogoutAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("清理过期会话，数量: {Count}", expiredSessions.Count);
            return expiredSessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期会话失败");
            return 0;
        }
    }

    public async Task<SessionStatistics> GetSessionStatisticsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var statistics = new SessionStatistics
            {
                TotalActiveSessions = await _context.UserSessions
                    .CountAsync(s => s.IsActive && s.ExpiresAt > now),
                
                JwtSessions = await _context.UserSessions
                    .CountAsync(s => s.IsActive && s.ExpiresAt > now && s.SessionType == SessionType.JwtToken),
                
                CookieSessions = await _context.UserSessions
                    .CountAsync(s => s.IsActive && s.ExpiresAt > now && s.SessionType == SessionType.Cookie),
                
                TodayNewSessions = await _context.UserSessions
                    .CountAsync(s => s.CreatedAt >= today),
                
                OnlineUsers = await _context.UserSessions
                    .Where(s => s.IsActive && s.ExpiresAt > now)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync()
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会话统计信息失败");
            return new SessionStatistics();
        }
    }
}
