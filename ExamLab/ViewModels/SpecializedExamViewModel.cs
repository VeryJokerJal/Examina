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
using ExamLab.Services.DocumentGeneration;
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
    /// 生成Word文档命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> GenerateWordDocumentCommand { get; }

    /// <summary>
    /// 生成Excel文档命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> GenerateExcelDocumentCommand { get; }

    /// <summary>
    /// 生成PowerPoint文档命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> GeneratePowerPointDocumentCommand { get; }

    /// <summary>
    /// 重置模块描述命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetModuleDescriptionCommand { get; }

    /// <summary>
    /// 保存模块描述命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveModuleDescriptionCommand { get; }

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

        // 文档生成命令
        GenerateWordDocumentCommand = ReactiveCommand.CreateFromTask(GenerateWordDocumentAsync);
        GenerateExcelDocumentCommand = ReactiveCommand.CreateFromTask(GenerateExcelDocumentAsync);
        GeneratePowerPointDocumentCommand = ReactiveCommand.CreateFromTask(GeneratePowerPointDocumentAsync);

        // 模块描述管理命令
        ResetModuleDescriptionCommand = ReactiveCommand.CreateFromTask(ResetModuleDescriptionAsync);
        SaveModuleDescriptionCommand = ReactiveCommand.CreateFromTask(SaveModuleDescriptionAsync);

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
            Content = "请输入题目解析",
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
    private async Task DeleteSpecializedExamAsync(SpecializedExam exam)
    {
        if (exam == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "确认删除",
            $"确定要删除专项试卷\"{exam.Name}\"吗？此操作不可撤销。");

        if (!confirmed)
        {
            return;
        }

        try
        {
            _ = SpecializedExams.Remove(exam);
            SpecializedExamCount = SpecializedExams.Count;

            if (SelectedSpecializedExam == exam)
            {
                SelectedSpecializedExam = SpecializedExams.FirstOrDefault();
            }

            await NotificationService.ShowSuccessAsync("删除成功", $"专项试卷\"{exam.Name}\"已删除");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", $"删除专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 克隆专项试卷
    /// </summary>
    private async Task CloneSpecializedExamAsync(SpecializedExam exam)
    {
        if (exam == null)
        {
            return;
        }

        string? newName = await NotificationService.ShowInputDialogAsync(
            "克隆专项试卷",
            "请输入新试卷名称",
            $"{exam.Name} - 副本");

        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        try
        {
            // 创建克隆
            SpecializedExam clonedExam = exam.Clone();
            clonedExam.Name = newName;
            clonedExam.Id = IdGeneratorService.GenerateExamId();
            clonedExam.CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clonedExam.LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 为克隆的模块生成新的ID
            foreach (ExamModule module in clonedExam.Modules)
            {
                module.Id = IdGeneratorService.GenerateModuleId();

                // 为克隆的题目生成新的ID
                foreach (Question question in module.Questions)
                {
                    question.Id = IdGeneratorService.GenerateQuestionId();

                    // 为克隆的操作点生成新的ID
                    foreach (OperationPoint operationPoint in question.OperationPoints)
                    {
                        operationPoint.Id = IdGeneratorService.GenerateOperationId();
                    }
                }
            }

            SpecializedExams.Add(clonedExam);
            SelectedSpecializedExam = clonedExam;
            SpecializedExamCount = SpecializedExams.Count;

            await NotificationService.ShowSuccessAsync("克隆成功", $"专项试卷\"{newName}\"已创建");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("克隆失败", $"克隆专项试卷时发生错误：{ex.Message}");
        }
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

            // 4. 智能解析文件内容
            Models.ImportExport.ExamExportDto? importDto = null;
            SpecializedExamImportResult? directImportResult = null;

            try
            {
                if (file.FileType.Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    // JSON格式：尝试专项试卷格式，失败则尝试通用格式
                    System.Text.Json.JsonSerializerOptions jsonOptions = new()
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    jsonOptions.Converters.Add(new Converters.ModuleTypeJsonConverter());
                    jsonOptions.Converters.Add(new Converters.PathTypeJsonConverter());

                    // 首先尝试专项试卷专用格式
                    try
                    {
                        SpecializedExamExportDto? specializedDto = System.Text.Json.JsonSerializer.Deserialize<SpecializedExamExportDto>(fileContent, jsonOptions);
                        if (specializedDto?.SpecializedExam != null && specializedDto.DataType == "SpecializedExam")
                        {
                            // 直接从专项试卷格式导入
                            SpecializedExam specializedExam = SpecializedExamMappingService.FromSpecializedExportDto(specializedDto);
                            directImportResult = SpecializedExamImportResult.Success(specializedExam);
                        }
                    }
                    catch
                    {
                        // 专项试卷格式解析失败，继续尝试通用格式
                    }

                    // 如果专项试卷格式失败，尝试通用格式
                    if (directImportResult == null)
                    {
                        importDto = System.Text.Json.JsonSerializer.Deserialize<Models.ImportExport.ExamExportDto>(fileContent, jsonOptions);
                    }
                }
                else if (file.FileType.Equals(".xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    // XML格式：只支持通用格式
                    importDto = XmlSerializationService.DeserializeFromXml(fileContent);
                }
            }
            catch (Exception parseEx)
            {
                await NotificationService.ShowErrorAsync("文件解析错误", $"无法解析文件内容：{parseEx.Message}");
                return;
            }

            // 5. 处理导入结果
            SpecializedExamImportResult importResult;

            if (directImportResult != null)
            {
                // 直接从专项试卷格式导入成功
                importResult = directImportResult;
            }
            else if (importDto?.Exam != null)
            {
                // 从通用格式智能导入
                importResult = SpecializedExamMappingService.SmartImport(importDto);
            }
            else
            {
                await NotificationService.ShowErrorAsync("数据格式错误", "文件中没有找到有效的试卷数据");
                return;
            }

            // 6. 检查导入结果
            if (!importResult.IsSuccess)
            {
                await NotificationService.ShowErrorAsync("导入失败", importResult.ErrorMessage ?? "未知错误");
                return;
            }

            SpecializedExam importedExam = importResult.SpecializedExam!;

            // 6. 数据验证
            ValidationResult validationResult = SpecializedExamMappingService.ValidateSpecializedExam(importedExam);
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
            string summaryInfo = SpecializedExamMappingService.CreateSummaryInfo(importedExam);
            string importInfo = $"{summaryInfo}\n文件大小：{fileSize}";

            // 如果有警告信息，添加到消息中
            if (importResult.Warnings.Count > 0)
            {
                importInfo += "\n\n注意事项：\n" + string.Join("\n", importResult.Warnings);
            }

            // 如果是从通用格式转换而来，特别提醒
            if (importResult.IsConvertedFromGeneric)
            {
                importInfo += "\n\n此专项试卷是从通用试卷格式自动转换而来，请检查数据完整性。";
            }

            await NotificationService.ShowSuccessAsync("导入成功", importInfo);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", $"导入专项试卷时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出专项试卷
    /// </summary>
    private async Task ExportSpecializedExamAsync(SpecializedExam exam)
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
            ValidationResult validationResult = SpecializedExamMappingService.ValidateSpecializedExam(exam);
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

            // 4. 根据文件格式选择导出方式
            string fileContent;

            if (file.FileType.Equals(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                // JSON格式：使用专项试卷专用格式
                SpecializedExamExportDto specializedExportDto = SpecializedExamMappingService.ToSpecializedExportDto(exam, exportLevel);

                System.Text.Json.JsonSerializerOptions jsonOptions = new()
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                // 添加自定义转换器
                jsonOptions.Converters.Add(new Converters.ModuleTypeJsonConverter());
                jsonOptions.Converters.Add(new Converters.CSharpQuestionTypeJsonConverter());
                jsonOptions.Converters.Add(new Converters.PathTypeJsonConverter());

                fileContent = System.Text.Json.JsonSerializer.Serialize(specializedExportDto, jsonOptions);
            }
            else if (file.FileType.Equals(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                // XML格式：使用通用格式（向后兼容）
                ExamExportDto genericExportDto = SpecializedExamMappingService.ToExportDto(exam, exportLevel);
                fileContent = XmlSerializationService.SerializeToXml(genericExportDto);
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
            string summaryInfo = SpecializedExamMappingService.CreateSummaryInfo(exam);
            string exportInfo = $"{summaryInfo}\n" +
                              $"导出级别：{GetExportLevelDisplayName(exportLevel)}\n" +
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

    /// <summary>
    /// 生成Word文档
    /// </summary>
    private async Task GenerateWordDocumentAsync()
    {
        await GenerateDocumentAsync(ModuleType.Word, new WordDocumentGenerator());
    }

    /// <summary>
    /// 生成Excel文档
    /// </summary>
    private async Task GenerateExcelDocumentAsync()
    {
        await GenerateDocumentAsync(ModuleType.Excel, new ExcelDocumentGenerator());
    }

    /// <summary>
    /// 生成PowerPoint文档
    /// </summary>
    private async Task GeneratePowerPointDocumentAsync()
    {
        await GenerateDocumentAsync(ModuleType.PowerPoint, new PowerPointDocumentGenerator());
    }

    /// <summary>
    /// 生成文档的通用方法
    /// </summary>
    private async Task GenerateDocumentAsync(ModuleType moduleType, IDocumentGenerationService documentService)
    {
        try
        {
            // 1. 验证当前状态
            if (SelectedSpecializedExam == null)
            {
                await NotificationService.ShowWarningAsync("提示", "请先选择一个专项试卷");
                return;
            }

            // 2. 获取对应模块
            ExamModule? module = SelectedSpecializedExam.Modules.FirstOrDefault(m => m.Type == moduleType);
            if (module == null)
            {
                await NotificationService.ShowWarningAsync("提示", $"当前专项试卷不包含{GetModuleTypeName(moduleType)}模块");
                return;
            }

            // 3. 验证模块内容
            DocumentValidationResult validation = documentService.ValidateModule(module);
            if (!validation.IsValid)
            {
                string errorMessage = string.Join("\n", validation.ErrorMessages);
                await NotificationService.ShowErrorAsync("验证失败", $"模块验证失败：\n{errorMessage}");
                return;
            }

            // 4. 选择保存位置
            string fileName = $"{SelectedSpecializedExam.Name}_{GetModuleTypeName(moduleType)}_答案_{DateTime.Now:yyyyMMdd_HHmmss}{documentService.GetRecommendedFileExtension()}";

            Windows.Storage.Pickers.FileSavePicker savePicker = new()
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = fileName
            };
            savePicker.FileTypeChoices.Add(documentService.GetFileTypeDescription(), [documentService.GetRecommendedFileExtension()]);

            // 获取当前窗口句柄
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            Windows.Storage.StorageFile? file = await savePicker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            // 5. 显示进度对话框
            ContentDialog progressDialog = new()
            {
                Title = "生成文档",
                Content = "正在生成文档，请稍候...",
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            // 在后台线程中生成文档
            Task generateTask = Task.Run(async () =>
            {
                Progress<DocumentGenerationProgress> progress = new(p =>
                {
                    // 在UI线程中更新进度
                    _ = (App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                    {
                        progressDialog.Content = $"{p.CurrentStep}\n进度：{p.ProgressPercentage}%\n已处理：{p.ProcessedCount}/{p.TotalCount}";
                        if (!string.IsNullOrEmpty(p.CurrentOperationPoint))
                        {
                            progressDialog.Content += $"\n当前操作：{p.CurrentOperationPoint}";
                        }
                    }));
                });

                DocumentGenerationResult result = await documentService.GenerateDocumentAsync(module, file.Path, progress);

                // 在UI线程中处理结果
                _ = (App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    progressDialog.Hide();

                    if (result.IsSuccess)
                    {
                        string successMessage = $"文档生成成功！\n" +
                                              $"保存位置：{result.FilePath}\n" +
                                              $"处理操作点：{result.ProcessedOperationPoints}/{result.TotalOperationPoints}\n" +
                                              $"耗时：{result.Duration.TotalSeconds:F1}秒";

                        if (!string.IsNullOrEmpty(result.Details))
                        {
                            successMessage += $"\n\n详细信息：\n{result.Details}";
                        }

                        ContentDialogResult dialogResult = await NotificationService.ShowSuccessWithActionAsync(
                            "生成成功",
                            successMessage,
                            "打开文件",
                            "确定");

                        if (dialogResult == ContentDialogResult.Primary)
                        {
                            // 打开生成的文件
                            _ = await Windows.System.Launcher.LaunchFileAsync(file);
                        }
                    }
                    else
                    {
                        string errorMessage = $"文档生成失败：{result.ErrorMessage}";
                        if (!string.IsNullOrEmpty(result.Details))
                        {
                            errorMessage += $"\n\n详细信息：\n{result.Details}";
                        }
                        await NotificationService.ShowErrorAsync("生成失败", errorMessage);
                    }
                }));
            });

            // 显示进度对话框
            _ = progressDialog.ShowAsync();

            // 等待生成完成
            await generateTask;
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("生成失败", $"生成{GetModuleTypeName(moduleType)}文档时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存模块描述
    /// </summary>
    private async Task SaveModuleDescriptionAsync()
    {
        try
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

            // 更新最后修改时间
            if (SelectedSpecializedExam != null)
            {
                SelectedSpecializedExam.UpdateLastModifiedTime();
                HasUnsavedChanges = true;
            }

            await NotificationService.ShowSuccessAsync("保存成功", "模块描述已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存模块描述时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 重置模块描述
    /// </summary>
    private async Task ResetModuleDescriptionAsync()
    {
        try
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

            // 更新最后修改时间
            if (SelectedSpecializedExam != null)
            {
                SelectedSpecializedExam.UpdateLastModifiedTime();
                HasUnsavedChanges = true;
            }

            await NotificationService.ShowSuccessAsync("重置成功", "模块描述已重置为默认值");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("重置失败", $"重置模块描述时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取默认模块描述
    /// </summary>
    private static string GetDefaultModuleDescription(ModuleType type)
    {
        return type switch
        {
            ModuleType.Windows => "Windows系统操作和文件管理相关题目",
            ModuleType.CSharp => "C#编程语言基础和应用开发题目",
            ModuleType.PowerPoint => "PowerPoint演示文稿制作和设计题目",
            ModuleType.Excel => "Excel电子表格操作和数据分析题目",
            ModuleType.Word => "Word文档编辑和排版设计题目",
            _ => "专项练习题目"
        };
    }

}
