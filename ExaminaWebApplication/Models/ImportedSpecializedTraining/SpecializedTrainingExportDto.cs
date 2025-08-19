using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.ImportedSpecializedTraining;

/// <summary>
/// 专项训练导出数据传输对象 - 用于接收 ExamLab 导出的专项试卷数据
/// </summary>
public class SpecializedTrainingExportDto
{
    /// <summary>
    /// 专项训练数据
    /// </summary>
    [JsonPropertyName("specializedExam")]
    public SpecializedTrainingDto SpecializedTraining { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    public ExportMetadataDto? Metadata { get; set; }
}

/// <summary>
/// 专项训练数据传输对象
/// </summary>
public class SpecializedTrainingDto
{
    /// <summary>
    /// 专项训练ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 专项训练名称
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 专项训练描述
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 专项模块类型
    /// </summary>
    [JsonPropertyName("moduleType")]
    [Required]
    [StringLength(50)]
    public string ModuleType { get; set; } = "Windows";

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string CreatedTime { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [JsonPropertyName("lastModifiedTime")]
    public string LastModifiedTime { get; set; } = string.Empty;

    /// <summary>
    /// 试卷总分
    /// </summary>
    [JsonPropertyName("totalScore")]
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 难度等级（1-5）
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 是否启用随机题目顺序
    /// </summary>
    [JsonPropertyName("randomizeQuestions")]
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 专项训练标签
    /// </summary>
    [JsonPropertyName("tags")]
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 模块列表
    /// </summary>
    [JsonPropertyName("modules")]
    public List<SpecializedTrainingModuleDto> Modules { get; set; } = [];
}

/// <summary>
/// 专项训练模块数据传输对象
/// </summary>
public class SpecializedTrainingModuleDto
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
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(500)]
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
    public List<SpecializedTrainingQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 专项训练题目数据传输对象
/// </summary>
public class SpecializedTrainingQuestionDto
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
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [JsonPropertyName("content")]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [JsonPropertyName("questionType")]
    [StringLength(50)]
    public string QuestionType { get; set; } = "Practical";

    /// <summary>
    /// 题目分值
    /// </summary>
    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    /// <summary>
    /// 难度等级
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 题目排序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 是否必答题
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = true;

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
    /// 题目标签
    /// </summary>
    [JsonPropertyName("tags")]
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    [JsonPropertyName("operationPoints")]
    public List<SpecializedTrainingOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 专项训练操作点数据传输对象
/// </summary>
public class SpecializedTrainingOperationPointDto
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
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [JsonPropertyName("moduleType")]
    [StringLength(50)]
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    /// <summary>
    /// 操作点排序
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
    /// 参数列表
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<SpecializedTrainingParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 专项训练参数数据传输对象
/// </summary>
public class SpecializedTrainingParameterDto
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数显示名称
    /// </summary>
    [JsonPropertyName("displayName")]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [JsonPropertyName("value")]
    [StringLength(1000)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    [JsonPropertyName("defaultValue")]
    [StringLength(1000)]
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否必需
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 参数排序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// 枚举选项
    /// </summary>
    [JsonPropertyName("enumOptions")]
    [StringLength(1000)]
    public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    [JsonPropertyName("validationRule")]
    [StringLength(500)]
    public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    [JsonPropertyName("validationErrorMessage")]
    [StringLength(200)]
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
/// 导出元数据数据传输对象
/// </summary>
public class ExportMetadataDto
{
    /// <summary>
    /// 导出版本
    /// </summary>
    [JsonPropertyName("exportVersion")]
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导出时间
    /// </summary>
    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导出者
    /// </summary>
    [JsonPropertyName("exportedBy")]
    public string ExportedBy { get; set; } = string.Empty;

    /// <summary>
    /// 数据源类型
    /// </summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = "SpecializedExam";
}
