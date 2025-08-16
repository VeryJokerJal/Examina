namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 组织成员DTO
/// </summary>
public class OrganizationMemberDto
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 学号/工号
    /// </summary>
    public string? StudentId { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 组织ID
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间（相当于加入时间）
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 关联的用户ID（如果已注册）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    public string? CreatedByUsername { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // 为了与现有视图兼容，添加映射属性
    
    /// <summary>
    /// 学生用户名（兼容性属性）
    /// </summary>
    public string StudentUsername => Username;

    /// <summary>
    /// 学生真实姓名（兼容性属性）
    /// </summary>
    public string? StudentRealName => RealName;

    /// <summary>
    /// 学生学号（兼容性属性）
    /// </summary>
    public string? StudentId_Number => StudentId;

    /// <summary>
    /// 学生手机号（兼容性属性）
    /// </summary>
    public string? StudentPhoneNumber => PhoneNumber;

    /// <summary>
    /// 邀请码（兼容性属性，OrganizationMember 中暂时返回空）
    /// </summary>
    public string InvitationCode => "直接添加";

    /// <summary>
    /// 学生ID（兼容性属性，使用成员ID）
    /// </summary>
    public int StudentId_Compat => Id;

    /// <summary>
    /// 学生ID（兼容性属性，用于JavaScript函数）
    /// </summary>
    public int StudentId => Id;
}
