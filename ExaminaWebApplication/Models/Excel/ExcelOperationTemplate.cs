using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel操作点模板表 - 存储Excel操作点的基础模板定义
/// </summary>
public class ExcelOperationTemplate
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作点编号（如：1, 4, 6, 10等）
    /// </summary>
    [Required]
    public int OperationNumber { get; set; }

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
    public string? Description { get; set; }

    /// <summary>
    /// 操作类型（A=基础操作，B=图表操作）
    /// </summary>
    [Required]
    [StringLength(1)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作分类（1=基础操作，2=数据清单操作，3=图表操作）
    /// </summary>
    [Required]
    public ExcelOperationCategory Category { get; set; }

    /// <summary>
    /// 目标对象类型（Worksheet=工作表，Chart=图表，Workbook=工作簿）
    /// </summary>
    [Required]
    public ExcelTargetType TargetType { get; set; }

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
    /// 模板的参数配置列表
    /// </summary>
    public virtual ICollection<ExcelOperationParameterTemplate> ParameterTemplates { get; set; } = new List<ExcelOperationParameterTemplate>();
}

/// <summary>
/// Excel操作点参数模板表 - 存储操作点参数的模板定义
/// </summary>
public class ExcelOperationParameterTemplate
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的操作点模板ID
    /// </summary>
    [Required]
    public int OperationTemplateId { get; set; }

    /// <summary>
    /// 参数顺序
    /// </summary>
    [Required]
    public int ParameterOrder { get; set; }

    /// <summary>
    /// 参数名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    [StringLength(500)]
    public string? ParameterDescription { get; set; }

    /// <summary>
    /// 参数数据类型
    /// </summary>
    [Required]
    public ExcelParameterDataType DataType { get; set; }

    /// <summary>
    /// 是否必填参数
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否允许多个值
    /// </summary>
    public bool AllowMultipleValues { get; set; } = false;

    /// <summary>
    /// 默认值
    /// </summary>
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 示例值
    /// </summary>
    [StringLength(500)]
    public string? ExampleValue { get; set; }

    /// <summary>
    /// 关联的枚举类型ID（如果参数是枚举类型）
    /// </summary>
    public int? EnumTypeId { get; set; }

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
    /// 关联的操作点模板
    /// </summary>
    public virtual ExcelOperationTemplate OperationTemplate { get; set; } = null!;

    /// <summary>
    /// 关联的枚举类型
    /// </summary>
    public virtual ExcelEnumType? EnumType { get; set; }
}
