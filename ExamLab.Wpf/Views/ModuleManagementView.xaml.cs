using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// 模块管理视图
/// </summary>
public sealed partial class ModuleManagementView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public ModuleManagementViewModel? ViewModel { get; set; }

    public ModuleManagementView()
    {
        InitializeComponent();
    }

    public ModuleManagementView(ModuleManagementViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
