using ExaminaWebApplication.Models.Organization;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 邀请码服务接口
/// </summary>
public interface IInvitationCodeService
{
    /// <summary>
    /// 生成唯一的7位邀请码
    /// </summary>
    /// <returns>7位邀请码</returns>
    Task<string> GenerateUniqueCodeAsync();

    /// <summary>
    /// 验证邀请码是否有效
    /// </summary>
    /// <param name="code">邀请码</param>
    /// <returns>邀请码实体，如果无效则返回null</returns>
    Task<InvitationCode?> ValidateInvitationCodeAsync(string code);

    /// <summary>
    /// 检查邀请码是否可用（激活、未过期、未达到使用上限）
    /// </summary>
    /// <param name="invitationCode">邀请码实体</param>
    /// <returns>是否可用</returns>
    bool IsInvitationCodeAvailable(InvitationCode invitationCode);

    /// <summary>
    /// 增加邀请码使用次数
    /// </summary>
    /// <param name="invitationCodeId">邀请码ID</param>
    /// <returns>是否成功</returns>
    Task<bool> IncrementUsageCountAsync(int invitationCodeId);

    /// <summary>
    /// 创建邀请码
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <param name="expiresAt">过期时间（可选）</param>
    /// <param name="maxUsage">最大使用次数（可选）</param>
    /// <returns>创建的邀请码</returns>
    Task<InvitationCode> CreateInvitationCodeAsync(int organizationId, DateTime? expiresAt = null, int? maxUsage = null);

    /// <summary>
    /// 停用邀请码
    /// </summary>
    /// <param name="invitationCodeId">邀请码ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeactivateInvitationCodeAsync(int invitationCodeId);

    /// <summary>
    /// 更新邀请码信息
    /// </summary>
    /// <param name="invitationCodeId">邀请码ID</param>
    /// <param name="maxUsage">最大使用次数（可选）</param>
    /// <param name="expiresAt">过期时间（可选）</param>
    /// <param name="isActive">是否激活（可选）</param>
    /// <returns>更新后的邀请码，如果不存在则返回null</returns>
    Task<InvitationCode?> UpdateInvitationCodeAsync(int invitationCodeId, int? maxUsage = null, DateTime? expiresAt = null, bool? isActive = null);

    /// <summary>
    /// 删除邀请码
    /// </summary>
    /// <param name="invitationCodeId">邀请码ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteInvitationCodeAsync(int invitationCodeId);

    /// <summary>
    /// 设置邀请码激活状态
    /// </summary>
    /// <param name="invitationCodeId">邀请码ID</param>
    /// <param name="isActive">是否激活</param>
    /// <returns>是否成功</returns>
    Task<bool> SetInvitationCodeStatusAsync(int invitationCodeId, bool isActive);

    /// <summary>
    /// 获取组织的邀请码列表
    /// </summary>
    /// <param name="organizationId">组织ID</param>
    /// <param name="includeInactive">是否包含非激活的邀请码</param>
    /// <returns>邀请码列表</returns>
    Task<List<InvitationCode>> GetOrganizationInvitationCodesAsync(int organizationId, bool includeInactive = false);
}
