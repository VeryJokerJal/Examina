using System.ComponentModel.DataAnnotations;

namespace Examina.Models.Api;

/// <summary>
/// 提交正式考试成绩请求DTO
/// </summary>
public class SubmitExamScoreRequestDto
{
    /// <summary>
    /// 得分
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "得分不能为负数")]
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "最大得分不能为负数")]
    public double? MaxScore { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "用时不能为负数")]
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(1000, ErrorMessage = "备注信息不能超过1000个字符")]
    public string? Notes { get; set; }

    /// <summary>
    /// BenchSuite评分结果（JSON格式）
    /// </summary>
    public string? BenchSuiteScoringResult { get; set; }

    /// <summary>
    /// 完成时间（可选，如果不提供则使用当前时间）
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
