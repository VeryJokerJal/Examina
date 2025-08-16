using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace ExamLab.Models;

/// <summary>
/// PPT知识点配置模型
/// </summary>
public class PowerPointKnowledgeConfig : ReactiveObject
{
    /// <summary>
    /// 知识点类型
    /// </summary>
    [Reactive] public PowerPointKnowledgeType KnowledgeType { get; set; }

    /// <summary>
    /// 知识点名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 知识点描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 知识点分类
    /// </summary>
    [Reactive] public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 配置参数模板
    /// </summary>
    public ObservableCollection<ConfigurationParameterTemplate> ParameterTemplates { get; set; } = [];
}

/// <summary>
/// 配置参数模板
/// </summary>
public class ConfigurationParameterTemplate : ReactiveObject
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数显示名称
    /// </summary>
    [Reactive] public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [Reactive] public ParameterType Type { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [Reactive] public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [Reactive] public bool IsRequired { get; set; }

    /// <summary>
    /// 参数排序
    /// </summary>
    [Reactive] public int Order { get; set; }

    /// <summary>
    /// 枚举选项（JSON格式）
    /// </summary>
    [Reactive] public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    [Reactive] public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    [Reactive] public string? ValidationErrorMessage { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    [Reactive] public double? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    [Reactive] public double? MaxValue { get; set; }
}
