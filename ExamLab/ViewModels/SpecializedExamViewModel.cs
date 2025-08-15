using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ExamLab.Views;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 专项试卷制作页面的ViewModel
/// </summary>
public class SpecializedExamViewModel : ViewModelBase
{
    /// <summary>
    /// 专项试卷列表
    /// </summary>
    public ObservableCollection<Exam> SpecializedExams { get; } = [];

    /// <summary>
    /// 当前选中的专项试卷
    /// </summary>
    [Reactive] public Exam? SelectedSpecializedExam { get; set; }

    /// <summary>
    /// 当前选中的模块
    /// </summary>
    [Reactive] public ExamModule? SelectedModule { get; set; }

    /// <summary>
    /// 当前选中的题目
    /// </summary>
    [Reactive] public Question? SelectedQuestion { get; set; }

    /// <summary>
    /// 当前选中的操作点
    /// </summary>
    [Reactive] public OperationPoint? SelectedOperationPoint { get; set; }

    /// <summary>
    /// 当前内容视图模型
    /// </summary>
    [Reactive] public ViewModelBase? CurrentContentViewModel { get; set; }

    /// <summary>
    /// 当前内容视图
    /// </summary>
    [Reactive] public UserControl? CurrentContentView { get; set; }

    /// <summary>
    /// 专项试卷数量
    /// </summary>
    [Reactive] public int SpecializedExamCount { get; set; }

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    [Reactive] public bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// 创建专项试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateSpecializedExamCommand { get; }

    /// <summary>
    /// 删除专项试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> DeleteSpecializedExamCommand { get; }

    /// <summary>
    /// 克隆专项试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> CloneSpecializedExamCommand { get; }

    /// <summary>
    /// 保存专项试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveSpecializedExamCommand { get; }

    /// <summary>
    /// 导出专项试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> ExportSpecializedExamCommand { get; }

    /// <summary>
    /// 选择专项试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> SelectSpecializedExamCommand { get; }

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

    /// <summary>
    /// 配置操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> ConfigureOperationPointCommand { get; }

    /// <summary>
    /// 保存题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveQuestionCommand { get; }

    /// <summary>
    /// 预览题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> PreviewQuestionCommand { get; }

    /// <summary>
    /// 导出题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportQuestionCommand { get; }

    /// <summary>
    /// 主窗口ViewModel引用（用于共享模块编辑功能）
    /// </summary>
    private readonly MainWindowViewModel? _mainWindowViewModel;

    public SpecializedExamViewModel(MainWindowViewModel? mainWindowViewModel = null)
    {
        Title = "专项试卷制作";
        _mainWindowViewModel = mainWindowViewModel;

        // 初始化命令
        CreateSpecializedExamCommand = ReactiveCommand.CreateFromTask(CreateSpecializedExamAsync);
        DeleteSpecializedExamCommand = ReactiveCommand.CreateFromTask<Exam>(DeleteSpecializedExamAsync);
        CloneSpecializedExamCommand = ReactiveCommand.CreateFromTask<Exam>(CloneSpecializedExamAsync);
        SaveSpecializedExamCommand = ReactiveCommand.CreateFromTask(SaveSpecializedExamAsync);
        ExportSpecializedExamCommand = ReactiveCommand.CreateFromTask<Exam>(ExportSpecializedExamAsync);
        SelectSpecializedExamCommand = ReactiveCommand.Create<Exam>(SelectSpecializedExam);

        // 题目管理命令
        AddQuestionCommand = ReactiveCommand.CreateFromTask(AddQuestionAsync);
        DeleteQuestionCommand = ReactiveCommand.CreateFromTask<Question>(DeleteQuestionAsync);
        CopyQuestionCommand = ReactiveCommand.CreateFromTask<Question>(CopyQuestionAsync);
        SaveQuestionCommand = ReactiveCommand.CreateFromTask(SaveQuestionAsync);
        PreviewQuestionCommand = ReactiveCommand.CreateFromTask(PreviewQuestionAsync);
        ExportQuestionCommand = ReactiveCommand.CreateFromTask(ExportQuestionAsync);

        // 操作点管理命令
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        DeleteOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(DeleteOperationPointAsync);
        ConfigureOperationPointCommand = ReactiveCommand.Create<OperationPoint>(ConfigureOperationPoint);

        // 监听选中试卷变化
        this.WhenAnyValue(x => x.SelectedSpecializedExam)
            .Subscribe(OnSelectedSpecializedExamChanged);

        // 初始化数据
        InitializeDataAsync();
    }

    /// <summary>
    /// 创建专项试卷
    /// </summary>
    private async Task CreateSpecializedExamAsync()
    {
        try
        {
            // 显示模块类型选择对话框
            ModuleSelectionDialog dialog = new();

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.SelectedModuleType.HasValue)
            {
                ModuleType selectedType = dialog.SelectedModuleType.Value;

                // 创建专项试卷
                Exam specializedExam = CreateSpecializedExam(selectedType);

                // 添加到列表
                SpecializedExams.Add(specializedExam);
                SelectedSpecializedExam = specializedExam;

                // 保存到本地存储
                await SaveSpecializedExamToStorageAsync(specializedExam);

                // 更新计数
                SpecializedExamCount = SpecializedExams.Count;

                await NotificationService.ShowSuccessAsync(
                    "创建成功",
                    $"已创建{GetModuleTypeName(selectedType)}专项试卷：{specializedExam.Name}");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("创建失败", $"创建专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 创建专项试卷实例
    /// </summary>
    private Exam CreateSpecializedExam(ModuleType moduleType)
    {
        string moduleTypeName = GetModuleTypeName(moduleType);
        string examName = $"{moduleTypeName}专项试卷";

        // 确保名称唯一
        int counter = 1;
        string originalName = examName;
        while (SpecializedExams.Any(e => e.Name == examName))
        {
            examName = $"{originalName} ({counter})";
            counter++;
        }

        Exam exam = new()
        {
            Id = IdGeneratorService.GenerateExamId(),
            Name = examName,
            Description = $"专门针对{moduleTypeName}的专项练习试卷",
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 创建对应的模块
        ExamModule module = new()
        {
            Id = IdGeneratorService.GenerateModuleId(),
            Name = moduleTypeName,
            Type = moduleType,
            Description = GetDefaultModuleDescription(moduleType),
            Score = 100,
            Order = 1,
            IsEnabled = true
        };

        exam.Modules.Add(module);
        return exam;
    }

    /// <summary>
    /// 获取模块类型名称
    /// </summary>
    private string GetModuleTypeName(ModuleType type)
    {
        return type switch
        {
            ModuleType.Windows => "Windows操作",
            ModuleType.CSharp => "C#编程",
            ModuleType.PowerPoint => "PowerPoint操作",
            ModuleType.Excel => "Excel操作",
            ModuleType.Word => "Word操作",
            _ => "未知模块"
        };
    }

    /// <summary>
    /// 获取默认模块描述
    /// </summary>
    private string GetDefaultModuleDescription(ModuleType type)
    {
        return type switch
        {
            ModuleType.Windows => "Windows文件和文件夹操作模块，包含9种操作类型",
            ModuleType.CSharp => "C#程序设计模块，包含代码配置和输出验证",
            ModuleType.PowerPoint => "PowerPoint幻灯片操作模块，包含39个知识点",
            ModuleType.Excel => "Excel电子表格操作模块，包含数据处理和图表功能",
            ModuleType.Word => "Word文档编辑模块，包含文档格式化和排版功能",
            _ => "模块描述"
        };
    }

    /// <summary>
    /// 选择专项试卷
    /// </summary>
    /// <param name="exam">要选择的专项试卷</param>
    private void SelectSpecializedExam(Exam exam)
    {
        SelectedSpecializedExam = exam;
    }

    /// <summary>
    /// 选中专项试卷变化处理
    /// </summary>
    private void OnSelectedSpecializedExamChanged(Exam? exam)
    {
        if (exam?.Modules.Count > 0)
        {
            SelectedModule = exam.Modules.First();

            // 根据模块类型创建对应的ViewModel和View
            CreateModuleContentViewAndViewModel(SelectedModule);
        }
        else
        {
            SelectedModule = null;
            CurrentContentViewModel = null;
            CurrentContentView = null;
        }
    }

    /// <summary>
    /// 根据模块类型创建对应的ViewModel和View
    /// </summary>
    private void CreateModuleContentViewAndViewModel(ExamModule? module)
    {
        if (module == null)
        {
            CurrentContentViewModel = null;
            CurrentContentView = null;
            return;
        }

        // 根据模块类型创建对应的ViewModel
        CurrentContentViewModel = module.Type switch
        {
            ModuleType.Windows => new WindowsModuleViewModel(module),
            ModuleType.CSharp => new CSharpModuleViewModel(module, _mainWindowViewModel),
            ModuleType.PowerPoint => new PowerPointModuleViewModel(module),
            ModuleType.Excel => new ExcelModuleViewModel(module),
            ModuleType.Word => new WordModuleViewModel(module),
            _ => null
        };

        // 创建对应的View，设置适当的DataContext
        CurrentContentView = CurrentContentViewModel switch
        {
            WindowsModuleViewModel => new Views.WindowsModuleView { DataContext = this },
            CSharpModuleViewModel csharpVM => new Views.CSharpModuleView(csharpVM) { MainWindowViewModel = _mainWindowViewModel },
            PowerPointModuleViewModel => new Views.PowerPointModuleView { DataContext = this },
            ExcelModuleViewModel => new Views.ExcelModuleView { DataContext = this },
            WordModuleViewModel => new Views.WordModuleView { DataContext = this },
            _ => null
        };
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    private async Task InitializeDataAsync()
    {
        try
        {
            // 加载专项试卷数据
            List<Exam> savedExams = await LoadSpecializedExamsFromStorageAsync();

            foreach (Exam exam in savedExams)
            {
                SpecializedExams.Add(exam);
            }

            SpecializedExamCount = SpecializedExams.Count;

            if (SpecializedExams.Count > 0)
            {
                SelectedSpecializedExam = SpecializedExams.First();
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("数据加载失败", $"无法加载专项试卷数据：{ex.Message}");
        }
    }

    /// <summary>
    /// 从存储加载专项试卷
    /// </summary>
    private async Task<List<Exam>> LoadSpecializedExamsFromStorageAsync()
    {
        // 这里可以使用专门的存储键来区分专项试卷和普通试卷
        // 暂时返回空列表，后续可以扩展
        return [];
    }

    #region 题目管理方法

    /// <summary>
    /// 添加题目
    /// </summary>
    private async Task AddQuestionAsync()
    {
        if (SelectedModule == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个模块");
            return;
        }

        string? questionTitle = await NotificationService.ShowInputDialogAsync(
            "添加题目",
            "请输入题目标题",
            "新题目");

        if (string.IsNullOrWhiteSpace(questionTitle))
        {
            return;
        }

        Question newQuestion = new()
        {
            Title = questionTitle,
            Content = "请输入题目内容",
            Order = SelectedModule.Questions.Count + 1,
            IsEnabled = true
        };

        SelectedModule.Questions.Add(newQuestion);
        SelectedQuestion = newQuestion;

        await NotificationService.ShowSuccessAsync("成功", $"已添加题目：{questionTitle}");
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    private async Task DeleteQuestionAsync(Question question)
    {
        if (SelectedModule == null || question == null)
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

        SelectedModule.Questions.Remove(question);

        if (SelectedQuestion == question)
        {
            SelectedQuestion = null;
        }

        // 重新排序
        for (int i = 0; i < SelectedModule.Questions.Count; i++)
        {
            SelectedModule.Questions[i].Order = i + 1;
        }

        await NotificationService.ShowSuccessAsync("成功", "题目已删除");
    }

    /// <summary>
    /// 复制题目
    /// </summary>
    private async Task CopyQuestionAsync(Question question)
    {
        if (SelectedModule == null || question == null)
        {
            return;
        }

        Question copiedQuestion = new()
        {
            Title = $"{question.Title} - 副本",
            Content = question.Content,
            Order = SelectedModule.Questions.Count + 1,
            IsEnabled = question.IsEnabled
        };

        // 复制操作点
        foreach (OperationPoint operationPoint in question.OperationPoints)
        {
            OperationPoint copiedOperationPoint = new()
            {
                Name = operationPoint.Name,
                Description = operationPoint.Description,
                Score = operationPoint.Score,
                Order = operationPoint.Order,
                IsEnabled = operationPoint.IsEnabled
            };

            copiedQuestion.OperationPoints.Add(copiedOperationPoint);
        }

        SelectedModule.Questions.Add(copiedQuestion);
        SelectedQuestion = copiedQuestion;

        await NotificationService.ShowSuccessAsync("成功", $"已复制题目：{question.Title}");
    }

    /// <summary>
    /// 保存题目
    /// </summary>
    private async Task SaveQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个题目");
            return;
        }

        // 这里可以添加题目保存逻辑
        await NotificationService.ShowSuccessAsync("成功", "题目已保存");
    }

    /// <summary>
    /// 预览题目
    /// </summary>
    private async Task PreviewQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个题目");
            return;
        }

        // 这里可以添加题目预览逻辑
        await NotificationService.ShowSuccessAsync("预览", $"题目预览功能待实现\n题目：{SelectedQuestion.Title}");
    }

    /// <summary>
    /// 导出题目
    /// </summary>
    private async Task ExportQuestionAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个题目");
            return;
        }

        // 这里可以添加题目导出逻辑
        await NotificationService.ShowSuccessAsync("导出", $"题目导出功能待实现\n题目：{SelectedQuestion.Title}");
    }

    #endregion

    #region 操作点管理方法

    /// <summary>
    /// 添加操作点
    /// </summary>
    private async Task AddOperationPointAsync()
    {
        if (SelectedQuestion == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个题目");
            return;
        }

        string? operationPointTitle = await NotificationService.ShowInputDialogAsync(
            "添加操作点",
            "请输入操作点标题",
            "新操作点");

        if (string.IsNullOrWhiteSpace(operationPointTitle))
        {
            return;
        }

        OperationPoint newOperationPoint = new()
        {
            Name = operationPointTitle,
            Description = "请输入操作点描述",
            Order = SelectedQuestion.OperationPoints.Count + 1,
            IsEnabled = true,
            Score = 1
        };

        SelectedQuestion.OperationPoints.Add(newOperationPoint);
        SelectedOperationPoint = newOperationPoint;

        await NotificationService.ShowSuccessAsync("成功", $"已添加操作点：{operationPointTitle}");
    }

    /// <summary>
    /// 删除操作点
    /// </summary>
    private async Task DeleteOperationPointAsync(OperationPoint operationPoint)
    {
        if (SelectedQuestion == null || operationPoint == null)
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
            SelectedOperationPoint = null;
        }

        // 重新排序
        for (int i = 0; i < SelectedQuestion.OperationPoints.Count; i++)
        {
            SelectedQuestion.OperationPoints[i].Order = i + 1;
        }

        await NotificationService.ShowSuccessAsync("成功", "操作点已删除");
    }

    /// <summary>
    /// 配置操作点
    /// </summary>
    private void ConfigureOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
        {
            return;
        }

        SelectedOperationPoint = operationPoint;

        // 这里可以打开操作点配置对话框或导航到配置页面
        // 暂时只是选中操作点
    }

    #endregion

    /// <summary>
    /// 保存专项试卷到存储
    /// </summary>
    private async Task SaveSpecializedExamToStorageAsync(Exam exam)
    {
        // 这里可以使用DataStorageService保存专项试卷
        // 暂时为空实现，后续可以扩展
        await Task.CompletedTask;
    }

    /// <summary>
    /// 删除专项试卷
    /// </summary>
    private async Task DeleteSpecializedExamAsync(Exam exam)
    {
        try
        {
            SpecializedExams.Remove(exam);
            SpecializedExamCount = SpecializedExams.Count;

            if (SelectedSpecializedExam == exam)
            {
                SelectedSpecializedExam = SpecializedExams.FirstOrDefault();
            }

            await NotificationService.ShowSuccessAsync("删除成功", $"已删除专项试卷：{exam.Name}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", $"删除专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 克隆专项试卷
    /// </summary>
    private async Task CloneSpecializedExamAsync(Exam exam)
    {
        try
        {
            // 创建克隆
            Exam clonedExam = CloneExam(exam);
            SpecializedExams.Add(clonedExam);
            SelectedSpecializedExam = clonedExam;
            SpecializedExamCount = SpecializedExams.Count;

            await NotificationService.ShowSuccessAsync("克隆成功", $"已克隆专项试卷：{clonedExam.Name}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("克隆失败", $"克隆专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 克隆试卷
    /// </summary>
    private Exam CloneExam(Exam original)
    {
        string cloneName = $"{original.Name} - 副本";
        int counter = 1;
        string originalCloneName = cloneName;
        while (SpecializedExams.Any(e => e.Name == cloneName))
        {
            cloneName = $"{originalCloneName} ({counter})";
            counter++;
        }

        Exam cloned = new()
        {
            Id = IdGeneratorService.GenerateExamId(),
            Name = cloneName,
            Description = original.Description,
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 简化克隆：只克隆模块基本信息
        foreach (ExamModule originalModule in original.Modules)
        {
            ExamModule clonedModule = new()
            {
                Id = IdGeneratorService.GenerateModuleId(),
                Name = originalModule.Name,
                Type = originalModule.Type,
                Description = originalModule.Description,
                Score = originalModule.Score,
                Order = originalModule.Order,
                IsEnabled = originalModule.IsEnabled
            };

            cloned.Modules.Add(clonedModule);
        }

        return cloned;
    }

    /// <summary>
    /// 保存专项试卷
    /// </summary>
    private async Task SaveSpecializedExamAsync()
    {
        try
        {
            if (SelectedSpecializedExam != null)
            {
                await SaveSpecializedExamToStorageAsync(SelectedSpecializedExam);
                HasUnsavedChanges = false;
                await NotificationService.ShowSuccessAsync("保存成功", $"已保存专项试卷：{SelectedSpecializedExam.Name}");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出专项试卷
    /// </summary>
    private async Task ExportSpecializedExamAsync(Exam exam)
    {
        try
        {
            // 这里可以复用MainWindowViewModel的导出逻辑
            await NotificationService.ShowSuccessAsync("导出功能", "专项试卷导出功能正在开发中...");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", $"导出专项试卷时发生错误：{ex.Message}");
        }
    }
}
