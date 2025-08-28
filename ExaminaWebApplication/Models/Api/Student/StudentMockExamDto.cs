using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 学生端模拟考试DTO
/// </summary>
public class StudentMockExamDto
{
    /// <summary>
    /// 模拟考试ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模拟考试名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模拟考试描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 总分值
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    public double PassingScore { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; }

    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

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
    /// 题目列表
    /// </summary>
    public List<StudentMockExamQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 学生端模拟考试题目DTO
/// </summary>
public class StudentMockExamQuestionDto
{
    /// <summary>
    /// 原始题目ID
    /// </summary>
    public int OriginalQuestionId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 题目配置（JSON格式）
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则（JSON格式）
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
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// Office文档文件路径（仅Office模块使用）
    /// </summary>
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public List<StudentMockExamOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 学生端模拟考试操作点DTO
/// </summary>
public class StudentMockExamOperationPointDto
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
    public List<StudentMockExamParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 学生端模拟考试参数DTO
/// </summary>
public class StudentMockExamParameterDto
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

/// <summary>
/// 创建模拟考试请求DTO
/// </summary>
public class CreateMockExamRequestDto
{
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
    [Range(1, 600)]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 总分值
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public double TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Required]
    [Range(1, 1000)]
    public double PassingScore { get; set; } = 60;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = true;

    /// <summary>
    /// 抽取规则列表
    /// </summary>
    [Required]
    public List<QuestionExtractionRuleDto> ExtractionRules { get; set; } = [];
}

/// <summary>
/// 题目抽取规则DTO
/// </summary>
public class QuestionExtractionRuleDto
{
    /// <summary>
    /// 科目类型（可选）
    /// </summary>
    [StringLength(100)]
    public string? SubjectType { get; set; }

    /// <summary>
    /// 题目类型（可选）
    /// </summary>
    [StringLength(100)]
    public string? QuestionType { get; set; }

    /// <summary>
    /// 难度等级（可选）
    /// </summary>
    [StringLength(50)]
    public string? DifficultyLevel { get; set; }

    /// <summary>
    /// 抽取数量
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int Count { get; set; }

    /// <summary>
    /// 每题分值
    /// </summary>
    [Required]
    [Range(1, 100)]
    public double ScorePerQuestion { get; set; }

    /// <summary>
    /// 是否必须
    /// </summary>
    public bool IsRequired { get; set; } = true;
}
