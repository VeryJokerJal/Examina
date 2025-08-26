using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ImportedExamEntity = ExaminaWebApplication.Models.ImportedExam.ImportedExam;
using OrganizationEntity = ExaminaWebApplication.Models.Organization.Organization;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 考试与学校关联实体模型
/// 表示哪些学校可以参与特定的统考
/// </summary>
public class ExamSchoolAssociation
{
    /// <summary>
    /// 关联ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 考试ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(Exam))]
    public int ExamId { get; set; }

    /// <summary>
    /// 学校组织ID（外键）
    /// </summary>
    [Required]
    [ForeignKey(nameof(School))]
    public int SchoolId { get; set; }

    /// <summary>
    /// 关联创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联创建者ID
    /// </summary>
    [Required]
    [ForeignKey(nameof(Creator))]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 是否激活状态
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500)]
    public string? Remarks { get; set; }

    #region 导航属性

    /// <summary>
    /// 关联的考试
    /// </summary>
    public virtual ImportedExamEntity Exam { get; set; } = null!;

    /// <summary>
    /// 关联的学校组织
    /// </summary>
    public virtual OrganizationEntity School { get; set; } = null!;

    /// <summary>
    /// 关联创建者
    /// </summary>
    public virtual User Creator { get; set; } = null!;

    #endregion
}
