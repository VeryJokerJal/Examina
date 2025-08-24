using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Examina.Views;

/// <summary>
/// 题目详情窗口
/// </summary>
public partial class QuestionDetailsWindow : Window
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionDetailsWindow()
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
