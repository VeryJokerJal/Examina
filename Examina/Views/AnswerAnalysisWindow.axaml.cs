using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Examina.Views;

/// <summary>
/// 答案解析窗口
/// </summary>
public partial class AnswerAnalysisWindow : Window
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AnswerAnalysisWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
