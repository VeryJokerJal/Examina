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
    public CSharpModuleViewModel(ExamModule module) : base(module)
    {
        // C#模块不需要操作点管理，直接使用Question的ProgramInput和ExpectedOutput属性
    }

    protected override void AddOperationPoint()
    {
        // C#模块不再使用操作点，此方法保留以满足基类要求但不执行任何操作
        // 实际的程序配置通过Question.ProgramInput和Question.ExpectedOutput属性管理
    }
}
