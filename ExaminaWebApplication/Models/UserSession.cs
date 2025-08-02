using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 会话类型枚举
/// </summary>
public enum SessionType
{
    /// <summary>
    /// JWT令牌会话（学生）
    /// </summary>
    JwtToken = 1,
    
    /// <summary>
    /// Cookie会话（管理员/教师）
    /// </summary>
    Cookie = 2
}

/// <summary>
/// 用户会话实体模型
/// </summary>
public class UserSession
{
    public int Id { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// 设备ID（可选，用于JWT会话）
    /// </summary>
    public int? DeviceId { get; set; }
    
    /// <summary>
    /// 会话令牌（JWT令牌或会话ID）
    /// </summary>
    [Required]
    [StringLength(500)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌（仅用于JWT会话）
    /// </summary>
    [StringLength(500)]
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionType SessionType { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 用户代理信息
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// 地理位置信息
    /// </summary>
    [StringLength(200)]
    public string? Location { get; set; }
    
    /// <summary>
    /// 会话创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 会话过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 登出时间（如果已登出）
    /// </summary>
    public DateTime? LogoutAt { get; set; }
    
    /// <summary>
    /// 关联的用户
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// 关联的设备（可选）
    /// </summary>
    public virtual UserDevice? Device { get; set; }
}

/// <summary>
/// 会话信息响应模型
/// </summary>
public class SessionInfo
{
    public int Id { get; set; }
    public SessionType SessionType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DeviceInfo? Device { get; set; }
}

/// <summary>
/// 刷新令牌请求模型
/// </summary>
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
    
    public string? DeviceFingerprint { get; set; }
}

/// <summary>
/// 刷新令牌响应模型
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
