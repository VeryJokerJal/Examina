using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

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

    /// <summary>
    /// MainWindowViewModel - 用于访问通用命令
    /// </summary>
    public MainWindowViewModel? MainWindowViewModel { get; set; }

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

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = DataContext as CSharpModuleViewModel;
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
