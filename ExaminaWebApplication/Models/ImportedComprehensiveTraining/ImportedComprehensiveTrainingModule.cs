using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedComprehensiveTraining;

/// <summary>
/// 导入的综合训练模块实体（ExamLab特有）
/// </summary>
public class ImportedComprehensiveTrainingModule
{
    /// <summary>
    /// 模块ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始模块ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalModuleId { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练ID
    /// </summary>
    [Required]
    public int ComprehensiveTrainingId { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 模块排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的综合训练
    /// </summary>
    [ForeignKey(nameof(ComprehensiveTrainingId))]
    public virtual ImportedComprehensiveTraining? ComprehensiveTraining { get; set; }

    /// <summary>
    /// 题目列表
    /// </summary>
    public virtual ICollection<ImportedComprehensiveTrainingQuestion> Questions { get; set; } = [];
}
