using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel操作点主表 - 存储所有Excel操作点的基本信息
/// </summary>
public class ExcelOperationPoint
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
    /// 操作点的参数配置列表
    /// </summary>
    public virtual ICollection<ExcelOperationParameter> Parameters { get; set; } = new List<ExcelOperationParameter>();

    /// <summary>
    /// 操作点的题目模板列表
    /// </summary>
    public virtual ICollection<ExcelQuestionTemplate> QuestionTemplates { get; set; } = new List<ExcelQuestionTemplate>();
}

/// <summary>
/// Excel操作分类枚举
/// </summary>
public enum ExcelOperationCategory
{
    /// <summary>
    /// 基础操作（23个操作点）
    /// </summary>
    BasicOperation = 1,

    /// <summary>
    /// 数据清单操作（6个操作点）
    /// </summary>
    DataListOperation = 2,

    /// <summary>
    /// 图表操作（22个操作点）
    /// </summary>
    ChartOperation = 3
}

/// <summary>
/// Excel目标对象类型枚举
/// </summary>
public enum ExcelTargetType
{
    /// <summary>
    /// 工作表
    /// </summary>
    Worksheet = 1,

    /// <summary>
    /// 图表
    /// </summary>
    Chart = 2,

    /// <summary>
    /// 工作簿
    /// </summary>
    Workbook = 3
}
