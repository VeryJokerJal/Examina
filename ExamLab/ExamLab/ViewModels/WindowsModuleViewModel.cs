using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ExamLab.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// Windows模块ViewModel
/// </summary>
public class WindowsModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 可用的操作类型
    /// </summary>
    public ObservableCollection<WindowsOperationType> AvailableOperationTypes { get; set; } = [];

    /// <summary>
    /// 当前选中的操作类型
    /// </summary>
    [Reactive] public WindowsOperationType? SelectedOperationType { get; set; }

    /// <summary>
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    /// <summary>
    /// 编辑操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> EditOperationPointCommand { get; }

    public WindowsModuleViewModel(ExamModule module) : base(module)
    {
        // 初始化可用操作类型
        WindowsOperationType[] operationTypes = new[]
        {
            WindowsOperationType.QuickCreate,
            WindowsOperationType.CreateOperation,
            WindowsOperationType.DeleteOperation,
            WindowsOperationType.CopyOperation,
            WindowsOperationType.MoveOperation,
            WindowsOperationType.RenameOperation,
            WindowsOperationType.ShortcutOperation,
            WindowsOperationType.FilePropertyModification,
            WindowsOperationType.CopyRenameOperation
        };

        foreach (WindowsOperationType operationType in operationTypes)
        {
            AvailableOperationTypes.Add(operationType);
        }

        // 初始化命令
        AddOperationPointByTypeCommand = ReactiveCommand.Create<string>(AddOperationPointByType);
        EditOperationPointCommand = ReactiveCommand.Create<OperationPoint>(EditOperationPoint);
    }

    protected override void AddOperationPoint()
    {
        if (SelectedQuestion == null)
        {
            SetError("请先选择一个题目");
            return;
        }

        if (SelectedOperationType == null)
        {
            SetError("请先选择一个操作类型");
            return;
        }

        OperationPoint operationPoint = new()
        {
            Name = GetOperationTypeName(SelectedOperationType.Value),
            Description = GetOperationTypeDescription(SelectedOperationType.Value),
            ModuleType = ModuleType.Windows,
            WindowsOperationType = SelectedOperationType.Value,
            ScoringQuestionId = SelectedQuestion.Id
        };

        // 根据操作类型添加相应的参数
        AddParametersForOperationType(operationPoint, SelectedOperationType.Value);

        SelectedQuestion.OperationPoints.Add(operationPoint);
        SelectedOperationPoint = operationPoint;

        ClearError();
    }

    private void AddOperationPointByType(string operationTypeString)
    {
        if (Enum.TryParse(operationTypeString, out WindowsOperationType operationType))
        {
            SelectedOperationType = operationType;
            AddOperationPoint();
        }
        else
        {
            SetError($"未知的操作类型：{operationTypeString}");
        }
    }

    private string GetOperationTypeName(WindowsOperationType operationType)
    {
        return operationType switch
        {
            WindowsOperationType.QuickCreate => "快捷创建",
            WindowsOperationType.CreateOperation => "创建操作",
            WindowsOperationType.DeleteOperation => "删除操作",
            WindowsOperationType.CopyOperation => "复制操作",
            WindowsOperationType.MoveOperation => "移动操作",
            WindowsOperationType.RenameOperation => "重命名操作",
            WindowsOperationType.ShortcutOperation => "快捷方式操作",
            WindowsOperationType.FilePropertyModification => "文件属性修改操作",
            WindowsOperationType.CopyRenameOperation => "复制重命名操作",
            _ => operationType.ToString()
        };
    }

    private string GetOperationTypeDescription(WindowsOperationType operationType)
    {
        return operationType switch
        {
            WindowsOperationType.QuickCreate => "快速创建文件或文件夹",
            WindowsOperationType.CreateOperation => "创建新的文件或文件夹",
            WindowsOperationType.DeleteOperation => "删除指定的文件或文件夹",
            WindowsOperationType.CopyOperation => "复制文件或文件夹",
            WindowsOperationType.MoveOperation => "移动文件或文件夹",
            WindowsOperationType.RenameOperation => "重命名文件或文件夹",
            WindowsOperationType.ShortcutOperation => "创建或管理快捷方式",
            WindowsOperationType.FilePropertyModification => "修改文件或文件夹属性",
            WindowsOperationType.CopyRenameOperation => "复制并重命名文件或文件夹",
            _ => "Windows操作"
        };
    }

    private void AddParametersForOperationType(OperationPoint operationPoint, WindowsOperationType operationType)
    {
        switch (operationType)
        {
            case WindowsOperationType.RenameOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "OriginalFileName",
                    DisplayName = "原文件名",
                    Description = "要重命名的原文件名",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "NewFileName",
                    DisplayName = "新文件名",
                    Description = "重命名后的文件名",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.CreateOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "ItemType",
                    DisplayName = "创建类型",
                    Description = "选择要创建的项目类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "ItemName",
                    DisplayName = "项目名称",
                    Description = "要创建的文件或文件夹名称",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.CopyOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "源路径",
                    Description = "要复制的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标路径",
                    Description = "复制到的目标路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.MoveOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "源路径",
                    Description = "要移动的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标路径",
                    Description = "移动到的目标路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.DeleteOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "TargetPath",
                    DisplayName = "目标路径",
                    Description = "要删除的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                break;

            case WindowsOperationType.CopyRenameOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "原文件路径",
                    Description = "要复制的原文件完整路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标文件路径",
                    Description = "复制到的目标文件完整路径（包含新文件名）",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.ShortcutOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "TargetPath",
                    DisplayName = "目标文件路径",
                    Description = "要创建快捷方式的目标文件路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "ShortcutPath",
                    DisplayName = "快捷方式路径",
                    Description = "快捷方式的保存路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.FilePropertyModification:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FilePath",
                    DisplayName = "文件路径",
                    Description = "要修改属性的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 1
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "PropertyType",
                    DisplayName = "属性类型",
                    Description = "要修改的属性类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 2,
                    EnumOptions = "只读,隐藏,系统,存档"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "PropertyValue",
                    DisplayName = "属性值",
                    Description = "属性的新值",
                    Type = ParameterType.Boolean,
                    IsRequired = true,
                    Order = 3
                });
                break;

                // 可以继续添加其他操作类型的参数配置
        }
    }

    /// <summary>
    /// 编辑操作点
    /// </summary>
    /// <param name="operationPoint">要编辑的操作点</param>
    private void EditOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
        {
            SetError("操作点不能为空");
            return;
        }

        // 这里可以打开编辑对话框或者切换到编辑模式
        // 暂时简单地选中该操作点，让用户可以在右侧面板编辑
        SelectedOperationPoint = operationPoint;

        ClearError();
    }
}
