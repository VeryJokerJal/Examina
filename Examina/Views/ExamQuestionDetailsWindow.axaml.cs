using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

/// <summary>
/// 考试题目详情窗口（通用版本，支持正式考试和模拟考试）
/// </summary>
public partial class ExamQuestionDetailsWindow : Window
{
    public ExamQuestionDetailsWindow()
    {
        InitializeComponent();
        
        // 设置DataContext
        DataContext = new ExamQuestionDetailsViewModel();
        
        System.Diagnostics.Debug.WriteLine("ExamQuestionDetailsWindow: 窗口已初始化");
    }

    public ExamQuestionDetailsWindow(ExamQuestionDetailsViewModel viewModel)
    {
        InitializeComponent();
        
        // 使用传入的ViewModel
        DataContext = viewModel;
        
        System.Diagnostics.Debug.WriteLine("ExamQuestionDetailsWindow: 窗口已初始化（使用自定义ViewModel）");
    }
}
