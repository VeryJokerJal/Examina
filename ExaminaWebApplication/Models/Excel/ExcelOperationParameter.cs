using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel操作参数配置表 - 存储每个操作点的可配置参数
/// </summary>
public class ExcelOperationParameter
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
    /// 参数序号（1-13，对应配置项三的子参数）
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
    /// 是否支持多值（如多个单元格、多个列等）
    /// </summary>
    public bool AllowMultipleValues { get; set; } = false;

    /// <summary>
    /// 关联的枚举值类型ID（如果参数是枚举类型）
    /// </summary>
    public int? EnumTypeId { get; set; }

    /// <summary>
    /// 参数验证规则（JSON格式存储）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 参数示例值
    /// </summary>
    [StringLength(500)]
    public string? ExampleValue { get; set; }

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
    /// 关联的操作点
    /// </summary>
    public virtual ExcelOperationPoint OperationPoint { get; set; } = null!;

    /// <summary>
    /// 关联的枚举值类型
    /// </summary>
    public virtual ExcelEnumType? EnumType { get; set; }
}

/// <summary>
/// Excel参数数据类型枚举
/// </summary>
public enum ExcelParameterDataType
{
    /// <summary>
    /// 字符串类型
    /// </summary>
    String = 1,

    /// <summary>
    /// 整数类型
    /// </summary>
    Integer = 2,

    /// <summary>
    /// 小数类型
    /// </summary>
    Decimal = 3,

    /// <summary>
    /// 布尔类型
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// 枚举类型
    /// </summary>
    Enum = 5,

    /// <summary>
    /// 单元格范围类型（如A1:B10）
    /// </summary>
    CellRange = 6,

    /// <summary>
    /// 颜色值类型（RGB）
    /// </summary>
    Color = 7,

    /// <summary>
    /// 公式类型
    /// </summary>
    Formula = 8,

    /// <summary>
    /// JSON对象类型（复杂配置）
    /// </summary>
    JsonObject = 9
}
