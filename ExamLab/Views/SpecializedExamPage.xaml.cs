using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

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
}
