namespace Examina.Models.Organization;

/// <summary>
/// 加入组织结果
/// </summary>
public class JoinOrganizationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 学生组织关系DTO
    /// </summary>
    public StudentOrganizationDto? StudentOrganization { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static JoinOrganizationResult CreateSuccess(StudentOrganizationDto studentOrganization)
    {
        return new JoinOrganizationResult
        {
            Success = true,
            StudentOrganization = studentOrganization
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static JoinOrganizationResult CreateFailure(string errorMessage)
    {
        return new JoinOrganizationResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
