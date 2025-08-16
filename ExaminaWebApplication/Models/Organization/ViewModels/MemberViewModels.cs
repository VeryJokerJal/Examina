namespace ExaminaWebApplication.Models.Organization.ViewModels;

/// <summary>
/// 成员管理视图模型
/// </summary>
public class MemberManagementViewModel
{
    /// <summary>
    /// 成员列表
    /// </summary>
    public List<MemberDto> Members { get; set; } = new();
}

/// <summary>
/// 成员DTO（非组织成员）
/// </summary>
public class MemberDto
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 加入时间
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
}
