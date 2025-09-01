using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Examina.Views;

/// <summary>
/// 综合实训结果窗口
/// </summary>
public partial class ComprehensiveTrainingResultWindow : Window
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ComprehensiveTrainingResultWindow()
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

    /// <summary>
    /// 等待窗口关闭
    /// </summary>
    public Task WaitForCloseAsync()
    {
        TaskCompletionSource<bool> tcs = new();
        
        Closed += (sender, e) => tcs.SetResult(true);
        
        return tcs.Task;
    }
}
