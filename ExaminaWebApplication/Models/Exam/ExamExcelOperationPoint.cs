using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 试卷Excel操作点表 - 存储每套试卷独立的Excel操作点配置
/// </summary>
public class ExamExcelOperationPoint
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的试卷ID
    /// </summary>
    [Required]
    public int ExamId { get; set; }

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
    /// 基于的模板ID（可选，用于追踪来源）
    /// </summary>
    public int? TemplateId { get; set; }

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
    /// 关联的试卷
    /// </summary>
    [JsonIgnore]
    public virtual Exam Exam { get; set; } = null!;

    /// <summary>
    /// 基于的模板（可选）
    /// </summary>
    [JsonIgnore]
    public virtual ExcelOperationTemplate? Template { get; set; }

    /// <summary>
    /// 操作点的参数配置列表
    /// </summary>
    public virtual ICollection<ExamExcelOperationParameter> Parameters { get; set; } = new List<ExamExcelOperationParameter>();
}

/// <summary>
/// 试卷Excel操作点参数表 - 存储每套试卷独立的Excel操作点参数配置
/// </summary>
public class ExamExcelOperationParameter
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的试卷Excel操作点ID
    /// </summary>
    [Required]
    public int ExamOperationPointId { get; set; }

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
    /// 参数值（试卷特定的配置值）
    /// </summary>
    [StringLength(500)]
    public string? ParameterValue { get; set; }

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
    /// 基于的参数模板ID（可选，用于追踪来源）
    /// </summary>
    public int? ParameterTemplateId { get; set; }

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
    /// 关联的试卷Excel操作点
    /// </summary>
    [JsonIgnore]
    public virtual ExamExcelOperationPoint ExamOperationPoint { get; set; } = null!;

    /// <summary>
    /// 关联的枚举类型
    /// </summary>
    [JsonIgnore]
    public virtual ExcelEnumType? EnumType { get; set; }

    /// <summary>
    /// 基于的参数模板（可选）
    /// </summary>
    [JsonIgnore]
    public virtual ExcelOperationParameterTemplate? ParameterTemplate { get; set; }
}
