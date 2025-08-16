using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 预配置用户信息模型
/// 用于存储尚未注册但已预先配置的用户信息
/// </summary>
public class PreConfiguredUser
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 预配置的用户名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 预配置的手机号
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 预配置的真实姓名
    /// </summary>
    [StringLength(100)]
    public string? RealName { get; set; }

    /// <summary>
    /// 预配置的学号/工号
    /// </summary>
    [StringLength(50)]
    public string? StudentId { get; set; }

    /// <summary>
    /// 关联的组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 关联的组织
    /// </summary>
    public Organization.Organization? Organization { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    public User? Creator { get; set; }

    /// <summary>
    /// 是否已被应用（用户注册后关联）
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// 应用时间
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// 应用到的用户ID
    /// </summary>
    public int? AppliedToUserId { get; set; }

    /// <summary>
    /// 应用到的用户
    /// </summary>
    public User? AppliedToUser { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
