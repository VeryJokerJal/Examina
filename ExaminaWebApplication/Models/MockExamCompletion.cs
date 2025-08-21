using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 模拟考试完成记录
/// </summary>
[Table("MockExamCompletions")]
public class MockExamCompletion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    [Required]
    public int StudentUserId { get; set; }

    /// <summary>
    /// 模拟考试ID
    /// </summary>
    [Required]
    public int MockExamId { get; set; }

    /// <summary>
    /// 完成状态
    /// </summary>
    [Required]
    public MockExamCompletionStatus Status { get; set; } = MockExamCompletionStatus.NotStarted;

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
    public decimal? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public decimal? MaxScore { get; set; }

    /// <summary>
    /// 完成百分比（0-100）
    /// </summary>
    public decimal? CompletionPercentage { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// BenchSuite评分结果（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? BenchSuiteScoringResult { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否活跃
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    #region 导航属性

    /// <summary>
    /// 关联的学生用户
    /// </summary>
    [ForeignKey(nameof(StudentUserId))]
    public virtual User? Student { get; set; }

    /// <summary>
    /// 关联的模拟考试
    /// </summary>
    [ForeignKey(nameof(MockExamId))]
    public virtual MockExam.MockExam? MockExam { get; set; }

    #endregion
}

/// <summary>
/// 模拟考试完成状态枚举
/// </summary>
public enum MockExamCompletionStatus
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
