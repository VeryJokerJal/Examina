using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 模拟考试综合训练DTO - 匹配ImportedComprehensiveTraining结构
/// </summary>
public class MockExamComprehensiveTrainingDto
{
    /// <summary>
    /// 综合训练ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始综合训练ID（来自ExamLab）
    /// </summary>
    public string OriginalComprehensiveTrainingId { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 综合训练类型
    /// </summary>
    public string ComprehensiveTrainingType { get; set; } = "MockExam";

    /// <summary>
    /// 综合训练状态
    /// </summary>
    public string Status { get; set; } = "InProgress";

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 训练时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 训练结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重做
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重做次数
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
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否启用试用功能
    /// </summary>
    public bool EnableTrial { get; set; } = true;

    /// <summary>
    /// 训练标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置（JSON格式）
    /// </summary>
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 导入者ID
    /// </summary>
    public int ImportedBy { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始创建者ID（来自ExamLab）
    /// </summary>
    public int OriginalCreatedBy { get; set; }

    /// <summary>
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; }

    /// <summary>
    /// 原始更新时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalUpdatedAt { get; set; }

    /// <summary>
    /// 原始发布时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalPublishedAt { get; set; }

    /// <summary>
    /// 原始发布者ID（来自ExamLab）
    /// </summary>
    public int? OriginalPublishedBy { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    public string? ImportFileName { get; set; }

    /// <summary>
    /// 导入文件大小（字节）
    /// </summary>
    public long ImportFileSize { get; set; }

    /// <summary>
    /// 导入版本
    /// </summary>
    public string ImportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导入状态
    /// </summary>
    public string ImportStatus { get; set; } = "Success";

    /// <summary>
    /// 导入错误信息
    /// </summary>
    public string? ImportErrorMessage { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    public List<MockExamSubjectDto> Subjects { get; set; } = [];

    /// <summary>
    /// 模块列表
    /// </summary>
    public List<MockExamModuleDto> Modules { get; set; } = [];

    /// <summary>
    /// 题目列表（包含所有科目和模块下的题目）
    /// 注意：在模块化考试中，当所有题目都已分组到模块时，此字段将被设置为null并在JSON响应中隐藏
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MockExamQuestionDto>? Questions { get; set; }
}

/// <summary>
/// 模拟考试科目DTO
/// </summary>
public class MockExamSubjectDto
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始科目ID（来自ExamLab）
    /// </summary>
    public string OriginalSubjectId { get; set; } = string.Empty;

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
    public decimal Score { get; set; }

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
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最低分数
    /// </summary>
    public decimal MinScore { get; set; }

    /// <summary>
    /// 权重
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 科目配置（JSON格式）
    /// </summary>
    public string? SubjectConfig { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<MockExamQuestionDto> Questions { get; set; } = [];
}

/// <summary>
/// 模拟考试题目DTO
/// </summary>
public class MockExamQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始题目ID（来自ExamLab）
    /// </summary>
    public string OriginalQuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练ID
    /// </summary>
    public int ComprehensiveTrainingId { get; set; }

    /// <summary>
    /// 科目ID（可选，如果题目属于科目）
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// 模块ID（可选，如果题目属于模块）
    /// </summary>
    public int? ModuleId { get; set; }

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
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 题目顺序
    /// </summary>
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 题目配置（JSON格式）
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则（JSON格式）
    /// </summary>
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 标准答案（JSON格式）
    /// </summary>
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则（JSON格式）
    /// </summary>
    public string? ScoringRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
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
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始更新时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalUpdatedAt { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 操作点列表
    /// </summary>
    public List<MockExamOperationPointDto> OperationPoints { get; set; } = [];
}

/// <summary>
/// 模拟考试操作点DTO
/// </summary>
public class MockExamOperationPointDto
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始操作点ID（来自ExamLab）
    /// </summary>
    public string OriginalOperationPointId { get; set; } = string.Empty;

    /// <summary>
    /// 题目ID
    /// </summary>
    public int QuestionId { get; set; }

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
    public decimal Score { get; set; }

    /// <summary>
    /// 操作点顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间（来自ExamLab）
    /// </summary>
    public string CreatedTime { get; set; } = string.Empty;

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 配置参数列表
    /// </summary>
    public List<MockExamParameterDto> Parameters { get; set; } = [];
}

/// <summary>
/// 模拟考试配置参数DTO
/// </summary>
public class MockExamParameterDto
{
    /// <summary>
    /// 参数ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作点ID
    /// </summary>
    public int OperationPointId { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 参数顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 枚举选项
    /// </summary>
    public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    public string? ValidationErrorMessage { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 模拟考试模块DTO
/// </summary>
public class MockExamModuleDto
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 原始模块ID（来自ExamLab）
    /// </summary>
    public string OriginalModuleId { get; set; } = string.Empty;

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
    public string? Description { get; set; }

    /// <summary>
    /// 模块分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 模块顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<MockExamQuestionDto> Questions { get; set; } = [];
}
