using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using ExamLab.Services;

namespace ExamLab.Models;

/// <summary>
/// 题目模型
/// </summary>
public class Question : ReactiveObject
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateQuestionId();

    /// <summary>
    /// 题目标题
    /// </summary>
    [Reactive] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Reactive] public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分值（自动计算，根据题目类型使用不同的计算方式）
    /// </summary>
    public decimal TotalScore
    {
        get
        {
            // C#模块：根据题目类型计算分数
            if (CSharpQuestionType == CSharpQuestionType.CodeCompletion)
            {
                // 代码补全类型：使用填空处分数总和
                if (CodeBlanks != null && CodeBlanks.Count > 0)
                {
                    return CodeBlanks.Where(cb => cb.IsEnabled).Sum(cb => cb.Score);
                }
                return 0;
            }
            else if (CSharpQuestionType == CSharpQuestionType.Debugging ||
                     CSharpQuestionType == CSharpQuestionType.Implementation)
            {
                // 调试纠错和编写实现类型：使用直接分数
                return CSharpDirectScore;
            }

            // 其他模块：使用操作点分数总和
            if (OperationPoints == null || OperationPoints.Count == 0)
                return 0;
            return OperationPoints.Where(op => op.IsEnabled).Sum(op => op.Score);
        }
    }

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
    /// 构造函数
    /// </summary>
    public Question()
    {
        // 监听操作点集合变化
        OperationPoints.CollectionChanged += OnOperationPointsCollectionChanged;

        // 为现有的操作点添加监听（如果有的话）
        foreach (OperationPoint operationPoint in OperationPoints)
        {
            operationPoint.PropertyChanged += OnOperationPointPropertyChanged;
        }

        // 监听代码填空处集合变化
        CodeBlanks.CollectionChanged += OnCodeBlanksCollectionChanged;

        // 为现有的代码填空处添加监听（如果有的话）
        foreach (CodeBlank codeBlank in CodeBlanks)
        {
            codeBlank.PropertyChanged += OnCodeBlankPropertyChanged;
        }

        // 监听C#题目类型和直接分数变化
        this.PropertyChanged += OnQuestionPropertyChanged;
    }

    /// <summary>
    /// 操作点集合变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnOperationPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 通知总分变化
        this.RaisePropertyChanged(nameof(TotalScore));

        // 为新添加的操作点添加属性变化监听
        if (e.NewItems != null)
        {
            foreach (OperationPoint operationPoint in e.NewItems.Cast<OperationPoint>())
            {
                operationPoint.PropertyChanged += OnOperationPointPropertyChanged;
            }
        }

        // 移除已删除操作点的监听
        if (e.OldItems != null)
        {
            foreach (OperationPoint operationPoint in e.OldItems.Cast<OperationPoint>())
            {
                operationPoint.PropertyChanged -= OnOperationPointPropertyChanged;
            }
        }
    }

    /// <summary>
    /// 操作点属性变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnOperationPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationPoint.Score) || e.PropertyName == nameof(OperationPoint.IsEnabled))
        {
            this.RaisePropertyChanged(nameof(TotalScore));
        }
    }

    /// <summary>
    /// 代码填空处集合变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnCodeBlanksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 通知总分变化
        this.RaisePropertyChanged(nameof(TotalScore));

        // 为新添加的代码填空处添加属性变化监听
        if (e.NewItems != null)
        {
            foreach (CodeBlank codeBlank in e.NewItems.Cast<CodeBlank>())
            {
                codeBlank.PropertyChanged += OnCodeBlankPropertyChanged;
            }
        }

        // 移除已删除代码填空处的监听
        if (e.OldItems != null)
        {
            foreach (CodeBlank codeBlank in e.OldItems.Cast<CodeBlank>())
            {
                codeBlank.PropertyChanged -= OnCodeBlankPropertyChanged;
            }
        }
    }

    /// <summary>
    /// 代码填空处属性变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnCodeBlankPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeBlank.Score) || e.PropertyName == nameof(CodeBlank.IsEnabled))
        {
            this.RaisePropertyChanged(nameof(TotalScore));
        }
    }

    /// <summary>
    /// 题目属性变化事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnQuestionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CSharpQuestionType) || e.PropertyName == nameof(CSharpDirectScore))
        {
            this.RaisePropertyChanged(nameof(TotalScore));
        }
    }

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
    /// C#题目类型（仅C#模块使用）
    /// </summary>
    [Reactive] public CSharpQuestionType CSharpQuestionType { get; set; } = CSharpQuestionType.CodeCompletion;

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    [Reactive] public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#题目直接分数（仅调试纠错和编写实现类型使用）
    /// </summary>
    [Reactive] public decimal CSharpDirectScore { get; set; } = 10.0m;

    /// <summary>
    /// 代码补全填空处集合（仅C#模块代码补全类型使用）
    /// </summary>
    public ObservableCollection<CodeBlank> CodeBlanks { get; set; } = new();
}
