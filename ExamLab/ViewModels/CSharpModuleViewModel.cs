using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ExamLab.Models;
using ExamLab.Services;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace ExamLab.ViewModels;

/// <summary>
/// C#模块ViewModel
/// </summary>
public class CSharpModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 添加指定类型操作点命令（C#模块不使用，但为了UI兼容性保留）
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    /// <summary>
    /// 编辑操作点命令（C#模块不使用，但为了UI兼容性保留）
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> EditOperationPointCommand { get; }

    /// <summary>
    /// 添加填空处命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddCodeBlankCommand { get; }

    /// <summary>
    /// 删除填空处命令
    /// </summary>
    public ReactiveCommand<CodeBlank, Unit> DeleteCodeBlankCommand { get; }

    /// <summary>
    /// 选择C#代码文件命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectCodeFileCommand { get; }

    /// <summary>
    /// 清除C#代码文件路径命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearCodeFilePathCommand { get; }

    /// <summary>
    /// 保存模块描述命令（代理到MainWindowViewModel）
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveModuleDescriptionCommand { get; }

    /// <summary>
    /// 重置模块描述命令（代理到MainWindowViewModel）
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetModuleDescriptionCommand { get; }

    /// <summary>
    /// 生成模板代码命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> GenerateTemplateCodeCommand { get; }

    /// <summary>
    /// 清除模板代码命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearTemplateCodeCommand { get; }

    /// <summary>
    /// 从CodeBlanks生成模板代码命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> GenerateTemplateFromBlanksCommand { get; }

    private readonly MainWindowViewModel? _mainWindowViewModel;

    public CSharpModuleViewModel(ExamModule module, MainWindowViewModel? mainWindowViewModel = null) : base(module)
    {
        _mainWindowViewModel = mainWindowViewModel;

        // C#模块不需要操作点管理，直接使用Question的ProgramInput和ExpectedOutput属性

        // 初始化命令（为了UI兼容性，但不执行任何操作）
        AddOperationPointByTypeCommand = ReactiveCommand.Create<string>(AddOperationPointByType);
        EditOperationPointCommand = ReactiveCommand.Create<OperationPoint>(EditOperationPoint);

        // 初始化填空处管理命令
        AddCodeBlankCommand = ReactiveCommand.Create(AddCodeBlank);
        DeleteCodeBlankCommand = ReactiveCommand.Create<CodeBlank>(DeleteCodeBlank);

        // 初始化文件选择命令
        SelectCodeFileCommand = ReactiveCommand.CreateFromTask(SelectCodeFileAsync);
        ClearCodeFilePathCommand = ReactiveCommand.Create(ClearCodeFilePath);

        // 初始化模块描述命令（代理到MainWindowViewModel）
        SaveModuleDescriptionCommand = ReactiveCommand.CreateFromTask(SaveModuleDescriptionAsync);
        ResetModuleDescriptionCommand = ReactiveCommand.CreateFromTask(ResetModuleDescriptionAsync);

        // 初始化模板代码相关命令
        GenerateTemplateCodeCommand = ReactiveCommand.Create(GenerateTemplateCode);
        ClearTemplateCodeCommand = ReactiveCommand.Create(ClearTemplateCode);
        GenerateTemplateFromBlanksCommand = ReactiveCommand.Create(GenerateTemplateFromBlanks);
    }

    protected override void AddOperationPoint()
    {
        // C#模块不再使用操作点，此方法保留以满足基类要求但不执行任何操作
        // 实际的程序配置通过Question.ProgramInput和Question.ExpectedOutput属性管理
    }

    /// <summary>
    /// 添加指定类型操作点（C#模块不使用，空实现）
    /// </summary>
    /// <param name="operationType">操作类型</param>
    private void AddOperationPointByType(string operationType)
    {
        // C#模块不使用操作点，空实现
    }

    /// <summary>
    /// 编辑操作点（C#模块不使用，空实现）
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    private void EditOperationPoint(OperationPoint operationPoint)
    {
        // C#模块不使用操作点，空实现
    }

    /// <summary>
    /// 添加填空处
    /// </summary>
    private void AddCodeBlank()
    {
        if (SelectedQuestion == null) return;

        CodeBlank newCodeBlank = new()
        {
            Description = GenerateTemplateCodeWithBlank(SelectedQuestion.CodeBlanks.Count + 1),
            DetailedDescription = "请在此处填写代码实现",
            Order = SelectedQuestion.CodeBlanks.Count + 1,
            StandardAnswer = "// 请填写标准答案"
        };

        SelectedQuestion.CodeBlanks.Add(newCodeBlank);

        // 如果是代码补全类型，自动更新TemplateCode
        if (SelectedQuestion.CSharpQuestionType == CSharpQuestionType.CodeCompletion)
        {
            GenerateTemplateFromBlanks();
        }
    }

    /// <summary>
    /// 生成包含NotImplementedException的模板代码
    /// </summary>
    /// <param name="blankNumber">填空序号</param>
    /// <returns>模板代码</returns>
    private static string GenerateTemplateCodeWithBlank(int blankNumber)
    {
        return $@"// 代码填空 {blankNumber}
public class CodeBlank{blankNumber}
{{
    public void Method{blankNumber}()
    {{
        throw new NotImplementedException();
    }}
}}";
    }

    /// <summary>
    /// 为选中的CodeBlank生成完整的模板代码
    /// </summary>
    /// <param name="codeBlank">代码填空对象</param>
    /// <param name="className">类名</param>
    /// <param name="methodName">方法名</param>
    /// <param name="returnType">返回类型</param>
    /// <param name="parameters">参数列表</param>
    public static void GenerateCompleteTemplateCode(CodeBlank codeBlank, string className = "Solution",
        string methodName = "Solve", string returnType = "void", string parameters = "")
    {
        string templateCode = $@"using System;

public class {className}
{{
    /// <summary>
    /// {codeBlank.DetailedDescription}
    /// </summary>
    public {returnType} {methodName}({parameters})
    {{
        throw new NotImplementedException();
    }}
}}";

        codeBlank.Description = templateCode;
    }

    /// <summary>
    /// 为所有CodeBlanks生成统一的模板代码结构
    /// </summary>
    public void GenerateUnifiedTemplateCode()
    {
        if (SelectedQuestion?.CodeBlanks == null || SelectedQuestion.CodeBlanks.Count == 0)
            return;

        string unifiedTemplate = @"using System;

public class Solution
{";

        for (int i = 0; i < SelectedQuestion.CodeBlanks.Count; i++)
        {
            CodeBlank blank = SelectedQuestion.CodeBlanks[i];
            unifiedTemplate += $@"

    /// <summary>
    /// 填空 {i + 1}: {blank.DetailedDescription}
    /// </summary>
    public void Method{i + 1}()
    {{
        throw new NotImplementedException();
    }}";
        }

        unifiedTemplate += @"
}";

        // 将统一的模板代码设置给第一个CodeBlank
        if (SelectedQuestion.CodeBlanks.Count > 0)
        {
            SelectedQuestion.CodeBlanks[0].Description = unifiedTemplate;
        }
    }

    /// <summary>
    /// 删除填空处
    /// </summary>
    /// <param name="codeBlank">要删除的填空处</param>
    private void DeleteCodeBlank(CodeBlank codeBlank)
    {
        if (SelectedQuestion == null || codeBlank == null) return;

        SelectedQuestion.CodeBlanks.Remove(codeBlank);

        // 重新排序剩余的填空处
        for (int i = 0; i < SelectedQuestion.CodeBlanks.Count; i++)
        {
            SelectedQuestion.CodeBlanks[i].Order = i + 1;
        }

        // 如果是代码补全类型，自动更新TemplateCode
        if (SelectedQuestion.CSharpQuestionType == CSharpQuestionType.CodeCompletion)
        {
            if (SelectedQuestion.CodeBlanks.Count > 0)
            {
                GenerateTemplateFromBlanks();
            }
            else
            {
                // 如果没有填空了，清除TemplateCode或生成标准模板
                GenerateTemplateCode();
            }
        }
    }

    /// <summary>
    /// 选择C#代码文件
    /// </summary>
    private async Task SelectCodeFileAsync()
    {
        if (SelectedQuestion == null) return;

        try
        {
            // 定义支持的C#文件类型
            List<string> csharpFileTypes = [".cs", ".csx"];

            // 打开文件选择对话框
            Windows.Storage.StorageFile? selectedFile = await FilePickerService.PickSingleFileAsync(
                csharpFileTypes,
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary);

            if (selectedFile != null)
            {
                // 验证文件是否为有效的C#代码文件
                if (IsValidCSharpFile(selectedFile))
                {
                    SelectedQuestion.CodeFilePath = selectedFile.Path;
                    await NotificationService.ShowSuccessAsync("文件选择成功", $"已选择C#代码文件：{selectedFile.Name}");
                }
                else
                {
                    await NotificationService.ShowErrorAsync("文件类型错误", "请选择有效的C#代码文件（.cs 或 .csx）");
                }
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件选择失败", $"选择C#代码文件时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除C#代码文件路径
    /// </summary>
    private void ClearCodeFilePath()
    {
        if (SelectedQuestion != null)
        {
            SelectedQuestion.CodeFilePath = null;
        }
    }

    /// <summary>
    /// 验证是否为有效的C#代码文件
    /// </summary>
    /// <param name="file">要验证的文件</param>
    /// <returns>是否为有效的C#代码文件</returns>
    private static bool IsValidCSharpFile(Windows.Storage.StorageFile file)
    {
        if (file == null) return false;

        string extension = Path.GetExtension(file.Name).ToLowerInvariant();
        return extension == ".cs" || extension == ".csx";
    }

    /// <summary>
    /// 验证文件路径是否有效
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为有效的文件路径</returns>
    public static bool IsValidCodeFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true; // 空路径是允许的

        try
        {
            // 检查路径格式是否有效
            string fullPath = Path.GetFullPath(filePath);

            // 检查文件扩展名
            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            if (extension != ".cs" && extension != ".csx")
                return false;

            // 检查文件是否存在（可选验证）
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 保存模块描述（代理到MainWindowViewModel）
    /// </summary>
    private Task SaveModuleDescriptionAsync()
    {
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.SaveModuleDescriptionCommand.Execute().Subscribe();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 重置模块描述（代理到MainWindowViewModel）
    /// </summary>
    private Task ResetModuleDescriptionAsync()
    {
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.ResetModuleDescriptionCommand.Execute().Subscribe();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成标准模板代码
    /// </summary>
    private void GenerateTemplateCode()
    {
        if (SelectedQuestion == null) return;

        string templateCode = @"using System;

public class Solution
{
    /// <summary>
    /// 请在此处实现您的代码
    /// </summary>
    public void Solve()
    {
        throw new NotImplementedException();
    }
}";

        SelectedQuestion.TemplateCode = templateCode;
    }

    /// <summary>
    /// 清除模板代码
    /// </summary>
    private void ClearTemplateCode()
    {
        if (SelectedQuestion != null)
        {
            SelectedQuestion.TemplateCode = null;
        }
    }

    /// <summary>
    /// 从CodeBlanks生成模板代码
    /// </summary>
    private void GenerateTemplateFromBlanks()
    {
        if (SelectedQuestion?.CodeBlanks == null || SelectedQuestion.CodeBlanks.Count == 0)
        {
            GenerateTemplateCode(); // 如果没有CodeBlanks，生成标准模板
            return;
        }

        string templateCode = @"using System;

public class Solution
{";

        for (int i = 0; i < SelectedQuestion.CodeBlanks.Count; i++)
        {
            CodeBlank blank = SelectedQuestion.CodeBlanks[i];
            templateCode += $@"

    /// <summary>
    /// 填空 {i + 1}: {blank.DetailedDescription}
    /// </summary>
    public void Method{i + 1}()
    {{
        throw new NotImplementedException();
    }}";
        }

        templateCode += @"
}";

        SelectedQuestion.TemplateCode = templateCode;
    }
}
