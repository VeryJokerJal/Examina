namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 邀请码信息DTO
/// </summary>
public class InvitationCodeDto
{
    /// <summary>
    /// 邀请码ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 7位邀请码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 最大使用次数
    /// </summary>
    public int? MaxUsage { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// 是否已达到使用上限
    /// </summary>
    public bool IsMaxUsageReached => MaxUsage.HasValue && UsageCount >= MaxUsage.Value;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable => IsActive && !IsExpired && !IsMaxUsageReached;
}
