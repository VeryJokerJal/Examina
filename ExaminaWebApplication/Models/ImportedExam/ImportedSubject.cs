using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedExam;

/// <summary>
/// 导入的科目实体
/// </summary>
public class ImportedSubject
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始科目ID（来自ExamLab）
    /// </summary>
    public int OriginalSubjectId { get; set; }

    /// <summary>
    /// 考试ID
    /// </summary>
    [Required]
    public int ExamId { get; set; }

    /// <summary>
    /// 科目类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>
    /// 科目名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    [Column(TypeName = "double")]
    public double Score { get; set; } = 20.0;

    /// <summary>
    /// 科目考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 科目顺序
    /// </summary>
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必考科目
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    [Column(TypeName = "double")]
    public double? MinScore { get; set; }

    /// <summary>
    /// 科目权重
    /// </summary>
    [Column(TypeName = "double")]
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 科目配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? SubjectConfig { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的考试
    /// </summary>
    [ForeignKey(nameof(ExamId))]
    public virtual ImportedExam? Exam { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public virtual ICollection<ImportedQuestion> Questions { get; set; } = [];
}
