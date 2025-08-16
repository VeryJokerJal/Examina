namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 组织信息DTO
/// </summary>
public class OrganizationDto
{
    /// <summary>
    /// 组织ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组织类型
    /// </summary>
    public OrganizationType Type { get; set; }

    /// <summary>
    /// 组织类型显示名称
    /// </summary>
    public string TypeDisplayName => Type switch
    {
        OrganizationType.School => "学校组织",
        OrganizationType.Institution => "机构组织",
        _ => "未知类型"
    };

    /// <summary>
    /// 组织描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string CreatorUsername { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 学生数量
    /// </summary>
    public int StudentCount { get; set; }

    /// <summary>
    /// 邀请码数量
    /// </summary>
    public int InvitationCodeCount { get; set; }
}
