using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 学生加入组织响应模型
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

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="studentOrganization">学生组织关系DTO</param>
    /// <returns>成功响应</returns>
    public static JoinOrganizationResponse CreateSuccess(StudentOrganizationDto studentOrganization)
    {
        return new JoinOrganizationResponse
        {
            Success = true,
            Organization = new OrganizationInfo
            {
                Id = studentOrganization.OrganizationId,
                Name = studentOrganization.OrganizationName,
                Type = studentOrganization.OrganizationType.ToString(),
                Description = studentOrganization.OrganizationDescription
            },
            StudentOrganization = new StudentOrganizationInfo
            {
                Id = studentOrganization.Id,
                JoinedAt = studentOrganization.JoinedAt,
                IsActive = studentOrganization.IsActive,
                Role = studentOrganization.Role.ToString()
            }
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>失败响应</returns>
    public static JoinOrganizationResponse CreateFailure(string errorMessage)
    {
        return new JoinOrganizationResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
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
