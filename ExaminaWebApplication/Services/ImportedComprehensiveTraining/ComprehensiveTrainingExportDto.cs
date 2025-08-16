using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ExaminaWebApplication.Services.ImportedComprehensiveTraining;

/// <summary>
/// 综合训练导出数据传输对象 - 支持JSON和XML格式
/// </summary>
[XmlRoot("ComprehensiveTrainingProject")]
public class ComprehensiveTrainingExportDto
{
    /// <summary>
    /// 综合训练信息
    /// </summary>
    [JsonPropertyName("exam")]
    [XmlElement("exam")]
    public ComprehensiveTrainingDto ComprehensiveTraining { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    [XmlElement("Metadata")]
    public ComprehensiveTrainingExportMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// 综合训练数据传输对象
/// </summary>
public class ComprehensiveTrainingDto
{
    /// <summary>
    /// 综合训练ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练名称
    /// </summary>
    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练描述
    /// </summary>
    [JsonPropertyName("description")]
    [XmlElement("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// 综合训练类型
    /// </summary>
    [JsonPropertyName("comprehensiveTrainingType")]
    [XmlElement("ComprehensiveTrainingType")]
    public string ComprehensiveTrainingType { get; set; } = "UnifiedTraining";

    /// <summary>
    /// 综合训练状态
    /// </summary>
    [JsonPropertyName("status")]
    [XmlElement("Status")]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("totalScore")]
    [XmlElement("TotalScore")]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 训练时长（分钟）
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    [XmlElement("DurationMinutes")]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 训练开始时间
    /// </summary>
    [JsonPropertyName("startTime")]
    [XmlElement("StartTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 训练结束时间
    /// </summary>
    [JsonPropertyName("endTime")]
    [XmlElement("EndTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重做
    /// </summary>
    [JsonPropertyName("allowRetake")]
    [XmlElement("AllowRetake")]
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重做次数
    /// </summary>
    [JsonPropertyName("maxRetakeCount")]
    [XmlElement("MaxRetakeCount")]
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [JsonPropertyName("passingScore")]
    [XmlElement("PassingScore")]
    public decimal PassingScore { get; set; } = 60.0m;

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
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [XmlElement("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 训练标签
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
    /// 创建者ID
    /// </summary>
    [JsonPropertyName("createdBy")]
    [XmlElement("CreatedBy")]
    public int CreatedBy { get; set; }

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
    /// 科目列表
    /// </summary>
    [JsonPropertyName("subjects")]
    [XmlArray("Subjects")]
    [XmlArrayItem("Subject")]
    public List<ComprehensiveTrainingSubjectDto> Subjects { get; set; } = [];

    /// <summary>
    /// 模块列表
    /// </summary>
    [JsonPropertyName("modules")]
    [XmlArray("Modules")]
    [XmlArrayItem("Module")]
    public List<ComprehensiveTrainingModuleDto> Modules { get; set; } = [];
}

/// <summary>
/// 综合训练科目数据传输对象
/// </summary>
public class ComprehensiveTrainingSubjectDto
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [JsonPropertyName("id")]
    [XmlElement("Id")]
    public int Id { get; set; }

    /// <summary>
    /// 综合训练ID
    /// </summary>
    [JsonPropertyName("comprehensiveTrainingId")]
    public int ComprehensiveTrainingId { get; set; }

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
    /// 科目训练时长（分钟）
    /// </summary>
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 科目顺序
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必做科目
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
    public List<ComprehensiveTrainingQuestionDto> Questions { get; set; } = [];

    /// <summary>
    /// 题目数量
    /// </summary>
    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; set; }
}
