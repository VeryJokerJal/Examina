using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Word;

/// <summary>
/// Word题目模板表 - 存储Word操作题目的模板配置
/// </summary>
public class WordQuestionTemplate
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
    /// 预估完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 标签（用于分类和搜索）
    /// </summary>
    [StringLength(200)]
    public string? Tags { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

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
    public virtual WordOperationPoint OperationPoint { get; set; } = null!;

    /// <summary>
    /// 基于此模板生成的题目实例列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<WordQuestionInstance> QuestionInstances { get; set; } = new List<WordQuestionInstance>();
}

/// <summary>
/// Word题目实例表 - 存储基于模板生成的具体题目实例
/// </summary>
public class WordQuestionInstance
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
    public virtual WordQuestionTemplate QuestionTemplate { get; set; } = null!;
}
