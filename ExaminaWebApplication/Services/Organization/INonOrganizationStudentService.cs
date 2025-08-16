using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 非组织学生管理服务接口
/// </summary>
public interface INonOrganizationStudentService
{
    /// <summary>
    /// 创建非组织学生
    /// </summary>
    /// <param name="realName">学生真实姓名</param>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <param name="notes">备注信息</param>
    /// <returns>创建的非组织学生DTO</returns>
    Task<NonOrganizationStudentDto?> CreateStudentAsync(string realName, string phoneNumber, int creatorUserId, string? notes = null);

    /// <summary>
    /// 更新非组织学生信息
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="realName">学生真实姓名</param>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <param name="notes">备注信息</param>
    /// <returns>更新后的非组织学生DTO</returns>
    Task<NonOrganizationStudentDto?> UpdateStudentAsync(int studentId, string realName, string phoneNumber, int updaterUserId, string? notes = null);

    /// <summary>
    /// 删除非组织学生（软删除）
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteStudentAsync(int studentId, int updaterUserId);

    /// <summary>
    /// 获取非组织学生列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活的学生</param>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>非组织学生DTO列表</returns>
    Task<List<NonOrganizationStudentDto>> GetStudentsAsync(bool includeInactive = false, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 根据ID获取非组织学生
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>非组织学生DTO</returns>
    Task<NonOrganizationStudentDto?> GetStudentByIdAsync(int studentId);

    /// <summary>
    /// 根据手机号搜索非组织学生
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="includeInactive">是否包含非激活的学生</param>
    /// <returns>非组织学生DTO列表</returns>
    Task<List<NonOrganizationStudentDto>> SearchStudentsByPhoneAsync(string phoneNumber, bool includeInactive = false);

    /// <summary>
    /// 根据姓名搜索非组织学生
    /// </summary>
    /// <param name="realName">学生真实姓名</param>
    /// <param name="includeInactive">是否包含非激活的学生</param>
    /// <returns>非组织学生DTO列表</returns>
    Task<List<NonOrganizationStudentDto>> SearchStudentsByNameAsync(string realName, bool includeInactive = false);

    /// <summary>
    /// 关联非组织学生到已注册用户
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> LinkStudentToUserAsync(int studentId, int userId, int updaterUserId);

    /// <summary>
    /// 获取非组织学生总数
    /// </summary>
    /// <param name="includeInactive">是否包含非激活的学生</param>
    /// <returns>学生总数</returns>
    Task<int> GetStudentCountAsync(bool includeInactive = false);
}
