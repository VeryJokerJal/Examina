namespace Examina.Models.Organization;

/// <summary>
/// 学生组织关系DTO
/// </summary>
public class StudentOrganizationDto
{
    /// <summary>
    /// 关系ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// 学生用户名
    /// </summary>
    public string StudentUsername { get; set; } = string.Empty;

    /// <summary>
    /// 学生真实姓名
    /// </summary>
    public string? StudentRealName { get; set; }

    /// <summary>
    /// 学生手机号
    /// </summary>
    public string? StudentPhoneNumber { get; set; }

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
    /// 邀请码
    /// </summary>
    public string InvitationCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
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
}
