using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 试卷题目表 - 存储试卷中的具体题目信息
/// </summary>
public class ExamQuestion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的试卷ID
    /// </summary>
    [Required]
    public int ExamId { get; set; }

    /// <summary>
    /// 关联的科目ID
    /// </summary>
    [Required]
    public int ExamSubjectId { get; set; }

    /// <summary>
    /// 题目编号（在试卷中的序号）
    /// </summary>
    [Required]
    public int QuestionNumber { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述/内容
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 难度级别（1-5）
    /// </summary>
    [Required]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目顺序
    /// </summary>
    [Required]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 关联的Excel操作点ID（仅Excel科目使用）
    /// </summary>
    public int? ExcelOperationPointId { get; set; }

    /// <summary>
    /// 关联的Excel题目模板ID（仅Excel科目使用）
    /// </summary>
    public int? ExcelQuestionTemplateId { get; set; }

    /// <summary>
    /// 关联的Excel题目实例ID（仅Excel科目使用）
    /// </summary>
    public int? ExcelQuestionInstanceId { get; set; }

    /// <summary>
    /// 题目参数配置（JSON格式）
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
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的试卷
    /// </summary>
    [JsonIgnore]
    public virtual Exam Exam { get; set; } = null!;

    /// <summary>
    /// 关联的科目
    /// </summary>
    [JsonIgnore]
    public virtual ExamSubject ExamSubject { get; set; } = null!;

    /// <summary>
    /// 关联的Excel操作点（仅Excel科目）
    /// </summary>
    public virtual ExcelOperationPoint? ExcelOperationPoint { get; set; }

    /// <summary>
    /// 关联的Excel题目模板（仅Excel科目）
    /// </summary>
    public virtual ExcelQuestionTemplate? ExcelQuestionTemplate { get; set; }

    /// <summary>
    /// 关联的Excel题目实例（仅Excel科目）
    /// </summary>
    public virtual ExcelQuestionInstance? ExcelQuestionInstance { get; set; }
}

/// <summary>
/// 题目类型枚举
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Excel操作题
    /// </summary>
    ExcelOperation = 1,

    /// <summary>
    /// PowerPoint操作题
    /// </summary>
    PowerPointOperation = 2,

    /// <summary>
    /// Word操作题
    /// </summary>
    WordOperation = 3,

    /// <summary>
    /// Windows操作题
    /// </summary>
    WindowsOperation = 4,

    /// <summary>
    /// C#编程题
    /// </summary>
    CSharpProgramming = 5,

    /// <summary>
    /// 选择题
    /// </summary>
    MultipleChoice = 6,

    /// <summary>
    /// 填空题
    /// </summary>
    FillInBlank = 7,

    /// <summary>
    /// 简答题
    /// </summary>
    ShortAnswer = 8,

    /// <summary>
    /// 综合题
    /// </summary>
    Comprehensive = 9
}

/// <summary>
/// Excel题目配置
/// </summary>
public class ExcelQuestionConfig
{
    /// <summary>
    /// 操作点编号
    /// </summary>
    public int OperationNumber { get; set; }

    /// <summary>
    /// 题目参数
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// 是否允许多种解法
    /// </summary>
    public bool AllowMultipleSolutions { get; set; } = false;

    /// <summary>
    /// 部分分值配置
    /// </summary>
    public Dictionary<string, int> PartialScoring { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// 提示信息
    /// </summary>
    public List<string> Hints { get; set; } = new List<string>();
}

/// <summary>
/// 题目统计信息
/// </summary>
public class QuestionStatistics
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 答题次数
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// 正确次数
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// 正确率
    /// </summary>
    public decimal CorrectRate => AttemptCount > 0 ? (decimal)CorrectCount / AttemptCount : 0;

    /// <summary>
    /// 平均分
    /// </summary>
    public decimal AverageScore { get; set; }

    /// <summary>
    /// 平均完成时间（分钟）
    /// </summary>
    public decimal AverageCompletionTime { get; set; }

    /// <summary>
    /// 难度系数
    /// </summary>
    public decimal DifficultyCoefficient { get; set; }
}
