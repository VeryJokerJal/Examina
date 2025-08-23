using Examina.Models;
using Examina.Models.SpecializedTraining;

namespace Examina.Services;

/// <summary>
/// 学生端专项训练服务接口
/// </summary>
public interface IStudentSpecializedTrainingService
{
    /// <summary>
    /// 获取学生可访问的专项训练列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetAvailableTrainingsAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取专项训练详情
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <returns>专项训练详情，如果无权限访问则返回null</returns>
    Task<StudentSpecializedTrainingDto?> GetTrainingDetailsAsync(int trainingId);

    /// <summary>
    /// 检查是否有权限访问指定专项训练
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToTrainingAsync(int trainingId);

    /// <summary>
    /// 获取学生可访问的专项训练总数
    /// </summary>
    /// <returns>专项训练总数</returns>
    Task<int> GetAvailableTrainingCountAsync();

    /// <summary>
    /// 搜索专项训练
    /// </summary>
    /// <param name="searchKeyword">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> SearchTrainingsAsync(string searchKeyword, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 按模块类型筛选专项训练
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetTrainingsByModuleTypeAsync(string moduleType, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取所有可用的模块类型列表
    /// </summary>
    /// <returns>模块类型列表</returns>
    Task<List<string>> GetAvailableModuleTypesAsync();

    /// <summary>
    /// 按难度等级筛选专项训练
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetTrainingsByDifficultyAsync(int difficultyLevel, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 标记专项训练为开始状态
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <returns>是否成功</returns>
    Task<bool> StartSpecializedTrainingAsync(int trainingId);

    /// <summary>
    /// 提交专项训练成绩并标记为完成
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <param name="score">得分</param>
    /// <param name="maxScore">最大得分</param>
    /// <param name="durationSeconds">用时（秒）</param>
    /// <param name="notes">备注</param>
    /// <returns>是否成功</returns>
    Task<bool> CompleteSpecializedTrainingAsync(int trainingId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null);

    /// <summary>
    /// 获取专项训练进度统计
    /// </summary>
    /// <returns>专项训练进度统计</returns>
    Task<SpecializedTrainingProgressDto> GetTrainingProgressAsync();
}

/// <summary>
/// 专项训练进度统计DTO
/// </summary>
public class SpecializedTrainingProgressDto
{
    /// <summary>
    /// 总专项训练数
    /// </summary>
    public int TotalTrainings { get; set; }

    /// <summary>
    /// 已完成专项训练数
    /// </summary>
    public int CompletedTrainings { get; set; }

    /// <summary>
    /// 进行中专项训练数
    /// </summary>
    public int InProgressTrainings { get; set; }

    /// <summary>
    /// 未开始专项训练数
    /// </summary>
    public int NotStartedTrainings { get; set; }

    /// <summary>
    /// 完成率（百分比）
    /// </summary>
    public decimal CompletionRate => TotalTrainings > 0 ? (decimal)CompletedTrainings / TotalTrainings * 100 : 0;

    /// <summary>
    /// 平均得分
    /// </summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// 最高得分
    /// </summary>
    public decimal? HighestScore { get; set; }

    /// <summary>
    /// 最近完成的专项训练
    /// </summary>
    public List<SpecialPracticeCompletionDto> RecentCompletions { get; set; } = [];
}
