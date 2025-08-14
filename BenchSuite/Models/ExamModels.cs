using System.Text.Json.Serialization;

namespace BenchSuite.Models;

/// <summary>
/// 试卷模型 - 统一版本，兼容ExamLab.Models.Exam
/// </summary>
public class ExamModel
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 试卷包含的模块
    /// </summary>
    public List<ExamModuleModel> Modules { get; set; } = [];

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 试卷总分
    /// </summary>
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    public decimal PassingScore { get; set; } = 60.0m;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 试卷标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置 (JSON格式)
    /// </summary>
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    public string ExamType { get; set; } = "UnifiedExam";

    /// <summary>
    /// 试卷状态
    /// </summary>
    public string Status { get; set; } = "Draft";
}

/// <summary>
/// 试卷模块模型 - 统一版本，兼容ExamLab
/// </summary>
public class ExamModuleModel
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType Type { get; set; }

    /// <summary>
    /// 模块类型字符串 (ExamLab兼容)
    /// </summary>
    [JsonIgnore]
    public string TypeString
    {
        get => Type.ToString();
        set => Type = Enum.TryParse<ModuleType>(value, true, out var result) ? result : ModuleType.Windows;
    }

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 模块包含的题目
    /// </summary>
    public List<QuestionModel> Questions { get; set; } = [];

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 模块排序
    /// </summary>
    public int Order { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 模块考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 模块权重
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 是否必考模块
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 模块配置 (JSON格式)
    /// </summary>
    public string? ModuleConfig { get; set; }

    /// <summary>
    /// 模块类型 (ExamLab格式兼容)
    /// </summary>
    [JsonPropertyName("subjectType")]
    public string? SubjectType { get; set; }

    /// <summary>
    /// 排序顺序 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder
    {
        get => Order;
        set => Order = value;
    }
}

/// <summary>
/// 题目模型 - 统一版本，兼容ExamLab
/// </summary>
public class QuestionModel
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值（已弃用，使用TotalScore）
    /// </summary>
    [Obsolete("使用TotalScore属性代替")]
    public decimal Score { get; set; }

    /// <summary>
    /// 题目总分值（基于所有操作点分数的总和）
    /// </summary>
    public decimal TotalScore
    {
        get
        {
            if (OperationPoints == null || OperationPoints.Count == 0)
                return Score; // 回退到Score字段
            return OperationPoints.Where(op => op.IsEnabled).Sum(op => op.Score);
        }
    }

    /// <summary>
    /// 题目排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    public List<OperationPointModel> OperationPoints { get; set; } = [];

    /// <summary>
    /// 是否启用该题目
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = "Practical";

    /// <summary>
    /// 难度级别 (1-5)
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 标准答案 (JSON格式)
    /// </summary>
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则 (JSON格式)
    /// </summary>
    public string? ScoringRules { get; set; }

    /// <summary>
    /// 答案验证规则 (JSON格式)
    /// </summary>
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 题目配置 (JSON格式)
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// 排序顺序 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder
    {
        get => Order;
        set => Order = value;
    }
}

/// <summary>
/// 操作点模型 - 统一版本，兼容ExamLab
/// </summary>
public class OperationPointModel
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    public string? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// Word知识点类型（当ModuleType为Word时使用）
    /// </summary>
    public string? WordKnowledgeType { get; set; }

    /// <summary>
    /// Excel知识点类型（当ModuleType为Excel时使用）
    /// </summary>
    public string? ExcelKnowledgeType { get; set; }

    /// <summary>
    /// 操作点分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    public List<ConfigurationParameterModel> Parameters { get; set; } = [];

    /// <summary>
    /// 是否启用该操作点
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 操作点排序
    /// </summary>
    public int Order { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 兼容ExamLab的字符串时间格式
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string CreatedTimeString
    {
        get => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        set => CreatedAt = DateTime.TryParse(value, out var result) ? result : DateTime.UtcNow;
    }

    /// <summary>
    /// 操作配置 (JSON格式)
    /// </summary>
    public string? OperationConfig { get; set; }

    /// <summary>
    /// 操作点标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Windows操作类型 (ExamLab兼容)
    /// </summary>
    public string? WindowsOperationType { get; set; }
}

/// <summary>
/// 配置参数模型 - 统一版本，兼容ExamLab
/// </summary>
public class ConfigurationParameterModel
{
    /// <summary>
    /// 参数ID (ExamLab兼容)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonIgnore]
    public ParameterType Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 验证规则 (JSON格式)
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 参数描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 选项列表 (用于枚举和多选类型)
    /// </summary>
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 参数类型字符串 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("type")]
    public string TypeString
    {
        get => Type.ToString();
        set => Type = Enum.TryParse<ParameterType>(value, true, out ParameterType result) ? result : ParameterType.Text;
    }
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
    Text,           // 文本
    Number,         // 数字
    Boolean,        // 布尔值
    Enum,           // 枚举
    Color,          // 颜色
    File,           // 文件路径
    MultipleChoice,  // 多选
    Date
}
