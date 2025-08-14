using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ExamLab.Models;

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

    public CSharpModuleViewModel(ExamModule module) : base(module)
    {
        // C#模块不需要操作点管理，直接使用Question的ProgramInput和ExpectedOutput属性

        // 初始化命令（为了UI兼容性，但不执行任何操作）
        AddOperationPointByTypeCommand = ReactiveCommand.Create<string>(AddOperationPointByType);
        EditOperationPointCommand = ReactiveCommand.Create<OperationPoint>(EditOperationPoint);

        // 初始化填空处管理命令
        AddCodeBlankCommand = ReactiveCommand.Create(AddCodeBlank);
        DeleteCodeBlankCommand = ReactiveCommand.Create<CodeBlank>(DeleteCodeBlank);
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
            Description = "// 请输入需要学生填写的代码",
            DetailedDescription = string.Empty,
            Order = SelectedQuestion.CodeBlanks.Count + 1
        };

        SelectedQuestion.CodeBlanks.Add(newCodeBlank);
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
    }
}
