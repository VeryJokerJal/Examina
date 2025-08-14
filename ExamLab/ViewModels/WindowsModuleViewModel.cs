using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要重命名的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "OriginalFileName",
                    DisplayName = "原文件名",
                    Description = "要重命名的原文件名",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "NewFileName",
                    DisplayName = "新文件名",
                    Description = "重命名后的文件名",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 3
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
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要复制的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "源路径",
                    Description = "要复制的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标路径",
                    Description = "复制到的目标路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 3
                });
                break;

            case WindowsOperationType.MoveOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要移动的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "源路径",
                    Description = "要移动的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标路径",
                    Description = "移动到的目标路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 3
                });
                break;

            case WindowsOperationType.DeleteOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要删除的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "TargetPath",
                    DisplayName = "目标路径",
                    Description = "要删除的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                break;

            case WindowsOperationType.CopyRenameOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要复制重命名的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "SourcePath",
                    DisplayName = "原文件路径",
                    Description = "要复制的原文件完整路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "DestinationPath",
                    DisplayName = "目标文件路径",
                    Description = "复制到的目标文件完整路径（包含新文件名）",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 3
                });
                break;

            case WindowsOperationType.ShortcutOperation:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要创建快捷方式的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "TargetPath",
                    DisplayName = "目标文件路径",
                    Description = "要创建快捷方式的目标文件路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "ShortcutPath",
                    DisplayName = "快捷方式路径",
                    Description = "快捷方式的保存路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 3
                });
                break;

            case WindowsOperationType.FilePropertyModification:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要修改属性的对象类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 1,
                    EnumOptions = "文件,文件夹"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FilePath",
                    DisplayName = "文件路径",
                    Description = "要修改属性的文件或文件夹路径",
                    Type = ParameterType.Text,
                    IsRequired = true,
                    Order = 2
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "PropertyType",
                    DisplayName = "属性类型",
                    Description = "要修改的属性类型",
                    Type = ParameterType.Enum,
                    IsRequired = true,
                    Order = 3,
                    EnumOptions = "只读,隐藏,系统,存档"
                });
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "PropertyValue",
                    DisplayName = "属性值",
                    Description = "属性的新值",
                    Type = ParameterType.Boolean,
                    IsRequired = true,
                    Order = 4
                });
                break;

            case WindowsOperationType.QuickCreate:
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "FileType",
                    DisplayName = "文件类型",
                    Description = "选择要快速创建的对象类型",
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
                operationPoint.Parameters.Add(new ConfigurationParameter
                {
                    Name = "CreatePath",
                    DisplayName = "创建路径",
                    Description = "创建文件或文件夹的路径",
                    Type = ParameterType.Text,
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
    private async void EditOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
        {
            SetError("操作点不能为空");
            return;
        }

        try
        {
            // 检查XamlRoot是否可用
            Microsoft.UI.Xaml.XamlRoot? xamlRoot = App.MainWindow?.Content.XamlRoot;
            if (xamlRoot == null)
            {
                SetError("无法显示编辑对话框：XamlRoot未设置");
                return;
            }

            // 创建编辑页面
            Views.OperationPointEditPage editPage = new();
            editPage.Initialize(operationPoint);

            // 创建ContentDialog并设置内容
            Microsoft.UI.Xaml.Controls.ContentDialog dialog = new()
            {
                Title = "编辑Windows操作点",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                Content = editPage,
                XamlRoot = xamlRoot,
                MinWidth = 650
            };

            // 显示对话框
            Microsoft.UI.Xaml.Controls.ContentDialogResult result = await dialog.ShowAsync();

            // 如果用户点击了保存，则验证并保存参数
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                // 更新参数值
                foreach (ConfigurationParameter parameter in operationPoint.Parameters)
                {
                    parameter.Value = editPage.GetParameterValue(parameter);
                }

                // 更新操作点分数
                operationPoint.Score = editPage.GetScore();

                // 验证参数
                if (ValidateOperationPointParameters(operationPoint))
                {
                    // 选中该操作点，让用户可以在右侧面板查看更新后的内容
                    SelectedOperationPoint = operationPoint;

                    // 刷新操作点列表显示
                    if (SelectedQuestion != null)
                    {
                        this.RaisePropertyChanged(nameof(SelectedQuestion));
                    }
                    ClearError();
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"编辑操作点失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证操作点参数
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>验证是否通过</returns>
    private bool ValidateOperationPointParameters(OperationPoint operationPoint)
    {
        // 验证必填参数
        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
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
                    // 如果值为-1，则允许（-1代表通配符，匹配任意值）
                    if (numValue != -1)
                    {
                        SetError($"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}（输入-1表示匹配任意值）");
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


}
