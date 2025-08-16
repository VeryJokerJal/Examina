namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 组织成员DTO - 简化版本，只包含必要的核心字段
/// </summary>
public class OrganizationMemberDto
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 关联的用户ID（如果已注册）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string? CreatedByUsername { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
