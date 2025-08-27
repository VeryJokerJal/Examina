using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 题目管理ViewModel
/// </summary>
public class QuestionManagementViewModel : ViewModelBase
{
    /// <summary>
    /// 当前选中的模块
    /// </summary>
    [Reactive] public ExamModule? SelectedModule { get; set; }

    /// <summary>
    /// 当前选中的题目
    /// </summary>
    [Reactive] public Question? SelectedQuestion { get; set; }

    /// <summary>
    /// 搜索文本
    /// </summary>
    [Reactive] public string SearchText { get; set; } = "";

    /// <summary>
    /// 筛选类型
    /// </summary>
    [Reactive] public string FilterType { get; set; } = "全部";

    /// <summary>
    /// 筛选后的题目列表
    /// </summary>
    [Reactive] public ObservableCollection<Question> FilteredQuestions { get; set; } = [];

    /// <summary>
    /// 总题目数
    /// </summary>
    [Reactive] public int TotalQuestions { get; set; }

    /// <summary>
    /// 描述题目数
    /// </summary>
    [Reactive] public int DescriptionQuestions { get; set; }

    /// <summary>
    /// 评分题目数
    /// </summary>
    [Reactive] public int ScoringQuestions { get; set; }

    /// <summary>
    /// 选中的题目是否为评分类型
    /// </summary>
    [Reactive] public bool IsSelectedQuestionScoringType { get; set; }

    /// <summary>
    /// 添加题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddQuestionCommand { get; }

    /// <summary>
    /// 删除题目命令
    /// </summary>
    public ReactiveCommand<Question, Unit> DeleteQuestionCommand { get; }

    /// <summary>
    /// 上移题目命令
    /// </summary>
    public ReactiveCommand<Question, Unit> MoveQuestionUpCommand { get; }

    /// <summary>
    /// 下移题目命令
    /// </summary>
    public ReactiveCommand<Question, Unit> MoveQuestionDownCommand { get; }

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

    /// <summary>
    /// 配置操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> ConfigureOperationPointCommand { get; }

    /// <summary>
    /// 保存题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveQuestionCommand { get; }

    /// <summary>
    /// 重置题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetQuestionCommand { get; }

    /// <summary>
    /// 导出题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportQuestionCommand { get; }

    /// <summary>
    /// 进入模块配置命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> EnterModuleConfigCommand { get; }

    /// <summary>
    /// 批量导入命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> BatchImportCommand { get; }

    public QuestionManagementViewModel()
    {
        Title = "题目管理";

        // 初始化命令
        AddQuestionCommand = ReactiveCommand.CreateFromTask(AddQuestionAsync);
        DeleteQuestionCommand = ReactiveCommand.CreateFromTask<Question>(DeleteQuestionAsync);
        MoveQuestionUpCommand = ReactiveCommand.Create<Question>(MoveQuestionUp);
        MoveQuestionDownCommand = ReactiveCommand.Create<Question>(MoveQuestionDown);
        CopyQuestionCommand = ReactiveCommand.CreateFromTask<Question>(CopyQuestionAsync);
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        DeleteOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(DeleteOperationPointAsync);
        ConfigureOperationPointCommand = ReactiveCommand.Create<OperationPoint>(ConfigureOperationPoint);
        SaveQuestionCommand = ReactiveCommand.CreateFromTask(SaveQuestionAsync);
        ResetQuestionCommand = ReactiveCommand.CreateFromTask(ResetQuestionAsync);
        ExportQuestionCommand = ReactiveCommand.CreateFromTask(ExportQuestionAsync);
        EnterModuleConfigCommand = ReactiveCommand.Create(EnterModuleConfig);
        BatchImportCommand = ReactiveCommand.CreateFromTask(BatchImportAsync);

        // 监听搜索和筛选变化
        _ = this.WhenAnyValue(x => x.SearchText, x => x.FilterType, x => x.SelectedModule)
            .Subscribe(_ => UpdateFilteredQuestions());

        // 监听选中题目变化
        _ = this.WhenAnyValue(x => x.SelectedQuestion)
            .Subscribe(question =>
            {
                IsSelectedQuestionScoringType = question != null;
            });

        // 监听模块变化
        _ = this.WhenAnyValue(x => x.SelectedModule)
            .Where(module => module != null)
            .Subscribe(module =>
            {
                UpdateStatistics();
                UpdateFilteredQuestions();
            });
    }

    private async Task AddQuestionAsync()
    {
        if (SelectedModule == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个模块");
            return;
        }

        string? questionType = await NotificationService.ShowSelectionDialogAsync(
            "选择题目类型",
            ["Description", "ScoringDescription"]);

        if (questionType == null)
        {
            return;
        }

        string? questionTitle = await NotificationService.ShowInputDialogAsync(
            "输入题目标题",
            "请输入题目标题",
            "新题目");

        if (string.IsNullOrWhiteSpace(questionTitle))
        {
            return;
        }

        Question newQuestion = new()
        {
            Title = questionTitle,
            Content = "请输入题目解析",
            Order = SelectedModule.Questions.Count + 1,
            IsEnabled = true
        };

        SelectedModule.Questions.Add(newQuestion);
        SelectedQuestion = newQuestion;
        UpdateStatistics();
        UpdateFilteredQuestions();
    }

    private async Task DeleteQuestionAsync(Question question)
    {
        if (question == null || SelectedModule == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowDeleteConfirmationAsync(question.Title);
        if (!confirmed)
        {
            return;
        }

        _ = SelectedModule.Questions.Remove(question);
        ReorderQuestions();

        if (SelectedQuestion == question)
        {
            SelectedQuestion = SelectedModule.Questions.FirstOrDefault();
        }

        UpdateStatistics();
        UpdateFilteredQuestions();
    }

    private void MoveQuestionUp(Question question)
    {
        if (question == null || SelectedModule == null)
        {
            return;
        }

        int currentIndex = SelectedModule.Questions.IndexOf(question);
        if (currentIndex > 0)
        {
            SelectedModule.Questions.Move(currentIndex, currentIndex - 1);
            ReorderQuestions();
            UpdateFilteredQuestions();
        }
    }

    private void MoveQuestionDown(Question question)
    {
        if (question == null || SelectedModule == null)
        {
            return;
        }

        int currentIndex = SelectedModule.Questions.IndexOf(question);
        if (currentIndex < SelectedModule.Questions.Count - 1)
        {
            SelectedModule.Questions.Move(currentIndex, currentIndex + 1);
            ReorderQuestions();
            UpdateFilteredQuestions();
        }
    }

    private async Task CopyQuestionAsync(Question question)
    {
        if (question == null || SelectedModule == null)
        {
            return;
        }

        string? newTitle = await NotificationService.ShowInputDialogAsync(
            "复制题目",
            "请输入新题目标题",
            $"{question.Title} - 副本");

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            return;
        }

        Question copiedQuestion = new()
        {
            Title = newTitle,
            Content = question.Content,
            Order = SelectedModule.Questions.Count + 1,
            IsEnabled = question.IsEnabled
        };

        // 复制操作点
        foreach (OperationPoint op in question.OperationPoints)
        {
            OperationPoint copiedOp = new()
            {
                Name = op.Name,
                Description = op.Description,
                ModuleType = op.ModuleType,
                WindowsOperationType = op.WindowsOperationType,
                PowerPointKnowledgeType = op.PowerPointKnowledgeType,
                Score = op.Score,
                Order = op.Order,
                IsEnabled = op.IsEnabled
            };

            foreach (ConfigurationParameter param in op.Parameters)
            {
                ConfigurationParameter copiedParam = new()
                {
                    Name = param.Name,
                    DisplayName = param.DisplayName,
                    Description = param.Description,
                    Type = param.Type,
                    Value = param.Value,
                    DefaultValue = param.DefaultValue,
                    IsRequired = param.IsRequired,
                    Order = param.Order,
                    EnumOptions = param.EnumOptions,
                    ValidationRule = param.ValidationRule,
                    ValidationErrorMessage = param.ValidationErrorMessage,
                    MinValue = param.MinValue,
                    MaxValue = param.MaxValue,
                    IsEnabled = param.IsEnabled
                };
                copiedOp.Parameters.Add(copiedParam);
            }
            copiedQuestion.OperationPoints.Add(copiedOp);
        }

        SelectedModule.Questions.Add(copiedQuestion);
        SelectedQuestion = copiedQuestion;
        UpdateStatistics();
        UpdateFilteredQuestions();
    }

    private async Task AddOperationPointAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个题目");
            return;
        }



        string? operationName = await NotificationService.ShowInputDialogAsync(
            "添加操作点",
            "请输入操作点名称",
            "新操作点");

        if (string.IsNullOrWhiteSpace(operationName))
        {
            return;
        }

        OperationPoint newOperationPoint = new()
        {
            Name = operationName,
            Description = "请输入操作点描述",
            ModuleType = SelectedModule?.Type ?? ModuleType.Windows,
            Score = 5.0m,
            Order = SelectedQuestion.OperationPoints.Count + 1,
            IsEnabled = true
        };

        SelectedQuestion.OperationPoints.Add(newOperationPoint);
    }

    private async Task DeleteOperationPointAsync(OperationPoint operationPoint)
    {
        if (operationPoint == null || SelectedQuestion == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowDeleteConfirmationAsync(operationPoint.Name);
        if (!confirmed)
        {
            return;
        }

        _ = SelectedQuestion.OperationPoints.Remove(operationPoint);
        ReorderOperationPoints();
    }

    private void ConfigureOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
        {
            return;
        }

        // 这里可以触发导航到操作点配置界面
        _ = NotificationService.ShowSuccessAsync("配置", $"即将进入 {operationPoint.Name} 操作点配置界面");
    }

    private async Task SaveQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有可保存的题目");
            return;
        }

        try
        {
            ValidationResult result = ValidationService.ValidateQuestion(SelectedQuestion);
            if (!result.IsValid)
            {
                await NotificationService.ShowValidationErrorsAsync(result);
                return;
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", ex.Message);
        }
    }

    private async Task ResetQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "重置题目",
            $"确定要重置题目 '{SelectedQuestion.Title}' 吗？这将清除所有操作点配置。");

        if (!confirmed)
        {
            return;
        }

        SelectedQuestion.Content = "请输入题目解析";
        SelectedQuestion.OperationPoints.Clear();
    }

    private async Task ExportQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            return;
        }

        try
        {
            // 这里应该导出题目数据
            await NotificationService.ShowSuccessAsync("导出成功", $"题目 {SelectedQuestion.Title} 已导出");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", ex.Message);
        }
    }

    private void EnterModuleConfig()
    {
        if (SelectedModule == null)
        {
            return;
        }

        // 这里可以触发导航到模块配置界面
        _ = NotificationService.ShowSuccessAsync("导航", $"即将进入 {SelectedModule.Name} 模块配置界面");
    }

    private async Task BatchImportAsync()
    {
        try
        {
            // 这里应该实现批量导入功能
            await NotificationService.ShowSuccessAsync("批量导入", "批量导入功能将在后续版本中实现");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", ex.Message);
        }
    }

    private void UpdateFilteredQuestions()
    {
        if (SelectedModule == null)
        {
            FilteredQuestions.Clear();
            return;
        }

        IEnumerable<Question> filtered = SelectedModule.Questions;

        // 移除了按类型筛选功能

        // 按搜索文本筛选
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string searchLower = SearchText.ToLower();
            filtered = filtered.Where(q =>
                q.Title.ToLower().Contains(searchLower) ||
                q.Content.ToLower().Contains(searchLower));
        }

        FilteredQuestions.Clear();
        foreach (Question question in filtered.OrderBy(q => q.Order))
        {
            FilteredQuestions.Add(question);
        }
    }

    private void UpdateStatistics()
    {
        if (SelectedModule == null)
        {
            TotalQuestions = 0;
            DescriptionQuestions = 0;
            ScoringQuestions = 0;
            return;
        }

        TotalQuestions = SelectedModule.Questions.Count;
        DescriptionQuestions = SelectedModule.Questions.Count;
        ScoringQuestions = SelectedModule.Questions.Count;
    }

    private void ReorderQuestions()
    {
        if (SelectedModule == null)
        {
            return;
        }

        for (int i = 0; i < SelectedModule.Questions.Count; i++)
        {
            SelectedModule.Questions[i].Order = i + 1;
        }
    }

    private void ReorderOperationPoints()
    {
        if (SelectedQuestion == null)
        {
            return;
        }

        for (int i = 0; i < SelectedQuestion.OperationPoints.Count; i++)
        {
            SelectedQuestion.OperationPoints[i].Order = i + 1;
        }
    }
}
