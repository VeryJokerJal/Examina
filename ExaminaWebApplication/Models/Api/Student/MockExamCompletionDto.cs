using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 模拟考试完成记录DTO
/// </summary>
public class MockExamCompletionDto
{
    /// <summary>
    /// 完成记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模拟考试ID
    /// </summary>
    public int MockExamId { get; set; }

    /// <summary>
    /// 模拟考试名称
    /// </summary>
    public string MockExamName { get; set; } = string.Empty;

    /// <summary>
    /// 模拟考试描述
    /// </summary>
    public string? MockExamDescription { get; set; }

    /// <summary>
    /// 考试总分
    /// </summary>
    public int MockExamTotalScore { get; set; }

    /// <summary>
    /// 完成状态
    /// </summary>
    public MockExamCompletionStatus Status { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分
    /// </summary>
    public double? MaxScore { get; set; }

    /// <summary>
    /// 完成百分比（0-100）
    /// </summary>
    public double? CompletionPercentage { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 用时显示文本（如：30分钟）
    /// </summary>
    public string? DurationText { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 是否及格
    /// </summary>
    public bool? IsPassed { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 格式化的完成时间
    /// </summary>
    public string FormattedCompletedAt { get; set; } = string.Empty;

    /// <summary>
    /// 得分显示文本（如：85/100）
    /// </summary>
    public string ScoreText { get; set; } = string.Empty;
}
