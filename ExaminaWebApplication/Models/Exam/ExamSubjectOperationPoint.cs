using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 科目操作点关联表 - 存储科目与操作点的关联关系
/// </summary>
public class ExamSubjectOperationPoint
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的科目ID
    /// </summary>
    [Required]
    public int ExamSubjectId { get; set; }

    /// <summary>
    /// 操作点编号
    /// </summary>
    [Required]
    public int OperationNumber { get; set; }

    /// <summary>
    /// 操作点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// 操作点类型（Excel、Windows等）
    /// </summary>
    [Required]
    public SubjectType OperationSubjectType { get; set; }

    /// <summary>
    /// 操作类型（如：BasicOperation、Create等）
    /// </summary>
    [StringLength(50)]
    public string? OperationType { get; set; }

    /// <summary>
    /// 分值权重
    /// </summary>
    [Range(0.1, 10.0)]
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 参数配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ParameterConfig { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
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
    /// 关联的科目
    /// </summary>
    [JsonIgnore]
    public virtual ExamSubject ExamSubject { get; set; } = null!;
}

/// <summary>
/// 操作点配置统计
/// </summary>
public class OperationPointStatistics
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// 科目名称
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目类型
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// 总操作点数量
    /// </summary>
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 已启用操作点数量
    /// </summary>
    public int EnabledOperationPoints { get; set; }

    /// <summary>
    /// 总权重
    /// </summary>
    public decimal TotalWeight { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}
