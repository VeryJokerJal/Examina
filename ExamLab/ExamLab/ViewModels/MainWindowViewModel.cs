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
/// 主窗口ViewModel
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 当前选中的试卷
    /// </summary>
    [Reactive] public Exam? SelectedExam { get; set; }

    /// <summary>
    /// 试卷列表
    /// </summary>
    public ObservableCollection<Exam> Exams { get; set; } = [];

    /// <summary>
    /// 当前选中的模块
    /// </summary>
    [Reactive] public ExamModule? SelectedModule { get; set; }

    /// <summary>
    /// 当前模块（用于绑定）
    /// </summary>
    public ExamModule? Module => SelectedModule;

    /// <summary>
    /// 当前选中的题目
    /// </summary>
    [Reactive] public Question? SelectedQuestion { get; set; }

    /// <summary>
    /// 当前内容视图模型
    /// </summary>
    [Reactive] public ViewModelBase? CurrentContentViewModel { get; set; }

    /// <summary>
    /// 当前内容视图
    /// </summary>
    [Reactive] public Microsoft.UI.Xaml.Controls.UserControl? CurrentContentView { get; set; }

    /// <summary>
    /// 评分题目列表（过滤后的题目）
    /// </summary>
    public ObservableCollection<Question> ScoringQuestions { get; set; } = [];

    /// <summary>
    /// 创建新试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateExamCommand { get; }

    /// <summary>
    /// 选择试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> SelectExamCommand { get; }

    /// <summary>
    /// 选择模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> SelectModuleCommand { get; }

    /// <summary>
    /// 删除试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> DeleteExamCommand { get; }

    /// <summary>
    /// 保存试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveExamCommand { get; }

    /// <summary>
    /// 导入试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportExamCommand { get; }

    /// <summary>
    /// 导出试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> ExportExamCommand { get; }

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    [Reactive] public bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// 添加题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddQuestionCommand { get; }

    /// <summary>
    /// 添加操作点命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddOperationPointCommand { get; }

    /// <summary>
    /// 保存模块描述命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveModuleDescriptionCommand { get; }

    /// <summary>
    /// 重置模块描述命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetModuleDescriptionCommand { get; }

    public MainWindowViewModel()
    {
        Title = "试卷制作系统";

        // 初始化命令
        CreateExamCommand = ReactiveCommand.Create(CreateExam);
        SelectExamCommand = ReactiveCommand.Create<Exam>(SelectExam);
        SelectModuleCommand = ReactiveCommand.Create<ExamModule>(SelectModule);
        DeleteExamCommand = ReactiveCommand.CreateFromTask<Exam>(DeleteExamAsync);
        SaveExamCommand = ReactiveCommand.CreateFromTask(SaveExamAsync);
        ImportExamCommand = ReactiveCommand.CreateFromTask(ImportExamAsync);
        ExportExamCommand = ReactiveCommand.CreateFromTask<Exam>(ExportExamAsync);
        AddQuestionCommand = ReactiveCommand.CreateFromTask(AddQuestionAsync);
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        SaveModuleDescriptionCommand = ReactiveCommand.CreateFromTask(SaveModuleDescriptionAsync);
        ResetModuleDescriptionCommand = ReactiveCommand.CreateFromTask(ResetModuleDescriptionAsync);

        // 初始化数据持久化
        InitializeDataPersistence();

        // 监听SelectedModule变化，通知Module属性变化
        _ = this.WhenAnyValue(x => x.SelectedModule)
            .Subscribe(x =>
            {
                this.RaisePropertyChanged(nameof(Module));
                UpdateScoringQuestions();
                SelectModule(x);
            });
    }

    private void CreateExam()
    {
        Exam newExam = new()
        {
            Name = $"新试卷 2025-08-10",
            Description = "新创建的试卷",
            CreatedTime = "2025-08-10",
            LastModifiedTime = "2025-08-10"
        };

        // 添加默认模块
        newExam.Modules.Add(new ExamModule { Name = "Windows操作", Type = ModuleType.Windows, Order = 1 });
        newExam.Modules.Add(new ExamModule { Name = "C#编程", Type = ModuleType.CSharp, Order = 2 });
        newExam.Modules.Add(new ExamModule { Name = "PowerPoint操作", Type = ModuleType.PowerPoint, Order = 3 });
        newExam.Modules.Add(new ExamModule { Name = "Excel操作", Type = ModuleType.Excel, Order = 4 });
        newExam.Modules.Add(new ExamModule { Name = "Word操作", Type = ModuleType.Word, Order = 5 });

        Exams.Add(newExam);
        SelectedExam = newExam;
    }

    private void SelectExam(Exam exam)
    {
        SelectedExam = exam;
    }

    private void SelectModule(ExamModule? module)
    {
        SelectedModule = module;

        // 根据模块类型创建对应的ViewModel和View
        CurrentContentViewModel = module?.Type switch
        {
            ModuleType.Windows => new WindowsModuleViewModel(module),
            ModuleType.CSharp => new CSharpModuleViewModel(module),
            ModuleType.PowerPoint => new PowerPointModuleViewModel(module),
            ModuleType.Excel => new ExcelModuleViewModel(module),
            ModuleType.Word => new WordModuleViewModel(module),
            _ => null
        };

        // 创建对应的View，将MainWindowViewModel设置为DataContext以便访问通用功能
        CurrentContentView = CurrentContentViewModel switch
        {
            WindowsModuleViewModel vm => new Views.WindowsModuleView { DataContext = this },
            CSharpModuleViewModel vm => new Views.CSharpModuleView { DataContext = this },
            PowerPointModuleViewModel vm => new Views.PowerPointModuleView { DataContext = this },
            ExcelModuleViewModel vm => new Views.ExcelModuleView { DataContext = this },
            WordModuleViewModel vm => new Views.WordModuleView { DataContext = this },
            _ => null
        };
    }

    private async Task DeleteExamAsync(Exam exam)
    {
        bool confirmed = await NotificationService.ShowDeleteConfirmationAsync(exam.Name);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await DataStorageService.Instance.DeleteExamAsync(exam.Id);
            _ = Exams.Remove(exam);

            if (SelectedExam == exam)
            {
                SelectedExam = Exams.Count > 0 ? Exams[0] : null;
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", ex.Message);
        }
    }

    private async Task SaveExamAsync()
    {
        if (SelectedExam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有可保存的试卷");
            return;
        }

        try
        {
            ValidationResult result = ValidationService.ValidateExam(SelectedExam);
            if (!result.IsValid)
            {
                await NotificationService.ShowValidationErrorsAsync(result);
                return;
            }

            await DataStorageService.Instance.SaveExamAsync(SelectedExam);
            AutoSaveService.Instance.MarkAsSaved();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", ex.Message);
        }
    }

    private async Task ImportExamAsync()
    {
        try
        {
            // 这里应该打开文件选择器，暂时使用示例
            // var picker = new Windows.Storage.Pickers.FileOpenPicker();
            // picker.FileTypeFilter.Add(".json");
            // var file = await picker.PickSingleFileAsync();

            // 暂时显示提示
            await NotificationService.ShowSuccessAsync("导入功能", "导入功能将在后续版本中实现");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", ex.Message);
        }
    }

    private async Task ExportExamAsync(Exam exam)
    {
        try
        {
            // 这里应该打开文件保存器，暂时使用示例
            // var picker = new Windows.Storage.Pickers.FileSavePicker();
            // picker.FileTypeChoices.Add("JSON文件", new List<string>() { ".json" });
            // var file = await picker.PickSaveFileAsync();

            // 暂时显示提示
            await NotificationService.ShowSuccessAsync("导出功能", $"试卷 {exam.Name} 导出功能将在后续版本中实现");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", ex.Message);
        }
    }

    private void OnExamSelected(Exam exam)
    {
        // 自动选择第一个模块
        if (exam.Modules.Count > 0)
        {
            SelectModule(exam.Modules[0]);
        }
    }

    private async void InitializeDataPersistence()
    {
        try
        {
            // 加载保存的试卷数据
            List<Exam> savedExams = await DataStorageService.Instance.LoadExamsAsync();

            foreach (Exam exam in savedExams)
            {
                Exams.Add(exam);
            }

            // 如果没有保存的数据，创建示例数据
            if (Exams.Count == 0)
            {
                CreateSampleData();
            }
            else
            {
                SelectedExam = Exams.FirstOrDefault();
            }

            // 启动自动保存
            AutoSaveService.Instance.StartAutoSave(Exams);
            AutoSaveService.Instance.UnsavedChangesChanged += OnUnsavedChangesChanged;
        }
        catch (Exception ex)
        {
            // 如果加载失败，创建示例数据
            CreateSampleData();
            await NotificationService.ShowErrorAsync("数据加载失败", $"无法加载保存的数据：{ex.Message}");
        }
    }

    private void CreateSampleData()
    {
        // 创建示例试卷
        Exam sampleExam = new()
        {
            Name = "计算机应用基础考试",
            Description = "包含Windows、C#、Office等模块的综合考试",
            TotalScore = 100,
            Duration = 120
        };

        // 添加模块
        sampleExam.Modules.Add(new ExamModule { Name = "Windows操作", Type = ModuleType.Windows, Score = 20, Order = 1 });
        sampleExam.Modules.Add(new ExamModule { Name = "C#编程", Type = ModuleType.CSharp, Score = 20, Order = 2 });
        sampleExam.Modules.Add(new ExamModule { Name = "PowerPoint操作", Type = ModuleType.PowerPoint, Score = 20, Order = 3 });
        sampleExam.Modules.Add(new ExamModule { Name = "Excel操作", Type = ModuleType.Excel, Score = 20, Order = 4 });
        sampleExam.Modules.Add(new ExamModule { Name = "Word操作", Type = ModuleType.Word, Score = 20, Order = 5 });

        Exams.Add(sampleExam);
        SelectedExam = sampleExam;
    }

    private void OnUnsavedChangesChanged(bool hasUnsavedChanges)
    {
        HasUnsavedChanges = hasUnsavedChanges;
    }

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
            Score = 10,
            Order = SelectedModule.Questions.Count + 1,
            IsEnabled = true
        };

        SelectedModule.Questions.Add(newQuestion);
        SelectedQuestion = newQuestion;
        UpdateScoringQuestions();
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
            Score = 5,
            Order = SelectedQuestion.OperationPoints.Count + 1,
            IsEnabled = true
        };

        SelectedQuestion.OperationPoints.Add(newOperationPoint);
    }

    /// <summary>
    /// 更新评分题目列表
    /// </summary>
    private void UpdateScoringQuestions()
    {
        ScoringQuestions.Clear();

        if (SelectedModule?.Questions != null)
        {
            IEnumerable<Question> scoringQuestions = SelectedModule.Questions
                .OrderBy(q => q.Order);

            foreach (Question question in scoringQuestions)
            {
                ScoringQuestions.Add(question);
            }
        }
    }

    /// <summary>
    /// 保存模块描述
    /// </summary>
    private async Task SaveModuleDescriptionAsync()
    {
        if (SelectedModule == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个模块");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedModule.Name))
        {
            await NotificationService.ShowErrorAsync("错误", "模块名称不能为空");
            return;
        }
    }

    /// <summary>
    /// 重置模块描述
    /// </summary>
    private async Task ResetModuleDescriptionAsync()
    {
        if (SelectedModule == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个模块");
            return;
        }

        bool confirmed = await NotificationService.ShowDeleteConfirmationAsync("重置模块描述");
        if (!confirmed)
        {
            return;
        }

        // 根据模块类型重置为默认描述
        SelectedModule.Description = GetDefaultModuleDescription(SelectedModule.Type);
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
}
