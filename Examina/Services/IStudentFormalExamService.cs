using Examina.Models.Api;

namespace Examina.Services;

/// <summary>
/// 学生正式考试服务接口
/// </summary>
public interface IStudentFormalExamService
{
    /// <summary>
    /// 开始正式考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>是否成功开始</returns>
    Task<bool> StartExamAsync(int examId);

    /// <summary>
    /// 提交正式考试成绩
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="scoreRequest">成绩数据</param>
    /// <returns>是否成功提交</returns>
    Task<bool> SubmitExamScoreAsync(int examId, SubmitExamScoreRequestDto scoreRequest);

    /// <summary>
    /// 完成正式考试（不包含成绩）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>是否成功完成</returns>
    Task<bool> CompleteExamAsync(int examId);
}
