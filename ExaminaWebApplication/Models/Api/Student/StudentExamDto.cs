using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 学生端考试DTO
/// </summary>
public class StudentExamDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 考试名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 考试描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    public string ExamType { get; set; } = string.Empty;

    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考（记录分数和排名）
    /// </summary>
    public bool AllowRetake { get; set; }

    /// <summary>
    /// 是否允许重做（不记录分数和排名，类似模拟考试）
    /// </summary>
    public bool AllowPractice { get; set; }

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    public double PassingScore { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; }

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; }

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; }

    /// <summary>
    /// 考试标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    public List<StudentSubjectDto> Subjects { get; set; } = [];

    /// <summary>
    /// 模块列表
    /// </summary>
    public List<StudentModuleDto> Modules { get; set; } = [];
}

/// <summary>
/// 学生端科目DTO
/// </summary>
public class StudentSubjectDto
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 科目类型
    /// </summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>
    /// 科目名称
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 科目时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最低分数
    /// </summary>
    public int MinScore { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<StudentQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 学生端模块DTO
/// </summary>
public class StudentModuleDto
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 模块排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<StudentQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 学生端题目DTO
/// </summary>
public class StudentQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int Id { get; set; }

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
    /// 题目分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 预计用时（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; }

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
    /// 操作点列表
    /// </summary>
    public List<StudentOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 学生端操作点DTO
/// </summary>
public class StudentOperationPointDto
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
    public List<StudentParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 学生端参数DTO
/// </summary>
public class StudentParameterDto
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
