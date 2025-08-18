using System;
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
    /// 参数依赖关系 - 依赖的参数名称
    /// </summary>
    [Reactive] public string? DependsOn { get; set; }

    /// <summary>
    /// 参数依赖值 - 当依赖参数的值等于此值时，该参数才可见
    /// </summary>
    [Reactive] public string? DependsOnValue { get; set; }

    /// <summary>
    /// 是否可见（基于依赖关系计算）
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 参数分组 - 用于位置参数的分组显示
    /// </summary>
    [Reactive] public string? Group { get; set; }

    /// <summary>
    /// 枚举选项列表（从EnumOptions解析而来）
    /// </summary>
    public List<string> EnumOptionsList => string.IsNullOrEmpty(EnumOptions) ? [] : ParseEnumOptions(EnumOptions);

    /// <summary>
    /// 解析枚举选项，特殊处理页码格式等包含逗号的选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>解析后的选项列表</returns>
    private static List<string> ParseEnumOptions(string enumOptions)
    {
        if (string.IsNullOrEmpty(enumOptions))
            return [];

        // 特殊处理页码格式：识别 "数字,数字,数字..." 这样的模式
        if (IsPageNumberFormatOptions(enumOptions))
        {
            return ParsePageNumberFormatOptions(enumOptions);
        }

        // 默认按逗号分割
        return enumOptions.Split(',').Select(s => s.Trim()).ToList();
    }

    /// <summary>
    /// 判断是否为页码格式选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>是否为页码格式选项</returns>
    private static bool IsPageNumberFormatOptions(string enumOptions)
    {
        // 检查是否包含页码格式的特征模式
        return enumOptions.Contains("1,2,3...") ||
               enumOptions.Contains("a,b,c...") ||
               enumOptions.Contains("A,B,C...") ||
               enumOptions.Contains("i,ii,iii...") ||
               enumOptions.Contains("I,II,III...");
    }

    /// <summary>
    /// 解析页码格式选项
    /// </summary>
    /// <param name="enumOptions">页码格式选项字符串</param>
    /// <returns>解析后的页码格式选项列表</returns>
    private static List<string> ParsePageNumberFormatOptions(string enumOptions)
    {
        List<string> options = [];

        // 定义页码格式模式
        string[] patterns = ["1,2,3...", "a,b,c...", "A,B,C...", "i,ii,iii...", "I,II,III..."];

        string remaining = enumOptions;

        foreach (string pattern in patterns)
        {
            if (remaining.Contains(pattern))
            {
                options.Add(pattern);
                // 从剩余字符串中移除已处理的模式
                remaining = remaining.Replace(pattern, "").Replace(",,", ",");
            }
        }

        // 处理剩余的选项（如果有的话）
        if (!string.IsNullOrEmpty(remaining))
        {
            string[] remainingOptions = remaining.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string option in remainingOptions)
            {
                string trimmed = option.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !options.Contains(trimmed))
                {
                    options.Add(trimmed);
                }
            }
        }

        return options;
    }
}
