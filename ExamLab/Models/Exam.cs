using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ExamLab.Services;

namespace ExamLab.Models;

/// <summary>
/// 试卷模型 - 用于考试试卷（包含多个模块的综合试卷）
/// </summary>
public class Exam : ReactiveObject
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateExamId();

    /// <summary>
    /// 试卷名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = "2025-08-10";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Reactive] public string LastModifiedTime { get; set; } = "2025-08-10";

    /// <summary>
    /// 试卷包含的模块
    /// </summary>
    public ObservableCollection<ExamModule> Modules { get; set; } = [];

    /// <summary>
    /// 试卷总分（基于所有模块中题目的分数总和自动计算）
    /// </summary>
    public double TotalScore => Modules.Sum(m => m.TotalScore);

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Reactive] public int Duration { get; set; } = 120;

    /// <summary>
    /// 获取题目总数
    /// </summary>
    public int TotalQuestionCount => Modules.Sum(m => m.Questions.Count);

    /// <summary>
    /// 获取操作点总数
    /// </summary>
    public int TotalOperationPointCount => Modules.Sum(m => m.Questions.Sum(q => q.OperationPoints.Count));

    /// <summary>
    /// 构造函数
    /// </summary>
    public Exam()
    {
        // 监听模块集合变化
        Modules.CollectionChanged += OnModulesCollectionChanged;

        // 为现有的模块添加监听（如果有的话）
        foreach (ExamModule module in Modules)
        {
            module.PropertyChanged += OnModulePropertyChanged;
        }
    }

    /// <summary>
    /// 模块集合变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnModulesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 通知总分变化
        this.RaisePropertyChanged(nameof(TotalScore));
        this.RaisePropertyChanged(nameof(TotalQuestionCount));
        this.RaisePropertyChanged(nameof(TotalOperationPointCount));

        // 为新添加的模块添加属性变化监听
        if (e.NewItems != null)
        {
            foreach (ExamModule module in e.NewItems.Cast<ExamModule>())
            {
                module.PropertyChanged += OnModulePropertyChanged;
            }
        }

        // 移除已删除模块的监听
        if (e.OldItems != null)
        {
            foreach (ExamModule module in e.OldItems.Cast<ExamModule>())
            {
                module.PropertyChanged -= OnModulePropertyChanged;
            }
        }
    }

    /// <summary>
    /// 模块属性变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnModulePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExamModule.TotalScore))
        {
            this.RaisePropertyChanged(nameof(TotalScore));
        }
        else if (e.PropertyName == nameof(ExamModule.QuestionCount))
        {
            this.RaisePropertyChanged(nameof(TotalQuestionCount));
        }
        else if (e.PropertyName == nameof(ExamModule.OperationPointCount))
        {
            this.RaisePropertyChanged(nameof(TotalOperationPointCount));
        }
    }
}
