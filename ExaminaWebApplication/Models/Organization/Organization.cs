using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization;

/// <summary>
/// 组织实体模型（支持学校-班级层次结构）
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
    /// 组织类型（学校或班级）
    /// </summary>
    [Required]
    public OrganizationType Type { get; set; } = OrganizationType.School;

    /// <summary>
    /// 父组织ID（班级的父组织是学校，学校的父组织为null）
    /// </summary>
    public int? ParentOrganizationId { get; set; }





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
    /// 父组织（学校）
    /// </summary>
    public Organization? ParentOrganization { get; set; }

    /// <summary>
    /// 子组织集合（班级）
    /// </summary>
    public ICollection<Organization> ChildOrganizations { get; set; } = new List<Organization>();

    /// <summary>
    /// 邀请码集合（仅班级有邀请码）
    /// </summary>
    public ICollection<InvitationCode> InvitationCodes { get; set; } = new List<InvitationCode>();

    /// <summary>
    /// 教师组织关系集合
    /// </summary>
    public ICollection<TeacherOrganization> TeacherOrganizations { get; set; } = new List<TeacherOrganization>();

    /// <summary>
    /// 学生组织关系集合
    /// </summary>
    public ICollection<StudentOrganization> StudentOrganizations { get; set; } = new List<StudentOrganization>();
}
