using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ExaminaWebApplication.Services.ImportedComprehensiveTraining;

/// <summary>
/// 综合训练模块数据传输对象
/// </summary>
public class ComprehensiveTrainingModuleDto
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
    public List<ComprehensiveTrainingQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 综合训练题目数据传输对象
/// </summary>
public class ComprehensiveTrainingQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    [JsonPropertyName("title")]
    [XmlElement("Title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [JsonPropertyName("content")]
    [XmlElement("Content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [JsonPropertyName("questionType")]
    [XmlElement("QuestionType")]
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [JsonPropertyName("score")]
    [XmlElement("Score")]
    public double Score { get; set; } = 10.0;

    /// <summary>
    /// 难度级别
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    [XmlElement("DifficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    [JsonPropertyName("estimatedMinutes")]
    [XmlElement("EstimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目顺序
    /// </summary>
    [JsonPropertyName("sortOrder")]
    [XmlElement("SortOrder")]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必答题
    /// </summary>
    [JsonPropertyName("isRequired")]
    [XmlElement("IsRequired")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [XmlElement("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 题目配置
    /// </summary>
    [JsonPropertyName("questionConfig")]
    [XmlElement("QuestionConfig")]
    public object? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则
    /// </summary>
    [JsonPropertyName("answerValidationRules")]
    [XmlElement("AnswerValidationRules")]
    public object? AnswerValidationRules { get; set; }

    /// <summary>
    /// 标准答案
    /// </summary>
    [JsonPropertyName("standardAnswer")]
    [XmlElement("StandardAnswer")]
    public object? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则
    /// </summary>
    [JsonPropertyName("scoringRules")]
    [XmlElement("ScoringRules")]
    public object? ScoringRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    [JsonPropertyName("tags")]
    [XmlElement("Tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    [JsonPropertyName("remarks")]
    [XmlElement("Remarks")]
    public string? Remarks { get; set; }

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("programInput")]
    [XmlElement("ProgramInput")]
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    [JsonPropertyName("expectedOutput")]
    [XmlElement("ExpectedOutput")]
    public string? ExpectedOutput { get; set; }

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
    /// C#题目类型（兼容ExamLab导出格式）
    /// </summary>
    [JsonPropertyName("csharpQuestionType")]
    [XmlElement("CsharpQuestionType")]
    public string? CsharpQuestionType { get; set; }

    /// <summary>
    /// C#题目类型（标准属性名）
    /// </summary>
    [JsonIgnore]
    public string? CSharpQuestionType => CsharpQuestionType;

    /// <summary>
    /// 代码文件路径（兼容ExamLab导出格式）
    /// </summary>
    [JsonPropertyName("codeFilePath")]
    [XmlElement("CodeFilePath")]
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#直接分数（兼容ExamLab导出格式）
    /// </summary>
    [JsonPropertyName("csharpDirectScore")]
    [XmlElement("CsharpDirectScore")]
    public double? CsharpDirectScore { get; set; }

    /// <summary>
    /// C#直接分数（标准属性名）
    /// </summary>
    [JsonIgnore]
    public double? CSharpDirectScore => CsharpDirectScore;

    /// <summary>
    /// 代码空白填充项（兼容ExamLab导出格式）
    /// </summary>
    [JsonPropertyName("codeBlanks")]
    [XmlArray("CodeBlanks")]
    [XmlArrayItem("CodeBlank")]
    public List<object>? CodeBlanks { get; set; }

    /// <summary>
    /// C#模板代码（仅C#模块代码补全类型使用，包含NotImplementedException的完整代码模板）
    /// </summary>
    [JsonPropertyName("templateCode")]
    [XmlElement("TemplateCode")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 文档文件路径（兼容ExamLab导出格式）
    /// </summary>
    [JsonPropertyName("documentFilePath")]
    [XmlElement("DocumentFilePath")]
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    [JsonPropertyName("operationPoints")]
    [XmlArray("OperationPoints")]
    [XmlArrayItem("OperationPoint")]
    public List<ComprehensiveTrainingOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 综合训练操作点数据传输对象
/// </summary>
public class ComprehensiveTrainingOperationPointDto
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
    public List<ComprehensiveTrainingParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 综合训练参数数据传输对象
/// </summary>
public class ComprehensiveTrainingParameterDto
{
    /// <summary>
    /// 参数ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

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
/// 综合训练导出元数据
/// </summary>
public class ComprehensiveTrainingExportMetadataDto
{
    /// <summary>
    /// 导出版本
    /// </summary>
    [JsonPropertyName("exportVersion")]
    [XmlElement("ExportVersion")]
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导出时间
    /// </summary>
    [JsonPropertyName("exportedAt")]
    [XmlElement("ExportedAt")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导出者
    /// </summary>
    [JsonPropertyName("exportedBy")]
    [XmlElement("ExportedBy")]
    public string ExportedBy { get; set; } = string.Empty;

    /// <summary>
    /// 导出级别
    /// </summary>
    [JsonPropertyName("exportLevel")]
    [XmlElement("ExportLevel")]
    public string ExportLevel { get; set; } = "Complete";

    /// <summary>
    /// 应用程序信息
    /// </summary>
    [JsonPropertyName("applicationInfo")]
    [XmlElement("ApplicationInfo")]
    public string ApplicationInfo { get; set; } = "ExamLab";

    /// <summary>
    /// 应用程序版本
    /// </summary>
    [JsonPropertyName("applicationVersion")]
    [XmlElement("ApplicationVersion")]
    public string ApplicationVersion { get; set; } = "1.0.0";
}
