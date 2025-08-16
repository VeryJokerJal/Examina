using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 组织实体模型
/// </summary>
public class Organization
{
    /// <summary>
    /// 组织ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性

    /// <summary>
    /// 创建者
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// 邀请码集合
    /// </summary>
    public ICollection<InvitationCode> InvitationCodes { get; set; } = [];

    /// <summary>
    /// 学生组织关系集合
    /// </summary>
    public ICollection<StudentOrganization> StudentOrganizations { get; set; } = [];
}
