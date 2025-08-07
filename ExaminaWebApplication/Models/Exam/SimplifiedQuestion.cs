using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 简化题目模型 - 用于新的简化题目创建流程
/// </summary>
public class SimplifiedQuestion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的科目ID
    /// </summary>
    [Required]
    public int SubjectId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 操作配置（JSON格式，存储具体的操作参数）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string OperationConfig { get; set; } = "{}";

    /// <summary>
    /// 自动生成的题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 自动生成的题目描述
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 输入示例
    /// </summary>
    [StringLength(500)]
    public string? InputExample { get; set; }

    /// <summary>
    /// 输入描述
    /// </summary>
    [StringLength(1000)]
    public string? InputDescription { get; set; }

    /// <summary>
    /// 输出示例
    /// </summary>
    [StringLength(500)]
    public string? OutputExample { get; set; }

    /// <summary>
    /// 输出描述
    /// </summary>
    [StringLength(1000)]
    public string? OutputDescription { get; set; }

    /// <summary>
    /// 题目要求（支持Markdown格式）
    /// </summary>
    [StringLength(2000)]
    public string? Requirements { get; set; }

    /// <summary>
    /// 题目类型（根据科目自动确定）
    /// </summary>
    [Required]
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的科目
    /// </summary>
    [JsonIgnore]
    public virtual ExamSubject Subject { get; set; } = null!;
}

/// <summary>
/// Windows操作配置模型
/// </summary>
public class WindowsOperationConfig
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型（File/Folder）
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// 是否为文件（true=文件，false=文件夹）
    /// </summary>
    public bool? IsFile { get; set; }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 目标路径
    /// </summary>
    public string? TargetPath { get; set; }

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// 源是否为文件（true=文件，false=文件夹）
    /// </summary>
    public bool? SourceIsFile { get; set; }

    /// <summary>
    /// 原文件名
    /// </summary>
    public string? OriginalName { get; set; }

    /// <summary>
    /// 新文件名
    /// </summary>
    public string? NewName { get; set; }

    /// <summary>
    /// 快捷方式位置
    /// </summary>
    public string? ShortcutLocation { get; set; }

    /// <summary>
    /// 属性类型
    /// </summary>
    public string? PropertyType { get; set; }

    /// <summary>
    /// 是否保留原文件
    /// </summary>
    public bool? KeepOriginal { get; set; }

    /// <summary>
    /// 是否启用属性
    /// </summary>
    public bool? Enable { get; set; }
}

/// <summary>
/// Excel操作配置模型
/// </summary>
public class ExcelOperationConfig
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 单元格范围
    /// </summary>
    public string? CellRange { get; set; }

    /// <summary>
    /// 数据范围
    /// </summary>
    public string? DataRange { get; set; }

    /// <summary>
    /// 格式类型
    /// </summary>
    public string? FormatType { get; set; }

    /// <summary>
    /// 格式值
    /// </summary>
    public string? FormatValue { get; set; }

    /// <summary>
    /// 操作列
    /// </summary>
    public string? Column { get; set; }

    /// <summary>
    /// 条件
    /// </summary>
    public string? Criteria { get; set; }

    /// <summary>
    /// 图表类型
    /// </summary>
    public string? ChartType { get; set; }

    /// <summary>
    /// 图表标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 图表位置
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// 是否显示图例
    /// </summary>
    public bool? ShowLegend { get; set; }

    /// <summary>
    /// 操作类型（基础操作的子类型）
    /// </summary>
    public string? Operation { get; set; }
}

/// <summary>
/// 简化题目创建请求模型
/// </summary>
public class CreateSimplifiedQuestionRequest
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [Required]
    public int SubjectId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Required]
    [Range(0.1, 100.0)]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 操作配置
    /// </summary>
    [Required]
    public object OperationConfig { get; set; } = new();

    /// <summary>
    /// 自动生成的题目标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 自动生成的题目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 输入示例
    /// </summary>
    public string? InputExample { get; set; }

    /// <summary>
    /// 输入描述
    /// </summary>
    public string? InputDescription { get; set; }

    /// <summary>
    /// 输出示例
    /// </summary>
    public string? OutputExample { get; set; }

    /// <summary>
    /// 输出描述
    /// </summary>
    public string? OutputDescription { get; set; }

    /// <summary>
    /// 题目要求（支持Markdown格式）
    /// </summary>
    public string? Requirements { get; set; }
}

/// <summary>
/// 简化题目响应模型
/// </summary>
public class SimplifiedQuestionResponse
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 科目ID
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作配置
    /// </summary>
    public object OperationConfig { get; set; } = new();

    /// <summary>
    /// 输入示例
    /// </summary>
    public string? InputExample { get; set; }

    /// <summary>
    /// 输入描述
    /// </summary>
    public string? InputDescription { get; set; }

    /// <summary>
    /// 输出示例
    /// </summary>
    public string? OutputExample { get; set; }

    /// <summary>
    /// 输出描述
    /// </summary>
    public string? OutputDescription { get; set; }

    /// <summary>
    /// 题目要求（支持Markdown格式）
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
