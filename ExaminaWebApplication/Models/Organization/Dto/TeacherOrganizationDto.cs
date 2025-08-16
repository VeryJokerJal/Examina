namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 教师组织关系DTO
/// </summary>
public class TeacherOrganizationDto
{
    /// <summary>
    /// 关系ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 教师用户ID
    /// </summary>
    public int TeacherId { get; set; }

    /// <summary>
    /// 教师用户名
    /// </summary>
    public string TeacherUsername { get; set; } = string.Empty;

    /// <summary>
    /// 教师真实姓名
    /// </summary>
    public string? TeacherRealName { get; set; }

    /// <summary>
    /// 教师邮箱
    /// </summary>
    public string TeacherEmail { get; set; } = string.Empty;

    /// <summary>
    /// 教师手机号
    /// </summary>
    public string? TeacherPhoneNumber { get; set; }

    /// <summary>
    /// 组织ID（班级）
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 组织名称（班级名称）
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 组织类型
    /// </summary>
    public OrganizationType OrganizationType { get; set; }

    /// <summary>
    /// 父组织ID（学校ID）
    /// </summary>
    public int? ParentOrganizationId { get; set; }

    /// <summary>
    /// 父组织名称（学校名称）
    /// </summary>
    public string? ParentOrganizationName { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 是否激活状态
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string CreatorUsername { get; set; } = string.Empty;

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }
}
