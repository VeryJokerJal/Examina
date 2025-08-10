using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ExamLab.Models;
using System.Collections.ObjectModel;
using System.Reactive;

namespace ExamLab.ViewModels;

/// <summary>
/// 模块ViewModel基类
/// </summary>
public abstract class ModuleViewModelBase : ViewModelBase
{
    /// <summary>
    /// 关联的模块
    /// </summary>
    [Reactive] public ExamModule Module { get; set; }

    /// <summary>
    /// 当前选中的题目
    /// </summary>
    [Reactive] public Question? SelectedQuestion { get; set; }

    /// <summary>
    /// 当前选中的操作点
    /// </summary>
    [Reactive] public OperationPoint? SelectedOperationPoint { get; set; }

    /// <summary>
    /// 添加题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddQuestionCommand { get; }

    /// <summary>
    /// 删除题目命令
    /// </summary>
    public ReactiveCommand<Question, Unit> DeleteQuestionCommand { get; }

    /// <summary>
    /// 添加操作点命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddOperationPointCommand { get; }

    /// <summary>
    /// 删除操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> DeleteOperationPointCommand { get; }

    protected ModuleViewModelBase(ExamModule module)
    {
        Module = module;
        Title = module.Name;

        // 初始化命令
        AddQuestionCommand = ReactiveCommand.Create(AddQuestion);
        DeleteQuestionCommand = ReactiveCommand.Create<Question>(DeleteQuestion);
        AddOperationPointCommand = ReactiveCommand.Create(AddOperationPoint);
        DeleteOperationPointCommand = ReactiveCommand.Create<OperationPoint>(DeleteOperationPoint);
    }

    protected virtual void AddQuestion()
    {
        Question newQuestion = new()
        {
            Title = "新题目",
            Content = "请输入题目内容",
            Order = Module.Questions.Count + 1
        };

        Module.Questions.Add(newQuestion);
        SelectedQuestion = newQuestion;
    }

    protected virtual void DeleteQuestion(Question question)
    {
        Module.Questions.Remove(question);
        if (SelectedQuestion == question)
        {
            SelectedQuestion = Module.Questions.Count > 0 ? Module.Questions[0] : null;
        }
    }

    protected abstract void AddOperationPoint();

    protected virtual void DeleteOperationPoint(OperationPoint operationPoint)
    {
        SelectedQuestion?.OperationPoints.Remove(operationPoint);
        if (SelectedOperationPoint == operationPoint)
        {
            SelectedOperationPoint = SelectedQuestion?.OperationPoints.Count > 0 ? SelectedQuestion.OperationPoints[0] : null;
        }
    }
}
