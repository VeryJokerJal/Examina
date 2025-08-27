using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Admin;

/// <summary>
/// 系统配置实体模型
/// </summary>
public class SystemConfiguration
{
    public int Id { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ConfigValue { get; set; } = string.Empty;

    /// <summary>
    /// 配置描述
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 配置分类
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 更新者用户ID
    /// </summary>
    public int? UpdatedBy { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    public virtual User? Creator { get; set; }

    /// <summary>
    /// 更新者用户
    /// </summary>
    public virtual User? Updater { get; set; }
}

/// <summary>
/// 系统配置常量
/// </summary>
public static class SystemConfigurationKeys
{
    /// <summary>
    /// 用户最大设备数量限制
    /// </summary>
    public const string MaxDeviceCountLimit = "MaxDeviceCountLimit";

    /// <summary>
    /// 是否启用设备数量限制
    /// </summary>
    public const string EnableDeviceCountLimit = "EnableDeviceCountLimit";

    /// <summary>
    /// 设备踢出策略（oldest: 踢出最早登录的设备, reject: 拒绝新登录）
    /// </summary>
    public const string DeviceKickoutPolicy = "DeviceKickoutPolicy";

    /// <summary>
    /// 设备会话过期时间（天）
    /// </summary>
    public const string DeviceSessionExpirationDays = "DeviceSessionExpirationDays";
}

/// <summary>
/// 设备踢出策略枚举
/// </summary>
public enum DeviceKickoutPolicy
{
    /// <summary>
    /// 踢出最早登录的设备
    /// </summary>
    KickoutOldest = 1,

    /// <summary>
    /// 拒绝新登录
    /// </summary>
    RejectNew = 2
}

/// <summary>
/// 系统配置DTO
/// </summary>
public class SystemConfigurationDto
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatorName { get; set; }
    public string? UpdaterName { get; set; }
}

/// <summary>
/// 系统配置更新请求模型
/// </summary>
public class UpdateSystemConfigurationRequest
{
    [Required]
    public string ConfigKey { get; set; } = string.Empty;

    [Required]
    public string ConfigValue { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>
/// 设备限制配置模型
/// </summary>
public class DeviceLimitConfigurationModel
{
    /// <summary>
    /// 最大设备数量
    /// </summary>
    [Range(1, 50, ErrorMessage = "设备数量限制必须在1到50之间")]
    public int MaxDeviceCount { get; set; } = 3;

    /// <summary>
    /// 是否启用设备数量限制
    /// </summary>
    public bool EnableDeviceLimit { get; set; } = true;

    /// <summary>
    /// 设备踢出策略
    /// </summary>
    public DeviceKickoutPolicy KickoutPolicy { get; set; } = DeviceKickoutPolicy.KickoutOldest;

    /// <summary>
    /// 设备会话过期天数
    /// </summary>
    [Range(1, 365, ErrorMessage = "设备会话过期天数必须在1到365之间")]
    public int SessionExpirationDays { get; set; } = 30;
}
