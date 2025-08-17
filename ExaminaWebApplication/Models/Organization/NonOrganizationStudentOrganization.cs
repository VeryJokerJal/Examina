using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 非组织学生与组织关系实体模型
/// 表示非组织学生与班级之间的关联关系
/// </summary>
public class NonOrganizationStudentOrganization
{
    /// <summary>
    /// 关系ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 非组织学生ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(NonOrganizationStudent))]
    public int NonOrganizationStudentId { get; set; }

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
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否激活状态
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [Required]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    #region 导航属性

    /// <summary>
    /// 关联的非组织学生
    /// </summary>
    public virtual NonOrganizationStudent NonOrganizationStudent { get; set; } = null!;

    /// <summary>
    /// 关联的组织（班级）
    /// </summary>
    public virtual Organization Organization { get; set; } = null!;

    /// <summary>
    /// 创建者用户
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User Creator { get; set; } = null!;

    #endregion
}
