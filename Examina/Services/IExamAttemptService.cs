using Examina.Models.Exam;

namespace Examina.Services;

/// <summary>
/// 考试尝试服务接口
/// </summary>
public interface IExamAttemptService
{
    /// <summary>
    /// 检查学生是否可以开始考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试次数限制验证结果</returns>
    Task<ExamAttemptLimitDto> CheckExamAttemptLimitAsync(int examId, int studentId);

    /// <summary>
    /// 开始考试尝试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="attemptType">尝试类型</param>
    /// <returns>考试尝试记录</returns>
    Task<ExamAttemptDto?> StartExamAttemptAsync(int examId, int studentId, ExamAttemptType attemptType);

    /// <summary>
    /// 完成考试尝试
    /// </summary>
    /// <param name="attemptId">尝试ID</param>
    /// <param name="score">得分</param>
    /// <param name="maxScore">最大得分</param>
    /// <param name="durationSeconds">用时（秒）</param>
    /// <param name="notes">备注</param>
    /// <returns>是否成功</returns>
    Task<bool> CompleteExamAttemptAsync(int attemptId, double? score = null, double? maxScore = null, int? durationSeconds = null, string? notes = null);

    /// <summary>
    /// 放弃考试尝试
    /// </summary>
    /// <param name="attemptId">尝试ID</param>
    /// <param name="reason">放弃原因</param>
    /// <returns>是否成功</returns>
    Task<bool> AbandonExamAttemptAsync(int attemptId, string? reason = null);

    /// <summary>
    /// 标记考试尝试为超时
    /// </summary>
    /// <param name="attemptId">尝试ID</param>
    /// <param name="score">得分（如果有）</param>
    /// <param name="maxScore">最大得分（如果有）</param>
    /// <param name="durationSeconds">实际用时（秒）</param>
    /// <returns>是否成功</returns>
    Task<bool> TimeoutExamAttemptAsync(int attemptId, double? score = null, double? maxScore = null, int? durationSeconds = null);

    /// <summary>
    /// 获取学生的考试尝试历史
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试尝试历史列表</returns>
    Task<List<ExamAttemptDto>> GetExamAttemptHistoryAsync(int examId, int studentId);

    /// <summary>
    /// 获取学生的所有考试尝试历史
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试尝试历史列表</returns>
    Task<List<ExamAttemptDto>> GetStudentExamAttemptHistoryAsync(int studentId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取当前进行中的考试尝试
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>进行中的考试尝试，如果没有则返回null</returns>
    Task<ExamAttemptDto?> GetCurrentExamAttemptAsync(int studentId);

    /// <summary>
    /// 获取指定考试尝试的详细信息
    /// </summary>
    /// <param name="attemptId">尝试ID</param>
    /// <returns>考试尝试详细信息</returns>
    Task<ExamAttemptDto?> GetExamAttemptDetailsAsync(int attemptId);

    /// <summary>
    /// 检查学生是否有进行中的考试
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>是否有进行中的考试</returns>
    Task<bool> HasActiveExamAttemptAsync(int studentId);

    /// <summary>
    /// 获取考试的统计信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试统计信息</returns>
    Task<ExamAttemptStatisticsDto> GetExamAttemptStatisticsAsync(int examId);

    /// <summary>
    /// 验证考试尝试权限
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="attemptType">尝试类型</param>
    /// <returns>验证结果和错误信息</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateExamAttemptPermissionAsync(int examId, int studentId, ExamAttemptType attemptType);

    /// <summary>
    /// 更新考试尝试的进度信息
    /// </summary>
    /// <param name="attemptId">尝试ID</param>
    /// <param name="progressData">进度数据（JSON格式）</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateExamAttemptProgressAsync(int attemptId, string? progressData);
}

/// <summary>
/// 考试尝试统计信息DTO
/// </summary>
public class ExamAttemptStatisticsDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 总参与人数
    /// </summary>
    public int TotalParticipants { get; set; }

    /// <summary>
    /// 总尝试次数
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// 首次尝试次数
    /// </summary>
    public int FirstAttempts { get; set; }

    /// <summary>
    /// 重考次数
    /// </summary>
    public int RetakeAttempts { get; set; }

    /// <summary>
    /// 练习次数
    /// </summary>
    public int PracticeAttempts { get; set; }

    /// <summary>
    /// 已完成次数
    /// </summary>
    public int CompletedAttempts { get; set; }

    /// <summary>
    /// 进行中次数
    /// </summary>
    public int InProgressAttempts { get; set; }

    /// <summary>
    /// 放弃次数
    /// </summary>
    public int AbandonedAttempts { get; set; }

    /// <summary>
    /// 超时次数
    /// </summary>
    public int TimedOutAttempts { get; set; }

    /// <summary>
    /// 平均得分
    /// </summary>
    public double? AverageScore { get; set; }

    /// <summary>
    /// 最高得分
    /// </summary>
    public double? HighestScore { get; set; }

    /// <summary>
    /// 最低得分
    /// </summary>
    public double? LowestScore { get; set; }

    /// <summary>
    /// 平均用时（秒）
    /// </summary>
    public int? AverageDurationSeconds { get; set; }

    /// <summary>
    /// 完成率
    /// </summary>
    public double CompletionRate
    {
        get
        {
            if (TotalAttempts == 0)
                return 0;

            return CompletedAttempts / TotalAttempts * 100;
        }
    }

    /// <summary>
    /// 平均用时显示
    /// </summary>
    public string AverageDurationDisplay
    {
        get
        {
            if (!AverageDurationSeconds.HasValue)
                return "无数据";

            TimeSpan duration = TimeSpan.FromSeconds(AverageDurationSeconds.Value);
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            else
                return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
}
