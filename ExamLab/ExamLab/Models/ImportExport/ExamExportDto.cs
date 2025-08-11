using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamLab.Models.ImportExport;

/// <summary>
/// 试卷导出数据传输对象 - 基于Exam模型文档定义的JSON格式
/// </summary>
public class ExamExportDto
{
    /// <summary>
    /// 试卷信息
    /// </summary>
    [JsonPropertyName("exam")]
    public ExamDto Exam { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
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
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 试卷名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    [JsonPropertyName("examType")]
    public string ExamType { get; set; } = "UnifiedExam";

    /// <summary>
    /// 试卷状态
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("totalScore")]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    [JsonPropertyName("allowRetake")]
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    [JsonPropertyName("maxRetakeCount")]
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [JsonPropertyName("passingScore")]
    public decimal PassingScore { get; set; } = 60.0m;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    [JsonPropertyName("randomizeQuestions")]
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    [JsonPropertyName("showScore")]
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    [JsonPropertyName("showAnswers")]
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建者ID
    /// </summary>
    [JsonPropertyName("createdBy")]
    public int CreatedBy { get; set; } = 1;

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
    /// 发布时间
    /// </summary>
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 发布者ID
    /// </summary>
    [JsonPropertyName("publishedBy")]
    public int? PublishedBy { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 试卷标签
    /// </summary>
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置
    /// </summary>
    [JsonPropertyName("extendedConfig")]
    public object? ExtendedConfig { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    [JsonPropertyName("subjects")]
    public List<SubjectDto> Subjects { get; set; } = new();

    /// <summary>
    /// 模块列表（ExamLab特有）
    /// </summary>
    [JsonPropertyName("modules")]
    public List<ModuleDto> Modules { get; set; } = new();
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
    public decimal Score { get; set; } = 20.0m;

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
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 科目配置
    /// </summary>
    [JsonPropertyName("subjectConfig")]
    public object? SubjectConfig { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    [JsonPropertyName("questions")]
    public List<QuestionDto> Questions { get; set; } = new();

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
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// 模块排序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 题目列表
    /// </summary>
    [JsonPropertyName("questions")]
    public List<QuestionDto> Questions { get; set; } = new();
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
    /// 题目内容
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
    public decimal Score { get; set; } = 10.0m;

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
    /// 操作点列表
    /// </summary>
    [JsonPropertyName("operationPoints")]
    public List<OperationPointDto> OperationPoints { get; set; } = new();
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
    public int Score { get; set; }

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
    public List<ParameterDto> Parameters { get; set; } = new();
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
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导出日期
    /// </summary>
    [JsonPropertyName("exportDate")]
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导出者
    /// </summary>
    [JsonPropertyName("exportedBy")]
    public string ExportedBy { get; set; } = "ExamLab";

    /// <summary>
    /// 总科目数
    /// </summary>
    [JsonPropertyName("totalSubjects")]
    public int TotalSubjects { get; set; }

    /// <summary>
    /// 总题目数
    /// </summary>
    [JsonPropertyName("totalQuestions")]
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 总操作点数
    /// </summary>
    [JsonPropertyName("totalOperationPoints")]
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 导出级别
    /// </summary>
    [JsonPropertyName("exportLevel")]
    public string ExportLevel { get; set; } = "Complete";
}
