using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// Windows模块视图
/// </summary>
public sealed partial class WindowsModuleView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public WindowsModuleViewModel? ViewModel { get; set; }

    public WindowsModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public WindowsModuleView(WindowsModuleViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as WindowsModuleViewModel;
    }
}
