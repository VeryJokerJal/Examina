using System;
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
/// 模块管理ViewModel
/// </summary>
public class ModuleManagementViewModel : ViewModelBase
{
    /// <summary>
    /// 当前选中的试卷
    /// </summary>
    [Reactive] public Exam? SelectedExam { get; set; }

    /// <summary>
    /// 当前选中的模块
    /// </summary>
    [Reactive] public ExamModule? SelectedModule { get; set; }

    /// <summary>
    /// 总操作点数量
    /// </summary>
    [Reactive] public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 添加模块命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddModuleCommand { get; }

    /// <summary>
    /// 删除模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> DeleteModuleCommand { get; }

    /// <summary>
    /// 上移模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> MoveModuleUpCommand { get; }

    /// <summary>
    /// 下移模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> MoveModuleDownCommand { get; }

    /// <summary>
    /// 复制模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> CopyModuleCommand { get; }

    /// <summary>
    /// 进入模块配置命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> EnterModuleConfigCommand { get; }

    /// <summary>
    /// 导出模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> ExportModuleCommand { get; }

    /// <summary>
    /// 重置模块命令
    /// </summary>
    public ReactiveCommand<ExamModule, Unit> ResetModuleCommand { get; }

    /// <summary>
    /// 保存配置命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveConfigurationCommand { get; }

    public ModuleManagementViewModel()
    {
        Title = "模块管理";

        // 初始化命令
        AddModuleCommand = ReactiveCommand.CreateFromTask(AddModuleAsync);
        DeleteModuleCommand = ReactiveCommand.CreateFromTask<ExamModule>(DeleteModuleAsync);
        MoveModuleUpCommand = ReactiveCommand.Create<ExamModule>(MoveModuleUp);
        MoveModuleDownCommand = ReactiveCommand.Create<ExamModule>(MoveModuleDown);
        CopyModuleCommand = ReactiveCommand.CreateFromTask<ExamModule>(CopyModuleAsync);
        EnterModuleConfigCommand = ReactiveCommand.Create<ExamModule>(EnterModuleConfig);
        ExportModuleCommand = ReactiveCommand.CreateFromTask<ExamModule>(ExportModuleAsync);
        ResetModuleCommand = ReactiveCommand.CreateFromTask<ExamModule>(ResetModuleAsync);
        SaveConfigurationCommand = ReactiveCommand.CreateFromTask(SaveConfigurationAsync);

        // 监听选中模块变化，更新操作点统计
        _ = this.WhenAnyValue(x => x.SelectedModule)
            .Where(module => module != null)
            .Subscribe(module => UpdateOperationPointsCount(module!));
    }

    private async Task AddModuleAsync()
    {
        if (SelectedExam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "请先选择一个试卷");
            return;
        }

        string? moduleType = await NotificationService.ShowSelectionDialogAsync(
            "选择模块类型",
            Enum.GetNames<ModuleType>());

        if (moduleType == null)
        {
            return;
        }

        if (!Enum.TryParse(moduleType, out ModuleType type))
        {
            await NotificationService.ShowErrorAsync("错误", "无效的模块类型");
            return;
        }

        string? moduleName = await NotificationService.ShowInputDialogAsync(
            "输入模块名称",
            "请输入模块名称",
            GetDefaultModuleName(type));

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return;
        }

        ExamModule newModule = new()
        {
            Name = moduleName,
            Type = type,
            Description = GetDefaultModuleDescription(type),
            Score = 20,
            Order = SelectedExam.Modules.Count + 1,
            IsEnabled = true
        };

        SelectedExam.Modules.Add(newModule);
        SelectedModule = newModule;
    }

    private async Task DeleteModuleAsync(ExamModule module)
    {
        if (module == null || SelectedExam == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowDeleteConfirmationAsync(module.Name);
        if (!confirmed)
        {
            return;
        }

        _ = SelectedExam.Modules.Remove(module);

        // 重新排序
        ReorderModules();

        if (SelectedModule == module)
        {
            SelectedModule = SelectedExam.Modules.FirstOrDefault();
        }
    }

    private void MoveModuleUp(ExamModule module)
    {
        if (module == null || SelectedExam == null)
        {
            return;
        }

        int currentIndex = SelectedExam.Modules.IndexOf(module);
        if (currentIndex > 0)
        {
            SelectedExam.Modules.Move(currentIndex, currentIndex - 1);
            ReorderModules();
        }
    }

    private void MoveModuleDown(ExamModule module)
    {
        if (module == null || SelectedExam == null)
        {
            return;
        }

        int currentIndex = SelectedExam.Modules.IndexOf(module);
        if (currentIndex < SelectedExam.Modules.Count - 1)
        {
            SelectedExam.Modules.Move(currentIndex, currentIndex + 1);
            ReorderModules();
        }
    }

    private async Task CopyModuleAsync(ExamModule module)
    {
        if (module == null || SelectedExam == null)
        {
            return;
        }

        string? newName = await NotificationService.ShowInputDialogAsync(
            "复制模块",
            "请输入新模块名称",
            $"{module.Name} - 副本");

        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        ExamModule copiedModule = new()
        {
            Name = newName,
            Type = module.Type,
            Description = module.Description,
            Score = module.Score,
            Order = SelectedExam.Modules.Count + 1,
            IsEnabled = module.IsEnabled
        };

        // 复制题目和操作点
        foreach (Question question in module.Questions)
        {
            Question copiedQuestion = new()
            {
                Title = question.Title,
                Content = question.Content,
                Order = question.Order,
                IsEnabled = question.IsEnabled
            };

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
            copiedModule.Questions.Add(copiedQuestion);
        }

        SelectedExam.Modules.Add(copiedModule);
        SelectedModule = copiedModule;
    }

    private void EnterModuleConfig(ExamModule module)
    {
        if (module == null)
        {
            return;
        }

        // 这里可以触发导航到具体的模块配置界面
        // 例如：NavigationService.NavigateToModuleConfig(module);

        // 暂时显示提示
        _ = NotificationService.ShowSuccessAsync("导航", $"即将进入 {module.Name} 模块配置界面");
    }

    private async Task ExportModuleAsync(ExamModule module)
    {
        if (module == null)
        {
            return;
        }

        try
        {
            // 创建临时试卷只包含当前模块
            Exam tempExam = new()
            {
                Name = $"{module.Name} 模块配置",
                Description = $"导出的 {module.Name} 模块配置",
                TotalScore = module.Score,
                Duration = 60
            };
            tempExam.Modules.Add(module);

            string exportData = await ExportService.ExportExamToJsonAsync(tempExam);

            // 这里应该保存到文件，暂时显示在对话框中
            await NotificationService.ShowSuccessAsync("导出成功", $"模块 {module.Name} 已导出");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", ex.Message);
        }
    }

    private async Task ResetModuleAsync(ExamModule module)
    {
        if (module == null)
        {
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "重置模块",
            $"确定要重置模块 '{module.Name}' 吗？这将清除所有题目和操作点配置。");

        if (!confirmed)
        {
            return;
        }

        module.Questions.Clear();
        module.Description = GetDefaultModuleDescription(module.Type);
        module.Score = 20;

        UpdateOperationPointsCount(module);
    }

    private async Task SaveConfigurationAsync()
    {
        if (SelectedExam == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有可保存的试卷");
            return;
        }

        try
        {
            // 验证试卷配置
            ValidationResult result = ValidationService.ValidateExam(SelectedExam);
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

    private void UpdateOperationPointsCount(ExamModule module)
    {
        TotalOperationPoints = module.Questions.Sum(q => q.OperationPoints.Count);
    }

    private void ReorderModules()
    {
        if (SelectedExam == null)
        {
            return;
        }

        for (int i = 0; i < SelectedExam.Modules.Count; i++)
        {
            SelectedExam.Modules[i].Order = i + 1;
        }
    }

    private string GetDefaultModuleName(ModuleType type)
    {
        return type switch
        {
            ModuleType.Windows => "Windows操作",
            ModuleType.CSharp => "C#编程",
            ModuleType.PowerPoint => "PowerPoint操作",
            ModuleType.Excel => "Excel操作",
            ModuleType.Word => "Word操作",
            _ => "新模块"
        };
    }

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
