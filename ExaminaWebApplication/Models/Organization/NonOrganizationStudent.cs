using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 非组织学生实体模型
/// 用于管理未通过组织邀请码加入的学生信息
/// </summary>
public class NonOrganizationStudent
{
    /// <summary>
    /// 学生ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 学生真实姓名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required]
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

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
    /// 最后更新时间
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新者用户ID
    /// </summary>
    [Required]
    public int UpdatedBy { get; set; }

    /// <summary>
    /// 是否激活状态
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联的用户ID（如果学生后来注册了账户）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    #region 导航属性

    /// <summary>
    /// 关联的用户（如果已注册）
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User Creator { get; set; } = null!;

    /// <summary>
    /// 最后更新者用户
    /// </summary>
    [ForeignKey(nameof(UpdatedBy))]
    public virtual User Updater { get; set; } = null!;

    #endregion
}
