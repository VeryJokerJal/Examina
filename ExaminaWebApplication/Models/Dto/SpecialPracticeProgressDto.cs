namespace ExaminaWebApplication.Models.Dto;

/// <summary>
/// 专项练习进度统计DTO
/// </summary>
public class SpecialPracticeProgressDto
{
    /// <summary>
    /// 总练习数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已完成练习数量
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 完成百分比（0-100）
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// 进行中的练习数量
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 未开始的练习数量
    /// </summary>
    public int NotStartedCount { get; set; }

    /// <summary>
    /// 最近完成的练习名称
    /// </summary>
    public string? LastCompletedPracticeName { get; set; }

    /// <summary>
    /// 最近完成时间
    /// </summary>
    public DateTime? LastCompletedAt { get; set; }
}
