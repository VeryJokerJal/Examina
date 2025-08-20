namespace Examina.Models;

/// <summary>
/// 完成综合训练请求DTO
/// </summary>
public class CompleteTrainingRequest
{
    /// <summary>
    /// 得分（可选）
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public decimal? MaxScore { get; set; }

    /// <summary>
    /// 用时（秒，可选）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Notes { get; set; }
}
