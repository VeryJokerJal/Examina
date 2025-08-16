namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 非组织学生DTO
/// </summary>
public class NonOrganizationStudentDto
{
    /// <summary>
    /// 学生ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 学生真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string CreatorUsername { get; set; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 最后更新者用户名
    /// </summary>
    public string UpdaterUsername { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活状态
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 关联的用户ID（如果学生后来注册了账户）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 关联的用户名（如果已注册）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }
}
