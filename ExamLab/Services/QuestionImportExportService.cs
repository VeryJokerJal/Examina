using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ExamLab.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ExamLab.Services;

/// <summary>
/// 题目导入导出服务
/// </summary>
public static class QuestionImportExportService
{
    /// <summary>
    /// 导出题目到JSON文件
    /// </summary>
    /// <param name="questions">要导出的题目列表</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>导出是否成功</returns>
    public static async Task<bool> ExportQuestionsAsync(IEnumerable<Question> questions, ModuleType moduleType)
    {
        try
        {
            if (!questions.Any())
            {
                await NotificationService.ShowErrorAsync("导出失败", "没有可导出的题目");
                return false;
            }

            // 创建文件保存选择器
            FileSavePicker savePicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"{GetModuleTypeName(moduleType)}_题目库_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            savePicker.FileTypeChoices.Add("JSON文件", new List<string>() { ".json" });

            // 获取当前窗口句柄
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file == null) return false;

            // 创建导出数据结构
            QuestionExportData exportData = new()
            {
                ModuleType = moduleType,
                ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Version = "1.0",
                Questions = questions.Select(CreateQuestionExportItem).ToList()
            };

            // 序列化为JSON
            string jsonContent = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // 写入文件
            await FileIO.WriteTextAsync(file, jsonContent);

            await NotificationService.ShowSuccessAsync("导出成功", $"已导出{questions.Count()}个题目到文件：{file.Name}");
            return true;
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导出失败", $"导出题目时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从JSON文件导入题目
    /// </summary>
    /// <param name="targetModule">目标模块</param>
    /// <returns>导入的题目列表</returns>
    public static async Task<List<Question>?> ImportQuestionsAsync(ExamModule targetModule)
    {
        try
        {
            // 创建文件选择器
            FileOpenPicker openPicker = new()
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".json");

            // 获取当前窗口句柄
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file == null) return null;

            // 读取文件内容
            string jsonContent = await FileIO.ReadTextAsync(file);
            
            // 反序列化
            QuestionExportData? exportData = JsonSerializer.Deserialize<QuestionExportData>(jsonContent);
            if (exportData == null)
            {
                await NotificationService.ShowErrorAsync("导入失败", "文件格式不正确");
                return null;
            }

            // 检查模块类型兼容性
            if (exportData.ModuleType != targetModule.Type)
            {
                bool confirmed = await NotificationService.ShowConfirmationAsync(
                    "模块类型不匹配",
                    $"导入文件的模块类型为{GetModuleTypeName(exportData.ModuleType)}，当前模块类型为{GetModuleTypeName(targetModule.Type)}。是否继续导入？");
                
                if (!confirmed) return null;
            }

            // 转换为Question对象
            List<Question> importedQuestions = exportData.Questions.Select(item => CreateQuestionFromExportItem(item, targetModule)).ToList();

            await NotificationService.ShowSuccessAsync("导入成功", $"已从文件{file.Name}导入{importedQuestions.Count}个题目");
            return importedQuestions;
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("导入失败", $"导入题目时发生错误：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 创建题目导出项
    /// </summary>
    private static QuestionExportItem CreateQuestionExportItem(Question question)
    {
        return new QuestionExportItem
        {
            Title = question.Title,
            Content = question.Content,
            Order = question.Order,
            IsEnabled = question.IsEnabled,
            CreatedTime = question.CreatedTime,
            CSharpQuestionType = question.CSharpQuestionType,
            CodeFilePath = question.CodeFilePath,
            CSharpDirectScore = question.CSharpDirectScore,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            OperationPoints = question.OperationPoints.Select(CreateOperationPointExportItem).ToList()
        };
    }

    /// <summary>
    /// 创建操作点导出项
    /// </summary>
    private static OperationPointExportItem CreateOperationPointExportItem(OperationPoint operationPoint)
    {
        return new OperationPointExportItem
        {
            Name = operationPoint.Name,
            Description = operationPoint.Description,
            Score = operationPoint.Score,
            Order = operationPoint.Order,
            IsEnabled = operationPoint.IsEnabled,
            ModuleType = operationPoint.ModuleType,
            WindowsOperationType = operationPoint.WindowsOperationType,
            PowerPointKnowledgeType = operationPoint.PowerPointKnowledgeType,
            WordKnowledgeType = operationPoint.WordKnowledgeType,
            ExcelKnowledgeType = operationPoint.ExcelKnowledgeType,
            ScoringQuestionId = operationPoint.ScoringQuestionId,
            Parameters = operationPoint.Parameters.Select(CreateParameterExportItem).ToList()
        };
    }

    /// <summary>
    /// 创建参数导出项
    /// </summary>
    private static ParameterExportItem CreateParameterExportItem(ConfigurationParameter parameter)
    {
        return new ParameterExportItem
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
    }

    /// <summary>
    /// 从导出项创建题目
    /// </summary>
    private static Question CreateQuestionFromExportItem(QuestionExportItem item, ExamModule targetModule)
    {
        Question question = new()
        {
            Id = IdGeneratorService.GenerateQuestionId(),
            Title = item.Title,
            Content = item.Content,
            Order = targetModule.Questions.Count + item.Order,
            IsEnabled = item.IsEnabled,
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            CSharpQuestionType = item.CSharpQuestionType,
            CodeFilePath = item.CodeFilePath,
            CSharpDirectScore = item.CSharpDirectScore,
            ProgramInput = item.ProgramInput,
            ExpectedOutput = item.ExpectedOutput
        };

        // 添加操作点
        foreach (OperationPointExportItem opItem in item.OperationPoints)
        {
            OperationPoint operationPoint = CreateOperationPointFromExportItem(opItem, targetModule.Type);
            question.OperationPoints.Add(operationPoint);
        }

        return question;
    }

    /// <summary>
    /// 从导出项创建操作点
    /// </summary>
    private static OperationPoint CreateOperationPointFromExportItem(OperationPointExportItem item, ModuleType moduleType)
    {
        OperationPoint operationPoint = new()
        {
            Id = IdGeneratorService.GenerateOperationId(),
            Name = item.Name,
            Description = item.Description,
            Score = item.Score,
            Order = item.Order,
            IsEnabled = item.IsEnabled,
            ModuleType = moduleType,
            WindowsOperationType = item.WindowsOperationType,
            PowerPointKnowledgeType = item.PowerPointKnowledgeType,
            WordKnowledgeType = item.WordKnowledgeType,
            ExcelKnowledgeType = item.ExcelKnowledgeType,
            ScoringQuestionId = item.ScoringQuestionId,
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 添加参数
        foreach (ParameterExportItem paramItem in item.Parameters)
        {
            ConfigurationParameter parameter = CreateParameterFromExportItem(paramItem);
            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    /// <summary>
    /// 从导出项创建参数
    /// </summary>
    private static ConfigurationParameter CreateParameterFromExportItem(ParameterExportItem item)
    {
        return new ConfigurationParameter
        {
            Id = IdGeneratorService.GenerateParameterId(),
            Name = item.Name,
            DisplayName = item.DisplayName,
            Description = item.Description,
            Type = item.Type,
            Value = item.Value,
            DefaultValue = item.DefaultValue,
            IsRequired = item.IsRequired,
            Order = item.Order,
            EnumOptions = item.EnumOptions,
            ValidationRule = item.ValidationRule,
            ValidationErrorMessage = item.ValidationErrorMessage,
            MinValue = item.MinValue,
            MaxValue = item.MaxValue,
            IsEnabled = item.IsEnabled
        };
    }

    /// <summary>
    /// 获取模块类型名称
    /// </summary>
    private static string GetModuleTypeName(ModuleType type)
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
}

/// <summary>
/// 题目导出数据结构
/// </summary>
public class QuestionExportData
{
    public ModuleType ModuleType { get; set; }
    public string ExportTime { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<QuestionExportItem> Questions { get; set; } = new();
}

/// <summary>
/// 题目导出项
/// </summary>
public class QuestionExportItem
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsEnabled { get; set; }
    public string CreatedTime { get; set; } = string.Empty;
    public CSharpQuestionType CSharpQuestionType { get; set; }
    public string? CodeFilePath { get; set; }
    public double CSharpDirectScore { get; set; }
    public string? ProgramInput { get; set; }
    public string? ExpectedOutput { get; set; }
    public List<OperationPointExportItem> OperationPoints { get; set; } = new();
}

/// <summary>
/// 操作点导出项
/// </summary>
public class OperationPointExportItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int Order { get; set; }
    public bool IsEnabled { get; set; }
    public ModuleType ModuleType { get; set; }
    public WindowsOperationType? WindowsOperationType { get; set; }
    public PowerPointKnowledgeType? PowerPointKnowledgeType { get; set; }
    public WordKnowledgeType? WordKnowledgeType { get; set; }
    public ExcelKnowledgeType? ExcelKnowledgeType { get; set; }
    public string? ScoringQuestionId { get; set; }
    public List<ParameterExportItem> Parameters { get; set; } = new();
}

/// <summary>
/// 参数导出项
/// </summary>
public class ParameterExportItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ParameterType Type { get; set; }
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public string? EnumOptions { get; set; }
    public string? ValidationRule { get; set; }
    public string? ValidationErrorMessage { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public bool IsEnabled { get; set; }
}
