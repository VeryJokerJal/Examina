using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// PowerPoint模块视图
/// </summary>
public sealed partial class PowerPointModuleView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public PowerPointModuleViewModel? ViewModel { get; set; }

    public PowerPointModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public PowerPointModuleView(PowerPointModuleViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as PowerPointModuleViewModel;
    }
}
