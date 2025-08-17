using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 用户管理服务接口
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 创建学生用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="email">邮箱</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="password">密码</param>
    /// <param name="realName">真实姓名</param>
    /// <param name="studentId">学号</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>创建的用户DTO</returns>
    Task<UserDto?> CreateStudentUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, int? creatorUserId = null);

    /// <summary>
    /// 创建教师用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="email">邮箱</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="password">密码</param>
    /// <param name="realName">真实姓名</param>
    /// <param name="schoolId">所属学校ID</param>
    /// <param name="classIds">所属班级ID列表</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>创建的用户DTO</returns>
    Task<UserDto?> CreateTeacherUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, int? schoolId = null, List<int>? classIds = null, int? creatorUserId = null);

    /// <summary>
    /// 更新用户基本信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="email">邮箱</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="realName">真实姓名</param>
    /// <param name="studentId">学号/工号</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>更新后的用户DTO</returns>
    Task<UserDto?> UpdateUserAsync(int userId, string? email = null, string? phoneNumber = null, string? realName = null, int? updaterUserId = null);

    /// <summary>
    /// 停用用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeactivateUserAsync(int userId, int updaterUserId);

    /// <summary>
    /// 激活用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> ActivateUserAsync(int userId, int updaterUserId);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="updaterUserId">更新者用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> ResetUserPasswordAsync(int userId, string newPassword, int updaterUserId);

    /// <summary>
    /// 获取用户列表
    /// </summary>
    /// <param name="role">用户角色过滤</param>
    /// <param name="includeInactive">是否包含非激活用户</param>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>用户DTO列表</returns>
    Task<List<UserDto>> GetUsersAsync(UserRole? role = null, bool includeInactive = false, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户DTO</returns>
    Task<UserDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// 搜索用户
    /// </summary>
    /// <param name="keyword">关键词（用户名、邮箱、手机号、真实姓名）</param>
    /// <param name="role">用户角色过滤</param>
    /// <param name="includeInactive">是否包含非激活用户</param>
    /// <returns>用户DTO列表</returns>
    Task<List<UserDto>> SearchUsersAsync(string keyword, UserRole? role = null, bool includeInactive = false);

    /// <summary>
    /// 获取教师用户列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活用户</param>
    /// <returns>教师用户DTO列表</returns>
    Task<List<UserDto>> GetTeachersAsync(bool includeInactive = false);

    /// <summary>
    /// 获取学生用户列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活用户</param>
    /// <returns>学生用户DTO列表</returns>
    Task<List<UserDto>> GetStudentsAsync(bool includeInactive = false);
}
