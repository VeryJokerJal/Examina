using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 组织成员实体模型
/// 用于存储组织成员的基本信息，不依赖于用户注册状态
/// </summary>
public class OrganizationMember
{
    /// <summary>
    /// 成员ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(100)]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号/工号
    /// </summary>
    [StringLength(50)]
    public string? StudentId { get; set; }

    /// <summary>
    /// 组织ID（外键，可为空表示非组织成员）
    /// </summary>
    [ForeignKey(nameof(Organization))]
    public int? OrganizationId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [Required]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

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
    /// 关联的用户ID（如果用户已注册）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    #region 导航属性

    /// <summary>
    /// 关联的组织（可为空，表示非组织成员）
    /// </summary>
    public virtual Organization? Organization { get; set; }

    /// <summary>
    /// 关联的用户（如果已注册）
    /// </summary>
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
