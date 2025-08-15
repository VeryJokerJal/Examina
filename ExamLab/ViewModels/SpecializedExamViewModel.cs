using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Models.ImportExport;
using ExamLab.Services;
using ExamLab.Views;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 专项试卷制作页面的ViewModel
///
/// 设计说明：
/// - 使用独立的SpecializedExam模型，与考试试卷的Exam模型分离
/// - 专项试卷专门针对单一模块类型的专项练习
/// - 具有专项试卷特有的属性和行为，如ModuleType、DifficultyLevel等
/// - 导入导出功能通过映射服务与通用格式兼容
/// </summary>
public class SpecializedExamViewModel : ViewModelBase
{
    /// <summary>
    /// 专项试卷列表
    /// </summary>
    public ObservableCollection<SpecializedExam> SpecializedExams { get; } = [];

    /// <summary>
    /// 当前选中的专项试卷
    /// </summary>
    [Reactive] public SpecializedExam? SelectedSpecializedExam { get; set; }

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
    public ReactiveCommand<SpecializedExam, Unit> DeleteSpecializedExamCommand { get; }

    /// <summary>
    /// 克隆专项试卷命令
    /// </summary>
    public ReactiveCommand<SpecializedExam, Unit> CloneSpecializedExamCommand { get; }

    /// <summary>
    /// 保存专项试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveSpecializedExamCommand { get; }

    /// <summary>
    /// 导入专项试卷命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportExamCommand { get; }

    /// <summary>
    /// 导出专项试卷命令
    /// </summary>
    public ReactiveCommand<SpecializedExam, Unit> ExportSpecializedExamCommand { get; }

    /// <summary>
    /// 选择专项试卷命令
    /// </summary>
    public ReactiveCommand<SpecializedExam, Unit> SelectSpecializedExamCommand { get; }

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
        DeleteSpecializedExamCommand = ReactiveCommand.CreateFromTask<SpecializedExam>(DeleteSpecializedExamAsync);
        CloneSpecializedExamCommand = ReactiveCommand.CreateFromTask<SpecializedExam>(CloneSpecializedExamAsync);
        SaveSpecializedExamCommand = ReactiveCommand.CreateFromTask(SaveSpecializedExamAsync);
        ImportExamCommand = ReactiveCommand.CreateFromTask(ImportSpecializedExamAsync);
        ExportSpecializedExamCommand = ReactiveCommand.CreateFromTask<SpecializedExam>(ExportSpecializedExamAsync);
        SelectSpecializedExamCommand = ReactiveCommand.Create<SpecializedExam>(SelectSpecializedExam);

        // 题目管理命令
        AddQuestionCommand = ReactiveCommand.CreateFromTask(AddQuestionAsync);
        DeleteQuestionCommand = ReactiveCommand.CreateFromTask<Question>(DeleteQuestionAsync);
        CopyQuestionCommand = ReactiveCommand.Create<Question>(CopyQuestion);
        SaveQuestionCommand = ReactiveCommand.CreateFromTask(SaveQuestionAsync);
        PreviewQuestionCommand = ReactiveCommand.CreateFromTask(PreviewQuestionAsync);
        ExportQuestionCommand = ReactiveCommand.CreateFromTask(ExportQuestionAsync);

        // 操作点管理命令
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        DeleteOperationPointCommand = ReactiveCommand.CreateFromTask<OperationPoint>(DeleteOperationPointAsync);
        ConfigureOperationPointCommand = ReactiveCommand.Create<OperationPoint>(ConfigureOperationPoint);

        // 监听选中试卷变化
        _ = this.WhenAnyValue(x => x.SelectedSpecializedExam)
            .Subscribe(OnSelectedSpecializedExamChanged);

        // 初始化数据
        _ = InitializeDataAsync();
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
                SpecializedExam specializedExam = CreateSpecializedExam(selectedType);

                // 添加到列表
                SpecializedExams.Add(specializedExam);
                SelectedSpecializedExam = specializedExam;

                // 保存到本地存储
                await SaveSpecializedExamToStorageAsync(specializedExam);

                // 更新计数
                SpecializedExamCount = SpecializedExams.Count;
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
    private SpecializedExam CreateSpecializedExam(ModuleType moduleType)
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

        SpecializedExam exam = new()
        {
            Id = IdGeneratorService.GenerateExamId(),
            Name = examName,
            Description = $"专门针对{moduleTypeName}的专项练习试卷",
            ModuleType = moduleType,
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 创建默认模块
        exam.CreateDefaultModule();

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
    private void SelectSpecializedExam(SpecializedExam exam)
    {
        SelectedSpecializedExam = exam;
    }

    /// <summary>
    /// 选中专项试卷变化处理
    /// </summary>
    private void OnSelectedSpecializedExamChanged(SpecializedExam? exam)
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
            List<SpecializedExam> savedExams = LoadSpecializedExamsFromStorage();

            foreach (SpecializedExam exam in savedExams)
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
    private List<SpecializedExam> LoadSpecializedExamsFromStorage()
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

        _ = SelectedModule.Questions.Remove(question);

        if (SelectedQuestion == question)
        {
            SelectedQuestion = null;
        }

        // 重新排序
        for (int i = 0; i < SelectedModule.Questions.Count; i++)
        {
            SelectedModule.Questions[i].Order = i + 1;
        }
    }

    /// <summary>
    /// 复制题目
    /// </summary>
    private void CopyQuestion(Question question)
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

        _ = SelectedQuestion.OperationPoints.Remove(operationPoint);

        if (SelectedOperationPoint == operationPoint)
        {
            SelectedOperationPoint = null;
        }

        // 重新排序
        for (int i = 0; i < SelectedQuestion.OperationPoints.Count; i++)
        {
            SelectedQuestion.OperationPoints[i].Order = i + 1;
        }
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
    private async Task SaveSpecializedExamToStorageAsync(SpecializedExam exam)
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
            _ = SpecializedExams.Remove(exam);
            SpecializedExamCount = SpecializedExams.Count;

            if (SelectedSpecializedExam == exam)
            {
                SelectedSpecializedExam = SpecializedExams.FirstOrDefault();
            }
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
            ExamType = ExamType.Specialized, // 克隆的专项试卷保持专项类型
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
    /// 导入专项试卷
    /// </summary>
    private async Task ImportSpecializedExamAsync()
    {
        try
        {
            // 1. 选择要导入的文件
            Windows.Storage.StorageFile? file = await FilePickerService.PickExamFileForImportAsync();

            if (file == null)
            {
                // 用户取消了导入操作
                return;
            }

            // 2. 验证文件类型
            List<string> supportedExtensions = [".json", ".xml"];
            if (!FilePickerService.IsValidFileType(file, supportedExtensions))
            {
                await NotificationService.ShowErrorAsync("文件类型错误", "请选择JSON或XML格式的试卷文件");
                return;
            }

            // 3. 读取文件内容
            string fileContent = await Windows.Storage.FileIO.ReadTextAsync(file);

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                await NotificationService.ShowErrorAsync("文件内容错误", "选择的文件为空或无法读取");
                return;
            }

            // 4. 解析文件内容
            Models.ImportExport.ExamExportDto? importDto = null;
            try
            {
                if (file.FileType.ToLowerInvariant() == ".json")
                {
                    // JSON反序列化
                    System.Text.Json.JsonSerializerOptions jsonOptions = new()
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    jsonOptions.Converters.Add(new Converters.ModuleTypeJsonConverter());
                    importDto = System.Text.Json.JsonSerializer.Deserialize<Models.ImportExport.ExamExportDto>(fileContent, jsonOptions);
                }
                else if (file.FileType.ToLowerInvariant() == ".xml")
                {
                    // XML反序列化
                    importDto = Services.XmlSerializationService.DeserializeFromXml(fileContent);
                }
            }
            catch (Exception parseEx)
            {
                await NotificationService.ShowErrorAsync("文件解析错误", $"无法解析文件内容：{parseEx.Message}");
                return;
            }

            if (importDto?.Exam == null)
            {
                await NotificationService.ShowErrorAsync("数据格式错误", "文件中没有找到有效的试卷数据");
                return;
            }

            // 5. 转换为ExamLab模型
            Exam importedExam = ExamMappingService.FromExportDto(importDto);

            // 5.1. 确保导入的试卷被标记为专项试卷类型
            importedExam.ExamType = ExamType.Specialized;

            // 6. 数据验证
            ValidationResult validationResult = ValidationService.ValidateExam(importedExam);
            if (!validationResult.IsValid)
            {
                bool continueWithErrors = await NotificationService.ShowConfirmationAsync(
                    "数据验证警告",
                    $"导入的专项试卷数据存在以下问题：\n{validationResult.GetErrorMessage()}\n\n是否继续导入？");

                if (!continueWithErrors)
                {
                    return;
                }
            }

            // 7. 检查重名并处理
            string originalName = importedExam.Name;
            int counter = 1;
            while (SpecializedExams.Any(e => e.Name == importedExam.Name))
            {
                importedExam.Name = $"{originalName} (导入{counter})";
                counter++;
            }

            // 8. 添加到专项试卷列表
            SpecializedExams.Add(importedExam);
            SelectedSpecializedExam = importedExam;

            // 9. 保存到本地存储
            await SaveSpecializedExamToStorageAsync(importedExam);

            // 10. 更新计数
            SpecializedExamCount = SpecializedExams.Count;

            // 11. 显示成功消息
            string fileSize = await FilePickerService.GetFileSizeStringAsync(file);
            string summaryInfo = $"专项试卷名称：{importedExam.Name}\n" +
                               $"模块数量：{importedExam.Modules.Count}\n" +
                               $"题目总数：{importedExam.Modules.Sum(m => m.Questions.Count)}\n" +
                               $"文件大小：{fileSize}";

            await NotificationService.ShowSuccessAsync("导入成功", summaryInfo);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", $"导入专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出专项试卷
    /// </summary>
    private async Task ExportSpecializedExamAsync(Exam exam)
    {
        if (exam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有选择要导出的专项试卷");
            return;
        }

        try
        {
            // 1. 选择导出级别
            ExportLevel exportLevel = ShowExportLevelSelection();

            // 2. 数据验证
            ValidationResult validationResult = ValidationService.ValidateExam(exam);
            if (!validationResult.IsValid)
            {
                bool continueWithErrors = await NotificationService.ShowConfirmationAsync(
                    "数据验证警告",
                    $"专项试卷数据存在以下问题：\n{validationResult.GetErrorMessage()}\n\n是否继续导出？");

                if (!continueWithErrors)
                {
                    return;
                }
            }

            // 3. 选择保存位置
            Windows.Storage.StorageFile? file = await FilePickerService.PickExamFileForExportAsync(exam.Name);

            if (file == null)
            {
                // 用户取消了导出操作
                return;
            }

            // 4. 转换为导出格式
            ExamExportDto exportDto = ExamMappingService.ToExportDto(exam, exportLevel);

            // 5. 根据文件扩展名选择序列化格式
            string fileContent;

            if (file.FileType.ToLowerInvariant() == ".json")
            {
                // JSON序列化
                System.Text.Json.JsonSerializerOptions jsonOptions = new()
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                // 添加自定义转换器
                jsonOptions.Converters.Add(new Converters.ModuleTypeJsonConverter());

                fileContent = System.Text.Json.JsonSerializer.Serialize(exportDto, jsonOptions);
            }
            else if (file.FileType.ToLowerInvariant() == ".xml")
            {
                // XML序列化
                fileContent = Services.XmlSerializationService.SerializeToXml(exportDto);
            }
            else
            {
                await NotificationService.ShowErrorAsync("文件类型错误", "不支持的文件类型，请选择JSON或XML格式");
                return;
            }

            // 6. 写入文件
            await Windows.Storage.FileIO.WriteTextAsync(file, fileContent);

            // 7. 显示成功消息
            string fileSize = await FilePickerService.GetFileSizeStringAsync(file);
            string exportInfo = $"专项试卷名称：{exam.Name}\n" +
                              $"导出级别：{GetExportLevelDisplayName(exportLevel)}\n" +
                              $"模块数量：{exam.Modules.Count}\n" +
                              $"题目总数：{exam.Modules.Sum(m => m.Questions.Count)}\n" +
                              $"保存位置：{file.Path}\n" +
                              $"文件大小：{fileSize}";

            await NotificationService.ShowSuccessAsync("导出成功", exportInfo);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", $"导出专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 显示导出级别选择对话框
    /// </summary>
    private ExportLevel ShowExportLevelSelection()
    {
        // 暂时返回完整导出级别，后续可以添加对话框让用户选择
        return ExportLevel.Complete;
    }

    /// <summary>
    /// 获取导出级别的显示名称
    /// </summary>
    private static string GetExportLevelDisplayName(ExportLevel exportLevel)
    {
        return exportLevel switch
        {
            ExportLevel.Basic => "基础信息",
            ExportLevel.WithoutAnswers => "标准信息",
            ExportLevel.Complete => "完整信息",
            _ => "未知级别"
        };
    }
}
