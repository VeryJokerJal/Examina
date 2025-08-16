namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 学生组织关系实体模型
/// </summary>
public class StudentOrganization
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
    /// 组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 使用的邀请码ID
    /// </summary>
    public int InvitationCodeId { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性

    /// <summary>
    /// 学生用户
    /// </summary>
    public User Student { get; set; } = null!;

    /// <summary>
    /// 组织
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// 邀请码
    /// </summary>
    public InvitationCode InvitationCode { get; set; } = null!;
}
