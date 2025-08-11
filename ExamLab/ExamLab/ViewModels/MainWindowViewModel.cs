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
    /// 试卷数量（用于绑定）
    /// </summary>
    [Reactive] public int ExamCount { get; set; }

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
    /// 克隆试卷命令
    /// </summary>
    public ReactiveCommand<Exam, Unit> CloneExamCommand { get; }

    /// <summary>
    /// 保存项目命令 - 将当前试卷保存为ExamLab项目文件
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
    /// 是否有未保存的项目更改
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
        CloneExamCommand = ReactiveCommand.CreateFromTask<Exam>(CloneExamAsync);
        SaveExamCommand = ReactiveCommand.CreateFromTask(SaveExamAsync);
        ImportExamCommand = ReactiveCommand.CreateFromTask(ImportExamAsync);
        ExportExamCommand = ReactiveCommand.CreateFromTask<Exam>(ExportExamAsync);

        AddQuestionCommand = ReactiveCommand.CreateFromTask(AddQuestionAsync);
        AddOperationPointCommand = ReactiveCommand.CreateFromTask(AddOperationPointAsync);
        SaveModuleDescriptionCommand = ReactiveCommand.CreateFromTask(SaveModuleDescriptionAsync);
        ResetModuleDescriptionCommand = ReactiveCommand.CreateFromTask(ResetModuleDescriptionAsync);

        // 初始化数据持久化
        InitializeDataPersistence();

        // 监听Exams集合变化，更新ExamCount
        Exams.CollectionChanged += (sender, e) =>
        {
            ExamCount = Exams.Count;
        };

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

    private async Task CloneExamAsync(Exam exam)
    {
        if (exam == null)
        {
            return;
        }

        string? newName = await NotificationService.ShowInputDialogAsync(
            "克隆试卷",
            "请输入新试卷名称",
            $"{exam.Name} - 副本");

        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        try
        {
            // 创建试卷的深度副本
            Exam clonedExam = new()
            {
                Name = newName,
                Description = exam.Description,
                TotalScore = exam.TotalScore,
                Duration = exam.Duration
            };

            // 克隆所有模块
            foreach (ExamModule module in exam.Modules)
            {
                ExamModule clonedModule = new()
                {
                    Name = module.Name,
                    Type = module.Type,
                    Description = module.Description,
                    Score = module.Score,
                    Order = module.Order,
                    IsEnabled = module.IsEnabled
                };

                // 克隆所有题目
                foreach (Question question in module.Questions)
                {
                    Question clonedQuestion = new()
                    {
                        Title = question.Title,
                        Content = question.Content,
                        Score = question.Score,
                        Order = question.Order,
                        IsEnabled = question.IsEnabled,
                        CreatedTime = question.CreatedTime,
                        ProgramInput = question.ProgramInput,
                        ExpectedOutput = question.ExpectedOutput
                    };

                    // 克隆所有操作点
                    foreach (OperationPoint operationPoint in question.OperationPoints)
                    {
                        OperationPoint clonedOperationPoint = new()
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

                        // 克隆所有配置参数
                        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
                        {
                            ConfigurationParameter clonedParameter = new()
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

                            clonedOperationPoint.Parameters.Add(clonedParameter);
                        }

                        clonedQuestion.OperationPoints.Add(clonedOperationPoint);
                    }

                    clonedModule.Questions.Add(clonedQuestion);
                }

                clonedExam.Modules.Add(clonedModule);
            }

            // 保存克隆的试卷
            await DataStorageService.Instance.SaveExamAsync(clonedExam);
            Exams.Add(clonedExam);
            SelectedExam = clonedExam;

            await NotificationService.ShowSuccessAsync("克隆成功", $"试卷 '{newName}' 已创建");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("克隆失败", ex.Message);
        }
    }

    private async Task SaveExamAsync()
    {
        if (SelectedExam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有可保存的项目");
            return;
        }

        try
        {
            // 1. 选择保存位置（移除数据验证，允许保存未完成的项目）
            string suggestedFileName = $"{SelectedExam.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            Windows.Storage.StorageFile? file = await FilePickerService.PickProjectFileForSaveAsync(suggestedFileName);

            if (file == null)
            {
                // 用户取消了保存操作
                return;
            }

            // 3. 转换为导出格式
            var exportDto = ExamMappingService.ToExportDto(SelectedExam, ExportLevel.Complete);

            // 4. JSON序列化
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(exportDto, jsonOptions);

            // 5. 写入文件
            await Windows.Storage.FileIO.WriteTextAsync(file, jsonContent);

            // 6. 同时保存到本地存储（用于应用内管理）
            await DataStorageService.Instance.SaveExamAsync(SelectedExam);
            AutoSaveService.Instance.MarkAsSaved();

            // 6. 显示成功消息
            string fileSize = await FilePickerService.GetFileSizeStringAsync(file);
            await NotificationService.ShowSuccessAsync(
                "项目保存成功",
                $"ExamLab项目已保存到：{file.Path}\n文件大小：{fileSize}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("项目保存失败", $"保存ExamLab项目时发生错误：{ex.Message}");
        }
    }

    private async Task ImportExamAsync()
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
            var supportedExtensions = new List<string> { ".json", ".xml" };
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

            if (file.FileType.ToLowerInvariant() == ".json")
            {
                // JSON格式解析
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    importDto = System.Text.Json.JsonSerializer.Deserialize<Models.ImportExport.ExamExportDto>(fileContent, jsonOptions);
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    await NotificationService.ShowErrorAsync("JSON解析错误", $"文件格式不正确：{jsonEx.Message}");
                    return;
                }
            }
            else if (file.FileType.ToLowerInvariant() == ".xml")
            {
                // XML格式解析（暂时不支持，显示提示）
                await NotificationService.ShowErrorAsync("格式不支持", "XML格式导入功能将在后续版本中实现，请使用JSON格式");
                return;
            }

            if (importDto?.Exam == null)
            {
                await NotificationService.ShowErrorAsync("数据格式错误", "文件中没有找到有效的试卷数据");
                return;
            }

            // 5. 版本兼容性检查
            if (!IsCompatibleVersion(importDto.Metadata?.ExportVersion))
            {
                bool continueImport = await NotificationService.ShowConfirmationAsync(
                    "版本兼容性警告",
                    $"导入的试卷版本（{importDto.Metadata?.ExportVersion}）可能与当前版本不完全兼容，是否继续导入？");

                if (!continueImport)
                {
                    return;
                }
            }

            // 6. 转换为ExamLab模型
            Exam importedExam = ExamMappingService.FromExportDto(importDto);

            // 7. 数据验证
            ValidationResult validationResult = ValidationService.ValidateExam(importedExam);
            if (!validationResult.IsValid)
            {
                bool continueWithErrors = await NotificationService.ShowConfirmationAsync(
                    "数据验证警告",
                    $"导入的试卷数据存在以下问题：\n{validationResult.GetErrorMessage()}\n\n是否继续导入？");

                if (!continueWithErrors)
                {
                    return;
                }
            }

            // 8. 检查重名并处理
            string originalName = importedExam.Name;
            int counter = 1;
            while (Exams.Any(e => e.Name == importedExam.Name))
            {
                importedExam.Name = $"{originalName} (导入{counter})";
                counter++;
            }

            // 9. 添加到试卷列表
            Exams.Add(importedExam);
            SelectedExam = importedExam;

            // 10. 保存到本地存储
            await DataStorageService.Instance.SaveExamAsync(importedExam);

            // 11. 显示成功消息
            string fileSize = await FilePickerService.GetFileSizeStringAsync(file);
            string summaryInfo = $"试卷名称：{importedExam.Name}\n" +
                               $"模块数量：{importedExam.Modules.Count}\n" +
                               $"题目总数：{importedExam.Modules.Sum(m => m.Questions.Count)}\n" +
                               $"文件大小：{fileSize}";

            await NotificationService.ShowSuccessAsync("导入成功", summaryInfo);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", $"导入试卷时发生错误：{ex.Message}");
        }
    }

    private async Task ExportExamAsync(Exam exam)
    {
        if (exam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有选择要导出的试卷");
            return;
        }

        try
        {
            // 1. 选择导出级别
            ExportLevel exportLevel = await ShowExportLevelSelectionAsync();

            // 2. 数据验证
            ValidationResult validationResult = ValidationService.ValidateExam(exam);
            if (!validationResult.IsValid)
            {
                bool continueWithErrors = await NotificationService.ShowConfirmationAsync(
                    "数据验证警告",
                    $"试卷数据存在以下问题：\n{validationResult.GetErrorMessage()}\n\n是否继续导出？");

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
            var exportDto = ExamMappingService.ToExportDto(exam, exportLevel);

            // 5. 根据文件扩展名选择序列化格式
            string fileContent;

            if (file.FileType.ToLowerInvariant() == ".json")
            {
                // JSON序列化
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                fileContent = System.Text.Json.JsonSerializer.Serialize(exportDto, jsonOptions);
            }
            else if (file.FileType.ToLowerInvariant() == ".xml")
            {
                // XML序列化（暂时不支持，显示提示）
                await NotificationService.ShowErrorAsync("格式不支持", "XML格式导出功能将在后续版本中实现，请选择JSON格式");
                return;
            }
            else
            {
                await NotificationService.ShowErrorAsync("文件类型错误", "不支持的文件类型，请选择JSON格式");
                return;
            }

            // 6. 写入文件
            await Windows.Storage.FileIO.WriteTextAsync(file, fileContent);

            // 7. 显示成功消息
            string fileSize = await FilePickerService.GetFileSizeStringAsync(file);
            string exportInfo = $"试卷名称：{exam.Name}\n" +
                              $"导出级别：{GetExportLevelDisplayName(exportLevel)}\n" +
                              $"模块数量：{exam.Modules.Count}\n" +
                              $"题目总数：{exam.Modules.Sum(m => m.Questions.Count)}\n" +
                              $"保存位置：{file.Path}\n" +
                              $"文件大小：{fileSize}";

            await NotificationService.ShowSuccessAsync("导出成功", exportInfo);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", $"导出试卷时发生错误：{ex.Message}");
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

            // 初始化试卷数量
            ExamCount = Exams.Count;
        }
        catch (Exception ex)
        {
            // 如果加载失败，创建示例数据
            CreateSampleData();
            await NotificationService.ShowErrorAsync("数据加载失败", $"无法加载保存的数据：{ex.Message}");

            // 初始化试卷数量
            ExamCount = Exams.Count;
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

    /// <summary>
    /// 显示导出级别选择对话框
    /// </summary>
    /// <returns>选择的导出级别</returns>
    private async Task<ExportLevel> ShowExportLevelSelectionAsync()
    {
        // 使用NotificationService显示选择对话框
        string? selectedLevel = await NotificationService.ShowSelectionDialogAsync(
            "选择导出级别",
            new[] { "基本信息", "完整内容（不含答案）", "完整内容（含答案）" });

        return selectedLevel switch
        {
            "基本信息" => ExportLevel.Basic,
            "完整内容（不含答案）" => ExportLevel.WithoutAnswers,
            "完整内容（含答案）" => ExportLevel.Complete,
            _ => ExportLevel.WithoutAnswers // 默认不含答案
        };
    }

    /// <summary>
    /// 获取导出级别的显示名称
    /// </summary>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>显示名称</returns>
    private static string GetExportLevelDisplayName(ExportLevel exportLevel)
    {
        return exportLevel switch
        {
            ExportLevel.Basic => "基本信息",
            ExportLevel.WithoutAnswers => "完整内容（不含答案）",
            ExportLevel.Complete => "完整内容（含答案）",
            _ => "未知级别"
        };
    }

    /// <summary>
    /// 检查版本兼容性
    /// </summary>
    /// <param name="exportVersion">导出版本</param>
    /// <returns>是否兼容</returns>
    private static bool IsCompatibleVersion(string? exportVersion)
    {
        if (string.IsNullOrWhiteSpace(exportVersion))
        {
            return false;
        }

        // 支持的版本列表
        var supportedVersions = new[] { "1.0" };

        return supportedVersions.Contains(exportVersion);
    }
}
