using System.Collections.ObjectModel;
using System.Text.Json.Serialization;


namespace BenchSuite.Models;

/// <summary>
/// 试卷模型（结构与 ExamLab.Models.ImportExport.ExamExportDto 对齐）
/// </summary>
public class ExamModel
{
    [JsonPropertyName("exam")] public ExamInfoModel Exam { get; set; } = new();
    [JsonPropertyName("metadata")] public ExportMetadataModel Metadata { get; set; } = new();
}

/// <summary>
/// exam 节点结构（对齐 ExamDto）
/// </summary>
public class ExamInfoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("examType")] public string ExamType { get; set; } = "UnifiedExam";
    [JsonPropertyName("status")] public string Status { get; set; } = "Draft";
    [JsonPropertyName("totalScore")] public decimal TotalScore { get; set; } = 100.0m;
    [JsonPropertyName("durationMinutes")] public int DurationMinutes { get; set; } = 120;
    [JsonPropertyName("startTime")] public DateTime? StartTime { get; set; }
    [JsonPropertyName("endTime")] public DateTime? EndTime { get; set; }
    [JsonPropertyName("allowRetake")] public bool AllowRetake { get; set; } = false;
    [JsonPropertyName("maxRetakeCount")] public int MaxRetakeCount { get; set; } = 0;
    [JsonPropertyName("passingScore")] public decimal PassingScore { get; set; } = 60.0m;
    [JsonPropertyName("randomizeQuestions")] public bool RandomizeQuestions { get; set; } = false;
    [JsonPropertyName("showScore")] public bool ShowScore { get; set; } = true;
    [JsonPropertyName("showAnswers")] public bool ShowAnswers { get; set; } = false;
    [JsonPropertyName("createdBy")] public int CreatedBy { get; set; } = 1;
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; }
    [JsonPropertyName("publishedBy")] public int? PublishedBy { get; set; }
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; } = true;
    [JsonPropertyName("tags")] public string? Tags { get; set; }
    [JsonPropertyName("extendedConfig")] public object? ExtendedConfig { get; set; }

    [JsonPropertyName("subjects")] public List<SubjectModel> Subjects { get; set; } = new();

    // 复用内部模块模型，对应 ExamExportDto.modules
    [JsonPropertyName("modules")] public ObservableCollection<ExamModuleModel> Modules { get; set; } = new();
}

/// <summary>
/// 科目（对齐 SubjectDto）
/// </summary>
public class SubjectModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("examId")] public int ExamId { get; set; }
    [JsonPropertyName("subjectType")] public string SubjectType { get; set; } = string.Empty;
    [JsonPropertyName("subjectName")] public string SubjectName { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("score")] public decimal Score { get; set; } = 20.0m;
    [JsonPropertyName("durationMinutes")] public int DurationMinutes { get; set; } = 30;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; } = 1;
    [JsonPropertyName("isRequired")] public bool IsRequired { get; set; } = true;
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; } = true;
    [JsonPropertyName("minScore")] public decimal? MinScore { get; set; }
    [JsonPropertyName("weight")] public decimal Weight { get; set; } = 1.0m;
    [JsonPropertyName("subjectConfig")] public object? SubjectConfig { get; set; }
    [JsonPropertyName("questions")] public List<QuestionModel> Questions { get; set; } = new();
    [JsonPropertyName("questionCount")] public int QuestionCount { get; set; }
}

/// <summary>
/// 导出元数据（对齐 ExportMetadataDto）
/// </summary>
public class ExportMetadataModel
{
    [JsonPropertyName("exportVersion")] public string ExportVersion { get; set; } = "1.0";
    [JsonPropertyName("exportDate")] public DateTime ExportDate { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("exportedBy")] public string ExportedBy { get; set; } = "ExamLab";
    [JsonPropertyName("totalSubjects")] public int TotalSubjects { get; set; }
    [JsonPropertyName("totalQuestions")] public int TotalQuestions { get; set; }
    [JsonPropertyName("totalOperationPoints")] public int TotalOperationPoints { get; set; }
    [JsonPropertyName("exportLevel")] public string ExportLevel { get; set; } = "Complete";
    [JsonPropertyName("exportFormat")] public string ExportFormat { get; set; } = "XML";
}

/// <summary>
/// 试卷模块模型
/// </summary>
public class ExamModuleModel
{
    /// <summary>
    /// 模块ID
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("type")] public ModuleType Type { get; set; }

    /// <summary>
    /// 模块描述
    /// </summary>
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    [JsonPropertyName("score")] public decimal Score { get; set; }

    /// <summary>
    /// 模块包含的题目
    /// </summary>
    [JsonPropertyName("questions")] public ObservableCollection<QuestionModel> Questions { get; set; } = new();

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 模块排序
    /// </summary>
    [JsonPropertyName("order")] public int Order { get; set; }
}

/// <summary>
/// 题目模型
/// </summary>
public class QuestionModel
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [JsonPropertyName("score")] public decimal Score { get; set; }

    /// <summary>
    /// 题目排序
    /// </summary>
    [JsonPropertyName("order")] public int Order { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    [JsonPropertyName("operationPoints")] public List<OperationPointModel> OperationPoints { get; set; } = [];

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("programInput")] public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("expectedOutput")] public string? ExpectedOutput { get; set; }

    /// <summary>
    /// 是否启用该题目
    /// </summary>
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; } = true;

    // ExamLab.Question 无CreatedAt/UpdatedAt等字段，保持精简一致
}

/// <summary>
/// 操作点模型
/// </summary>
public class OperationPointModel
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 操作点名称
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("moduleType")] public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    [JsonPropertyName("powerPointKnowledgeType")] public string? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// Word知识点类型（当ModuleType为Word时使用）
    /// </summary>
    [JsonPropertyName("wordKnowledgeType")] public string? WordKnowledgeType { get; set; }

    /// <summary>
    /// Excel知识点类型（当ModuleType为Excel时使用）
    /// </summary>
    [JsonPropertyName("excelKnowledgeType")] public string? ExcelKnowledgeType { get; set; }

    /// <summary>
    /// Windows操作类型（当ModuleType为Windows时使用）
    /// </summary>
    [JsonPropertyName("windowsOperationType")] public string? WindowsOperationType { get; set; }

    /// <summary>
    /// 操作点分值
    /// </summary>
    [JsonPropertyName("score")] public decimal Score { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    [JsonPropertyName("parameters")] public List<ConfigurationParameterModel> Parameters { get; set; } = [];

    /// <summary>
    /// 是否启用该操作点
    /// </summary>
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 操作点排序
    /// </summary>
    [JsonPropertyName("order")] public int Order { get; set; }
}

/// <summary>
/// 配置参数模型
/// </summary>
public class ConfigurationParameterModel
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonPropertyName("type")] public ParameterType Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [JsonPropertyName("isRequired")] public bool IsRequired { get; set; }
}

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    Excel = 1,
    Word = 2,
    PowerPoint = 3,
    CSharp = 4,
    Windows = 5
}

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ParameterType
{
    Text = 1,
    Number = 2,
    Boolean = 3,
    Enum = 4,
    Date = 5,
    Color = 6



}
