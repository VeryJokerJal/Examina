using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Windows;

/// <summary>
/// Windows题目模板表 - 存储Windows操作题目的模板配置
/// </summary>
public class WindowsQuestionTemplate
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
    /// 题目模板内容（支持参数占位符）
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string QuestionTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 参数配置（JSON格式，定义模板中使用的参数）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string ParameterConfiguration { get; set; } = "{}";

    /// <summary>
    /// 难度等级（1-5，1最简单，5最难）
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

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
    /// 标签（用于分类和搜索）
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
    [JsonIgnore]
    public virtual WindowsOperationPoint OperationPoint { get; set; } = null!;

    /// <summary>
    /// 基于此模板生成的题目实例列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<WindowsQuestionInstance> QuestionInstances { get; set; } = new List<WindowsQuestionInstance>();
}

/// <summary>
/// Windows题目实例表 - 存储基于模板生成的具体题目实例
/// </summary>
public class WindowsQuestionInstance
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的题目模板ID
    /// </summary>
    [Required]
    public int QuestionTemplateId { get; set; }

    /// <summary>
    /// 题目实例名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// 生成的具体题目内容
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string QuestionContent { get; set; } = string.Empty;

    /// <summary>
    /// 实际使用的参数值（JSON格式）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string ParameterValues { get; set; } = "{}";

    /// <summary>
    /// 预期答案配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ExpectedAnswer { get; set; }

    /// <summary>
    /// 评分标准（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ScoringCriteria { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 使用次数统计
    /// </summary>
    public int UsageCount { get; set; } = 0;

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
    [JsonIgnore]
    public virtual WindowsQuestionTemplate QuestionTemplate { get; set; } = null!;
}
