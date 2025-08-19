using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

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

    /// <summary>
    /// 删除题目点击事件
    /// </summary>
    private void DeleteQuestion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Question question && ViewModel != null)
        {
            _ = ViewModel.DeleteQuestionCommand.Execute(question).Subscribe(_ => { });
        }
    }
}
