using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;

namespace ExaminaWebApplication.Services.School;

/// <summary>
/// 学校权限验证服务接口
/// </summary>
public interface ISchoolPermissionService
{
    /// <summary>
    /// 检查学生是否有权限访问指定的学校统考
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="examId">考试ID</param>
    /// <returns>是否有权限访问</returns>
    Task<bool> HasAccessToSchoolExamAsync(int studentUserId, int examId);

    /// <summary>
    /// 获取学生所属的学校ID
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>学校ID，如果未加入学校则返回null</returns>
    Task<int?> GetStudentSchoolIdAsync(int studentUserId);

    /// <summary>
    /// 获取学生所属的学校信息
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>学校信息，如果未加入学校则返回null</returns>
    Task<Organization?> GetStudentSchoolAsync(int studentUserId);

    /// <summary>
    /// 检查考试是否对指定学校开放
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolId">学校ID</param>
    /// <returns>是否对该学校开放</returns>
    Task<bool> IsExamAvailableForSchoolAsync(int examId, int schoolId);

    /// <summary>
    /// 获取考试关联的所有学校列表
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>学校列表</returns>
    Task<List<Organization>> GetExamAssociatedSchoolsAsync(int examId);

    /// <summary>
    /// 为考试添加学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolId">学校ID</param>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="remarks">备注</param>
    /// <returns>是否添加成功</returns>
    Task<bool> AddExamSchoolAssociationAsync(int examId, int schoolId, int createdBy, string? remarks = null);

    /// <summary>
    /// 移除考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolId">学校ID</param>
    /// <returns>是否移除成功</returns>
    Task<bool> RemoveExamSchoolAssociationAsync(int examId, int schoolId);

    /// <summary>
    /// 批量添加考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolIds">学校ID列表</param>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="remarks">备注</param>
    /// <returns>成功添加的数量</returns>
    Task<int> BatchAddExamSchoolAssociationsAsync(int examId, List<int> schoolIds, int createdBy, string? remarks = null);

    /// <summary>
    /// 批量移除考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolIds">学校ID列表</param>
    /// <returns>成功移除的数量</returns>
    Task<int> BatchRemoveExamSchoolAssociationsAsync(int examId, List<int> schoolIds);

    /// <summary>
    /// 获取学生可访问的学校统考ID列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>可访问的考试ID列表</returns>
    Task<List<int>> GetAccessibleSchoolExamIdsAsync(int studentUserId);
}
