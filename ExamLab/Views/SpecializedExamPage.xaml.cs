using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using System;
using System.Reactive.Linq;

namespace ExamLab.Views;

/// <summary>
/// 专项试卷页面用户控件
/// </summary>
public sealed partial class SpecializedExamPage : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public SpecializedExamViewModel? ViewModel => DataContext as SpecializedExamViewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SpecializedExamPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 带ViewModel参数的构造函数
    /// </summary>
    /// <param name="viewModel">专项试卷ViewModel</param>
    public SpecializedExamPage(SpecializedExamViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// 克隆专项试卷点击事件
    /// </summary>
    private void CloneSpecializedExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is SpecializedExam exam && ViewModel != null)
        {
            _ = ViewModel.CloneSpecializedExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }

    /// <summary>
    /// 删除专项试卷点击事件
    /// </summary>
    private void DeleteSpecializedExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is SpecializedExam exam && ViewModel != null)
        {
            _ = ViewModel.DeleteSpecializedExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }
}
