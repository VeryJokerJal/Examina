using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 用户设备实体模型
/// </summary>
public class UserDevice
{
    public int Id { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// 设备指纹（基于浏览器特征生成的唯一标识）
    /// </summary>
    [Required]
    [StringLength(255)]
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备名称（用户自定义或自动生成）
    /// </summary>
    [StringLength(100)]
    public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备类型（Desktop, Mobile, Tablet等）
    /// </summary>
    [StringLength(50)]
    public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统信息
    /// </summary>
    [StringLength(100)]
    public string? OperatingSystem { get; set; }
    
    /// <summary>
    /// 浏览器信息
    /// </summary>
    [StringLength(200)]
    public string? BrowserInfo { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 地理位置信息
    /// </summary>
    [StringLength(200)]
    public string? Location { get; set; }
    
    /// <summary>
    /// 设备首次绑定时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 设备令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 是否为受信任设备
    /// </summary>
    public bool IsTrusted { get; set; } = false;
    
    /// <summary>
    /// 关联的用户
    /// </summary>
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// 设备绑定请求模型
/// </summary>
public class DeviceBindRequest
{
    /// <summary>
    /// 设备指纹
    /// </summary>
    [Required]
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备类型
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统信息
    /// </summary>
    public string? OperatingSystem { get; set; }
    
    /// <summary>
    /// 浏览器信息
    /// </summary>
    public string? BrowserInfo { get; set; }
}

/// <summary>
/// 设备信息响应模型
/// </summary>
public class DeviceInfo
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? OperatingSystem { get; set; }
    public string? BrowserInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrusted { get; set; }
}
