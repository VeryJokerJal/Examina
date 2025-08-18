using ExaminaWebApplication.Models.Api.Student;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端考试服务接口
/// </summary>
public interface IStudentExamService
{
    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试列表</returns>
    Task<List<StudentExamDto>> GetAvailableExamsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>考试详情，如果学生无权限访问则返回null</returns>
    Task<StudentExamDto?> GetExamDetailsAsync(int examId, int studentUserId);

    /// <summary>
    /// 检查学生是否有权限访问指定考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToExamAsync(int examId, int studentUserId);

    /// <summary>
    /// 获取学生可访问的考试总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>考试总数</returns>
    Task<int> GetAvailableExamCountAsync(int studentUserId);
}
