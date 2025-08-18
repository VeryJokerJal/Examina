namespace Examina.Models.Organization;

/// <summary>
/// 加入组织API响应模型
/// </summary>
public class JoinOrganizationResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 加入的组织信息（成功时）
    /// </summary>
    public OrganizationInfo? Organization { get; set; }

    /// <summary>
    /// 学生在组织中的信息（成功时）
    /// </summary>
    public StudentOrganizationInfo? StudentOrganization { get; set; }
}

/// <summary>
/// 组织信息
/// </summary>
public class OrganizationInfo
{
    /// <summary>
    /// 组织ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组织类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 组织描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 学生组织关系信息
/// </summary>
public class StudentOrganizationInfo
{
    /// <summary>
    /// 关系ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
