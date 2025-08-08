using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Word;

/// <summary>
/// Word枚举类型表 - 存储Word操作中使用的枚举类型定义
/// </summary>
public class WordEnumType
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 枚举类型名称（如：FontFamily、FontStyle、ParagraphAlignment等）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举类型显示名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举类型描述
    /// </summary>
    [StringLength(300)]
    public string? Description { get; set; }

    /// <summary>
    /// 枚举类别（用于分组管理）
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
    public virtual ICollection<WordEnumValue> EnumValues { get; set; } = new List<WordEnumValue>();

    /// <summary>
    /// 使用此枚举类型的参数列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<WordOperationParameter> Parameters { get; set; } = new List<WordOperationParameter>();
}

/// <summary>
/// Word枚举值表 - 存储具体的枚举值
/// </summary>
public class WordEnumValue
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
    /// 枚举值（程序中使用的值）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值显示名称（界面显示的文本）
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值描述
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 排序顺序
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
    [JsonIgnore]
    public virtual WordEnumType EnumType { get; set; } = null!;
}
