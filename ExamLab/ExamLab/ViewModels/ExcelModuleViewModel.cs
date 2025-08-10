using System.Reactive;
using ExamLab.Models;
using ReactiveUI;

namespace ExamLab.ViewModels;

/// <summary>
/// Excel模块ViewModel
/// </summary>
public class ExcelModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    public ExcelModuleViewModel(ExamModule module) : base(module)
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
            Name = "Excel操作配置",
            Description = "配置Excel相关操作",
            ModuleType = ModuleType.Excel,
            ScoringQuestionId = SelectedQuestion.Id
        };

        // 添加Excel特有的参数（类似Windows操作，但用于Excel Interop判断）
        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "OperationType",
            DisplayName = "操作类型",
            Description = "选择Excel操作类型",
            Type = ParameterType.Enum,
            IsRequired = true,
            Order = 1,
            EnumOptions = "创建工作簿,格式化单元格,插入图表,创建公式,数据筛选"
        });

        operationPoint.Parameters.Add(new ConfigurationParameter
        {
            Name = "TargetRange",
            DisplayName = "目标区域",
            Description = "操作的目标单元格区域",
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
        // Excel 模块目前只有一种操作类型，直接调用 AddOperationPoint
        AddOperationPoint();
    }
}
