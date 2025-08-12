using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// Excel模块视图
/// </summary>
public sealed partial class ExcelModuleView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public ExcelModuleViewModel? ViewModel { get; set; }

    public ExcelModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public ExcelModuleView(ExcelModuleViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as ExcelModuleViewModel;
    }
}
