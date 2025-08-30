using System.Collections.Generic;

namespace BenchSuite.Models;

/// <summary>
/// Word操作点配置模型
/// </summary>
public class WordOperationConfig
{
    /// <summary>
    /// 操作点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 参数模板列表
    /// </summary>
    public List<ParameterTemplate> ParameterTemplates { get; set; } = [];
}

/// <summary>
/// 参数模板
/// </summary>
public class ParameterTemplate
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public ParameterType Type { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 参数排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 枚举选项（当Type为Enum时使用）
    /// </summary>
    public string? EnumOptions { get; set; }

    /// <summary>
    /// 最小值（数字类型时使用）
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// 最大值（数字类型时使用）
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// 参数依赖关系 - 依赖的参数名称
    /// </summary>
    public string? DependsOn { get; set; }

    /// <summary>
    /// 参数依赖值 - 当依赖参数的值等于此值时，该参数才可见
    /// </summary>
    public string? DependsOnValue { get; set; }

    /// <summary>
    /// 参数分组 - 用于位置参数的分组显示
    /// </summary>
    public string? Group { get; set; }
}
