using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.Dto;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端综合训练服务接口
/// </summary>
public interface IStudentComprehensiveTrainingService
{
    /// <summary>
    /// 获取学生可访问的综合训练列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>综合训练列表</returns>
    Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练详情，如果学生无权限访问则返回null</returns>
    Task<StudentComprehensiveTrainingDto?> GetTrainingDetailsAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 检查学生是否有权限访问指定综合训练
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToTrainingAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练总数</returns>
    Task<int> GetAvailableTrainingCountAsync(int studentUserId);

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练进度统计</returns>
    Task<ComprehensiveTrainingProgressDto> GetTrainingProgressAsync(int studentUserId);

    /// <summary>
    /// 标记综合训练为已完成
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="trainingId">训练ID</param>
    /// <param name="score">得分（可选）</param>
    /// <param name="maxScore">最大得分（可选）</param>
    /// <param name="durationSeconds">用时（秒，可选）</param>
    /// <param name="notes">备注（可选）</param>
    /// <returns>是否标记成功</returns>
    Task<bool> MarkTrainingAsCompletedAsync(int studentUserId, int trainingId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null);

    /// <summary>
    /// 标记综合训练为开始状态
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="trainingId">训练ID</param>
    /// <returns>是否标记成功</returns>
    Task<bool> MarkTrainingAsStartedAsync(int studentUserId, int trainingId);
}
