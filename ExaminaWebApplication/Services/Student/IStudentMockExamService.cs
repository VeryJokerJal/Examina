using ExaminaWebApplication.Models.Api.Student;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端模拟考试服务接口
/// </summary>
public interface IStudentMockExamService
{
    /// <summary>
    /// 创建模拟考试
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>创建的模拟考试</returns>
    Task<StudentMockExamDto?> CreateMockExamAsync(CreateMockExamRequestDto request, int studentUserId);

    /// <summary>
    /// 获取学生的模拟考试列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>模拟考试列表</returns>
    Task<List<StudentMockExamDto>> GetStudentMockExamsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取模拟考试详情
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>模拟考试详情，如果无权限访问则返回null</returns>
    Task<StudentMockExamDto?> GetMockExamDetailsAsync(int mockExamId, int studentUserId);

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否成功开始</returns>
    Task<bool> StartMockExamAsync(int mockExamId, int studentUserId);

    /// <summary>
    /// 完成模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否成功完成</returns>
    Task<bool> CompleteMockExamAsync(int mockExamId, int studentUserId);

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否成功删除</returns>
    Task<bool> DeleteMockExamAsync(int mockExamId, int studentUserId);

    /// <summary>
    /// 获取学生可访问的模拟考试总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>模拟考试总数</returns>
    Task<int> GetStudentMockExamCountAsync(int studentUserId);

    /// <summary>
    /// 检查是否有权限访问指定模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToMockExamAsync(int mockExamId, int studentUserId);
}
