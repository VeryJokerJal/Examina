using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 学生组织关系实体模型
/// 表示学生与组织之间的关联关系，包含加入时间、邀请码等核心信息
/// </summary>
public class StudentOrganization
{
    /// <summary>
    /// 关系ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 学生用户ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(Student))]
    public int StudentId { get; set; }

    /// <summary>
    /// 组织ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(Organization))]
    public int OrganizationId { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    [Required]
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 使用的邀请码ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(InvitationCode))]
    public int InvitationCodeId { get; set; }

    /// <summary>
    /// 是否激活状态
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    #region 导航属性

    /// <summary>
    /// 关联的学生用户
    /// 通过此属性可以访问学生的详细信息（姓名、电话、学号等）
    /// </summary>
    public virtual User Student { get; set; } = null!;

    /// <summary>
    /// 关联的组织
    /// 通过此属性可以访问组织的详细信息（名称、描述等）
    /// </summary>
    public virtual Organization Organization { get; set; } = null!;

    /// <summary>
    /// 关联的邀请码
    /// 通过此属性可以访问邀请码的详细信息（代码、有效期等）
    /// </summary>
    public virtual InvitationCode InvitationCode { get; set; } = null!;

    #endregion
}
