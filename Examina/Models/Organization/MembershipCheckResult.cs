namespace Examina.Models.Organization;

/// <summary>
/// 组织成员检查结果
/// </summary>
public class MembershipCheckResult
{
    /// <summary>
    /// 是否已是组织成员
    /// </summary>
    public bool IsMember { get; set; }

    /// <summary>
    /// 组织信息（如果是成员）
    /// </summary>
    public StudentOrganizationDto? Organization { get; set; }

    /// <summary>
    /// 检查结果消息
    /// </summary>
    public string? Message { get; set; }
}
