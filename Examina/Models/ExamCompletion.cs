namespace Examina.Models;

/// <summary>
/// 正式考试完成记录
/// </summary>
public class ExamCompletion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    public int StudentUserId { get; set; }

    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 完成状态
    /// </summary>
    public ExamCompletionStatus Status { get; set; } = ExamCompletionStatus.NotStarted;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 得分（可选）
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
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
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// BenchSuite评分结果（JSON格式）
    /// </summary>
    public string? BenchSuiteScoringResult { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 正式考试完成状态枚举
/// </summary>
public enum ExamCompletionStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}
