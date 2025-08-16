using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedComprehensiveTraining;

/// <summary>
/// 导入的综合训练题目实体
/// </summary>
public class ImportedComprehensiveTrainingQuestion
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始题目ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalQuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练ID
    /// </summary>
    [Required]
    public int ComprehensiveTrainingId { get; set; }

    /// <summary>
    /// 科目ID（可选，如果题目属于科目）
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// 模块ID（可选，如果题目属于模块）
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 难度级别
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目顺序
    /// </summary>
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 题目配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 标准答案（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ScoringRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    [StringLength(1000)]
    public string? Remarks { get; set; }

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    [StringLength(1000)]
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    [StringLength(2000)]
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始更新时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalUpdatedAt { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的综合训练
    /// </summary>
    [ForeignKey(nameof(ComprehensiveTrainingId))]
    public virtual ImportedComprehensiveTraining? ComprehensiveTraining { get; set; }

    /// <summary>
    /// 关联的科目
    /// </summary>
    [ForeignKey(nameof(SubjectId))]
    public virtual ImportedComprehensiveTrainingSubject? Subject { get; set; }

    /// <summary>
    /// 关联的模块
    /// </summary>
    [ForeignKey(nameof(ModuleId))]
    public virtual ImportedComprehensiveTrainingModule? Module { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public virtual ICollection<ImportedComprehensiveTrainingOperationPoint> OperationPoints { get; set; } = [];
}
