using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedSpecializedTraining;

/// <summary>
/// 导入的专项训练题目实体
/// </summary>
public class ImportedSpecializedTrainingQuestion
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
    /// 专项训练ID
    /// </summary>
    [Required]
    public int SpecializedTrainingId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [StringLength(50)]
    public string QuestionType { get; set; } = "Practical";

    /// <summary>
    /// 题目分值
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public double Score { get; set; }

    /// <summary>
    /// 难度等级
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 标准答案
    /// </summary>
    [Column(TypeName = "json")]
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

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
    /// C#题目类型（仅C#模块使用）
    /// </summary>
    [StringLength(50)]
    public string? CSharpQuestionType { get; set; }

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    [StringLength(500)]
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#题目直接分数（仅调试纠错和编写实现类型使用）
    /// </summary>
    [Column(TypeName = "double")]
    public double? CSharpDirectScore { get; set; }

    /// <summary>
    /// 代码补全填空处集合（JSON格式，仅C#模块代码补全类型使用）
    /// </summary>
    [Column(TypeName = "json")]
    public string? CodeBlanks { get; set; }

    /// <summary>
    /// C#模板代码（仅C#模块代码补全类型使用，包含NotImplementedException的完整代码模板）
    /// </summary>
    [Column(TypeName = "text")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Office文档文件路径（仅Office模块使用）
    /// </summary>
    [StringLength(500)]
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的专项训练
    /// </summary>
    [ForeignKey(nameof(SpecializedTrainingId))]
    public virtual ImportedSpecializedTraining? SpecializedTraining { get; set; }

    /// <summary>
    /// 关联的模块
    /// </summary>
    [ForeignKey(nameof(ModuleId))]
    public virtual ImportedSpecializedTrainingModule? Module { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public virtual ICollection<ImportedSpecializedTrainingOperationPoint> OperationPoints { get; set; } = [];
}
