using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ParameterType
{
    Text,           // 文本
    Number,         // 数字
    Boolean,        // 布尔值
    Enum,           // 枚举
    Color,          // 颜色
    File,           // 文件路径
    MultipleChoice  // 多选
}

/// <summary>
/// 配置参数模型
/// </summary>
public class ConfigurationParameter : ReactiveObject
{
    /// <summary>
    /// 参数ID
    /// </summary>
    [Reactive] public string Id { get; set; } = "param-1";

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
    /// 参数值
    /// </summary>
    [Reactive] public string? Value { get; set; }

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
    /// 枚举选项（当Type为Enum或MultipleChoice时使用）
    /// </summary>
    [Reactive] public string? EnumOptions { get; set; }

    /// <summary>
    /// 验证规则（正则表达式）
    /// </summary>
    [Reactive] public string? ValidationRule { get; set; }

    /// <summary>
    /// 验证错误消息
    /// </summary>
    [Reactive] public string? ValidationErrorMessage { get; set; }

    /// <summary>
    /// 最小值（数字类型时使用）
    /// </summary>
    [Reactive] public double? MinValue { get; set; }

    /// <summary>
    /// 最大值（数字类型时使用）
    /// </summary>
    [Reactive] public double? MaxValue { get; set; }

    /// <summary>
    /// 是否启用该参数
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 枚举选项列表（从EnumOptions解析而来）
    /// </summary>
    public List<string> EnumOptionsList => string.IsNullOrEmpty(EnumOptions) ? [] : EnumOptions.Split(',').Select(s => s.Trim()).ToList();
}
