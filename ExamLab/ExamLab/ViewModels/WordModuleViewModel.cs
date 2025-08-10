using System.Reactive;
using ExamLab.Models;
using ReactiveUI;

namespace ExamLab.ViewModels;

/// <summary>
/// Word模块ViewModel
/// </summary>
public class WordModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    public WordModuleViewModel(ExamModule module) : base(module)
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
            Name = "Word操作配置",
            Description = "配置Word相关操作",
            ModuleType = ModuleType.Word,
            ScoringQuestionId = SelectedQuestion.Id
        };

        // 添加Word特有的参数（类似Windows操作，但用于Word Interop判断）
        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "OperationType",
            DisplayName = "操作类型",
            Description = "选择Word操作类型",
            Type = ParameterType.Enum,
            IsRequired = true,
            Order = 1,
            EnumOptions = "创建文档,格式化文本,插入表格,插入图片,创建页眉页脚"
        });

        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "Content",
            DisplayName = "内容",
            Description = "操作相关的内容",
            Type = ParameterType.Text,
            IsRequired = false,
            Order = 2
        });

        SelectedQuestion.OperationPoints.Add(operationPoint);
        SelectedOperationPoint = operationPoint;

        ClearError();
    }

    private void AddOperationPointByType(string operationType)
    {
        // Word 模块目前只有一种操作类型，直接调用 AddOperationPoint
        AddOperationPoint();
    }
}
