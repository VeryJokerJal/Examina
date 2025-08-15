using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedExam;

/// <summary>
/// 导入的操作点实体
/// </summary>
public class ImportedOperationPoint
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始操作点ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalOperationPointId { get; set; } = string.Empty;

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 操作点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
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
    [StringLength(50)]
    public string CreatedTime { get; set; } = string.Empty;

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的题目
    /// </summary>
    [ForeignKey(nameof(QuestionId))]
    public virtual ImportedQuestion? Question { get; set; }

    /// <summary>
    /// 配置参数列表
    /// </summary>
    public virtual ICollection<ImportedParameter> Parameters { get; set; } = new List<ImportedParameter>();
}
