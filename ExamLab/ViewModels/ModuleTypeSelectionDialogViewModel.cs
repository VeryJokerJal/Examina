using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ExamLab.Models;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 模块类型选择对话框的ViewModel
/// </summary>
public class ModuleTypeSelectionDialogViewModel : ViewModelBase
{
    /// <summary>
    /// 可选择的模块类型列表
    /// </summary>
    public ObservableCollection<ModuleTypeItem> ModuleTypes { get; } = [];

    /// <summary>
    /// 当前选中的模块类型
    /// </summary>
    [Reactive] public ModuleTypeItem? SelectedModuleType { get; set; }

    /// <summary>
    /// 确认命令
    /// </summary>
    public ReactiveCommand<Unit, ModuleType?> ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// 对话框结果
    /// </summary>
    [Reactive] public ModuleType? DialogResult { get; set; }

    /// <summary>
    /// 是否已确认
    /// </summary>
    [Reactive] public bool IsConfirmed { get; set; }

    public ModuleTypeSelectionDialogViewModel()
    {
        Title = "选择模块类型";

        // 初始化模块类型列表
        InitializeModuleTypes();

        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create<ModuleType?>(Confirm, this.WhenAnyValue(x => x.SelectedModuleType).Select(x => x != null));
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    /// <summary>
    /// 初始化模块类型列表
    /// </summary>
    private void InitializeModuleTypes()
    {
        ModuleTypes.Clear();

        ModuleTypes.Add(new ModuleTypeItem
        {
            Type = ModuleType.Windows,
            Name = "Windows操作",
            Description = "Windows文件和文件夹操作模块，包含9种操作类型",
            Icon = Symbol.Folder
        });

        ModuleTypes.Add(new ModuleTypeItem
        {
            Type = ModuleType.CSharp,
            Name = "C#编程",
            Description = "C#程序设计模块，包含代码配置和输出验证",
            Icon = Symbol.Library
        });

        ModuleTypes.Add(new ModuleTypeItem
        {
            Type = ModuleType.PowerPoint,
            Name = "PowerPoint操作",
            Description = "PowerPoint幻灯片操作模块，包含39个知识点",
            Icon = Symbol.SlideShow
        });

        ModuleTypes.Add(new ModuleTypeItem
        {
            Type = ModuleType.Excel,
            Name = "Excel操作",
            Description = "Excel电子表格操作模块，包含数据处理和图表功能",
            Icon = Symbol.Calculator
        });

        ModuleTypes.Add(new ModuleTypeItem
        {
            Type = ModuleType.Word,
            Name = "Word操作",
            Description = "Word文档编辑模块，包含文档格式化和排版功能",
            Icon = Symbol.Document
        });

        // 默认选择第一个
        SelectedModuleType = ModuleTypes.FirstOrDefault();
    }

    /// <summary>
    /// 确认选择
    /// </summary>
    private ModuleType? Confirm()
    {
        if (SelectedModuleType != null)
        {
            DialogResult = SelectedModuleType.Type;
            IsConfirmed = true;
            return SelectedModuleType.Type;
        }
        return null;
    }

    /// <summary>
    /// 取消选择
    /// </summary>
    private void Cancel()
    {
        DialogResult = null;
        IsConfirmed = false;
    }
}

/// <summary>
/// 模块类型项目
/// </summary>
public class ModuleTypeItem
{
    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType Type { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标符号
    /// </summary>
    public Symbol Icon { get; set; } = Symbol.Document;
}
