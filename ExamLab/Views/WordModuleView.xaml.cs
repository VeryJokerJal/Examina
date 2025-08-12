using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// Word模块视图
/// </summary>
public sealed partial class WordModuleView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public WordModuleViewModel? ViewModel { get; set; }

    public WordModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public WordModuleView(WordModuleViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as WordModuleViewModel;
    }
}
