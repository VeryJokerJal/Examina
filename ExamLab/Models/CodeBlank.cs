using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 代码填空处模型
/// </summary>
public class CodeBlank : ReactiveObject
{
    /// <summary>
    /// 填空处ID
    /// </summary>
    [Reactive] public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 填空处名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 填空处描述/提示文本
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 填空处的详细说明
    /// </summary>
    [Reactive] public string DetailedDescription { get; set; } = string.Empty;

    /// <summary>
    /// 填空处的排序
    /// </summary>
    [Reactive] public int Order { get; set; }

    /// <summary>
    /// 是否启用该填空处
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 填空处分数（仅代码补全类型使用）
    /// </summary>
    [Reactive] public double Score { get; set; } = 5.0;

    /// <summary>
    /// 标准答案
    /// </summary>
    [Reactive] public string? StandardAnswer { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
