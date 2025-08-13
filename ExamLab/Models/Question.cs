using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;

namespace ExamLab.Models;

/// <summary>
/// 题目模型
/// </summary>
public class Question : ReactiveObject
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Reactive] public string Id { get; set; } = GenerateQuestionId();

    /// <summary>
    /// 题目标题
    /// </summary>
    [Reactive] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Reactive] public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Reactive] public int Score { get; set; }

    /// <summary>
    /// 题目排序
    /// </summary>
    [Reactive] public int Order { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    public ObservableCollection<OperationPoint> OperationPoints { get; set; } = new();

    /// <summary>
    /// 是否启用该题目
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = "2025-08-10";

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    [Reactive] public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    [Reactive] public string? ExpectedOutput { get; set; }

    /// <summary>
    /// 生成新的题目ID
    /// </summary>
    private static string GenerateQuestionId()
    {
        return $"question-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
