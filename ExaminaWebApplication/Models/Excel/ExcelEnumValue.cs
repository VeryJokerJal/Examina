using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel枚举类型表 - 管理枚举值的分类
/// </summary>
public class ExcelEnumType
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 枚举类型名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举类型描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 枚举类型分类（如：对齐方式、边框样式、图表类型等）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

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
    /// 枚举值列表
    /// </summary>
    public virtual ICollection<ExcelEnumValue> EnumValues { get; set; } = new List<ExcelEnumValue>();

    /// <summary>
    /// 使用此枚举类型的参数列表
    /// </summary>
    public virtual ICollection<ExcelOperationParameter> Parameters { get; set; } = new List<ExcelOperationParameter>();
}

/// <summary>
/// Excel枚举值表 - 存储操作中使用的枚举值和选项
/// </summary>
public class ExcelEnumValue
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的枚举类型ID
    /// </summary>
    [Required]
    public int EnumTypeId { get; set; }

    /// <summary>
    /// 枚举值的键（如：xlLeft、xlCenter等VBA常量名）
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EnumKey { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值的数值（如：-4131、-4108等）
    /// </summary>
    public int? EnumValue { get; set; }

    /// <summary>
    /// 枚举值的中文显示名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否为默认值
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 扩展属性（JSON格式，存储额外的配置信息）
    /// </summary>
    public string? ExtendedProperties { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的枚举类型
    /// </summary>
    public virtual ExcelEnumType EnumType { get; set; } = null!;
}
