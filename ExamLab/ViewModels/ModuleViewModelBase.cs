using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

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
    /// 复制题目命令
    /// </summary>
    public ReactiveCommand<Question, Unit> CopyQuestionCommand { get; }

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
        DeleteQuestionCommand = ReactiveCommand.CreateFromTask<Question>(DeleteQuestionAsync);
        CopyQuestionCommand = ReactiveCommand.Create<Question>(CopyQuestion);
        AddOperationPointCommand = ReactiveCommand.Create(AddOperationPoint);
        DeleteOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(DeleteOperationPointAsync);
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

    /// <summary>
    /// 异步删除题目（带确认对话框）
    /// </summary>
    protected virtual async Task DeleteQuestionAsync(Question question)
    {
        if (question == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "确认删除",
            $"确定要删除题目\"{question.Title}\"吗？此操作不可撤销。");

        if (!confirmed)
        {
            return;
        }

        Module.Questions.Remove(question);

        if (SelectedQuestion == question)
        {
            SelectedQuestion = Module.Questions.Count > 0 ? Module.Questions[0] : null;
        }

        // 重新排序
        for (int i = 0; i < Module.Questions.Count; i++)
        {
            Module.Questions[i].Order = i + 1;
        }
    }

    protected virtual void CopyQuestion(Question question)
    {
        if (question == null) return;

        Question copiedQuestion = new()
        {
            Title = $"{question.Title} - 副本",
            Content = question.Content,

            Order = Module.Questions.Count + 1,
            IsEnabled = question.IsEnabled,
            CreatedTime = question.CreatedTime,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            CSharpQuestionType = question.CSharpQuestionType
        };

        // 复制所有操作点
        foreach (OperationPoint operationPoint in question.OperationPoints)
        {
            OperationPoint copiedOperationPoint = new()
            {
                Name = operationPoint.Name,
                Description = operationPoint.Description,
                ModuleType = operationPoint.ModuleType,
                WindowsOperationType = operationPoint.WindowsOperationType,
                PowerPointKnowledgeType = operationPoint.PowerPointKnowledgeType,
                WordKnowledgeType = operationPoint.WordKnowledgeType,
                ExcelKnowledgeType = operationPoint.ExcelKnowledgeType,
                Score = operationPoint.Score,
                ScoringQuestionId = operationPoint.ScoringQuestionId,
                IsEnabled = operationPoint.IsEnabled,
                Order = operationPoint.Order,
                CreatedTime = operationPoint.CreatedTime
            };

            // 复制所有配置参数
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                ConfigurationParameter copiedParameter = new()
                {
                    Name = parameter.Name,
                    DisplayName = parameter.DisplayName,
                    Description = parameter.Description,
                    Type = parameter.Type,
                    Value = parameter.Value,
                    DefaultValue = parameter.DefaultValue,
                    IsRequired = parameter.IsRequired,
                    Order = parameter.Order,
                    EnumOptions = parameter.EnumOptions,
                    ValidationRule = parameter.ValidationRule,
                    ValidationErrorMessage = parameter.ValidationErrorMessage,
                    MinValue = parameter.MinValue,
                    MaxValue = parameter.MaxValue,
                    IsEnabled = parameter.IsEnabled
                };

                copiedOperationPoint.Parameters.Add(copiedParameter);
            }

            copiedQuestion.OperationPoints.Add(copiedOperationPoint);
        }

        // 复制所有填空处
        foreach (CodeBlank codeBlank in question.CodeBlanks)
        {
            CodeBlank copiedCodeBlank = new()
            {
                Description = codeBlank.Description,
                DetailedDescription = codeBlank.DetailedDescription,
                Order = codeBlank.Order,
                IsEnabled = codeBlank.IsEnabled,
                Score = codeBlank.Score,
                CreatedTime = codeBlank.CreatedTime
            };

            copiedQuestion.CodeBlanks.Add(copiedCodeBlank);
        }

        Module.Questions.Add(copiedQuestion);
        SelectedQuestion = copiedQuestion;
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

    /// <summary>
    /// 异步删除操作点（带确认对话框）
    /// </summary>
    protected virtual async Task DeleteOperationPointAsync(OperationPoint operationPoint)
    {
        if (operationPoint == null || SelectedQuestion == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "确认删除",
            $"确定要删除操作点\"{operationPoint.Name}\"吗？此操作不可撤销。");

        if (!confirmed)
        {
            return;
        }

        SelectedQuestion.OperationPoints.Remove(operationPoint);

        if (SelectedOperationPoint == operationPoint)
        {
            SelectedOperationPoint = SelectedQuestion.OperationPoints.Count > 0 ? SelectedQuestion.OperationPoints[0] : null;
        }

        // 重新排序
        for (int i = 0; i < SelectedQuestion.OperationPoints.Count; i++)
        {
            SelectedQuestion.OperationPoints[i].Order = i + 1;
        }
    }
}
