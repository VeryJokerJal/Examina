using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.ImportedExam;

/// <summary>
/// 导入的配置参数实体
/// </summary>
public class ImportedParameter
{
    /// <summary>
    /// 参数ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 操作点ID
    /// </summary>
    [Required]
    public int OperationPointId { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    [StringLength(1000)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    [StringLength(1000)]
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
    [StringLength(2000)]
    public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    [StringLength(500)]
    public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    [StringLength(200)]
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
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的操作点
    /// </summary>
    [ForeignKey(nameof(OperationPointId))]
    public virtual ImportedOperationPoint? OperationPoint { get; set; }
}
