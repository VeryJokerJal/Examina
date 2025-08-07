using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Windows;

/// <summary>
/// Windows枚举类型表 - 管理枚举值的分类
/// </summary>
public class WindowsEnumType
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
    /// 枚举类型分类（如：文件属性、权限设置等）
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
    public virtual ICollection<WindowsEnumValue> EnumValues { get; set; } = new List<WindowsEnumValue>();

    /// <summary>
    /// 使用此枚举类型的参数列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<WindowsOperationParameter> Parameters { get; set; } = new List<WindowsOperationParameter>();
}

/// <summary>
/// Windows枚举值表 - 存储操作中使用的枚举值和选项
/// </summary>
public class WindowsEnumValue
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
    /// 枚举值的键（如：ReadOnly、Hidden等）
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EnumKey { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值的数值
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
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 1;

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
    public virtual WindowsEnumType EnumType { get; set; } = null!;
}
