using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System;
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
    public ExamModule()
    {
        // 使用ReactiveUI的方式监听Questions集合变化
        Questions.CollectionChanged += (sender, e) =>
        {
            this.RaisePropertyChanged(nameof(QuestionCount));
            this.RaisePropertyChanged(nameof(TotalScore));
            this.RaisePropertyChanged(nameof(OperationPointCount));

            // 监听新添加的题目的属性变化
            if (e.NewItems != null)
            {
                foreach (Question question in e.NewItems.Cast<Question>())
                {
                    // 监听题目分值变化
                    question.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(Question.Score))
                        {
                            this.RaisePropertyChanged(nameof(TotalScore));
                        }
                    };

                    // 监听题目的操作点集合变化
                    question.OperationPoints.CollectionChanged += (s, args) =>
                    {
                        this.RaisePropertyChanged(nameof(OperationPointCount));
                    };
                }
            }

            // 监听移除的题目，清理订阅（防止内存泄漏）
            if (e.OldItems != null)
            {
                // 这里可以添加清理逻辑，但由于使用了ReactiveUI的Subscribe，
                // 当对象被垃圾回收时会自动清理
            }
        };
    }
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

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount => Questions.Count;

    /// <summary>
    /// 总分
    /// </summary>
    public double TotalScore => Questions.Sum(q => q.Score);

    /// <summary>
    /// 操作点数量
    /// </summary>
    public int OperationPointCount => Questions.Sum(q => q.OperationPoints.Count);
}
