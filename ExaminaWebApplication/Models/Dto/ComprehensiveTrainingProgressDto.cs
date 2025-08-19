namespace ExaminaWebApplication.Models.Dto;

/// <summary>
/// 综合训练进度统计DTO
/// </summary>
public class ComprehensiveTrainingProgressDto
{
    /// <summary>
    /// 总训练数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已完成训练数量
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 完成百分比（0-100）
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// 进行中的训练数量
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 未开始的训练数量
    /// </summary>
    public int NotStartedCount { get; set; }

    /// <summary>
    /// 最近完成的训练名称
    /// </summary>
    public string? LastCompletedTrainingName { get; set; }

    /// <summary>
    /// 最近完成时间
    /// </summary>
    public DateTime? LastCompletedAt { get; set; }
}
