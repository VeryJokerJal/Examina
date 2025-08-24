using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Examina.ViewModels.Dialogs;

namespace Examina.Views.Dialogs;

/// <summary>
/// 上机统考规则说明对话框
/// </summary>
public partial class FormalExamRulesDialog : Window
{
    private readonly FormalExamRulesViewModel? _viewModel;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public FormalExamRulesDialog()
    {
        InitializeComponent();

        // 全屏 & 去系统装饰
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;

        // 透明/虚化能力提示
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Transparent
        ];

        Background = Brushes.Transparent;

        _viewModel = new FormalExamRulesViewModel();
        DataContext = _viewModel;

        SetupCommandSubscriptions();
    }

    /// <summary>
    /// 带ViewModel的构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public FormalExamRulesDialog(FormalExamRulesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SetupCommandSubscriptions();
    }

    /// <summary>
    /// 设置命令订阅
    /// </summary>
    private void SetupCommandSubscriptions()
    {
        if (_viewModel != null)
        {
            // 订阅确认命令
            _ = _viewModel.ConfirmCommand.Subscribe(result =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 确认命令执行，结果: {result}");
                    Close(result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 确认命令异常: {ex.Message}");
                }
            });

            // 订阅取消命令
            _ = _viewModel.CancelCommand.Subscribe(result =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 取消命令执行，结果: {result}");
                    Close(result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 取消命令异常: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// 取消按钮点击事件处理器
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("FormalExamRulesDialog: 取消按钮被点击");
            Close(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 取消按钮点击异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 确认按钮点击事件处理器
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("FormalExamRulesDialog: 确认按钮被点击");
            Close(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FormalExamRulesDialog: 确认按钮点击异常: {ex.Message}");
        }
    }
}
