using System;
using System.Reactive.Linq;
using ExamLab.Models;
using ExamLab.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 专项试卷制作页面
/// </summary>
public sealed partial class SpecializedExamPage : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public SpecializedExamViewModel ViewModel { get; }

    /// <summary>
    /// 主窗口ViewModel引用
    /// </summary>
    private readonly MainWindowViewModel? _mainWindowViewModel;

    public SpecializedExamPage(MainWindowViewModel? mainWindowViewModel = null)
    {
        InitializeComponent();
        _mainWindowViewModel = mainWindowViewModel;
        ViewModel = new SpecializedExamViewModel(mainWindowViewModel);
        DataContext = ViewModel;

        // 监听选中模块变化，更新内容视图
        ViewModel.WhenAnyValue(x => x.SelectedModule)
            .Subscribe(OnSelectedModuleChanged);
    }

    /// <summary>
    /// 选中模块变化处理
    /// </summary>
    private void OnSelectedModuleChanged(ExamModule? module)
    {
        if (module == null || _mainWindowViewModel == null)
        {
            ViewModel.CurrentContentView = null;
            ViewModel.CurrentContentViewModel = null;
            return;
        }

        // 根据模块类型创建对应的ViewModel和View
        ViewModel.CurrentContentViewModel = module.Type switch
        {
            ModuleType.Windows => new WindowsModuleViewModel(module),
            ModuleType.CSharp => new CSharpModuleViewModel(module, _mainWindowViewModel),
            ModuleType.PowerPoint => new PowerPointModuleViewModel(module),
            ModuleType.Excel => new ExcelModuleViewModel(module),
            ModuleType.Word => new WordModuleViewModel(module),
            _ => null
        };

        // 创建对应的View，设置适当的DataContext
        ViewModel.CurrentContentView = ViewModel.CurrentContentViewModel switch
        {
            WindowsModuleViewModel => new WindowsModuleView { DataContext = _mainWindowViewModel },
            CSharpModuleViewModel csharpVM => new CSharpModuleView(csharpVM) { MainWindowViewModel = _mainWindowViewModel },
            PowerPointModuleViewModel => new PowerPointModuleView { DataContext = _mainWindowViewModel },
            ExcelModuleViewModel => new ExcelModuleView { DataContext = _mainWindowViewModel },
            WordModuleViewModel => new WordModuleView { DataContext = _mainWindowViewModel },
            _ => null
        };

        // 如果有View，需要设置SelectedModule
        if (ViewModel.CurrentContentView != null && _mainWindowViewModel != null)
        {
            _mainWindowViewModel.SelectedModule = module;
        }
    }

    /// <summary>
    /// 克隆专项试卷菜单项点击事件
    /// </summary>
    private void CloneSpecializedExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Exam exam)
        {
            ViewModel.CloneSpecializedExamCommand.Execute(exam).Subscribe();
        }
    }

    /// <summary>
    /// 删除专项试卷菜单项点击事件
    /// </summary>
    private void DeleteSpecializedExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Exam exam)
        {
            ViewModel.DeleteSpecializedExamCommand.Execute(exam).Subscribe();
        }
    }
}
