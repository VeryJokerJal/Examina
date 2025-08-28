using ExaminaWebApplication.Models.Dto;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生专项练习服务接口
/// </summary>
public interface IStudentSpecialPracticeService
{
    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>专项练习进度统计</returns>
    Task<SpecialPracticeProgressDto> GetPracticeProgressAsync(int studentUserId);

    /// <summary>
    /// 标记专项练习为已完成
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="practiceId">练习ID</param>
    /// <param name="score">得分（可选）</param>
    /// <param name="maxScore">最大得分（可选）</param>
    /// <param name="durationSeconds">用时（秒，可选）</param>
    /// <param name="notes">备注（可选）</param>
    /// <returns>是否标记成功</returns>
    Task<bool> MarkPracticeAsCompletedAsync(int studentUserId, int practiceId, double? score = null, double? maxScore = null, int? durationSeconds = null, string? notes = null);

    /// <summary>
    /// 标记专项练习为开始状态
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="practiceId">练习ID</param>
    /// <returns>是否标记成功</returns>
    Task<bool> MarkPracticeAsStartedAsync(int studentUserId, int practiceId);

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>专项练习总数</returns>
    Task<int> GetAvailablePracticeCountAsync(int studentUserId);

    /// <summary>
    /// 获取学生专项练习完成记录
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项练习完成记录列表</returns>
    Task<List<SpecialPracticeCompletionDto>> GetPracticeCompletionsAsync(int studentUserId, int pageNumber = 1, int pageSize = 20);
}
