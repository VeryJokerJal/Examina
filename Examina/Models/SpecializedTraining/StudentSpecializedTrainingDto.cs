using System.Collections.ObjectModel;

namespace Examina.Models.SpecializedTraining;

/// <summary>
/// 学生端专项训练DTO
/// </summary>
public class StudentSpecializedTrainingDto
{
    /// <summary>
    /// 专项训练ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 专项训练名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 专项训练描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模块类型
    /// </summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 原始创建时间
    /// </summary>
    public DateTime OriginalCreatedTime { get; set; }

    /// <summary>
    /// 原始最后修改时间
    /// </summary>
    public DateTime OriginalLastModifiedTime { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// 模块数量
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 是否启用试做功能
    /// </summary>
    public bool EnableTrial { get; set; } = false;

    /// <summary>
    /// 模块列表
    /// </summary>
    public ObservableCollection<StudentSpecializedTrainingModuleDto> Modules { get; set; } = [];

    /// <summary>
    /// 题目列表
    /// </summary>
    public ObservableCollection<StudentSpecializedTrainingQuestionDto> Questions { get; set; } = [];



    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> TagList => string.IsNullOrEmpty(Tags)
        ? []
        : [.. Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim())];
}

/// <summary>
/// 学生端专项训练模块DTO
/// </summary>
public class StudentSpecializedTrainingModuleDto
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
    /// 排序顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public ObservableCollection<StudentSpecializedTrainingQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 学生端专项训练题目DTO
/// </summary>
public class StudentSpecializedTrainingQuestionDto
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
    public int Order { get; set; }

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
    /// C#题目类型（仅C#模块使用）
    /// </summary>
    public string? CSharpQuestionType { get; set; }

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#题目直接分数（仅调试纠错和编写实现类型使用）
    /// </summary>
    public double? CSharpDirectScore { get; set; }

    /// <summary>
    /// 代码补全填空处集合（JSON格式，仅C#模块代码补全类型使用）
    /// </summary>
    public string? CodeBlanks { get; set; }

    /// <summary>
    /// C#模板代码（仅C#模块代码补全类型使用，包含NotImplementedException的完整代码模板）
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Office文档文件路径（仅Office模块使用）
    /// </summary>
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public ObservableCollection<StudentSpecializedTrainingOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 学生端专项训练操作点DTO
/// </summary>
public class StudentSpecializedTrainingOperationPointDto
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
    public ObservableCollection<StudentSpecializedTrainingParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 学生端专项训练参数DTO
/// </summary>
public class StudentSpecializedTrainingParameterDto
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
