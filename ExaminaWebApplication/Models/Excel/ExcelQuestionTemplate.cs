using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel题目模板表 - 管理题目生成的模板和配置
/// </summary>
public class ExcelQuestionTemplate
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的操作点ID
    /// </summary>
    [Required]
    public int OperationPointId { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述模板（支持参数占位符）
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string QuestionTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 参数配置（JSON格式，存储具体的参数值配置）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string ParameterConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// 难度级别（1=简单，2=中等，3=困难）
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 题目标签（用于分类和搜索）
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 使用次数统计
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    public virtual ExcelOperationPoint OperationPoint { get; set; } = null!;

    /// <summary>
    /// 创建者用户
    /// </summary>
    public virtual User? Creator { get; set; }
}

/// <summary>
/// Excel题目实例表 - 存储生成的具体题目实例
/// </summary>
public class ExcelQuestionInstance
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的题目模板ID
    /// </summary>
    [Required]
    public int TemplateId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述（已填充参数的完整描述）
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string QuestionDescription { get; set; } = string.Empty;

    /// <summary>
    /// 实际参数值（JSON格式，存储生成题目时使用的具体参数值）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string ActualParameters { get; set; } = string.Empty;

    /// <summary>
    /// 答案验证规则（JSON格式，存储如何验证学生答案）
    /// </summary>
    [Column(TypeName = "json")]
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 题目状态
    /// </summary>
    public ExcelQuestionStatus Status { get; set; } = ExcelQuestionStatus.Draft;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的题目模板
    /// </summary>
    public virtual ExcelQuestionTemplate Template { get; set; } = null!;

    /// <summary>
    /// 创建者用户
    /// </summary>
    public virtual User? Creator { get; set; }
}

/// <summary>
/// Excel题目状态枚举
/// </summary>
public enum ExcelQuestionStatus
{
    /// <summary>
    /// 草稿状态
    /// </summary>
    Draft = 1,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 2,

    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 3,

    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled = 4
}
