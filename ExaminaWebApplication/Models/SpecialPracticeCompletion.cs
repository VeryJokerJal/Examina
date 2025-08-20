using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models;

/// <summary>
/// 专项练习完成记录
/// </summary>
[Table("SpecialPracticeCompletions")]
public class SpecialPracticeCompletion
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
    /// 专项练习ID
    /// </summary>
    [Required]
    public int PracticeId { get; set; }

    /// <summary>
    /// 完成状态
    /// </summary>
    [Required]
    public SpecialPracticeCompletionStatus Status { get; set; } = SpecialPracticeCompletionStatus.NotStarted;

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
    /// 学生用户
    /// </summary>
    [ForeignKey(nameof(StudentUserId))]
    public virtual User? Student { get; set; }

    // 注意：这里暂时不添加Practice导航属性，因为专项练习的实体结构可能不同
    // 如果有具体的专项练习实体，可以在后续添加

    #endregion
}

/// <summary>
/// 专项练习完成状态枚举
/// </summary>
public enum SpecialPracticeCompletionStatus
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
    /// 已放弃
    /// </summary>
    Abandoned = 3,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout = 4
}
