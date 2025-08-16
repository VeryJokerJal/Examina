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
    /// 组织类型（学校或班级）
    /// </summary>
    public OrganizationType Type { get; set; }

    /// <summary>
    /// 父组织ID（班级的父组织是学校）
    /// </summary>
    public int? ParentOrganizationId { get; set; }

    /// <summary>
    /// 父组织名称（学校名称）
    /// </summary>
    public string? ParentOrganizationName { get; set; }





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
    /// <summary>
    /// 子组织数量（学校下的班级数量）
    /// </summary>
    public int ChildOrganizationCount { get; set; }
    public int InvitationCodeCount { get; set; }
}
