using System;
using System.Reactive;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 题目配置视图模型
/// </summary>
public class QuestionConfigurationViewModel : ViewModelBase
{
    /// <summary>
    /// 当前编辑的题目
    /// </summary>
    [Reactive] public Question Question { get; set; }

    /// <summary>
    /// 所属模块
    /// </summary>
    [Reactive] public ExamModule Module { get; set; }

    /// <summary>
    /// 当前选中的操作点
    /// </summary>
    [Reactive] public OperationPoint? SelectedOperationPoint { get; set; }

    /// <summary>
    /// 是否为C#模块
    /// </summary>
    public bool IsCSharpModule => Module?.Type == ModuleType.CSharp;

    /// <summary>
    /// 是否为直接分数类型（调试纠错和编写实现）
    /// </summary>
    public bool IsDirectScoreType => Question?.CSharpQuestionType == CSharpQuestionType.Debugging ||
                                     Question?.CSharpQuestionType == CSharpQuestionType.Implementation;

    /// <summary>
    /// 是否为新题目
    /// </summary>
    [Reactive] public bool IsNewQuestion { get; set; }

    /// <summary>
    /// 添加操作点命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddOperationPointCommand { get; }

    /// <summary>
    /// 编辑操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> EditOperationPointCommand { get; }

    /// <summary>
    /// 配置操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> ConfigureOperationPointCommand { get; }

    /// <summary>
    /// 删除操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> DeleteOperationPointCommand { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="module">所属模块</param>
    /// <param name="isNewQuestion">是否为新题目</param>
    public QuestionConfigurationViewModel(Question question, ExamModule module, bool isNewQuestion = false)
    {
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        IsNewQuestion = isNewQuestion;

        Title = isNewQuestion ? "添加题目" : "编辑题目";

        // 初始化命令
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        EditOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(EditOperationPointAsync);
        ConfigureOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(ConfigureOperationPointAsync);
        DeleteOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(DeleteOperationPointAsync);

        // 监听C#题目类型变化，更新IsDirectScoreType
        this.WhenAnyValue(x => x.Question.CSharpQuestionType)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsDirectScoreType)));
    }

    /// <summary>
    /// 保存题目
    /// </summary>
    public async Task SaveQuestionAsync()
    {
        try
        {
            // 更新题目的创建时间（如果是新题目）
            if (IsNewQuestion)
            {
                Question.CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 设置题目排序
                if (Question.Order <= 0)
                {
                    Question.Order = Module.Questions.Count + 1;
                }
                
                // 添加到模块
                Module.Questions.Add(Question);
            }

            await NotificationService.ShowSuccessAsync("保存成功", $"题目"{Question.Title}"已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存题目时发生错误：{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 添加操作点
    /// </summary>
    private async Task AddOperationPointAsync()
    {
        try
        {
            string? operationPointName = await NotificationService.ShowInputDialogAsync(
                "添加操作点",
                "请输入操作点名称",
                "新操作点");

            if (string.IsNullOrWhiteSpace(operationPointName))
            {
                return;
            }

            OperationPoint newOperationPoint = new()
            {
                Name = operationPointName,
                Description = "请输入操作点描述",
                ModuleType = Module.Type,
                Score = 1,
                Order = Question.OperationPoints.Count + 1,
                IsEnabled = true,
                CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Question.OperationPoints.Add(newOperationPoint);
            SelectedOperationPoint = newOperationPoint;

            await NotificationService.ShowSuccessAsync("添加成功", $"已添加操作点：{operationPointName}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("添加失败", $"添加操作点时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 编辑操作点
    /// </summary>
    private async Task EditOperationPointAsync(OperationPoint operationPoint)
    {
        if (operationPoint == null) return;

        try
        {
            // 创建操作点编辑对话框
            OperationPointEditViewModel editViewModel = new(operationPoint, Module.Type);
            OperationPointEditDialog editDialog = new(editViewModel);

            var result = await editDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                await NotificationService.ShowSuccessAsync("编辑成功", $"操作点"{operationPoint.Name}"已更新");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("编辑失败", $"编辑操作点时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 配置操作点
    /// </summary>
    private async Task ConfigureOperationPointAsync(OperationPoint operationPoint)
    {
        if (operationPoint == null) return;

        try
        {
            // 创建操作点配置对话框
            OperationPointConfigurationViewModel configViewModel = new(operationPoint);
            OperationPointConfigurationDialog configDialog = new(configViewModel);

            var result = await configDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                await NotificationService.ShowSuccessAsync("配置成功", $"操作点"{operationPoint.Name}"配置已更新");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("配置失败", $"配置操作点时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 删除操作点
    /// </summary>
    private async Task DeleteOperationPointAsync(OperationPoint operationPoint)
    {
        if (operationPoint == null) return;

        try
        {
            bool confirmed = await NotificationService.ShowConfirmationAsync(
                "确认删除",
                $"确定要删除操作点"{operationPoint.Name}"吗？此操作不可撤销。");

            if (!confirmed) return;

            Question.OperationPoints.Remove(operationPoint);

            if (SelectedOperationPoint == operationPoint)
            {
                SelectedOperationPoint = null;
            }

            // 重新排序
            for (int i = 0; i < Question.OperationPoints.Count; i++)
            {
                Question.OperationPoints[i].Order = i + 1;
            }

            await NotificationService.ShowSuccessAsync("删除成功", $"操作点"{operationPoint.Name}"已删除");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", $"删除操作点时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        await NotificationService.ShowErrorAsync(title, message);
    }
}
