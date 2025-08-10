using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    Windows,
    CSharp,
    PowerPoint,
    Excel,
    Word
}

/// <summary>
/// 试卷模块模型
/// </summary>
public class ExamModule : ReactiveObject
{
    /// <summary>
    /// 模块ID
    /// </summary>
    [Reactive] public string Id { get; set; } = "module-1";

    /// <summary>
    /// 模块名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [Reactive] public ModuleType Type { get; set; }

    /// <summary>
    /// 模块描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    [Reactive] public int Score { get; set; }

    /// <summary>
    /// 模块包含的题目
    /// </summary>
    public ObservableCollection<Question> Questions { get; set; } = [];

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 模块排序
    /// </summary>
    [Reactive] public int Order { get; set; }
}
