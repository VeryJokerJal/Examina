using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ExaminaWebApplication.Models.ImportedExam;

/// <summary>
/// 导入的考试实体
/// </summary>
public class ImportedExam
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始考试ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalExamId { get; set; } = string.Empty;

    /// <summary>
    /// 考试名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 考试描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ExamType { get; set; } = "UnifiedExam";

    /// <summary>
    /// 考试状态
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 总分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal PassingScore { get; set; } = 60.0m;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 考试标签
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 导入者ID
    /// </summary>
    [Required]
    public string ImportedBy { get; set; } = string.Empty;

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始创建者ID（来自ExamLab）
    /// </summary>
    public int OriginalCreatedBy { get; set; }

    /// <summary>
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; }

    /// <summary>
    /// 原始更新时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalUpdatedAt { get; set; }

    /// <summary>
    /// 原始发布时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalPublishedAt { get; set; }

    /// <summary>
    /// 原始发布者ID（来自ExamLab）
    /// </summary>
    public int? OriginalPublishedBy { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    [StringLength(255)]
    public string? ImportFileName { get; set; }

    /// <summary>
    /// 导入文件大小（字节）
    /// </summary>
    public long ImportFileSize { get; set; }

    /// <summary>
    /// 导入版本
    /// </summary>
    [StringLength(20)]
    public string ImportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导入状态
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ImportStatus { get; set; } = "Success";

    /// <summary>
    /// 导入错误信息
    /// </summary>
    [StringLength(2000)]
    public string? ImportErrorMessage { get; set; }

    /// <summary>
    /// 导入者用户
    /// </summary>
    [ForeignKey(nameof(ImportedBy))]
    public virtual IdentityUser? Importer { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    public virtual ICollection<ImportedSubject> Subjects { get; set; } = new List<ImportedSubject>();

    /// <summary>
    /// 模块列表
    /// </summary>
    public virtual ICollection<ImportedModule> Modules { get; set; } = new List<ImportedModule>();
}
