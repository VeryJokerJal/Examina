using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ExamLab.Models.ImportExport;

/// <summary>
/// 试卷导出数据传输对象 - 支持JSON和XML格式
/// </summary>
[XmlRoot("ExamProject")]
public class ExamExportDto
{
    /// <summary>
    /// 试卷信息
    /// </summary>
    [JsonPropertyName("exam")]
    [XmlElement("Exam")]
    public ExamDto Exam { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    [XmlElement("Metadata")]
    public ExportMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// 试卷数据传输对象
/// </summary>
public class ExamDto
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 试卷名称
    /// </summary>
    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [JsonPropertyName("description")]
    [XmlElement("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    [JsonPropertyName("examType")]
    [XmlElement("ExamType")]
    public string ExamType { get; set; } = "UnifiedExam";

    /// <summary>
    /// 试卷状态
    /// </summary>
    [JsonPropertyName("status")]
    [XmlElement("Status")]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("totalScore")]
    [XmlElement("TotalScore")]
    public double TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    [XmlElement("DurationMinutes")]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    [JsonPropertyName("startTime")]
    [XmlElement("StartTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    [JsonPropertyName("endTime")]
    [XmlElement("EndTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    [JsonPropertyName("allowRetake")]
    [XmlElement("AllowRetake")]
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    [JsonPropertyName("maxRetakeCount")]
    [XmlElement("MaxRetakeCount")]
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [JsonPropertyName("passingScore")]
    [XmlElement("PassingScore")]
    public double PassingScore { get; set; } = 60.0;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    [JsonPropertyName("randomizeQuestions")]
    [XmlElement("RandomizeQuestions")]
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    [JsonPropertyName("showScore")]
    [XmlElement("ShowScore")]
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    [JsonPropertyName("showAnswers")]
    [XmlElement("ShowAnswers")]
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建者ID
    /// </summary>
    [JsonPropertyName("createdBy")]
    [XmlElement("CreatedBy")]
    public int CreatedBy { get; set; } = 1;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    [XmlElement("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [XmlElement("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [JsonPropertyName("publishedAt")]
    [XmlElement("PublishedAt")]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 发布者ID
    /// </summary>
    [JsonPropertyName("publishedBy")]
    [XmlElement("PublishedBy")]
    public int? PublishedBy { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [XmlElement("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 试卷标签
    /// </summary>
    [JsonPropertyName("tags")]
    [XmlElement("Tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置
    /// </summary>
    [JsonPropertyName("extendedConfig")]
    [XmlElement("ExtendedConfig")]
    public object? ExtendedConfig { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    [JsonPropertyName("subjects")]
    [XmlArray("Subjects")]
    [XmlArrayItem("Subject")]
    public List<SubjectDto> Subjects { get; set; } = [];

    /// <summary>
    /// 模块列表（ExamLab特有）
    /// </summary>
    [JsonPropertyName("modules")]
    [XmlArray("Modules")]
    [XmlArrayItem("Module")]
    public List<ModuleDto> Modules { get; set; } = [];
}

/// <summary>
/// 科目数据传输对象
/// </summary>
public class SubjectDto
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 试卷ID
    /// </summary>
    [JsonPropertyName("examId")]
    public int ExamId { get; set; }

    /// <summary>
    /// 科目类型
    /// </summary>
    [JsonPropertyName("subjectType")]
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>
    /// 科目名称
    /// </summary>
    [JsonPropertyName("subjectName")]
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// 科目考试时长（分钟）
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 科目顺序
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必考科目
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    [JsonPropertyName("minScore")]
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 科目权重
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 科目配置
    /// </summary>
    [JsonPropertyName("subjectConfig")]
    public object? SubjectConfig { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    [JsonPropertyName("questions")]
    public List<QuestionDto> Questions { get; set; } = [];

    /// <summary>
    /// 题目数量（简化版导出使用）
    /// </summary>
    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; set; }
}

/// <summary>
/// 模块数据传输对象（ExamLab特有）
/// </summary>
public class ModuleDto
{
    /// <summary>
    /// 模块ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("type")]
    [XmlElement("Type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    [JsonPropertyName("description")]
    [XmlElement("Description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    [JsonPropertyName("score")]
    [XmlElement("Score")]
    public double Score { get; set; }

    /// <summary>
    /// 模块排序
    /// </summary>
    [JsonPropertyName("order")]
    [XmlElement("Order")]
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [XmlElement("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 题目列表
    /// </summary>
    [JsonPropertyName("questions")]
    [XmlArray("Questions")]
    [XmlArrayItem("Question")]
    public List<QuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 题目数据传输对象
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目解析
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [JsonPropertyName("questionType")]
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// 难度级别
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目顺序
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必答题
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 题目配置
    /// </summary>
    [JsonPropertyName("questionConfig")]
    public object? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则
    /// </summary>
    [JsonPropertyName("answerValidationRules")]
    public object? AnswerValidationRules { get; set; }

    /// <summary>
    /// 标准答案
    /// </summary>
    [JsonPropertyName("standardAnswer")]
    public object? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则
    /// </summary>
    [JsonPropertyName("scoringRules")]
    public object? ScoringRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("programInput")]
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("expectedOutput")]
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// C#题目类型（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("csharpQuestionType")]
    public string? CSharpQuestionType { get; set; }

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("codeFilePath")]
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#题目直接分数（仅调试纠错和编写实现类型使用）
    /// </summary>
    [JsonPropertyName("csharpDirectScore")]
    public double? CSharpDirectScore { get; set; }

    /// <summary>
    /// 代码补全填空处集合（仅C#模块代码补全类型使用）
    /// </summary>
    [JsonPropertyName("codeBlanks")]
    public List<CodeBlankDto>? CodeBlanks { get; set; }

    /// <summary>
    /// Office文档文件路径（仅Office模块使用）
    /// </summary>
    [JsonPropertyName("documentFilePath")]
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    [JsonPropertyName("operationPoints")]
    public List<OperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 代码填空处数据传输对象
/// </summary>
public class CodeBlankDto
{
    /// <summary>
    /// 填空处ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 填空处名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 填空处描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 填空处分数
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; } = 1.0;

    /// <summary>
    /// 填空处顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 标准答案
    /// </summary>
    [JsonPropertyName("standardAnswer")]
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = string.Empty;
}

/// <summary>
/// 操作点数据传输对象
/// </summary>
public class OperationPointDto
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 操作点名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("moduleType")]
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// 操作点顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = string.Empty;

    /// <summary>
    /// 配置参数列表
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<ParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 配置参数数据传输对象
/// </summary>
public class ParameterDto
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否必填
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// 参数顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 枚举选项
    /// </summary>
    [JsonPropertyName("enumOptions")]
    public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    [JsonPropertyName("validationRule")]
    public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    [JsonPropertyName("validationErrorMessage")]
    public string? ValidationErrorMessage { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    [JsonPropertyName("minValue")]
    public double? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    [JsonPropertyName("maxValue")]
    public double? MaxValue { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 导出元数据
/// </summary>
public class ExportMetadataDto
{
    /// <summary>
    /// 导出版本
    /// </summary>
    [JsonPropertyName("exportVersion")]
    [XmlElement("ExportVersion")]
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导出日期
    /// </summary>
    [JsonPropertyName("exportDate")]
    [XmlElement("ExportDate")]
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导出者
    /// </summary>
    [JsonPropertyName("exportedBy")]
    [XmlElement("ExportedBy")]
    public string ExportedBy { get; set; } = "ExamLab";

    /// <summary>
    /// 总科目数
    /// </summary>
    [JsonPropertyName("totalSubjects")]
    [XmlElement("TotalSubjects")]
    public int TotalSubjects { get; set; }

    /// <summary>
    /// 总题目数
    /// </summary>
    [JsonPropertyName("totalQuestions")]
    [XmlElement("TotalQuestions")]
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 总操作点数
    /// </summary>
    [JsonPropertyName("totalOperationPoints")]
    [XmlElement("TotalOperationPoints")]
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 导出级别
    /// </summary>
    [JsonPropertyName("exportLevel")]
    [XmlElement("ExportLevel")]
    public string ExportLevel { get; set; } = "Complete";

    /// <summary>
    /// 导出格式
    /// </summary>
    [JsonPropertyName("exportFormat")]
    [XmlElement("ExportFormat")]
    public string ExportFormat { get; set; } = "XML";
}
