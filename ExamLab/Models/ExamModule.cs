using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    Excel = 1,
    Word = 2,
    PowerPoint = 3,
    CSharp = 4,
    Windows = 5
}

/// <summary>
/// 试卷模块模型
/// </summary>
public class ExamModule : ReactiveObject
{
    public ExamModule()
    {
        // 使用ReactiveUI的方式监听Questions集合变化
        Questions.CollectionChanged += OnQuestionsCollectionChanged;

        // 为已存在的题目设置监听（如果有的话）
        SetupQuestionListeners();
    }

    /// <summary>
    /// 题目集合变化事件处理
    /// </summary>
    private void OnQuestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RaisePropertyChanged(nameof(QuestionCount));
        this.RaisePropertyChanged(nameof(TotalScore));
        this.RaisePropertyChanged(nameof(OperationPointCount));

        // 监听新添加的题目的属性变化
        if (e.NewItems != null)
        {
            foreach (Question question in e.NewItems.Cast<Question>())
            {
                SetupQuestionListener(question);
            }
        }

        // 移除已删除题目的监听
        if (e.OldItems != null)
        {
            foreach (Question question in e.OldItems.Cast<Question>())
            {
                RemoveQuestionListener(question);
            }
        }
    }

    /// <summary>
    /// 为所有现有题目设置监听
    /// </summary>
    private void SetupQuestionListeners()
    {
        foreach (Question question in Questions)
        {
            SetupQuestionListener(question);
        }
    }

    /// <summary>
    /// 为单个题目设置监听
    /// </summary>
    private void SetupQuestionListener(Question question)
    {
        // 监听题目总分变化
        question.PropertyChanged += OnQuestionPropertyChanged;

        // 监听题目的操作点集合变化
        question.OperationPoints.CollectionChanged += OnQuestionOperationPointsChanged;
    }

    /// <summary>
    /// 移除单个题目的监听
    /// </summary>
    private void RemoveQuestionListener(Question question)
    {
        question.PropertyChanged -= OnQuestionPropertyChanged;
        question.OperationPoints.CollectionChanged -= OnQuestionOperationPointsChanged;
    }

    /// <summary>
    /// 题目属性变化事件处理
    /// </summary>
    private void OnQuestionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Question.TotalScore))
        {
            this.RaisePropertyChanged(nameof(TotalScore));
        }
    }

    /// <summary>
    /// 题目操作点集合变化事件处理
    /// </summary>
    private void OnQuestionOperationPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RaisePropertyChanged(nameof(OperationPointCount));
    }

    /// <summary>
    /// 重新初始化事件监听（用于反序列化后）
    /// </summary>
    public void ReinitializeEventListeners()
    {
        // 清除可能存在的旧监听
        Questions.CollectionChanged -= OnQuestionsCollectionChanged;

        // 重新设置监听
        Questions.CollectionChanged += OnQuestionsCollectionChanged;

        // 为所有现有题目设置监听
        SetupQuestionListeners();

        // 为每个题目重新初始化事件监听
        foreach (Question question in Questions)
        {
            question.ReinitializeEventListeners();
        }
    }
    /// <summary>
    /// 模块ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateModuleId();

    /// <summary>
    /// 模块名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [Reactive] public ModuleType Type { get; set; } = ModuleType.Windows;

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
    public ObservableCollection<Question> Questions { get; } = [];

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
    public double TotalScore => Questions.Sum(q => q.TotalScore);

    /// <summary>
    /// 操作点数量
    /// </summary>
    public int OperationPointCount => Questions.Sum(q => q.OperationPoints.Count);
}
