using System.Reactive;
using ExamLab.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 操作点编辑对话框ViewModel
/// </summary>
public class OperationPointEditDialogViewModel : ViewModelBase
{
    /// <summary>
    /// 要编辑的操作点
    /// </summary>
    [Reactive] public OperationPoint? OperationPoint { get; set; }

    /// <summary>
    /// 是否保存成功
    /// </summary>
    [Reactive] public bool IsSaved { get; set; }

    /// <summary>
    /// 保存命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public OperationPointEditDialogViewModel()
    {
        // 初始化命令
        SaveCommand = ReactiveCommand.Create(Save);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    public OperationPointEditDialogViewModel(OperationPoint operationPoint) : this()
    {
        OperationPoint = operationPoint;
    }

    /// <summary>
    /// 验证参数
    /// </summary>
    /// <returns>验证是否通过</returns>
    public bool ValidateParameters()
    {
        if (OperationPoint == null)
        {
            SetError("操作点不能为空");
            return false;
        }

        // 验证必填参数
        foreach (ConfigurationParameter parameter in OperationPoint.Parameters)
        {
            if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.Value))
            {
                SetError($"参数 '{parameter.DisplayName}' 是必填项");
                return false;
            }

            // 验证数字类型参数
            if (parameter.Type == ParameterType.Number && !string.IsNullOrWhiteSpace(parameter.Value))
            {
                if (!int.TryParse(parameter.Value, out int numValue))
                {
                    SetError($"参数 '{parameter.DisplayName}' 必须是有效的数字");
                    return false;
                }

                if (parameter.MinValue.HasValue && numValue < parameter.MinValue.Value)
                {
                    // 如果是编号参数且值为-1，则允许（-1代表任意一个）
                    bool isIndexParameter = IsIndexParameter(parameter.Name);
                    if (!(isIndexParameter && numValue == -1))
                    {
                        SetError($"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}");
                        return false;
                    }
                }

                if (parameter.MaxValue.HasValue && numValue > parameter.MaxValue.Value)
                {
                    SetError($"参数 '{parameter.DisplayName}' 不能大于 {parameter.MaxValue.Value}");
                    return false;
                }
            }

            // 验证枚举类型参数
            if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(parameter.Value) && !parameter.EnumOptionsList.Contains(parameter.Value))
                {
                    SetError($"参数 '{parameter.DisplayName}' 的值必须是以下选项之一：{string.Join(", ", parameter.EnumOptionsList)}");
                    return false;
                }
            }
        }

        ClearError();
        return true;
    }

    /// <summary>
    /// 检查参数是否为编号类型
    /// </summary>
    private static bool IsIndexParameter(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        string[] indexPatterns =
        {
            "SlideIndex", "SlideNumber", "SlideIndexes",
            "TextBoxIndex", "TextBoxOrder", "TextBoxNumber",
            "ElementIndex", "ElementNumber", "ElementOrder",
            "ShapeIndex", "ShapeNumber", "ShapeOrder",
            "TableIndex", "TableNumber", "TableOrder",
            "ImageIndex", "ImageNumber", "ImageOrder",
            "ChartIndex", "ChartNumber", "ChartOrder",
            "RowIndex", "RowNumber", "ColumnIndex", "ColumnNumber",
            "CellIndex", "CellNumber", "ParagraphNumbers"
        };

        return indexPatterns.Any(pattern =>
            parameterName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Index", StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Order", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 保存操作
    /// </summary>
    private void Save()
    {
        if (ValidateParameters())
        {
            IsSaved = true;
        }
    }

    /// <summary>
    /// 取消操作
    /// </summary>
    private void Cancel()
    {
        IsSaved = false;
    }
}
