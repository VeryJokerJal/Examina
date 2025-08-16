using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 教师组织关系实体模型
/// 表示教师与组织（班级）之间的多对多关联关系
/// </summary>
public class TeacherOrganization
{
    /// <summary>
    /// 关系ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 教师用户ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(Teacher))]
    public int TeacherId { get; set; }

    /// <summary>
    /// 组织ID（外键，通常是班级）
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
    /// 关联的教师用户
    /// </summary>
    public virtual User Teacher { get; set; } = null!;

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
