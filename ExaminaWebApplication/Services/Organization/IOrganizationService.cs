using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 组织管理服务接口
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// 创建组织
    /// </summary>
    /// <param name="request">创建组织请求</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>创建的组织DTO</returns>
    Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationRequest request, int creatorUserId);

    /// <summary>
    /// 获取组织列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活的组织</param>
    /// <returns>组织DTO列表</returns>
    Task<List<OrganizationDto>> GetOrganizationsAsync(bool includeInactive = false);

    /// <summary>
    /// 根据ID获取组织详情
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <returns>组织DTO，如果不存在则返回null</returns>
    Task<OrganizationDto?> GetOrganizationByIdAsync(int organizationId);

    /// <summary>
    /// 更新组织信息
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <param name="name">组织名称</param>
    /// <param name="description">组织描述</param>
    /// <returns>更新后的组织DTO，如果失败则返回null</returns>
    Task<OrganizationDto?> UpdateOrganizationAsync(int organizationId, string name, string? description = null);

    /// <summary>
    /// 停用组织
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeactivateOrganizationAsync(int organizationId);

    /// <summary>
    /// 用户加入组织（支持学生和教师）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userRole">用户角色</param>
    /// <param name="invitationCode">邀请码</param>
    /// <returns>加入结果</returns>
    Task<JoinOrganizationResult> JoinOrganizationAsync(int userId, UserRole userRole, string invitationCode);

    /// <summary>
    /// 用户退出组织
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="organizationId">组织ID</param>
    /// <returns>是否成功</returns>
    Task<bool> LeaveOrganizationAsync(int userId, int organizationId);

    /// <summary>
    /// 获取用户已加入的组织列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户组织关系DTO列表</returns>
    Task<List<StudentOrganizationDto>> GetUserOrganizationsAsync(int userId);

    /// <summary>
    /// 获取组织的成员列表
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <param name="includeInactive">是否包含非激活的关系</param>
    /// <returns>组织成员关系DTO列表</returns>
    Task<List<StudentOrganizationDto>> GetOrganizationMembersAsync(int organizationId, bool includeInactive = false);

    /// <summary>
    /// 检查用户是否已在组织中
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="organizationId">组织ID</param>
    /// <returns>是否已在组织中</returns>
    Task<bool> IsUserInOrganizationAsync(int userId, int organizationId);
}

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
