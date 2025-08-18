using Examina.Models;
using Examina.Models.Organization;

namespace Examina.Services;

/// <summary>
/// 组织服务接口
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// 用户加入组织
    /// </summary>
    /// <param name="invitationCode">邀请码</param>
    /// <returns>加入结果</returns>
    Task<JoinOrganizationResult> JoinOrganizationAsync(string invitationCode);

    /// <summary>
    /// 检查用户是否已加入组织
    /// </summary>
    /// <returns>是否已加入组织</returns>
    Task<bool> IsUserInOrganizationAsync();

    /// <summary>
    /// 获取用户的组织信息
    /// </summary>
    /// <returns>用户组织信息，如果未加入则返回null</returns>
    Task<StudentOrganizationDto?> GetUserOrganizationAsync();
}
