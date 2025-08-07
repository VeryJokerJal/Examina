using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Windows;

/// <summary>
/// Windows操作点主表 - 存储所有Windows文件系统操作点的基本信息
/// </summary>
public class WindowsOperationPoint
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作点编号（如：1, 2, 3, 4等）
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
    /// 操作类型
    /// </summary>
    [Required]
    public WindowsOperationType OperationType { get; set; }

    /// <summary>
    /// 操作模式（文件模式或文件夹模式）
    /// </summary>
    [Required]
    public WindowsOperationMode OperationMode { get; set; }

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
    public virtual ICollection<WindowsOperationParameter> Parameters { get; set; } = new List<WindowsOperationParameter>();

    /// <summary>
    /// 操作点的题目模板列表
    /// </summary>
    public virtual ICollection<WindowsQuestionTemplate> QuestionTemplates { get; set; } = new List<WindowsQuestionTemplate>();
}

/// <summary>
/// Windows操作类型枚举
/// </summary>
public enum WindowsOperationType
{
    /// <summary>
    /// 创建文件/文件夹
    /// </summary>
    Create = 1,

    /// <summary>
    /// 复制文件/文件夹
    /// </summary>
    Copy = 2,

    /// <summary>
    /// 移动文件/文件夹
    /// </summary>
    Move = 3,

    /// <summary>
    /// 删除文件/文件夹
    /// </summary>
    Delete = 4,

    /// <summary>
    /// 重命名文件/文件夹
    /// </summary>
    Rename = 5,

    /// <summary>
    /// 创建快捷方式
    /// </summary>
    CreateShortcut = 6,

    /// <summary>
    /// 修改属性
    /// </summary>
    ModifyProperties = 7,

    /// <summary>
    /// 复制并重命名
    /// </summary>
    CopyAndRename = 8
}

/// <summary>
/// Windows操作模式枚举
/// </summary>
public enum WindowsOperationMode
{
    /// <summary>
    /// 文件模式
    /// </summary>
    File = 1,

    /// <summary>
    /// 文件夹模式
    /// </summary>
    Folder = 2,

    /// <summary>
    /// 通用模式（支持文件和文件夹）
    /// </summary>
    Universal = 3
}
