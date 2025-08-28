using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.MockExam;

/// <summary>
/// 模拟考试配置
/// </summary>
public class MockExamConfiguration
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 模拟考试名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模拟考试描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Required]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 总分值
    /// </summary>
    [Required]
    public double TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Required]
    public double PassingScore { get; set; } = 60;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = true;

    /// <summary>
    /// 抽取规则配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ExtractionRules { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [Required]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建者用户
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
}

/// <summary>
/// 模拟考试实例
/// </summary>
public class MockExam
{
    /// <summary>
    /// 模拟考试ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 配置ID
    /// </summary>
    [Required]
    public int ConfigurationId { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    [Required]
    public int StudentId { get; set; }

    /// <summary>
    /// 模拟考试名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模拟考试描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Required]
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 总分值
    /// </summary>
    [Required]
    public double TotalScore { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    [Required]
    public double PassingScore { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; }

    /// <summary>
    /// 抽取的题目数据（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string ExtractedQuestions { get; set; } = string.Empty;

    /// <summary>
    /// 考试状态
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Created"; // Created, InProgress, Completed, Expired

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 配置
    /// </summary>
    [ForeignKey(nameof(ConfigurationId))]
    public virtual MockExamConfiguration? Configuration { get; set; }

    /// <summary>
    /// 学生用户
    /// </summary>
    [ForeignKey(nameof(StudentId))]
    public virtual User? Student { get; set; }
}

/// <summary>
/// 题目抽取规则
/// </summary>
public class QuestionExtractionRule
{
    /// <summary>
    /// 科目类型（可选）
    /// </summary>
    public string? SubjectType { get; set; }

    /// <summary>
    /// 题目类型（可选）
    /// </summary>
    public string? QuestionType { get; set; }

    /// <summary>
    /// 难度等级（可选）
    /// </summary>
    public string? DifficultyLevel { get; set; }

    /// <summary>
    /// 抽取数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 每题分值
    /// </summary>
    public double ScorePerQuestion { get; set; }

    /// <summary>
    /// 是否必须
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// 抽取的题目信息
/// </summary>
public class ExtractedQuestionInfo
{
    /// <summary>
    /// 原始题目ID
    /// </summary>
    public int OriginalQuestionId { get; set; }

    /// <summary>
    /// 综合训练ID
    /// </summary>
    public int ComprehensiveTrainingId { get; set; }

    /// <summary>
    /// 科目ID（可选）
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// 模块ID（可选）
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 难度等级
    /// </summary>
    public string DifficultyLevel { get; set; } = string.Empty;

    /// <summary>
    /// 预计用时（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 题目配置
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则
    /// </summary>
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// C#程序参数输入
    /// </summary>
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期输出
    /// </summary>
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public List<ExtractedOperationPointInfo> OperationPoints { get; set; } = [];
}

/// <summary>
/// 抽取的操作点信息
/// </summary>
public class ExtractedOperationPointInfo
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 参数列表
    /// </summary>
    public List<ExtractedParameterInfo> Parameters { get; set; } = [];
}

/// <summary>
/// 抽取的参数信息
/// </summary>
public class ExtractedParameterInfo
{
    /// <summary>
    /// 参数ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public string ParameterType { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string? MaxValue { get; set; }
}
