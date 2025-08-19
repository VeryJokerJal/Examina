using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

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
