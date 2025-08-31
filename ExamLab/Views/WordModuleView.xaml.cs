using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

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

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as WordModuleViewModel;
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
