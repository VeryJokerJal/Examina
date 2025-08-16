using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 教师组织关系服务接口
/// </summary>
public interface ITeacherOrganizationService
{
    /// <summary>
    /// 教师加入班级
    /// </summary>
    /// <param name="teacherId">教师用户ID</param>
    /// <param name="classId">班级ID</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <param name="notes">备注信息</param>
    /// <returns>教师组织关系DTO</returns>
    Task<TeacherOrganizationDto?> AddTeacherToClassAsync(int teacherId, int classId, int creatorUserId, string? notes = null);

    /// <summary>
    /// 教师退出班级
    /// </summary>
    /// <param name="teacherId">教师用户ID</param>
    /// <param name="classId">班级ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveTeacherFromClassAsync(int teacherId, int classId);

    /// <summary>
    /// 获取教师的班级列表
    /// </summary>
    /// <param name="teacherId">教师用户ID</param>
    /// <param name="includeInactive">是否包含非激活的关系</param>
    /// <returns>教师组织关系DTO列表</returns>
    Task<List<TeacherOrganizationDto>> GetTeacherClassesAsync(int teacherId, bool includeInactive = false);

    /// <summary>
    /// 获取班级的教师列表
    /// </summary>
    /// <param name="classId">班级ID</param>
    /// <param name="includeInactive">是否包含非激活的关系</param>
    /// <returns>教师组织关系DTO列表</returns>
    Task<List<TeacherOrganizationDto>> GetClassTeachersAsync(int classId, bool includeInactive = false);

    /// <summary>
    /// 批量添加教师到班级
    /// </summary>
    /// <param name="teacherIds">教师用户ID列表</param>
    /// <param name="classId">班级ID</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>成功添加的数量</returns>
    Task<int> AddTeachersToClassAsync(List<int> teacherIds, int classId, int creatorUserId);

    /// <summary>
    /// 检查教师是否在班级中
    /// </summary>
    /// <param name="teacherId">教师用户ID</param>
    /// <param name="classId">班级ID</param>
    /// <returns>是否在班级中</returns>
    Task<bool> IsTeacherInClassAsync(int teacherId, int classId);
}
