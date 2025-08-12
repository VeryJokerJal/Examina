using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;

namespace ExamLab.Views;

/// <summary>
/// 题目管理视图
/// </summary>
public sealed partial class QuestionManagementView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public QuestionManagementViewModel? ViewModel { get; set; }

    public QuestionManagementView()
    {
        InitializeComponent();
    }

    public QuestionManagementView(QuestionManagementViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
