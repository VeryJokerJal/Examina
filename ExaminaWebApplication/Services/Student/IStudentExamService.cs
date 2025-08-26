using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.ImportedExam;

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

    /// <summary>
    /// 按考试类型获取学生可访问的考试列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="examCategory">考试类型（全省统考或学校统考）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>指定类型的考试列表</returns>
    Task<List<StudentExamDto>> GetAvailableExamsByCategoryAsync(int studentUserId, ExamCategory examCategory, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 按考试类型获取学生可访问的考试总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="examCategory">考试类型（全省统考或学校统考）</param>
    /// <returns>指定类型的考试总数</returns>
    Task<int> GetAvailableExamCountByCategoryAsync(int studentUserId, ExamCategory examCategory);

    /// <summary>
    /// 开始正式考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否成功开始</returns>
    Task<bool> StartExamAsync(int examId, int studentUserId);

    /// <summary>
    /// 提交正式考试成绩
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="scoreRequest">成绩数据</param>
    /// <returns>是否成功提交</returns>
    Task<bool> SubmitExamScoreAsync(int examId, int studentUserId, SubmitExamScoreRequestDto scoreRequest);

    /// <summary>
    /// 标记正式考试为已完成（不包含成绩）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否成功完成</returns>
    Task<bool> CompleteExamAsync(int examId, int studentUserId);

    /// <summary>
    /// 获取学生的考试完成记录
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="examId">考试ID（可选）</param>
    /// <returns>考试完成记录列表</returns>
    Task<List<ExamCompletion>> GetExamCompletionsAsync(int studentUserId, int? examId = null);
}
