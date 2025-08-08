using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Word;

/// <summary>
/// Word操作点主表 - 存储所有Word段落操作点的基本信息
/// </summary>
public class WordOperationPoint
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作点编号（知识点编号：1-14）
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
    /// 操作类别
    /// </summary>
    [Required]
    public WordOperationCategory Category { get; set; }

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
    public virtual ICollection<WordOperationParameter> Parameters { get; set; } = [];

    /// <summary>
    /// 操作点的题目模板列表
    /// </summary>
    public virtual ICollection<WordQuestionTemplate> QuestionTemplates { get; set; } = [];
}

/// <summary>
/// Word操作类别枚举
/// </summary>
public enum WordOperationCategory
{
    /// <summary>
    /// 段落文字样式（字体、字号、字形、颜色等）
    /// </summary>
    ParagraphTextStyle = 1,

    /// <summary>
    /// 段落格式设置（文字位置与布局）
    /// </summary>
    ParagraphFormat = 2,

    /// <summary>
    /// 段落间距与边框
    /// </summary>
    ParagraphSpacingBorder = 3,

    /// <summary>
    /// 段落背景设置
    /// </summary>
    ParagraphBackground = 4,

    /// <summary>
    /// 页面设置
    /// </summary>
    PageSetup = 5,

    /// <summary>
    /// 页眉页脚设置
    /// </summary>
    HeaderFooter = 6,

    /// <summary>
    /// 页码与背景设置
    /// </summary>
    PageNumberBackground = 7,

    /// <summary>
    /// 页面边框设置
    /// </summary>
    PageBorder = 8,

    /// <summary>
    /// 水印设置
    /// </summary>
    Watermark = 9,

    /// <summary>
    /// 项目符号与编号
    /// </summary>
    ListNumbering = 10,

    /// <summary>
    /// 表格操作
    /// </summary>
    Table = 11,

    /// <summary>
    /// 自选图形
    /// </summary>
    Shape = 12,

    /// <summary>
    /// 自选图形文字
    /// </summary>
    ShapeText = 13,

    /// <summary>
    /// 自选图形位置
    /// </summary>
    ShapePosition = 14,

    /// <summary>
    /// 图片设置
    /// </summary>
    Picture = 15,

    /// <summary>
    /// 图片尺寸
    /// </summary>
    PictureSize = 16,

    /// <summary>
    /// 文本框设置
    /// </summary>
    TextBox = 17,

    /// <summary>
    /// 其他操作
    /// </summary>
    Other = 18
}

/// <summary>
/// Word操作参数表 - 存储每个操作点的参数定义
/// </summary>
public class WordOperationParameter
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
    /// 参数键名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ParameterKey { get; set; } = string.Empty;

    /// <summary>
    /// 参数显示名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 参数数据类型
    /// </summary>
    [Required]
    public WordParameterDataType DataType { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 默认值
    /// </summary>
    [StringLength(200)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 关联的枚举类型ID（当DataType为Enum时使用）
    /// </summary>
    public int? EnumTypeId { get; set; }

    /// <summary>
    /// 参数在界面中的显示顺序
    /// </summary>
    public int ParameterOrder { get; set; } = 0;

    /// <summary>
    /// 参数描述或提示信息
    /// </summary>
    [StringLength(300)]
    public string? Description { get; set; }

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
    [JsonIgnore]
    public virtual WordOperationPoint OperationPoint { get; set; } = null!;

    /// <summary>
    /// 关联的枚举类型
    /// </summary>
    [JsonIgnore]
    public virtual WordEnumType? EnumType { get; set; }
}

/// <summary>
/// Word参数数据类型枚举
/// </summary>
public enum WordParameterDataType
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
    /// 颜色类型
    /// </summary>
    Color = 6
}
