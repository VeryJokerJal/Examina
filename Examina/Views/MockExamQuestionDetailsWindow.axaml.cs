using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

/// <summary>
/// 模拟考试题目详情窗口
/// </summary>
public partial class MockExamQuestionDetailsWindow : Window
{
    public MockExamQuestionDetailsWindow()
    {
        InitializeComponent();
        
        // 设置DataContext
        DataContext = new MockExamQuestionDetailsViewModel();
        
        System.Diagnostics.Debug.WriteLine("MockExamQuestionDetailsWindow: 窗口已初始化");
    }

    public MockExamQuestionDetailsWindow(MockExamQuestionDetailsViewModel viewModel)
    {
        InitializeComponent();
        
        // 使用传入的ViewModel
        DataContext = viewModel;
        
        System.Diagnostics.Debug.WriteLine("MockExamQuestionDetailsWindow: 窗口已初始化（使用自定义ViewModel）");
    }
}
