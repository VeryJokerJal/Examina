using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// C#模块视图
/// </summary>
public sealed partial class CSharpModuleView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public CSharpModuleViewModel? ViewModel { get; set; }

    public CSharpModuleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public CSharpModuleView(CSharpModuleViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as CSharpModuleViewModel;
    }
}
