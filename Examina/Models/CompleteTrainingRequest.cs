namespace Examina.Models;

/// <summary>
/// 完成综合训练请求DTO
/// </summary>
public class CompleteTrainingRequest
{
    /// <summary>
    /// 得分（可选）
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public double? MaxScore { get; set; }

    /// <summary>
    /// 用时（秒，可选）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// BenchSuite评分结果JSON（可选）
    /// </summary>
    public string? BenchSuiteScoringResult { get; set; }

    /// <summary>
    /// 提交时间（UTC时间）
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
