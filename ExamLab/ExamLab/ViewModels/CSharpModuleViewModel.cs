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
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    public CSharpModuleViewModel(ExamModule module) : base(module)
    {
        // 初始化命令
        AddOperationPointByTypeCommand = ReactiveCommand.Create<string>(AddOperationPointByType);
    }

    protected override void AddOperationPoint()
    {
        if (SelectedQuestion == null)
        {
            SetError("请先选择一个题目");
            return;
        }

        OperationPoint operationPoint = new()
        {
            Name = "C#程序配置",
            Description = "配置C#程序的参数和输出",
            ModuleType = ModuleType.CSharp,
            ScoringQuestionId = SelectedQuestion.Id
        };

        // 添加C#特有的参数
        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "ProgramInput",
            DisplayName = "程序参数输入",
            Description = "程序运行时的输入参数",
            Type = ParameterType.Text,
            IsRequired = false,
            Order = 1
        });

        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "ExpectedOutput",
            DisplayName = "程序控制台输出",
            Description = "程序预期的控制台输出结果",
            Type = ParameterType.Text,
            IsRequired = true,
            Order = 2
        });

        SelectedQuestion.OperationPoints.Add(operationPoint);
        SelectedOperationPoint = operationPoint;

        ClearError();
    }

    private void AddOperationPointByType(string operationType)
    {
        // C# 模块目前只有一种操作类型，直接调用 AddOperationPoint
        AddOperationPoint();
    }
}
