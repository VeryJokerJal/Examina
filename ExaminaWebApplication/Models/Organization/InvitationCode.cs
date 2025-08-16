using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 邀请码实体模型
/// </summary>
public class InvitationCode
{
    /// <summary>
    /// 邀请码ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 7位邀请码
    /// </summary>
    [Required]
    [StringLength(7, MinimumLength = 7)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 过期时间（可选，null表示永不过期）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 最大使用次数（可选，null表示无限制）
    /// </summary>
    public int? MaxUsage { get; set; }

    // 导航属性

    /// <summary>
    /// 所属组织
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// 使用此邀请码的学生组织关系集合
    /// </summary>
    public ICollection<StudentOrganization> StudentOrganizations { get; set; } = [];
}
