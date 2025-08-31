using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

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

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as ExcelModuleViewModel;
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
